using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.IO;
using System;
using PlayniteSounds.Services.Files;
using System.Linq;
using PlayniteSounds.Models;
using PlayniteSounds.Files.Download;
using PlayniteSounds.Common.Extensions;

namespace PlayniteSounds.Services.State
{
    public class ItemCollectionChangedHandler : IItemCollectionChangedHandler
    {
        private readonly IPathingService        _pathingService;
        private readonly IFileManager           _fileManager;
        private readonly IDownloadManager       _downloadManager;
        private readonly PlayniteSoundsSettings _settings;

        public bool ExtraMetaDataPluginIsLoaded { get; set; }

        public ItemCollectionChangedHandler(
            IGameDatabaseAPI gameDatabaseAPI,
            IPathingService pathingService,
            IFileManager fileManager,
            IDownloadManager downloadManager,
            PlayniteSoundsSettings settings)
        {
            _pathingService = pathingService;
            _fileManager = fileManager;
            _downloadManager = downloadManager;
            _settings = settings;
            gameDatabaseAPI.Games.ItemCollectionChanged += UpdateGames;
            gameDatabaseAPI.Platforms.ItemCollectionChanged += UpdatePlatforms;
            gameDatabaseAPI.FilterPresets.ItemCollectionChanged += UpdateFilters;
        }

        private void UpdateGames(object sender, ItemCollectionChangedEventArgs<Game> args)
        {
            if (_settings.AutoDownload)
            {
                _downloadManager.CreateDownloadDialogue(args.AddedItems, Source.All);
            }

            // Let ExtraMetaDataLoader handle cleanup if it exists
            if (!ExtraMetaDataPluginIsLoaded) /* Then */ foreach (var game in args.RemovedItems)
            {
                _fileManager.DeleteMusicDirectory(game);
            }
        }

        private void UpdatePlatforms(object _, ItemCollectionChangedEventArgs<Platform> args)
            => UpdateItems(args, _fileManager.CreatePlatformDirectory, _pathingService.GetPlatformDirectoryPath);

        private void UpdateFilters(object _, ItemCollectionChangedEventArgs<FilterPreset> args)
            => UpdateItems(args, _fileManager.CreateFilterDirectory, _pathingService.GetFilterDirectoryPath);

        private void UpdateItems<T>(
            ItemCollectionChangedEventArgs<T> args, 
            Func<T, string> addedItemAction,
            Func<T, string> itemPathSelector) where T : DatabaseObject
        {
            args.AddedItems.ForEach(addedItemAction);
            args.RemovedItems.
                Select(itemPathSelector).
                Where(Directory.Exists).
                ForEach(f => Directory.Delete(f, true));
        }
    }
}
