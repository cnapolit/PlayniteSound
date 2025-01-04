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
using PlayniteSounds.Services.Audio;
using System.Runtime.InteropServices;
using System.Text;
using TagLib.Id3v2;

namespace PlayniteSounds.Services.Files
{
    public class FileManager : IFileManager
    {
        #region Infrastructure

        private static readonly ILogger                Logger = LogManager.GetLogger();
        private        readonly IErrorHandler          _errorHandler;
        private        readonly IPathingService        _pathingService;
        private        readonly IPromptFactory         _promptFactory;
        private        readonly IMainViewAPI           _mainViewAPI;
        private        readonly ITagger                _tagger;
        private        readonly IMusicPlayer           _musicPlayer;
        private        readonly PlayniteSoundsSettings _settings;

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
        {
            games = games.Where(g => _errorHandler.TryWithPrompt(() => DeleteMusicDirectory(g)));
            if (_settings.TagMissingEntries)
            {
                _tagger.AddTag(games, Resource.MissingTag);
            }
        }

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
            var updatedGames = new List<Game>();
            foreach (var game in games)
            {
                var files = SelectMusicForDirectory(CreateMusicDirectory(game), _promptFactory.PromptForMp3());
                if (files.HasNonEmptyItems() || _pathingService.GetGameMusicFiles(game).HasNonEmptyItems())
                {
                    if (_tagger.RemoveTag(game, Resource.MissingTag))
                    {
                        updatedGames.Add(game);
                    }
                }
                else if (_tagger.AddTag(game, Resource.MissingTag))
                {
                    updatedGames.Add(game);
                }
            }
            _tagger.UpdateGames(updatedGames);
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
            => SelectMusicForDirectory(CreatePlatformDirectory(platform), _promptFactory.PromptForMp3());

        #endregion

        #region SelectMusicForFilter

        public void SelectMusicForFilter(FilterPreset filter) 
            => SelectMusicForDirectory(CreateFilterDirectory(filter), _promptFactory.PromptForMp3());

        #endregion

        #region SelectMusicForDefault

        public void SelectMusicForDefault()
            =>  SelectMusicForDirectory(_pathingService.DefaultMusicPath, _promptFactory.PromptForMp3());

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

        [DllImport("kernel32.dll")]
        private static extern bool CreateSymbolicLinkA(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        enum SymbolicLink
        {
            File = 0,
            Directory = 1,
            UnPrivileged = 2
        }

        public void CreateSymLinks()
        {
            foreach (var gameDir in Directory.GetDirectories(_pathingService.GameMusicFilePath))
            {
                CreateSymLink(gameDir);
            }
        }

        public void CreateSymLink(string dir)
        {
            var musicDir = Path.Combine(dir, SoundDirectory.Music);
            if (Directory.Exists(musicDir))
            {
                var targetFiles = Directory.GetFiles(musicDir);
                if (targetFiles.Any())
                {
                    var targetFile = targetFiles.First();
                    var destination = Path.Combine(dir, "Music.mp3");

                    if (Directory.GetFiles(dir).Contains("Music.mp3"))
                        /* Then */
                        Logger.Info($"Symlink in directory '{dir}' already exists");
                    else if (!CreateSymbolicLinkA(destination, targetFile, SymbolicLink.UnPrivileged))
                    {
                        Logger.Warn($"Failed to create symbolic link from '{targetFile}' to '{destination}'");
                    }
                }
                else /* Then */ Logger.Info($"Directory '{musicDir}' does not contain music files");
            }
            else /* Then */ Logger.Info($"Directory '{musicDir}' does not exist");
        }

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

        private void PerformDeleteAction(string message, Action deleteAction)
        {
            if (!_promptFactory.PromptForApproval(message)) /* Then */ return;
            deleteAction();
        }

        public void ApplyTags(Game game, Song song, string filePath)
        {
            using (var fileTags = TagLib.File.Create(filePath))
            {
                fileTags.Mode = TagLib.File.AccessMode.Write;

                var tag = (TagLib.Id3v2.Tag)fileTags.GetTag(TagLib.TagTypes.Id3v2);
                PrivateFrame.Get(tag, "Source", true).PrivateData = Encoding.Unicode.GetBytes(song.Source.ToString());
                PrivateFrame.Get(tag, "Id", true).PrivateData = Encoding.Unicode.GetBytes(song.Id);

                if (fileTags.Tag.Performers is null)  /* Then */ fileTags.Tag.Performers = song.Artists?.ToArray();
                if (fileTags.Tag.Title is null)       /* Then */ fileTags.Tag.Title = song.ParentAlbum.Name;
                if (fileTags.Tag.Year is 0)           /* Then */ fileTags.Tag.Year = (uint)(game.ReleaseYear ?? 0);
                if (fileTags.Tag.Description is null) /* Then */ fileTags.Tag.Description = song.Description;

                if (song.ParentAlbum is null) /* Then */ return;

                if (fileTags.Tag.Album is null)        /* Then */ fileTags.Tag.Album = song.ParentAlbum.Name;
                if (fileTags.Tag.AlbumArtists is null) /* Then */ fileTags.Tag.AlbumArtists = song.ParentAlbum.Artists?.ToArray();
            }
        }

        #endregion

        #endregion
    }
}
