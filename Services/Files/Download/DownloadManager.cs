using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System;
using Playnite.SDK.Models;
using Playnite.SDK;
using PlayniteSounds.Common.Constants;
using PlayniteSounds.Common.Utilities;
using PlayniteSounds.Common;
using PlayniteSounds.Files.Download.Downloaders;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Play;
using PlayniteSounds.Services.Files;
using PlayniteSounds.Services;
using PlayniteSounds.Services.Audio;

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
        private        readonly ITagger                _tagger;
        private        readonly IFileManager           _fileManager;
        private        readonly INormalizer            _normalizer;
        private        readonly IPromptFactory         _promptFactory;
        private        readonly IMusicPlayer           _musicPlayer;
        private        readonly IDownloader            _khDownloader;
        private        readonly IDownloader            _ytDownloader;
        private        readonly PlayniteSoundsSettings _settings;

        public DownloadManager(
            ILogger logger,
            ITagger tagger,
            IFileManager fileManager,
            INormalizer normalizer,
            IPromptFactory promptFactory,
            IMusicPlayer musicPlayer,
            PlayniteSoundsSettings settings)
        {
            _logger = logger;
            _tagger = tagger;
            _fileManager = fileManager;
            _normalizer = normalizer;
            _promptFactory = promptFactory;
            _musicPlayer = musicPlayer;
            _settings = settings;
            _khDownloader = new KhDownloader(logger, HttpClient, Web);
            _ytDownloader = new YtDownloader(logger, HttpClient, _settings);
        }

        #endregion

        #region Implementation

        #region DownloadMusicForGames

        public void DownloadMusicForGames(Source source, IList<Game> games)
        {
            var albumSelect = true;
            var songSelect = true;
            if (games.Count > 1)
            {
                albumSelect = _promptFactory.PromptForApproval(Resource.DialogMessageAlbumSelect);
                songSelect = _promptFactory.PromptForApproval(Resource.DialogMessageSongSelect);
            }

            var overwriteSelect = _promptFactory.PromptForApproval(Resource.DialogMessageOverwriteSelect);

            CreateDownloadDialogue(games, source, albumSelect, songSelect, overwriteSelect);

            _promptFactory.ShowMessage(Resource.DialogMessageDone);

            _musicPlayer.Resume(false);
        }

        #endregion

        #region CreateDownloadDialogue

        public void CreateDownloadDialogue(
            IEnumerable<Game> games,
            Source source,
            bool albumSelect = false,
            bool songSelect = false,
            bool overwriteSelect = false)
        {
            void DownloadAction(GlobalProgressActionArgs args, string title)
                => StartDownload(args, games.ToList(), source, title, albumSelect, songSelect, overwriteSelect);

            _promptFactory.CreateGlobalProgress(Resource.DialogMessageDownloadingFiles, DownloadAction);
        }

        #endregion

        private Song SelectSongFromAlbum(
            Album album,
            string gameName,
            string strippedGameName,
            string regexGameName,
            bool songSelect)
        {
            Song song = null;

            if (OnlySearchForYoutubeVideos(album.Source))
            {
                song = songSelect
                    ? PromptUserForYoutubeSearch(strippedGameName)
                    : BestSongPick(album.Songs.ToList(), regexGameName);
            }
            else
            {
                var songs = GetSongsFromAlbum(album).ToList();
                if (!songs.Any())
                {
                    _logger.Info($"Did not find any songs for album '{album.Name}' of game '{gameName}'");
                }
                else
                {
                    _logger.Info($"Found songs for album '{album.Name}' of game '{gameName}'");
                    song = songSelect
                        ? PromptForSong(songs, regexGameName)
                        : BestSongPick(songs, regexGameName);
                }
            }

            return song;
        }

        private IEnumerable<Song> SearchYoutube(string search)
        {
            var album = GetAlbumsForGame(search, Source.Youtube).First();
            return GetSongsFromAlbum(album);
        }

        private bool OnlySearchForYoutubeVideos(Source source) => source is Source.Youtube && !_settings.YtPlaylists;

        public void StartDownload(
            GlobalProgressActionArgs args,
            List<Game> games,
            Source source,
            string progressTitle,
            bool albumSelect,
            bool songSelect,
            bool overwrite)
        {
            args.ProgressMaxValue = games.Count;
            var gamesToUpdateTags = new List<Game>();
            foreach (var game in games.TakeWhile(_ => !args.CancelToken.IsCancellationRequested))
            {
                args.Text = UIUtilities.GenerateTitle(args, game, progressTitle);

                var gameDirectory = _fileManager.CreateMusicDirectory(game);

                var newFilePath =
                    DownloadSongFromGame(source, game.Name, gameDirectory, songSelect, albumSelect, overwrite);

                var addToUpdatedGames = false;
                var fileDownloaded = newFilePath != null;

                if (fileDownloaded)
                {
                    addToUpdatedGames = _tagger.RemoveTag(game, Resource.MissingTag);
                }
                else if (!Directory.Exists(gameDirectory) || !Directory.GetFiles(gameDirectory).Any())
                {
                    addToUpdatedGames = _tagger.AddTag(game, Resource.MissingTag);
                }

                if (_settings.NormalizeMusic && fileDownloaded)
                {
                    args.Text += $" - {Resource.DialogMessageNormalizingFiles}";
                    if (_normalizer.NormalizeAudioFile(newFilePath))
                    {
                        addToUpdatedGames |= _tagger.AddTag(game, Resource.NormTag);
                    }
                }

                if (addToUpdatedGames)
                {
                    gamesToUpdateTags.Add(game);
                }
            }

            if (gamesToUpdateTags.Any())
            {
                args.Text = progressTitle + "Updating Tags";
                _tagger.UpdateGames(gamesToUpdateTags);
            }
        }

        private string DownloadSongFromGame(
            Source source,
            string gameName,
            string gameDirectory,
            bool songSelect,
            bool albumSelect,
            bool overwrite)
        {

            var strippedGameName = StringUtilities.StripStrings(gameName);

            var regexGameName = songSelect && albumSelect
                ? string.Empty
                : StringUtilities.ReplaceStrings(strippedGameName);

            var album = SelectAlbumForGame(source, gameName, strippedGameName, regexGameName, albumSelect, songSelect);
            if (album is null)
            {
                return null;
            }

            _logger.Info($"Selected album '{album.Name}' from source '{album.Source}' for game '{gameName}'");

            var song = SelectSongFromAlbum(album, gameName, strippedGameName, regexGameName, songSelect);
            if (song is null)
            {
                return null;
            }

            _logger.Info($"Selected song '{song.Name}' from album '{album.Name}' for game '{gameName}'");

            var sanitizedFileName = StringUtilities.Sanitize(song.Name) + ".mp3";
            var newFilePath = Path.Combine(gameDirectory, sanitizedFileName);
            if (!overwrite && File.Exists(newFilePath))
            {
                _logger.Info($"Song file '{sanitizedFileName}' for game '{gameName}' already exists. Skipping....");
                return null;
            }

            _logger.Info($"Overwriting song file '{sanitizedFileName}' for game '{gameName}'.");

            if (!DownloadSong(song, newFilePath))
            {
                _logger.Info($"Failed to download song '{song.Name}' for album '{album.Name}' of game '{gameName}' with source {song.Source} and Id '{song.Id}'");
                return null;
            }

            _logger.Info($"Downloaded file '{sanitizedFileName}' in album '{album.Name}' of game '{gameName}'");
            return newFilePath;
        }

        private Album SelectAlbumForGame(
            Source source,
            string gameName,
            string strippedGameName,
            string regexGameName,
            bool albumSelect,
            bool songSelect)
        {
            Album album = null;

            var skipAlbumSearch = OnlySearchForYoutubeVideos(source) && songSelect;
            if (skipAlbumSearch)
            {
                _logger.Info($"Skipping album search for game '{gameName}'");
                album = new Album { Name = Resource.YoutubeSearch, Source = Source.Youtube };
            }
            else
            {
                _logger.Info($"Starting album search for game '{gameName}'");

                if (albumSelect)
                {
                    album = PromptForAlbum(strippedGameName, source);
                }
                else
                {
                    var albums = GetAlbumsForGame(strippedGameName, source, true).ToList();
                    if (albums.Any())
                    {
                        album = BestAlbumPick(albums, strippedGameName, regexGameName);
                    }
                    else
                    {
                        _logger.Info($"Did not find any albums for game '{gameName}' from source '{source}'");
                    }
                }
            }

            return album;
        }

        #region GetAlbums

        public IEnumerable<Album> GetAlbumsForGame(string gameName, Source source, bool auto = false)
        {
            if ((source is Source.All || source is Source.Youtube) && string.IsNullOrWhiteSpace(_settings.FFmpegPath))
            {
                throw new Exception("Cannot download from YouTube without the FFmpeg Path specified in settings.");
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

        #region Prompts

        private Song PromptUserForYoutubeSearch(string gameName)
            => _promptFactory.PromptForSelect<Song>(Resource.DialogMessageCaptionSong,
                gameName,
                s => SearchYoutube(s).Select(a => new GenericObjectOption(a.Name, a.ToString(), a) as GenericItemOption).ToList(),
                gameName + " soundtrack");
        private Album PromptForAlbum(string gameName, Source source)
            => _promptFactory.PromptForSelect<Album>(Resource.DialogMessageCaptionAlbum,
                gameName,
                s => GetAlbumsForGame(s, source)
                    .Select(a => new GenericObjectOption(a.Name, a.ToString(), a) as GenericItemOption).ToList(),
                gameName + (source is Source.Youtube ? " soundtrack" : string.Empty));

        private Song PromptForSong(List<Song> songsToPartialUrls, string albumName)
            => _promptFactory.PromptForSelect<Song>(Resource.DialogMessageCaptionSong,
                albumName,
                a => songsToPartialUrls.OrderByDescending(s => s.Name.StartsWith(a))
                    .Select(s =>
                        new GenericObjectOption(s.Name, s.ToString(), s) as GenericItemOption).ToList(),
                string.Empty);

        #endregion

        #endregion

        #endregion
    }
}
