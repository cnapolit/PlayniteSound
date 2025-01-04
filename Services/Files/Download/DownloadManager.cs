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

        public IAsyncEnumerable<Album> GetAlbumsForGameAsync(
            Game game, string searchTerm, Source source, bool auto = false, CancellationToken? token = null)
        {
            if ((source is Source.All || source is Source.Youtube) && string.IsNullOrWhiteSpace(_settings.FFmpegPath))
            throw new Exception("Cannot download from YouTube without the FFmpeg path specified in settings.");

            return SourceToDownloader(source).GetAlbumsForGameAsync(game, searchTerm, auto);
        }

        public IAsyncEnumerable<IEnumerable<Album>> GetAlbumBatchesForGameAsync(
            Game game, string searchTerm, Source source, bool auto = false, CancellationToken? token = null)
        {
            if ((source is Source.All || source is Source.Youtube) && string.IsNullOrWhiteSpace(_settings.FFmpegPath))
            /* Then */ throw new Exception("Cannot download from YouTube without the FFmpeg path specified in settings.");

            var downloader = SourceToDownloader(source);
            if (downloader is not IBatchDownloader batchloader)
            /* Then */ throw new Exception($"Downloader '{source}' does not support batching");

            return batchloader.GetAlbumBatchesForGameAsync(game, searchTerm, auto);
        }

        public IAsyncEnumerable<Song> GetSongsFromAlbumAsync(Album album, CancellationToken? token = null)
            => SourceToDownloader(album.Source).GetSongsFromAlbumAsync(album)
                                               .Select(s => { s.ParentAlbum = album; return s; }); 

        public Task GetAlbumInfoAsync(
            Album album, CancellationToken token, Func<Action<Song>, Song, Task> updateCallback)
            => SourceToDownloader(album.Source).GetAlbumInfoAsync(album, token, updateCallback);

        public async Task<bool> DownloadAsync(
            Game game, DownloadItem downloadItem, string path, IProgress<double> progress, CancellationToken token)
        {
            var downloader = SourceToDownloader(downloadItem.Source);
            if (downloadItem is Album album)
            {
                if (!downloader.SupportsBulkDownload) /* Then */ return false;

                if (album.Songs is null || album.Songs.Count < album.Count)
                {
                    album.Songs = await GetSongsFromAlbumAsync(album).ToObservableCollectionAsync(token);
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
                if (progress is null) /* Then */ await song.Stream.CopyToAsync(fileStream);
                else                  /* Then */ await DownloadCommon.DownloadStreamAsync(song.Stream, fileStream, progress, token);
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

        public DownloadCapabilities GetCapabilities(DownloadItem item)
            => SourceToDownloader(item.Source).GetCapabilities(item);

        public DownloadCapabilities GetCapabilities(Source source)
            => SourceToDownloader(source).GetCapabilities();

        public IAsyncEnumerable<Song> SearchSongsAsync(
            Game game, string searchTerm, Source source, CancellationToken? token = null)
            => SourceToDownloader(source).SearchSongsAsync(game, searchTerm);

        public string GetItemUrl(DownloadItem item) => SourceToDownloader(item.Source).GetItemUrl(item);

        public string GetSourceIcon(Source source) => SourceToDownloader(source).SourceIcon;
        public string GetSourceLogo(Source source) => SourceToDownloader(source).SourceLogo;

        private const int BatchCount = 5;

        public async Task<DownloadStatus> DownloadAsync(Game game, CancellationToken token)
        {
            var sanitizedName = StringUtilities.Sanitize(game.Name);
            
            foreach (var source in _settings.Downloaders)
            {
                var downloader = SourceToDownloader(source);
                List<Album> albums;
                if (downloader is IBatchDownloader batchloader)
                {
                    var i = 0;
                    albums = new List<Album>();
                    var batches = batchloader.GetAlbumBatchesForGameAsync(game, game.Name, true, token);
                    await foreach (var albumBatch in batches)
                    {
                        albums.AddRange(albumBatch);
                        if (i++ == BatchCount) /* Then */ break;
                    }
                }
                else /* Then */ albums = await downloader.GetAlbumsForGameAsync(game, game.Name, true, token)
                                                         .ToListAsync(token);

                var selectedAlbum = BestAlbumPick(albums, game.Name, sanitizedName);
                if (selectedAlbum is null) /* Then */ continue;
                _logger.Info($"Selected album '{selectedAlbum.Name}' from source '{source}' for game '{game.Name}'");

                var songs = await downloader.GetSongsFromAlbumAsync(selectedAlbum, token).ToListAsync(token);
                var selectedSong = BestSongPick(songs, sanitizedName);
                if (selectedSong is null) /* Then */ continue;
                _logger.Info($"Selected album '{selectedSong.Name}' from source '{source}' for game '{game.Name}'");

                var dir = _fileManager.CreateMusicDirectory(game);
                var path = Path.Combine(dir, sanitizedName + (selectedSong.Types?.FirstOrDefault() ?? ".mp3"));
                if (!await downloader.DownloadAsync(selectedSong, path, null, token)) /* Then */ continue;
                _logger.Info($"Downloaded from source '{source}' for game '{game.Name}'");

                _fileManager.ApplyTags(game, selectedSong, path);

                return await _normalizer.NormalizeAudioFileAsync(path) 
                    ? DownloadStatus.Downloaded | DownloadStatus.Normalized 
                    : DownloadStatus.Downloaded;
            }

            return DownloadStatus.Failed;
        }

        public Album BestAlbumPick(IList<Album> albums, string gameName, string regexGameName)
        {
            if (albums.Count is 1) /* Then */ return albums.First();

            var ostRegex = new Regex($"{regexGameName}.*(Soundtrack|OST|Score)", RegexOptions.IgnoreCase);
            return albums.FirstOrDefault(a => ostRegex.IsMatch(a.Name))
                ?? albums.FirstOrDefault(a => string.Equals(a.Name, gameName, StringComparison.OrdinalIgnoreCase))
                ?? albums.FirstOrDefault(a => a.Name.StartsWith(gameName, StringComparison.OrdinalIgnoreCase))
                ?? albums.FirstOrDefault();
        }

        public Song BestSongPick(IEnumerable<Song> songs, string regexGameName)
        {
            var songsList = songs.Where(s => !s.Length.HasValue || s.Length.Value < MaxTime).ToList();
            if (songsList.Count is 1) /* Then */ return songsList.First();

            var nameRegex = new Regex(regexGameName, RegexOptions.IgnoreCase);
            return songsList.FirstOrDefault(s => SongTitleEnds.Any(e => s.Name.EndsWith(e)))
                ?? songsList.FirstOrDefault(s => nameRegex.IsMatch(s.Name))
                ?? songsList.FirstOrDefault();
        }

        #region Helpers

        private IDownloader SourceToDownloader(Source source)
        {
            switch (source)
            {
                case Source.KHInsider: return _khDownloader;
                case Source.Youtube:   return _ytDownloader;
                case Source.Local:     return _localDownloader;
                default: throw new ArgumentException($"Unrecognized download source: {source}");
            }
        }

        #endregion

        #endregion
    }
}
