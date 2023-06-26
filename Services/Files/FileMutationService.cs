using Playnite.SDK.Models;
using Playnite.SDK;
using PlayniteSounds.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using PlayniteSounds.Common.Constants;
using System.Threading;
using PlayniteSounds.Common;
using PlayniteSounds.Services.Audio;
using System.IO;
using PlayniteSounds.Files.Download;
using PlayniteSounds.Services.Play;

namespace PlayniteSounds.Services.Files
{
    public class FileMutationService// : IFileMutationService
    {
        #region Infrastructure

        private static readonly ILogger                Logger            = LogManager.GetLogger();
        private        readonly IDownloadManager       _downloadManager;
        private        readonly IErrorHandler          _errorHandler;
        private        readonly IFileManager           _fileManager;
        private        readonly IPathingService        _pathingService;
        private        readonly INormalizer            _normalizer;
        private        readonly ITagger                _tagger;
        private        readonly IMainViewAPI           _mainViewAPI;
        private        readonly IPromptFactory         _promptFactory;
        private        readonly IMusicPlayer           _musicPlayer;
        private        readonly PlayniteSoundsSettings _settings;

        public FileMutationService(
            IDownloadManager downloadManager,
            IErrorHandler errorHandler,
            IFileManager fileManager,
            IPathingService pathingService,
            INormalizer normalizer,
            ITagger tagger,
            IMainViewAPI mainViewAPI,
            IPromptFactory promptFactory,
            IMusicPlayer musicPlayer,
            PlayniteSoundsSettings settings)
        {
            _mainViewAPI = mainViewAPI;
            _downloadManager = downloadManager;
            _errorHandler = errorHandler;
            _fileManager = fileManager;
            _pathingService = pathingService;
            _normalizer = normalizer;
            _tagger = tagger;
            _promptFactory = promptFactory;
            _musicPlayer = musicPlayer;
            _settings = settings;
        }

        #endregion

        #region Implementation

        #region SelectMusicForPlatform

        #endregion

        #region SelectMusicForGames

        #endregion

        #region SelectMusicForDefault


        #endregion

        #region SelectMusicForFilter

        #endregion

        #region DeleteMusicDirectories

        #endregion

        #region DeleteMusicFile

        #endregion

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

        #region CreateNormalizationDialogue

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

        #region Helpers

        private void RestartMusicAfterSelect(Func<IEnumerable<string>> selectFunc, bool playNewMusic)
        {
            var newMusic = selectFunc();
            var newMusicFile = newMusic.FirstOrDefault();

            if (playNewMusic && newMusicFile != null)
            {
                _musicPlayer.CurrentMusicFile = newMusicFile;
            }
            else
            {
                _musicPlayer.Play(_mainViewAPI.SelectedGames);
            }
        }

        private void PerformDeleteAction(string message, Action deleteAction)
        {
            if (!_promptFactory.PromptForApproval(message)) return;

            deleteAction();

            Thread.Sleep(250);

            _musicPlayer.Play(_mainViewAPI.SelectedGames);
        }

        private static string GenerateTitle(GlobalProgressActionArgs args, Game game, string progressTitle)
            => $"{progressTitle}\n\n{++args.CurrentProgressValue}/{args.ProgressMaxValue}\n{game.Name}";

        #region Download

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
                    : _downloadManager.BestSongPick(album.Songs.ToList(), regexGameName);
            }
            else
            {
                var songs = _downloadManager.GetSongsFromAlbum(album).ToList();
                if (!songs.Any())
                {
                    Logger.Info($"Did not find any songs for album '{album.Name}' of game '{gameName}'");
                }
                else
                {
                    Logger.Info($"Found songs for album '{album.Name}' of game '{gameName}'");
                    song = songSelect
                        ? PromptForSong(songs, regexGameName)
                        : _downloadManager.BestSongPick(songs, regexGameName);
                }
            }

            return song;
        }

        private IEnumerable<Song> SearchYoutube(string search)
        {
            var album = _downloadManager.GetAlbumsForGame(search, Source.Youtube).First();
            return _downloadManager.GetSongsFromAlbum(album);
        }

        private bool OnlySearchForYoutubeVideos(Source source) => source is Source.Youtube && !_settings.YtPlaylists;

        private void StartDownload(
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
                args.Text = GenerateTitle(args, game, progressTitle);

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

            Logger.Info($"Selected album '{album.Name}' from source '{album.Source}' for game '{gameName}'");

            var song = SelectSongFromAlbum(album, gameName, strippedGameName, regexGameName, songSelect);
            if (song is null)
            {
                return null;
            }

            Logger.Info($"Selected song '{song.Name}' from album '{album.Name}' for game '{gameName}'");

            var sanitizedFileName = StringUtilities.Sanitize(song.Name) + ".mp3";
            var newFilePath = Path.Combine(gameDirectory, sanitizedFileName);
            if (!overwrite && File.Exists(newFilePath))
            {
                Logger.Info($"Song file '{sanitizedFileName}' for game '{gameName}' already exists. Skipping....");
                return null;
            }

            Logger.Info($"Overwriting song file '{sanitizedFileName}' for game '{gameName}'.");

            if (!_downloadManager.DownloadSong(song, newFilePath))
            {
                Logger.Info($"Failed to download song '{song.Name}' for album '{album.Name}' of game '{gameName}' with source {song.Source} and Id '{song.Id}'");
                return null;
            }

            Logger.Info($"Downloaded file '{sanitizedFileName}' in album '{album.Name}' of game '{gameName}'");
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
                Logger.Info($"Skipping album search for game '{gameName}'");
                album = new Album { Name = Resource.YoutubeSearch, Source = Source.Youtube };
            }
            else
            {
                Logger.Info($"Starting album search for game '{gameName}'");

                if (albumSelect)
                {
                    album = PromptForAlbum(strippedGameName, source);
                }
                else
                {
                    var albums = _downloadManager.GetAlbumsForGame(strippedGameName, source, true).ToList();
                    if (albums.Any())
                    {
                        album = _downloadManager.BestAlbumPick(albums, strippedGameName, regexGameName);
                    }
                    else
                    {
                        Logger.Info($"Did not find any albums for game '{gameName}' from source '{source}'");
                    }
                }
            }

            return album;
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
                s => _downloadManager.GetAlbumsForGame(s, source)
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

        #endregion
    }
}
