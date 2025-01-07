using Playnite.SDK;
using PlayniteSounds.Models;
using PlayniteSounds.Models.Download;
using PlayniteSounds.Services.State;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK.Models;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Playlists;

namespace PlayniteSounds.Files.Download.Downloaders
{
    internal class YtDownloader : IDownloader
    {
        #region Infrastructure

        private const          Source                 DlSource    = Source.Youtube;
        private const          string                 BaseUrl     = "https://www.youtube.com/embed/";
        private const          string                 PlaylistUrl = BaseUrl + "videoseries?list=";
        private       readonly ILogger                _logger;
        private       readonly IAssemblyResolver      _assemblyResolver;
        private       readonly YoutubeClient          _youtubeClient;
        private       readonly PlayniteSoundsSettings _settings;

        public YtDownloader(
            ILogger logger, IAssemblyResolver assemblyResolver, HttpClient httpClient, PlayniteSoundsSettings settings)
        {
            _logger = logger;
            _assemblyResolver = assemblyResolver;
            _youtubeClient = new YoutubeClient(httpClient);
            _settings = settings;
        }

        #endregion

        #region Implementation

        public bool   SupportsBulkDownload => true;
        public string SourceLogo           => "YouTube.png";
        public string SourceIcon           => "YouTube.ico";

        public string GetItemUrl(DownloadItem item) => (item is Album ? PlaylistUrl : BaseUrl) + item.Id;

        public DownloadCapabilities GetCapabilities(DownloadItem item) => GetCapabilities();
        public DownloadCapabilities GetCapabilities()
            => DownloadCapabilities.Batching | DownloadCapabilities.Bulk | DownloadCapabilities.FlatSearch;

        public string GenerateSearchStr(string gameName) => gameName + " Soundtrack";

        #region GetAlbums

        public IAsyncEnumerable<Album> GetAlbumsForGameAsync(Game game, string searchTerm, CancellationToken? token = null)
            => GetAlbumBatchesForGameAsync(game, searchTerm).SelectMany(e => e.ToAsyncEnumerable());

        public IAsyncEnumerable<IEnumerable<Album>> GetAlbumBatchesForGameAsync(
            Game game, string searchTerm, CancellationToken? token = null)
        {
            using (_assemblyResolver.HandleAssemblies(
                    typeof(System.Text.Json.JsonSerializer).Assembly,
                    typeof(IAsyncDisposable).Assembly, 
                    typeof(Memory<>).Assembly))
            /* Then */ return GetAlbumsFromExplodeApiAsync(searchTerm, token);
        }

        private IAsyncEnumerable<IEnumerable<Album>> GetAlbumsFromExplodeApiAsync(string gameName, CancellationToken? token = null)
            => _youtubeClient.Search.GetResultBatchesAsync(gameName, SearchFilter.Playlist, token ?? CancellationToken.None)
                             .Select(b => b.Items.OfType<PlaylistSearchResult>().Select(PlaylistToAlbum));

        private Album PlaylistToAlbum(PlaylistSearchResult playlist)
        {
            var (icon, cover) = GetAlbumImages(playlist.Thumbnails);
            return new Album(GetSongsFromAlbumAsync)
            {
                Name = playlist.Title,
                Id = playlist.Id,
                Source = DlSource,
                IconUri = icon,
                CoverUri = cover,
                Uploader = playlist.Author?.ChannelTitle
            };
        }

        private static (string Icon, string Cover) GetAlbumImages(IReadOnlyList<Thumbnail> thumbnails)
        {
            var thumbnailsBySize = thumbnails.OrderBy(t => t.Resolution.Area)
                                             .Select(t => t.Url)
                                             .ToList();
            return (thumbnailsBySize.FirstOrDefault(), thumbnailsBySize.LastOrDefault());
        }

        #endregion

        public IAsyncEnumerable<Song> SearchSongsAsync(Game game, string searchTerm, CancellationToken? token = null)
            => SearchSongBatchesAsync(game, searchTerm, token).SelectMany(e => e.ToAsyncEnumerable());

        public IAsyncEnumerable<IEnumerable<Song>> SearchSongBatchesAsync(Game game, string searchTerm, CancellationToken? token = null)
            => _youtubeClient.Search.GetResultBatchesAsync(searchTerm, SearchFilter.Video, token ?? CancellationToken.None)
                                    .Select(b => b.Items.OfType<VideoSearchResult>().Select(VideoToSong));

        public async Task GetAlbumInfoAsync(
            Album album, CancellationToken token, Func<Action<Song>, Song, Task> updateCallback)
        {
            var youtubeAlbum = await _youtubeClient.Playlists.GetAsync(album.Id, token);
            album.Description = youtubeAlbum.Description;
            if (youtubeAlbum.Count is null)
            {
                throw new Exception("Infinite playlists are not supported");
            }

            album.Count = (uint)youtubeAlbum.Count;

            if (album.Songs.Count is 0 || album.Songs.Count < album.Count)
            {
                var length = new TimeSpan();

                var tasks = new List<Task<double>>();
                await foreach (var videoBatch in _youtubeClient.Playlists.GetVideoBatchesAsync(album.Id, token))
                {
                    foreach (var playlistVideo in videoBatch.Items)
                    {
                        var (icon, cover) = GetAlbumImages(playlistVideo.Thumbnails);
                        var song = new Song
                        {
                            Name = playlistVideo.Title,
                            Id = playlistVideo.Id,
                            Album = album.Name,
                            ParentAlbum = album,
                            Length = playlistVideo.Duration,
                            Source = DlSource,
                            IconUri = icon,
                            CoverUri = cover
                        };
                        tasks.Add(UpdateSongAsync(song, playlistVideo, token));
                        await updateCallback(album.Songs.Add, song);

                        if (playlistVideo.Duration != null)
                        /* Then */ length = length.Add((TimeSpan)playlistVideo.Duration); 
                    }
                }

                if (token.IsCancellationRequested) /* Then */ return;

                var sizes = await Task.WhenAll(tasks);

                if (token.IsCancellationRequested) /* Then */ return;

                album.Sizes = new Dictionary<string, string>
                {
                    ["Size"] = $"{Math.Round(sizes.Sum(), 2)} MB"
                };
            }

            album.HasSongsToEnumerate = album.HasExtraInfo = token.IsCancellationRequested;
            await updateCallback(null, null);
        }

        private async Task<double> UpdateSongAsync(Song song, PlaylistVideo playlistVideo, CancellationToken token)
        {
            var manifestTask = _youtubeClient.Videos.Streams.GetManifestAsync(playlistVideo.Url, token);
            Video video;
            using (_assemblyResolver.HandleAssemblies(typeof(System.Text.CodePagesEncodingProvider).Assembly))
            /* Then */ video = await _youtubeClient.Videos.GetAsync(playlistVideo.Url, token);
            song.CreationDate = video.UploadDate.ToLocalTime().ToString();
            song.Description = video.Description;

            var streamManifest = await manifestTask;
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            song.StreamFunc = t => CreateAudioStreamAsync(streamInfo, t);

            song.Sizes = new Dictionary<string, string>
            {
                [streamInfo.Container.Name] = $"{Math.Round(streamInfo.Size.MegaBytes)} MB"
            };
            return streamInfo.Size.MegaBytes;
        }

        #region GetSongsFromAlbumAsync

        public IAsyncEnumerable<Song> GetSongsFromAlbumAsync(Album album, CancellationToken? token = null) 
            => _youtubeClient.Playlists.GetVideosAsync(album.Id).Select(VideoToSong);

        private async Task<Stream> CreateAudioStreamAsync(string url, CancellationToken token)
        {
            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(url, token);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            return await CreateAudioStreamAsync(streamInfo, token);
        }

        #endregion

        #region DownloadStreamAsync

        public async Task<bool> DownloadAsync(
            DownloadItem item, string path, IProgress<double> progress, CancellationToken token)
        {
            using  (_assemblyResolver.HandleAssemblies(typeof(CliWrap.Cli).Assembly))
            return await DownloadSongExplodeAsync(item as Song, path, progress, token);
        }

        private async Task<bool> DownloadSongExplodeAsync(
            Song song, string path, IProgress<double> progress, CancellationToken token)
        {
            try
            {
                await _youtubeClient.Videos.DownloadAsync(
                    song.Id, path, o => o.SetFFmpegPath(_settings.FFmpegPath), progress, token);
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Something went wrong when attempting to download from Youtube with Id '{song.Id}' and Path '{path}'");
                return false;
            }
        }

        #endregion

        #region Helpers

        private Song VideoToSong(IVideo video) => new Song
        {
            Name = video.Title,
            Id = video.Id,
            Length = video.Duration,
            Source = DlSource,
            CoverUri = video.Thumbnails.FirstOrDefault()?.Url,
            Uploader = video.Author.ChannelTitle,
            StreamFunc = t => CreateAudioStreamAsync(video.Url, t)
        };

        private async Task<Stream> CreateAudioStreamAsync(IStreamInfo streamInfo, CancellationToken token)
        {
            var memoryStream = new MemoryStream();
            await _youtubeClient.Videos.Streams.CopyToAsync(streamInfo, memoryStream, cancellationToken: token);

            if (!token.IsCancellationRequested) /* Then */ return memoryStream;

            memoryStream.Dispose();
            return null;
        }

        #endregion

        #endregion
    }
}
