using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.IO;
using System;
using PlayniteSounds.Services.Files;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using PlayniteSounds.Common.Constants;
using PlayniteSounds.Models;
using PlayniteSounds.Files.Download;
using PlayniteSounds.Common.Extensions;
using PlayniteSounds.Models.Download;
using PlayniteSounds.Models.UI;
using PlayniteSounds.Services.Play;

namespace PlayniteSounds.Services.State;

public class ItemCollectionChangedHandler : IItemCollectionChangedHandler
{
    private readonly ILogger                  _logger;
    private readonly IPathingService          _pathingService;
    private readonly IFileManager             _fileManager;
    private readonly IDownloadManager         _downloadManager;
    private readonly IPromptFactory           _promptFactory;
    private readonly ITagger                  _tagger;
    private readonly IAssemblyResolver        _assemblyResolver;
    private readonly PlayniteSoundsSettings   _settings;
    private readonly CancellationTokenSource  _downloadTokenSource = new();
    private readonly object                   _taskLock            = new();
    private          ConfiguredTaskAwaitable? _waitTask;
    private          GlobalProgressActionArgs _progressArgs;


    private readonly List<Guid> _manualGuids = [];
    private readonly List<(Game Game, CancellationTokenSource tokenSource, Task<DownloadStatus> Task)> _downloadTasks = [];

    public bool ExtraMetaDataPluginIsLoaded { get; set; }

    public ItemCollectionChangedHandler(
        ILogger logger,
        IGameDatabaseAPI gameDatabaseAPI,
        IPathingService pathingService,
        IFileManager fileManager,
        IDownloadManager downloadManager,
        IPlayniteEventHandler eventHandler,
        IPromptFactory promptFactory,
        ITagger tagger,
        IAssemblyResolver assemblyResolver,
        PlayniteSoundsSettings settings)
    {
        _logger = logger;
        _pathingService = pathingService;
        _fileManager = fileManager;
        _downloadManager = downloadManager;
        _promptFactory = promptFactory;
        _tagger = tagger;
        _assemblyResolver = assemblyResolver;
        _settings = settings;
        eventHandler.PlayniteEventOccurred                  += PlayniteEventOccurred;
        gameDatabaseAPI.Games        .ItemUpdated           += UpdateGame;
        gameDatabaseAPI.Games        .ItemCollectionChanged += UpdateGames;
        gameDatabaseAPI.Platforms    .ItemCollectionChanged += UpdatePlatforms;
        gameDatabaseAPI.FilterPresets.ItemCollectionChanged += UpdateFilters;
    }

    private void PlayniteEventOccurred(object sender, Models.State.PlayniteEventOccurredArgs e)
    {
        _logger.Info($"Playnite event {e.Event} occurred");
        switch (e.Event)
        {
            case PlayniteEvent.AppStopped:     if (_downloadTasks.Count > 0)   /* Then */ _promptFactory.CreateGlobalProgress("Downloading", Update); break;
            case PlayniteEvent.LibraryUpdated: if (_settings.AutoDownload) /* Then */ _waitTask = WaitForDownloadsAsync().ConfigureAwait(false); break;
            default: return;
        }
        _logger.Info($"Finished handling Playnite event {e.Event}");
    }

    private async void Update(GlobalProgressActionArgs arg1, string arg2)
    {
        arg1.ProgressMaxValue = _downloadTasks.Count;
        _progressArgs = arg1;
        _waitTask ??= WaitForDownloadsAsync().ConfigureAwait(false);
        await _waitTask.Value;
    }

    private async Task WaitForDownloadsAsync()
    {
        await Task.Yield();

        var finishedTasks = new List<(Game, CancellationTokenSource, Task<DownloadStatus>)>();
        while (_downloadTasks.Count > 0)
        {
            var task = await Task.WhenAny(_downloadTasks.Select(t => t.Task)).ConfigureAwait(false);
            var finishedTask = _downloadTasks.First(t => t.Task == task);
            finishedTasks.Add(finishedTask);
            _downloadTasks.Remove(finishedTask);

            if (_progressArgs is null) /* Then */ continue;

            _progressArgs.Text = finishedTask.Game?.Name ?? _progressArgs.Text;
            _progressArgs.CurrentProgressValue++;

            if (!_progressArgs.CancelToken.IsCancellationRequested) /* Then */ continue;

            _downloadTokenSource.Cancel();
            _progressArgs.Text += " - Cancelling";
            await Task.WhenAll(_downloadTasks.Select(t => t.Task)).ConfigureAwait(false);

            lock(_taskLock) /* Then */ finishedTasks.AddRange(_downloadTasks);
            break;
        }

        var normalizedGames = new List<Game>();
        var failedGames = new List<Game>();
        foreach (var (game, _, task) in finishedTasks)
            if (task.Result.HasFlag(DownloadStatus.Normalized)) /* Then */ normalizedGames.Add(game);
            else if (task.Result.HasFlag(DownloadStatus.Failed))     /* Then */ failedGames.Add(game);

        _tagger.AddTag(failedGames,     Resource.MissingTag);
        _tagger.AddTag(normalizedGames, Resource.NormTag);

        failedGames.AddRange(normalizedGames);
        _tagger.UpdateGames(failedGames);
    }

    private void UpdateGame(object sender, ItemUpdatedEventArgs<Game> e)
    {
        lock (_taskLock)
        {
            var manualTasks = e.UpdatedItems.Where(g => _manualGuids.Contains(g.NewData.Id))
                .Select(g => CreateDownloadTask(g.NewData));
            _downloadTasks.AddRange(manualTasks);
            _manualGuids.Clear();
        }
    }

    public void AddGames(IList<Game> games, Source? source)
    {
        lock (_taskLock)
        {
            games = games.Where(g => _downloadTasks.FirstOrDefault(t => t.Game.Id == g.Id).Game is null).ToList();
            AddGameTasks(games, source);
        }
    }

    private void AddGameTasks(IEnumerable<Game> games, Source? source)
    {
        using (_assemblyResolver.HandleAssemblies(typeof(IAsyncDisposable)))
            /* Then */ _downloadTasks.AddRange(games.Select(g => CreateDownloadTask(g, source)));
    }

    private async void UpdateGames(object sender, ItemCollectionChangedEventArgs<Game> args)
    {
        if   (_settings.AutoDownload && args.AddedItems.Any())
            lock (_taskLock)
                /* Then */ AddGameTasks(args.AddedItems.Where(g => g.Source != null), null);

        // Manually added games don't yet have info
        if (args.AddedItems.Count is 1 && args.AddedItems[0].Source is null)
            /* Then */ _manualGuids.Add(args.AddedItems[0].Id);

        // Let ExtraMetaDataLoader handle cleanup if it exists
        if      (!ExtraMetaDataPluginIsLoaded || _settings.AutoDownload) 
            foreach (var game in args.RemovedItems)
            {
                if (_settings.AutoDownload)
                {
                    var task = _downloadTasks.FirstOrDefault(t => t.Game.Equals(game));
                    if (task.Task != null)
                    {
                        task.tokenSource.Cancel();
                        lock (_taskLock) /* Then */ _downloadTasks.Remove(task);
                        await task.Task.ConfigureAwait(false);
                    }
                }
                
                if (!ExtraMetaDataPluginIsLoaded) /* Then */ _fileManager.DeleteMusicDirectory(game);
            }
    }

    private (Game, CancellationTokenSource, Task<DownloadStatus>) CreateDownloadTask(Game game, Source? source = null)
    {
        var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(_downloadTokenSource.Token);
        return (game, tokenSource, _downloadManager.DownloadAsync(game, tokenSource.Token, source));
    }

    private void UpdatePlatforms(object _, ItemCollectionChangedEventArgs<Platform> args)
        => UpdateItems(args, _fileManager.CreatePlatformDirectory, _pathingService.GetPlatformDirectoryPath);

    private void UpdateFilters(object _, ItemCollectionChangedEventArgs<FilterPreset> args)
        => UpdateItems(args, _fileManager.CreateFilterDirectory, _pathingService.GetFilterDirectoryPath);

    private static void UpdateItems<T>(
        ItemCollectionChangedEventArgs<T> args, 
        Func<T, string> addedItemAction,
        Func<T, string> itemPathSelector) where T : DatabaseObject
    {
        args.AddedItems.ForEach(addedItemAction);
        args.RemovedItems.Select(itemPathSelector).Where(Directory.Exists).ForEach(f => Directory.Delete(f, true));
    }
}