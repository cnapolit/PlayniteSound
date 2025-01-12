using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System;
using System.Threading;
using Playnite.SDK.Models;
using Playnite.SDK;
using PlayniteSounds.Files.Download.Downloaders;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Files;
using PlayniteSounds.Services.State;
using PlayniteSounds.Common.Extensions;
using System.Threading.Tasks;
using PlayniteSounds.Models.Download;
using PlayniteSounds.Services.Files.Download.Downloaders;
using PlayniteSounds.Common;
using System.Runtime.CompilerServices;

namespace PlayniteSounds.Files.Download
{
    public class DownloadManager : IDownloadManager
    {
        #region Infrastructure

        private static readonly TimeSpan               MaxTime       = new TimeSpan(0, 8, 0);
        private static readonly HtmlWeb                Web           = new HtmlWeb();
        private static readonly HttpClient             HttpClient    = new HttpClient();
        private static readonly List<string>           SongTitleEnds = new List<string> { "Theme", "Title", "Menu" };
        private        readonly ILogger                _logger;
        private        readonly IFileManager           _fileManager;
        private        readonly INormalizer            _normalizer;
        private        readonly IDownloader            _khDownloader;
        private        readonly IDownloader            _ytDownloader;
        private        readonly IDownloader            _localDownloader;
        private        readonly IDownloader            _scDownloader;
        private        readonly PlayniteSoundsSettings _settings;

        public DownloadManager(
            ILogger logger,
            IFileManager fileManager,
            INormalizer normalizer,
            IAssemblyResolver assemblyResolver,
            IPathingService pathingService,
            PlayniteSoundsSettings settings)
        {
            _logger = logger;
            _fileManager = fileManager;
            _normalizer = normalizer;
            _settings = settings;
            _khDownloader = new KhDownloader(logger, HttpClient, Web);
            _ytDownloader = new YtDownloader(logger, assemblyResolver, HttpClient, _settings);
            _localDownloader = new LocalDownloader(logger, pathingService);
        }

        #endregion

        #region Implementation

        public IAsyncEnumerable<Album> GetAlbumsForGameAsync(Game game, string searchTerm, Source source, CancellationToken token)
            => SourceToDownloader(source).GetAlbumsForGameAsync(game, searchTerm, token);

        public IAsyncEnumerable<IEnumerable<Album>> GetAlbumBatchesForGameAsync(
            Game game, string searchTerm, Source source, CancellationToken token) 
            => GetAlbumBatchesForGameAsync(SourceToDownloader(source), game, searchTerm, token);

        private static IAsyncEnumerable<IEnumerable<Album>> GetAlbumBatchesForGameAsync(
            IDownloader downloader, Game game, string searchTerm, CancellationToken token)
        {
            return downloader.GetCapabilities().HasFlag(DownloadCapabilities.Batching)
                ? downloader.GetAlbumBatchesForGameAsync(game, searchTerm, token)
                : CreateBatchEnumerableAsync(downloader.GetAlbumsForGameAsync(game, searchTerm, token), token);
        }

        public IAsyncEnumerable<Song> GetSongsFromAlbumAsync(Album album, CancellationToken token)
            => SourceToDownloader(album.Source).GetSongsFromAlbumAsync(album, token)
                                               .Select(s => { s.ParentAlbum = album; return s; }); 

        public IAsyncEnumerable<IEnumerable<Song>> GetSongBatchesFromAlbumAsync(Album album, CancellationToken token)
        {
            var downloader = SourceToDownloader(album.Source);
            var songs = downloader.GetCapabilities().HasFlag(DownloadCapabilities.Batching) 
                ? downloader.GetSongBatchesFromAlbumAsync(album, token)
                : CreateBatchEnumerableAsync(downloader.GetSongsFromAlbumAsync(album, token), token);
            return songs.Select(b => b.Select(s => { s.ParentAlbum = album; return s; }));
        }

        public Task GetAlbumInfoAsync(
            Album album, CancellationToken token, Func<Action<Song>, Song, Task> updateCallback)
            => SourceToDownloader(album.Source).GetAlbumInfoAsync(album, token, updateCallback);

        public async Task<bool> DownloadAsync(
            Game game, DownloadItem downloadItem, string path, IProgress<double> progress, CancellationToken token)
        {
            var downloader = SourceToDownloader(downloadItem.Source);
            if (downloadItem is Album album)
            {
                var capabilities = downloader.GetCapabilities();
                if (!capabilities.HasFlag(DownloadCapabilities.Bulk)) /* Then */ return false;

                if (album.Songs is null || album.Songs.Count < album.Count)
                {
                    album.Songs = await GetSongsFromAlbumAsync(album, token).ToObservableCollectionAsync(token);
                    if (token.IsCancellationRequested) /* Then */ return false;
                }

                var results = await Task.WhenAll(album.Songs.Select(s => DownloadAsync(
                    game, s, path + StringUtilities.Sanitize(s.Name) + (s.Types.FirstOrDefault() ?? ".mp3"), progress, token)));
                return results.All();
            }

            var song = downloadItem as Song;
            var result = false;
            if (song.Stream != null) /* Then */ try
            {
                song.Stream.Seek(0, SeekOrigin.Begin);
                using (var fileStream = File.Create(path))
                if    (progress is null) /* Then */ await song.Stream.CopyToAsync(fileStream);
                else                     /* Then */ await DownloadCommon.DownloadStreamAsync(song.Stream, fileStream, progress, token);
                result = true;
            }
            catch (Exception e)
            {
                _logger.Warn(e, $"Unable to convert stream to file for song '{song.Name}' with Id '{song.Id}' and Path '{path}' from source '{song.Source}'. Attempting direct download instead...");
                progress?.Report(0);
            }
            else /* Then */ result = await downloader.DownloadAsync(downloadItem, path, progress, token);

            if (token.IsCancellationRequested)
            {
                if (File.Exists(path)) /* Then */ File.Delete(path);
                return false;
            }

            if (result) /* Then */ _fileManager.ApplyTags(game, song, path);
            return result;
        }

        public DownloadCapabilities GetCapabilities(Source source)
            => SourceToDownloader(source).GetCapabilities();

        public IAsyncEnumerable<Song> SearchSongsAsync(
            Game game, string searchTerm, Source source, CancellationToken token)
            => SourceToDownloader(source).SearchSongsAsync(game, searchTerm, token);

        public IAsyncEnumerable<IEnumerable<Song>> SearchSongBatchesAsync(
            Game game, string searchTerm, Source source, CancellationToken token)
        {
            var downloader = SourceToDownloader(source);

            return downloader.GetCapabilities().HasFlag(DownloadCapabilities.Batching)
                ? downloader.SearchSongBatchesAsync(game, searchTerm, token)
                : CreateBatchEnumerableAsync(downloader.SearchSongsAsync(game, searchTerm, token), token);
        }

        public string GetItemUrl(DownloadItem item) => SourceToDownloader(item.Source).GetItemUrl(item);
        public string GetSourceIcon(Source source)  => SourceToDownloader(source).SourceIcon;
        public string GetSourceLogo(Source source)  => SourceToDownloader(source).SourceLogo;

        public string GenerateSearchStr(Source source, string gameName)
            => SourceToDownloader(source).GenerateSearchStr(gameName);

        private const int BatchCount = 5;

        public async Task<DownloadStatus> DownloadAsync(Game game, CancellationToken token)
        {
            var sanitizedName = StringUtilities.Sanitize(game.Name);
            IDownloader downloader = null;
            
            Album selectedAlbum = null;
            if (_settings.AutoParallelDownload)
            {
                selectedAlbum = await GetAlbumAsync(game, sanitizedName, token);
                downloader = SourceToDownloader(selectedAlbum.Source);
            }
            else /* Then */ foreach (var source in _settings.Downloaders)
            {
                downloader = SourceToDownloader(source);

                var searchStr = downloader.GenerateSearchStr(sanitizedName);
                if (downloader.GetCapabilities().HasFlag(DownloadCapabilities.Batching))
                {
                    var i = 0;
                    Album firstAlbum = null;
                    var batches = downloader.GetAlbumBatchesForGameAsync(game, searchStr, token);
                    await foreach (var batch in batches)
                    {
                        var batchList = batch.ToList();
                        selectedAlbum = SelectAlbum(batchList, game.Name, sanitizedName);
                        if (selectedAlbum != null || i++ == BatchCount) /* Then */ break;
                        if (firstAlbum is null) /* Then */ firstAlbum = batchList.FirstOrDefault();
                    }

                    if (selectedAlbum is null) /* Then */ selectedAlbum = firstAlbum;
                }
                else 
                {
                    var albums = await downloader.GetAlbumsForGameAsync(game, searchStr, token).ToListAsync(token);
                    selectedAlbum = SelectAlbum(albums, game.Name, sanitizedName) ?? albums.FirstOrDefault();
                }

                if (selectedAlbum != null) /* Then */ break;
            }

            if (selectedAlbum is null) /* Then */ return DownloadStatus.Failed;
            _logger.Info($"Selected album '{selectedAlbum.Name}' from source '{selectedAlbum.Source}' for game '{game.Name}'");

            Song selectedSong = null;
            if (downloader.GetCapabilities().HasFlag(DownloadCapabilities.Batching))
            {
                var i = 0;
                Song firstSong = null;
                var batches = downloader.GetSongBatchesFromAlbumAsync(selectedAlbum, token);
                await foreach (var batch in batches)
                {
                    var batchList = batch.ToList();
                    selectedSong = SelectSong(batchList, sanitizedName);
                    if (selectedSong != null || i++ == BatchCount) /* Then */ break;
                    if (firstSong is null) /* Then */ firstSong = batchList.FirstOrDefault();
                }
            }
            else
            {
                var songs = await downloader.GetSongsFromAlbumAsync(selectedAlbum, token).ToListAsync(token);
                selectedSong = SelectSong(songs, sanitizedName) ?? songs.FirstOrDefault();
            }

            if (selectedSong is null) /* Then */ return DownloadStatus.Failed;
            _logger.Info($"Selected song '{selectedSong.Name}' from album '{selectedAlbum.Name}' for game '{game.Name}'");

                var dir = _fileManager.CreateMusicDirectory(game);
                var path = Path.Combine(dir, sanitizedName + (selectedSong.Types?.FirstOrDefault() ?? ".mp3"));
            if (!await downloader.DownloadAsync(selectedSong, path, null, token)) /* Then */ return DownloadStatus.Failed;
            _logger.Info($"Downloaded from source '{selectedAlbum.Source}' for game '{game.Name}'");

                _fileManager.ApplyTags(game, selectedSong, path);

                return await _normalizer.NormalizeAudioFileAsync(path) 
                    ? DownloadStatus.Downloaded | DownloadStatus.Normalized 
                    : DownloadStatus.Downloaded;
            }

        private async Task<Album> GetAlbumAsync(Game game, string sanitizedGameName, CancellationToken token)
        {
            Album album = null;
            var downloaders = new List<IDownloader>();
            List<IAsyncEnumerator<IEnumerable<Album>>> enumerators = null;
            try
            {
                var albums = new List<Album>();
                foreach (var source in _settings.Downloaders)
                {
                    var downloader = SourceToDownloader(source);
                    if (downloader.GetCapabilities().HasFlag(DownloadCapabilities.Batching))
                    {
                        downloaders.Add(downloader);
                        break;
                    }
                    var searchStr = downloader.GenerateSearchStr(sanitizedGameName);
                    albums.AddRange(await downloader.GetAlbumsForGameAsync(game, searchStr, token).ToListAsync(token));
                }

                IAsyncEnumerator<IEnumerable<Album>> GetEnumerators(IDownloader downloader)
                {
                    var searchTerm = downloader.GenerateSearchStr(sanitizedGameName);
                    var enumerator = GetAlbumBatchesForGameAsync(downloader, game, searchTerm, token)
                                    .GetAsyncEnumerator(token);
                    return new TimedAsyncEnumerator<IEnumerable<Album>>(_logger, enumerator, TimeSpan.FromSeconds(10));
                }

                enumerators = downloaders.Select(GetEnumerators).ToList();

                for (var i = 0; i < BatchCount; i++)
                {
                    var results = await Task.WhenAll(enumerators.Select(async d => await d.MoveNextAsync(token)));
                    var moreBatches = false;
                    for  (var j = 0; j < results.Length; j++)
                    if   (results[j]) moreBatches = true;
                    else              await enumerators.Pop(j).DisposeAsync();

                    albums.AddRange(enumerators.SelectMany(d => d.Current).ToList());
                    var selectedAlbum = SelectAlbum(albums, game.Name, sanitizedGameName);

                    if (selectedAlbum != null) /* Then */ break;
                    if (album is null)         /* Then */ album = albums.FirstOrDefault();

                    if (!moreBatches) /* Then */ break;
                    albums.Clear();
                }
            }
            finally
            {
                if (enumerators != null) /* Then */ await Task.WhenAll(enumerators.Select(async d => await d.DisposeAsync()));
            }

            return album;
        }

        #region Helpers

        private static Album SelectAlbum(IList<Album> albums, string gameName, string regexGameName)
        {
            var ostRegex = new Regex($"{regexGameName}.*(Soundtrack|OST|Score)", RegexOptions.IgnoreCase);
            return albums.FirstOrDefault(a => ostRegex.IsMatch(a.Name))
                ?? albums.FirstOrDefault(a => string.Equals(a.Name, gameName, StringComparison.OrdinalIgnoreCase))
                ?? albums.FirstOrDefault(a => a.Name.StartsWith(gameName, StringComparison.OrdinalIgnoreCase));
        }

        private static Song SelectSong(IEnumerable<Song> songs, string regexGameName)
        {
            var songsList = songs.Where(s => !s.Length.HasValue || s.Length.Value < MaxTime).ToList();
            if (songsList.Count is 1) /* Then */ return songsList.First();

            var nameRegex = new Regex(regexGameName, RegexOptions.IgnoreCase);
            return songsList.FirstOrDefault(s => SongTitleEnds.Any(e => s.Name.EndsWith(e)))
                ?? songsList.FirstOrDefault(s => nameRegex.IsMatch(s.Name))
                ?? songsList.FirstOrDefault();
        }

        private IDownloader SourceToDownloader(Source source)
        {
            switch (source)
            {
                case Source.KHInsider:  return _khDownloader;
                case Source.Youtube:    return _ytDownloader;
                case Source.Local:      return _localDownloader;
                case Source.SoundCloud: return _scDownloader;
                default: throw new ArgumentException($"Unrecognized download source: {source}");
            }
        }

        private static async IAsyncEnumerable<IEnumerable<T>> CreateBatchEnumerableAsync<T>(
            IAsyncEnumerable<T> items, [EnumeratorCancellation] CancellationToken token)
        {
            var enumerator = items.GetAsyncEnumerator(token);
            var hasItems = true;
            do
            {
                var batch = new List<T>();
                for (var i = 0; i < 10; i++)
                {
                    hasItems = await enumerator.MoveNextAsync(token);
                    if (!hasItems) /* Then */ break;
                    batch.Add(enumerator.Current);
                }
                yield return batch;
            } while (hasItems);
        }

        #endregion

        #endregion
    }
}
