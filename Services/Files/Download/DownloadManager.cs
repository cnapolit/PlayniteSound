using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System;
using PlayniteSounds.Models;
using PlayniteSounds.Files.Download.Downloaders;

namespace PlayniteSounds.Files.Download
{

    public class DownloadManager : IDownloadManager
    {
        #region Infrastructure

        private static readonly TimeSpan               MaxTime       = new TimeSpan(0, 8, 0);
        private static readonly HtmlWeb                Web           = new HtmlWeb();
        private static readonly HttpClient             HttpClient    = new HttpClient();
        private static readonly List<string>           SongTitleEnds = new List<string> { "Theme", "Title", "Menu" };
        private        readonly PlayniteSoundsSettings _settings;
        private        readonly IDownloader            _khDownloader;
        private        readonly IDownloader            _ytDownloader;

        public DownloadManager(PlayniteSoundsSettings settings)
        {
            _settings = settings;
            _khDownloader = new KhDownloader(HttpClient, Web);
            _ytDownloader = new YtDownloader(HttpClient, _settings);
        }

        #endregion

        #region Implementation

        #region GetAlbums

        public IEnumerable<Album> GetAlbumsForGame(string gameName, Source source, bool auto = false)
        {
            if ((source is Source.All || source is Source.Youtube) && string.IsNullOrWhiteSpace(_settings.FFmpegPath))
            {
                throw new Exception("Cannot download from Youtube without the FFmpeg Path specified in settings.");
            }

            if (source is Source.All)
            {
                IEnumerable<Album> retrieveAlbums(Source d) 
                    => SourceToDownloader(d).GetAlbumsForGame(gameName, auto);

                return ShouldSearchInParrallel(auto)
                    ? _settings.Downloaders.SelectMany(retrieveAlbums)
                    : _settings.Downloaders.Select(retrieveAlbums).FirstOrDefault(dl => dl.Any());
            }

            return SourceToDownloader(source).GetAlbumsForGame(gameName, auto);
        }

        private bool ShouldSearchInParrallel(bool auto)
            => (_settings.AutoParallelDownload && auto) || (_settings.ManualParallelDownload && !auto);

        #endregion

        #region GetSongs

        public IEnumerable<Song> GetSongsFromAlbum(Album album)
            => SourceToDownloader(album.Source).GetSongsFromAlbum(album);

        #endregion

        #region DownloadSong

        public bool DownloadSong(Song song, string path)
            => SourceToDownloader(song.Source).DownloadSong(song, path);

        #endregion

        #region GetAlbumUrl

        public string GetItemUrl(DownloadItem item)
        {
            var downloader = SourceToDownloader(item.Source);
            return item is Album album
            ? downloader.AlbumUrl(album)
            : downloader.SongUrl(item as Song);
        }

        #endregion

        #region BestAlbumPick

        public Album BestAlbumPick(IEnumerable<Album> albums, string gameName, string regexGameName)
        {
            var albumsList = albums.ToList();

            if (albumsList.Count is 1)
            {
                return albumsList.First();
            }

            var ostRegex = new Regex($@"{regexGameName}.*(Soundtrack|OST|Score)", RegexOptions.IgnoreCase);
            var ostMatch = albumsList.FirstOrDefault(a => ostRegex.IsMatch(a.Name));
            if (ostMatch != null)
            {
                return ostMatch;
            }

            var exactMatch = albumsList.FirstOrDefault(a => string.Equals(a.Name, gameName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
            {
                return exactMatch;
            }

            var closeMatch = albumsList.FirstOrDefault(a => a.Name.StartsWith(gameName, StringComparison.OrdinalIgnoreCase));
            return closeMatch ?? albumsList.FirstOrDefault();
        }

        #endregion

        #region BestSongPick

        public Song BestSongPick(IEnumerable<Song> songs, string regexGameName)
        {
            var songsList = songs.Where(s => !s.Length.HasValue || s.Length.Value < MaxTime).ToList();

            if (songsList.Count is 1)
            {
                return songsList.First();
            }

            var titleMatch = songsList.FirstOrDefault(s => SongTitleEnds.Any(e => s.Name.EndsWith(e)));
            if (titleMatch != null)
            {
                return titleMatch;
            }

            var nameRegex = new Regex(regexGameName, RegexOptions.IgnoreCase);
            var gameNameMatch = songsList.FirstOrDefault(s => nameRegex.IsMatch(s.Name));
            return gameNameMatch ?? songsList.FirstOrDefault();
        }

        #endregion

        #region Helpers

        private IDownloader SourceToDownloader(Source source)
        {
            switch (source)
            {
                case Source.KHInsider: return _khDownloader;
                case Source.Youtube: return _ytDownloader;
                default: throw new ArgumentException($"Unrecognized download source: {source}");
            }
        }

        #endregion

        #endregion
    }
}
