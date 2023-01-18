using Playnite.SDK.Models;
using Playnite.SDK;
using PlayniteSounds.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using PlayniteSounds.Common.Constants;
using System.Threading;
using PlayniteSounds.Common.Extensions;
using PlayniteSounds.Common;
using PlayniteSounds.Services.Audio;
using System.IO;
using Castle.Core.Internal;
using PlayniteSounds.Files.Download;
using PlayniteSounds.Services.Play;

namespace PlayniteSounds.Services.Files
{
    internal class FileMutationService : IFileMutationService
    {
        #region Infrastructure

        private static readonly ILogger                Logger            = LogManager.GetLogger();
        private        readonly IPlayniteAPI           _api;
        private        readonly IDownloadManager       _downloadManager;
        private        readonly IErrorHandler          _errorHandler;
        private        readonly IFileManager           _fileManager;
        private        readonly IPathingService        _pathingService;
        private        readonly INormalizer            _normalizer;
        private        readonly ITagger                _tagger;
        private        readonly IPromptFactory         _promptFactory;
        private        readonly IMusicPlayer           _musicPlayer;
        private        readonly PlayniteSoundsSettings _settings;

        public FileMutationService(
            IPlayniteAPI api,
            IDownloadManager downloadManager,
            IErrorHandler errorHandler,
            IFileManager fileManager,
            IPathingService pathingService,
            INormalizer normalizer,
            ITagger tagger,
            IPromptFactory promptFactory,
            IMusicPlayer musicPlayer,
            PlayniteSoundsSettings settings)
        {
            _api = api;
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

        public void SelectMusicForPlatform(Platform platform)
        {
            var playNewMusic =
                _settings.MusicType is MusicType.Platform
                && _api.SingleGame()
                && _api.SelectedGames().First().Platforms.Contains(platform);

            RestartMusicAfterSelect(
                () => _fileManager.SelectMusicForPlatform(platform, _promptFactory.PromptForMp3()), playNewMusic);
        }

        #endregion

        #region SelectMusicForGames

        public void SelectMusicForGames(IEnumerable<Game> games)
        {
            RestartMusicAfterSelect(
                () => games.Select(
                    g => _fileManager.SelectMusicForGame(g, _promptFactory.PromptForMp3()).FirstOrDefault()),
                games.Count() is 1 && _settings.MusicType is MusicType.Game);

            var gamesToNewFiles = games.ToDictionary(
                g => g, g => _fileManager.SelectMusicForGame(g, _promptFactory.PromptForMp3()));

            foreach (var gameToFiles in gamesToNewFiles)
            {
                var hasMusicFiles = gameToFiles.Value.HasNonEmptyItems()
                    || _pathingService.GetGameMusicFiles(gameToFiles.Key).HasNonEmptyItems();
                if (hasMusicFiles)
                {
                    _tagger.UpdateMissingTag(gameToFiles.Key, true, _fileManager.CreateMusicDirectory(gameToFiles.Key));
                }
            }
        }

        #endregion

        #region SelectStartSoundForGame

        public void SelectStartSoundForGame(Game game)
        {
            var filePath = _promptFactory.PromptForAudioFile().FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                _fileManager.SelectStartSoundForGame(game, filePath);
            }
        }

        #endregion

        #region SelectMusicForDefault

        public void SelectMusicForDefault()
            => RestartMusicAfterSelect(
                () => _fileManager.SelectMusicForDefault(_promptFactory.PromptForMp3()),
                _settings.MusicType is MusicType.Default);

        #endregion

        #region SelectMusicForFilter

        public void SelectMusicForFilter(FilterPreset filter)
        {
            var playNewMusic = _settings.MusicType is MusicType.Filter
                && _api.MainView.GetActiveFilterPreset() == filter.Id;

            RestartMusicAfterSelect(
                () => _fileManager.SelectMusicForFilter(filter, _promptFactory.PromptForMp3()), playNewMusic);
        }

        #endregion

        #region DeleteMusicDirectories

        public void DeleteMusicDirectories(IEnumerable<Game> games)
            => PerformDeleteAction(
                Resource.DialogDeleteMusicDirectory,
                () => games.ForEach(g => _errorHandler.Try(() => DeleteMusicDirectory(g))));

        private void DeleteMusicDirectory(Game game)
        {
            if (_fileManager.DeleteMusicDirectory(game))
            {
                _tagger.AddMissingTag(game);
            }
        }

        #endregion

        #region DeleteMusicFile

        public void DeleteMusicFile(string musicFile, string musicFileName, Game game)
        {
            var deletePromptMessage = string.Format(Resource.DialogDeleteMusicFile, musicFileName);
            PerformDeleteAction(deletePromptMessage, () => _fileManager.DeleteMusicFile(musicFile, musicFileName, game));

            if (_settings.TagMissingEntries && game != null)
            {
                _tagger.UpdateMissingTag(game, false, _pathingService.GetGameDirectoryPath(game));
            }
        }

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

            _musicPlayer.Close();

            CreateDownloadDialogue(games, source, albumSelect, songSelect, overwriteSelect);

            _promptFactory.ShowMessage(Resource.DialogMessageDone);

            _musicPlayer.Resume(false);
        }

        #endregion

        #region CreateNormalizationDialogue

        public void CreateNormalizationDialogue()
        {
            _musicPlayer.Close();
            var failedGames = new List<string>();

            _promptFactory.CreateGlobalProgress(Resource.DialogMessageNormalizingFiles,(args, title)
                => failedGames = NormalizeSelectedGameMusicFiles(args, _api.SelectedGames().ToList(), title));

            if (failedGames.Any())
            {
                _promptFactory.ShowError($"The following games had at least one file fail to normalize (see logs for details): {string.Join(", ", failedGames)}");
            }
            else
            {
                _promptFactory.ShowMessage(Resource.DialogMessageDone);
            }

            _musicPlayer.Play(_api.SelectedGames());
        }

        private List<string> NormalizeSelectedGameMusicFiles(
            GlobalProgressActionArgs args, IList<Game> games, string progressTitle)
        {
            var failedGames = new List<string>();

            args.ProgressMaxValue = games.Count;
            foreach (var game in games.TakeWhile(_ => !args.CancelToken.IsCancellationRequested))
            {
                args.Text = GenerateTitle(args, game, progressTitle);

                var musicFiles = _pathingService.GetGameMusicFiles(game);
                if (musicFiles.IsEmpty())
                {
                    continue;
                }

                var allMusicNormalized = musicFiles.ForAny(f => _normalizer.NormalizeAudioFile(f));
                if (allMusicNormalized)
                {
                    _tagger.AddNormalizedTag(game);
                }
                else
                {
                    failedGames.Add(game.Name);
                }
            }

            return failedGames;
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

        #region Helpers

        private void RestartMusicAfterSelect(Func<IEnumerable<string>> selectFunc, bool playNewMusic)
        {
            _musicPlayer.Close();

            var newMusic = selectFunc();
            var newMusicFile = newMusic.FirstOrDefault();

            if (playNewMusic && newMusicFile != null)
            {
                _musicPlayer.CurrentMusicFile = newMusicFile;
            }
            else
            {
                _musicPlayer.Play(_api.SelectedGames());
            }
        }

        private void PerformDeleteAction(string message, Action deleteAction)
        {
            if (!_promptFactory.PromptForApproval(message)) return;

            _musicPlayer.Close();

            deleteAction();

            Thread.Sleep(250);

            _musicPlayer.Play(_api.SelectedGames());
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
            foreach (var game in games.TakeWhile(_ => !args.CancelToken.IsCancellationRequested))
            {
                args.Text = GenerateTitle(args, game, progressTitle);

                var gameDirectory = _fileManager.CreateMusicDirectory(game);

                var newFilePath =
                    DownloadSongFromGame(source, game.Name, gameDirectory, songSelect, albumSelect, overwrite);

                var fileDownloaded = newFilePath != null;
                if (_settings.NormalizeMusic && fileDownloaded)
                {
                    args.Text += $" - {Resource.DialogMessageNormalizingFiles}";
                    if (_normalizer.NormalizeAudioFile(newFilePath))
                    {
                        _tagger.AddNormalizedTag(game);
                    }
                }

                _tagger.UpdateMissingTag(game, fileDownloaded, gameDirectory);
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
