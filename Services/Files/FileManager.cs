using Playnite.SDK.Models;
using Playnite.SDK;
using PlayniteSounds.Common.Constants;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using File = System.IO.File;

namespace PlayniteSounds.Services.Files
{
    public class FileManager : IFileManager
    {
        #region Infrastructure

        private static readonly ILogger         Logger           = LogManager.GetLogger();
        private        readonly IErrorHandler   _errorHandler;
        private        readonly IPathingService _pathingService;

        public FileManager(
            IErrorHandler errorHandler,
            IPathingService pathingService)
        {
            _errorHandler = errorHandler;
            _pathingService = pathingService;

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
                _errorHandler.Try(() => AttemptCopyAudioFiles(soundFilesInstallPath));
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

        public IEnumerable<Game> DeleteMusicDirectories(IEnumerable<Game> games)
            => games.Where(g => DeleteMusicDirectory(g));

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
            => File.Delete(musicFile);

        #endregion

        #region SelectMusicForGame

        public IEnumerable<string> SelectMusicForGame(Game game, IEnumerable<string> files)
            => SelectMusicForDirectory(CreateMusicDirectory(game), files);

        #endregion

        #region  SelectStartSoundForGame

        public string SelectStartSoundForGame(Game game, string file)
        {
            var dir = Path.Combine(CreateMusicDirectory(game), SoundDirectory.StartingSoundFolder);
            Directory.CreateDirectory(dir);
            return CopyFileToDirectory(dir, file);
        }

        #endregion

        #region SelectMusicForPlatform

        public IEnumerable<string> SelectMusicForPlatform(Platform platform, IEnumerable<string> files)
            => SelectMusicForDirectory(CreatePlatformDirectory(platform), files);

        #endregion

        #region SelectMusicForFilter

        public IEnumerable<string> SelectMusicForFilter(FilterPreset filter, IEnumerable<string> files)
            => SelectMusicForDirectory(CreateFilterDirectory(filter), files);

        #endregion

        #region SelectMusicForDefault

        public IEnumerable<string> SelectMusicForDefault(IEnumerable<string> files)
            => SelectMusicForDirectory(_pathingService.DefaultMusicPath, files);

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
            => _errorHandler.Try(() => games.ForEach(g => Process.Start(_pathingService.GetGameDirectoryPath(g))));

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

        #endregion

        #endregion
    }
}
