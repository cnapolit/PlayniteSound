using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Playnite.SDK;
using PlayniteSounds.Models;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Search;

namespace PlayniteSounds.Files.Download.Downloaders
{
    internal class YtDownloader : IDownloader
    {
        #region Infrastructure

        private const           Source                 DlSource        = Source.Youtube;
        private const           string                 BaseYtUrl       = "https://www.youtube.com";
        private static readonly ILogger                Logger          = LogManager.GetLogger();
        private        readonly YoutubeClient          _youtubeClient;
        private        readonly PlayniteSoundsSettings _settings;

        public YtDownloader(HttpClient httpClient, PlayniteSoundsSettings settings)
        {
            _youtubeClient = new YoutubeClient(httpClient);
            _settings = settings;
        }

        #endregion

        #region Implementation

        #region DownloadSource

        public Source DownloadSource() => DlSource;

        #endregion

        #region BaseUrl

        public string BaseUrl() => BaseYtUrl;

        #endregion

        #region GetAlbums

        public IEnumerable<Album> GetAlbumsForGame(string gameName, bool auto = false)
            => GetAlbumsFromExplodeApiAsync(gameName, auto).Result;

        private async Task<List<Album>> GetAlbumsFromExplodeApiAsync(string gameName, bool auto)
        {
            if (auto)
            {
                gameName += " Soundtrack";
            }

            var albums = new List<Album>();
            var videos = new List<Song>();

            var videoResults = _youtubeClient.Search.GetResultBatchesAsync(gameName, SearchFilter.Video);
            var videoEnumerator = videoResults.GetAsyncEnumerator();

            for (var i = 0; i < 1 && await videoEnumerator.MoveNextAsync(); i++)
            {
                var batchOfVideos =
                    from VideoSearchResult videoSearchResult in videoEnumerator.Current.Items
                    select new Song
                    {
                        Name = videoSearchResult.Title,
                        Id = videoSearchResult.Id,
                        Length = videoSearchResult.Duration,
                        Source = DlSource,
                        IconUrl = videoSearchResult.Thumbnails.FirstOrDefault()?.Url
                    };

                videos.AddRange(batchOfVideos);
                
                await videoEnumerator.MoveNextAsync();
            }

            if (videos.Any()) albums.Add(new Album
            {
                Name = Common.Constants.Resource.YoutubeSearch,
                Songs = videos,
                Source = DlSource
            });

            if (_settings.YtPlaylists)
            {
                var playlistResults = _youtubeClient.Search.GetResultBatchesAsync(gameName, SearchFilter.Playlist);

                var playlistEnumerator = playlistResults.GetAsyncEnumerator();
                for (var i = 0; i < 1 && await playlistEnumerator.MoveNextAsync(); i++)
                {
                    var batchOfPlaylists =
                        from PlaylistSearchResult playlistSearchResult in playlistEnumerator.Current.Items
                        select new Album
                        {
                            Name = playlistSearchResult.Title,
                            Id = playlistSearchResult.Id,
                            Source = DlSource,
                            IconUrl = playlistSearchResult.Thumbnails.FirstOrDefault()?.Url
                        };

                    albums.AddRange(batchOfPlaylists);
                }
            }

            return albums;
        }

        #endregion

        #region GetSongs

        public IEnumerable<Song> GetSongsFromAlbum(Album album)
            => album.Songs ?? GetSongsFromExplodeApiAsync(album).ToEnumerable();

        private IAsyncEnumerable<Song> GetSongsFromExplodeApiAsync(Album album)
            => _youtubeClient.Playlists.GetVideosAsync(album.Id).Select(video => new Song
            {
                Name = video.Title,
                Id = video.Id,
                Length = video.Duration,
                Source = DlSource,
                IconUrl= video.Thumbnails.FirstOrDefault()?.Url
            });

        #endregion

        #region DownloadSong

        public bool DownloadSong(Song song, string path) => DownloadSongExplodeAsync(song, path).Result;

        private async Task<bool> DownloadSongExplodeAsync(Song song, string path)
        {
            try
            {
                await _youtubeClient.Videos.DownloadAsync(song.Id, path, o => o.SetFFmpegPath(_settings.FFmpegPath));
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Something went wrong when attempting to download from Youtube with Id '{song.Id}' and Path '{path}'");
                return false;
            }
        }

        #endregion

        #endregion
    }
}
