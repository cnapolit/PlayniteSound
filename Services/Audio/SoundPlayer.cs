using PlayniteSounds.Common.Constants;
using PlayniteSounds.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Media;
using PlayniteSounds.Services.Files;
using Playnite.SDK.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PlayniteSounds.Services.Audio
{
    public class SoundPlayer : BasePlayer, ISoundPlayer
    {
        #region Infrastructure

        private readonly IErrorHandler      _errorHandler;
        private readonly IPathingService    _pathingService;
        private readonly IList<PlayerEntry> _players          = new List<PlayerEntry>(new PlayerEntry[9]);
        private          bool               _firstSelectSound = true;

        public SoundPlayer(
            IErrorHandler errorHandler,
            IPathingService pathingService,
            PlayniteSoundsSettings settings) : base(settings)
        {
            _errorHandler = errorHandler;
            _pathingService = pathingService;
        }

        public override void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            
            // Give AppStopped and any other sounds 5 seconds to finish
            for (var i = 0; _players.Any(e => e?.IsPlaying ?? false) && i < 50; i++)
            {
                Thread.Sleep(100);
            }

            Application.Current.Dispatcher.Invoke(Close);
        }

        #endregion

        #region Implementation

        #region Close

        public void Close()
        {
            for (var i = 0; i < _players.Count; i++)
            {
                var player = _players[i];

                if (player is null) continue;

                _errorHandler.Try(CloseAudioFile, player);

                _players[i] = null;
            }
        }

        private void CloseAudioFile(PlayerEntry entry)
        {
            entry.MediaPlayer.Stop();
            entry.MediaPlayer.Close();
            entry.MediaPlayer.MediaEnded -= MediaEnded;
            entry.MediaPlayer = null;

            var filename = entry.MediaPlayer?.Source.LocalPath;
            if (File.Exists(filename))
            {
                var fileInfo = new FileInfo(filename);
                for (var count = 0; IsFileLocked(fileInfo) && count < 100; count++)
                {
                    Thread.Sleep(5);
                }
            }
        }

        private static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (var stream = file.Open(FileMode.Open, FileAccess.Write, FileShare.None))
                    stream.Close();

                //file is not locked
                return false;
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
        }

        #endregion

        #region PlayGameSelected

        public void PlayGameSelected()
        {
            if (!_firstSelectSound || !_settings.SkipFirstSelectSound)
            {
                PlaySound(_modeSettings.PlayGameSelected, SoundType.GameSelected);
            }
            _firstSelectSound = false;
        }

        #endregion

        #region PlayAppStarted

        public void PlayAppStarted(EventHandler mediaEndedHandler) 
            => HandlePlayAction(_modeSettings.PlayAppStart, () => AttemptPlayAppStarted(mediaEndedHandler));

        private void AttemptPlayAppStarted(EventHandler mediaEndedHandler)
        {
            var playerEntry = GetOrCreatePlayerEntry(SoundType.AppStarted);
            if (playerEntry is null)
            {
                // Interpret missing file as 'instantly' ended
                mediaEndedHandler.Invoke(null, null);
            }
            else
            {
                playerEntry.MediaPlayer.MediaEnded += mediaEndedHandler;
                StartMedia(playerEntry, SoundType.AppStarted);
            }
        }

        #endregion

        #region PlayGameStarting

        public void PlayGameStarting(Game startingGame)
            => HandlePlayAction(_modeSettings.PlayGameStarting, () => AttemptPlayGameStarting(startingGame));

        private void AttemptPlayGameStarting(Game startingGame)
        {
            string gameStartingSoundFile = null;
            if (_settings.PerGameStartSound)
            {
                gameStartingSoundFile = _pathingService.GetGameStartSoundFile(startingGame);
            }
            
            if (gameStartingSoundFile is null)
            {
                gameStartingSoundFile = Path.Combine(
                    _pathingService.ExtraMetaDataFolder, 
                    SoundDirectory.Sound,
                    SoundTypeToFileName(SoundType.GameStarting));
            }

            var entry = GetOrCreatePlayerEntry(SoundType.GameStarting, gameStartingSoundFile);
            if (entry is null)
            {
                return;
            }

            if (entry.FilePath != gameStartingSoundFile)
            {
                entry.MediaPlayer.Open(new Uri(gameStartingSoundFile));
            }

            StartMedia(entry, SoundType.GameStarting);
        }

        #endregion

        #region PlayGameStarted

        public void PlayGameStarted() => HandlePlayAction(_modeSettings.PlayGameStarted, AttemptPlayGameStarted);
        private void AttemptPlayGameStarted()
        {
            var entry = GetOrCreatePlayerEntry(SoundType.GameStarted);
            if (entry is null)
            {
                return;
            }

            var startingEntry = _players[(int)SoundType.GameStarting];
            if (startingEntry != null) for (var i = 0; startingEntry.IsPlaying || i < 200; i++)
            {
                Task.Delay(100);
            }

            StartMedia(entry, SoundType.GameStarted);
        }

        #endregion

        public void PlayAppStopped() => PlaySound(_modeSettings.PlayAppStart, SoundType.AppStopped);

        public void PlayGameInstalled() => PlaySound(_modeSettings.PlayGameInstalled, SoundType.GameInstalled);

        public void PlayGameUnInstalled() => PlaySound(_modeSettings.PlayGameUninstalled, SoundType.GameUninstalled);

        public void PlayGameStopped() => PlaySound(_modeSettings.PlayGameStopped, SoundType.GameStopped);

        public void PlayLibraryUpdated() => PlaySound(_modeSettings.PlayLibraryUpdate, SoundType.LibraryUpdated);

        public void Preview(SoundType soundType, bool playDesktop)
        {
            if (_settings.IsDesktop == playDesktop)
            {
                _errorHandler.Try(AttemptPlay, soundType);
                return;
            }

            var prefix = playDesktop ? SoundFile.DesktopPrefix : SoundFile.FullScreenPrefix;
            var baseFileName = SoundTypeToBaseFileName(soundType);
            var filePath = Path.Combine(_pathingService.ExtraMetaDataFolder, SoundDirectory.Sound, prefix + baseFileName);

            var mediaPlayer = new MediaPlayer();
            mediaPlayer.Open(new Uri(filePath));
            StartMedia(mediaPlayer, soundType);

        }
        
        private static string SoundTypeToBaseFileName(SoundType soundType)
        {
            switch (soundType)
            {
                case SoundType.AppStarted:      return SoundFile. BaseApplicationStartedSound;
                case SoundType.AppStopped:      return SoundFile. BaseApplicationStoppedSound;
                case SoundType.GameStarting:    return SoundFile.       BaseGameStartingSound;
                case SoundType.GameStarted:     return SoundFile.        BaseGameStartedSound;
                case SoundType.GameStopped:     return SoundFile.        BaseGameStoppedSound;
                case SoundType.GameSelected:    return SoundFile.       BaseGameSelectedSound;
                case SoundType.GameInstalled:   return SoundFile.      BaseGameInstalledSound;
                case SoundType.GameUninstalled: return SoundFile.    BaseGameUninstalledSound;
                default:                        return SoundFile.     BaseLibraryUpdatedSound;
            }
        }

        #region Helpers

        private void HandlePlayAction(bool playEnabled, Action action)
        {
            if (playEnabled)
            {
                _errorHandler.Try(action);
            }
        }

        private void PlaySound(bool playEnabled, SoundType soundType)
            => HandlePlayAction(playEnabled, () => AttemptPlay(soundType));
        private void AttemptPlay(SoundType soundType)
        {
            var entry = GetOrCreatePlayerEntry(soundType);
            if (entry is null)
            {
                return;
            }
            
            StartMedia(entry, soundType);
        }

        private void StartMedia(PlayerEntry entry, SoundType soundType)
        {
            entry.IsPlaying = true;
            StartMedia(entry.MediaPlayer, soundType);
        }

        private void StartMedia(MediaPlayer player, SoundType soundType)
        {
            player.Stop();
            player.Volume = SoundTypeToVolume(soundType);
            player.Play();
        }

        private void MediaEnded(object sender, EventArgs args)
        {
            var entry = _players.FirstOrDefault(p => p?.MediaPlayer == sender);
            entry.IsPlaying = false;
        }

        private PlayerEntry GetOrCreatePlayerEntry(SoundType soundType, string filePath = null)
        {
            var entry = _players[(int)soundType];
            if (entry != null)
            {
                return entry;
            }

            if (filePath is null)
            {
                var fileName = SoundTypeToFileName(soundType);
                filePath = Path.Combine(_pathingService.ExtraMetaDataFolder, SoundDirectory.Sound, fileName);
            }

            if (!File.Exists(filePath))
            {
                return null;
            }

            entry = new PlayerEntry
            {
                MediaPlayer = new MediaPlayer()
            };

            entry.MediaPlayer.MediaEnded += MediaEnded;
            entry.MediaPlayer.Open(new Uri(filePath));

            return _players[(int)soundType] = entry;
        }
        
        private string SoundTypeToFileName(SoundType soundType)
        {
            switch (soundType)
            {
                case SoundType.AppStarted:      return SoundFile. ApplicationStartedSound;
                case SoundType.AppStopped:      return SoundFile. ApplicationStoppedSound;
                case SoundType.GameStarting:    return SoundFile.       GameStartingSound;
                case SoundType.GameStarted:     return SoundFile.        GameStartedSound;
                case SoundType.GameStopped:     return SoundFile.        GameStoppedSound;
                case SoundType.GameSelected:    return SoundFile.       GameSelectedSound;
                case SoundType.GameInstalled:   return SoundFile.      GameInstalledSound;
                case SoundType.GameUninstalled: return SoundFile.    GameUninstalledSound;
                default:                        return SoundFile.     LibraryUpdatedSound;
            }
        }


        private double SoundTypeToVolume(SoundType soundType)
        {
            switch (soundType)
            {
                case SoundType.AppStarted:      return _modeSettings.        AppStartVolume;
                case SoundType.AppStopped:      return _modeSettings.         AppStopVolume;
                case SoundType.GameStarting:    return _modeSettings.    GameStartingVolume;
                case SoundType.GameStarted:     return _modeSettings.     GameStartedVolume;
                case SoundType.GameStopped:     return _modeSettings.     GameStoppedVolume;
                case SoundType.GameSelected:    return _modeSettings.    GameSelectedVolume;
                case SoundType.GameInstalled:   return _modeSettings.   GameInstalledVolume;
                case SoundType.GameUninstalled: return _modeSettings. GameUninstalledVolume;
                default:                        return _modeSettings.   LibraryUpdateVolume;
            }
        }

        #endregion

        #endregion
    }
}
