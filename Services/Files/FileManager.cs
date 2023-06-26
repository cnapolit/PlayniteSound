using Playnite.SDK.Models;
using Playnite.SDK;
using PlayniteSounds.Common.Constants;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Play;
using PlayniteSounds.Common.Extensions;
using PlayniteSounds.Services.Audio;

namespace PlayniteSounds.Services.Files
{
    public class FileManager : IFileManager
    {
        #region Infrastructure

        private static readonly ILogger         Logger           = LogManager.GetLogger();
        private        readonly IErrorHandler   _errorHandler;
        private        readonly IPathingService _pathingService;
        private readonly IPromptFactory _promptFactory;
        private readonly IMainViewAPI _mainViewAPI;
        private readonly ITagger _tagger;
        private readonly IMusicPlayer _musicPlayer;
        private readonly PlayniteSoundsSettings _settings;

        public FileManager(
            IMainViewAPI mainViewAPI,
            IErrorHandler errorHandler,
            IPromptFactory promptFactory,
            IPathingService pathingService,
            ITagger tagger,
            IMusicPlayer musicPlayer,
            PlayniteSoundsSettings settings)
        {
            _errorHandler = errorHandler;
            _pathingService = pathingService;
            _mainViewAPI = mainViewAPI;
            _promptFactory = promptFactory;
            _tagger = tagger;
            _musicPlayer = musicPlayer;
            _settings = settings;

            Directory.CreateDirectory(_pathingService.DefaultMusicPath);
            Directory.CreateDirectory(_pathingService.PlatformMusicFilePath);
            Directory.CreateDirectory(_pathingService.FilterMusicFilePath);
            Directory.CreateDirectory(_pathingService.GameMusicFilePath);
        }

        #endregion

        #region Implmentation

        #region CopyAudioFiles

        public void CopyAudioFiles()
        {
            var soundFilesInstallPath = Path.Combine(SoundDirectory.PluginFolder, SoundDirectory.Sound);

            if (Directory.Exists(soundFilesInstallPath) && !Directory.Exists(_pathingService.SoundFilesDataPath))
            {
                _errorHandler.TryWithPrompt(() => AttemptCopyAudioFiles(soundFilesInstallPath));
            }

            var defaultMusicFile = Path.Combine(soundFilesInstallPath, SoundFile.DefaultMusicName);
            if (File.Exists(defaultMusicFile) && !Directory.Exists(_pathingService.DefaultMusicPath))
            {
                Directory.CreateDirectory(_pathingService.DefaultMusicPath);
                File.Move(defaultMusicFile, Path.Combine(_pathingService.DefaultMusicPath, SoundFile.DefaultMusicName));
            }
        }

        private void AttemptCopyAudioFiles(string soundFilesInstallPath)
        {
            Directory.CreateDirectory(_pathingService.SoundFilesDataPath);
            var files = Directory.GetFiles(soundFilesInstallPath).Where(f => f.EndsWith(".wav"));
            files.ForEach(f => File.Move(f, Path.Combine(_pathingService.SoundFilesDataPath, Path.GetFileName(f))));
        }

        #endregion

        #region DeleteMusicDirectories

        public void DeleteMusicDirectories(IEnumerable<Game> games)
            => PerformDeleteAction(Resource.DialogDeleteMusicDirectory, () => DeleteDirectories(games));

        private void DeleteDirectories(IEnumerable<Game> games) 
            => _tagger.UpdateGames(games.Where(g => _errorHandler.TryWithPrompt(() => DeleteMusicDirectory(g))));

        #endregion

        #region DeleteMusicDirectory

        public bool DeleteMusicDirectory(Game game)
        {
            var gameDirectory = string.Empty;
            try
            {
                gameDirectory = _pathingService.GetGameDirectoryPath(game);
                if (Directory.Exists(gameDirectory))
                {
                    Directory.Delete(gameDirectory, true);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to delete directory '{gameDirectory}'");
                return false;
            }
        }

        #endregion

        #region DeleteMusicFile

        public void DeleteMusicFile(string musicFile, string musicFileName, Game game)
        {
            var deletePromptMessage = string.Format(Resource.DialogDeleteMusicFile, musicFileName);
            PerformDeleteAction(deletePromptMessage, () => File.Delete(musicFile));

            if (_settings.TagMissingEntries && game != null)
            {
                var dir = _pathingService.GetGameDirectoryPath(game);
                if (!Directory.Exists(dir) || !Directory.GetFiles(dir).Any())
                {
                    _tagger.AddTag(game, Resource.MissingTag);
                    _tagger.UpdateGames(new List<Game> { game });
                }
            }
        }

        #endregion

        #region SelectMusicForGames

        public void SelectMusicForGames(IEnumerable<Game> games)
        {
            IEnumerable<string> SelectMusicForGame(Game game) 
                => SelectMusicForDirectory(CreateMusicDirectory(game), _promptFactory.PromptForMp3());

            if (games.Count() is 1)
            {
                RestartMusicAfterSelect(() => games.Select(SelectMusicForGame).FirstOrDefault(),
                games.Count() is 1 && _settings.CurrentUIStateSettings.MusicSource is AudioSource.Game);
            }
            else
            {
                var updatedGames = games.Where(g =>
                    (SelectMusicForGame(g).HasNonEmptyItems()
                        || _pathingService.GetGameMusicFiles(g).HasNonEmptyItems())
                    && _tagger.AddTag(g, Resource.MissingTag));
                _tagger.UpdateGames(updatedGames);
            }
        }

        #endregion

        #region  SelectStartSoundForGame

        public string SelectStartSoundForGame(Game game)
        {
            var filePath = _promptFactory.PromptForAudioFile().FirstOrDefault();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            var dir = Path.Combine(CreateMusicDirectory(game), SoundDirectory.StartingSoundFolder);
            Directory.CreateDirectory(dir);
            return CopyFileToDirectory(dir, filePath);
        }

        #endregion

        #region SelectMusicForPlatform

        public void SelectMusicForPlatform(Platform platform)
        {
            var playNewMusic = _settings.CurrentUIStateSettings.MusicSource is AudioSource.Platform
                && _mainViewAPI.SingleGame()
                && _mainViewAPI.SelectedGames.First().Platforms.Contains(platform);
            
            RestartMusicAfterSelect(
                () => SelectMusicForDirectory(CreatePlatformDirectory(platform), _promptFactory.PromptForMp3()),
                playNewMusic);
        }

        #endregion

        #region SelectMusicForFilter

        public void SelectMusicForFilter(FilterPreset filter)
        {
            var playNewMusic = _settings.CurrentUIStateSettings.MusicSource is AudioSource.Filter
                && _mainViewAPI.GetActiveFilterPreset() == filter.Id;
            RestartMusicAfterSelect(
                () => SelectMusicForDirectory(CreateFilterDirectory(filter), _promptFactory.PromptForMp3()), 
                playNewMusic);
        }

        #endregion

        #region SelectMusicForDefault

        public void SelectMusicForDefault()
            => RestartMusicAfterSelect(
                () => SelectMusicForDirectory(_pathingService.DefaultMusicPath, _promptFactory.PromptForMp3()),
                 _settings.CurrentUIStateSettings.MusicSource is AudioSource.Default);

        #endregion

        #region CreatePlatformDirectoryPathFromGame

        public string CreatePlatformDirectoryPathFromGame(Game game)
            => CreatePlatformDirectory(game?.Platforms?.FirstOrDefault());

        #endregion

        #region CreateMusicDirectory

        public string CreateMusicDirectory(Game game)
            => Directory.CreateDirectory(_pathingService.GetGameDirectoryPath(game)).FullName;

        #endregion

        #region CreatePlatformDirectory

        public string CreatePlatformDirectory(Platform platform)
            => Directory.CreateDirectory(_pathingService.GetPlatformDirectoryPath(platform)).FullName;

        #endregion

        #region CreateFilterDirectory

        public string CreateFilterDirectory(FilterPreset filter)
            => Directory.CreateDirectory(_pathingService.GetFilterDirectoryPath(filter)).FullName;


        #endregion

        #region OpenGameDirectories

        public void OpenGameDirectories(IEnumerable<Game> games)
            => _errorHandler.TryWithPrompt(
                () => games.ForEach(g => Process.Start(_pathingService.GetGameDirectoryPath(g))));

        #endregion

        #region Helpers

        private static IEnumerable<string> SelectMusicForDirectory(string directory, IEnumerable<string> files)
        {
            foreach (var musicFile in files)
            {
                yield return CopyFileToDirectory(directory, musicFile);
            }
        }

        private static string CopyFileToDirectory(string directory, string musicFile)
        {
            var newMusicFile = Path.Combine(directory, Path.GetFileName(musicFile));

            File.Copy(musicFile, newMusicFile, true);

            return newMusicFile;
        }

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
            _musicPlayer.Play(_mainViewAPI.SelectedGames);
        }

        #endregion

        #endregion
    }
}
