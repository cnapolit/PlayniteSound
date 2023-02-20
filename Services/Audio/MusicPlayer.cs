using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteSounds.Common.Utilities;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace PlayniteSounds.Services.Audio
{
    public class MusicPlayer : BasePlayer, IMusicPlayer
    {
        #region Infrastructure

        private Game _currentGame;
        public Game CurrentGame
        {
            get => _currentGame;
            set
            {
                if (_currentGame?.Id != value?.Id)
                {
                    _currentGame = value;
                    PlayNextFile(false);
                }
            }
        }

        // Prevents restarting same file
        private string _currentMusicFileName = string.Empty;
        public string CurrentMusicFile
        {
            set
            {
                if (_currentMusicFileName != value)
                {
                    _currentMusicFileName = value;
                    if (ShouldPlayMusic())
                    {
                        _errorHandler.Try(() => AttemptPlay(_currentMusicFileName));
                    }
                }
            }
        }

        private UIState _uiState;
        public UIState UIState
        {
            get => _uiState;
            set
            {
                if (_uiState != value)
                {
                    _uiState = value;

                    PlayNextFile(false);
                }
            }
        }

        private readonly IMainViewAPI _mainView;
        private readonly IErrorHandler _errorHandler;
        private readonly IPathingService _pathingService;
        private readonly IMusicFileSelector _musicFileSelector;
        private readonly ISet<string> _pausers = new HashSet<string>();

        private MediaTimeline _timeLine;
        private MediaPlayer _musicPlayer;

        private          Guid _currentFilterGuid;
        private          Platform _currentPlatform;
        private          MusicType _lastPlayedType;
        private          uint _gamesRunning;

        public MusicPlayer(
            IErrorHandler errorHandler,
            IPathingService pathingService,
            IPlayniteAPI api,
            IMusicFileSelector musicFileSelector,
            PlayniteSoundsSettings _settings) : base(_settings)
        {
            _errorHandler = errorHandler;
            _pathingService = pathingService;
            _mainView = api.MainView;
            _musicFileSelector = musicFileSelector;
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                Application.Current.Dispatcher.Invoke(DisposePlayer);
                _disposed = true;
            }
        }

        private void DisposePlayer()
        {
            Close();
            _musicPlayer.MediaEnded -= MediaEnded;
            _musicPlayer = null;
        }

        #endregion

        #region Implementation

        #region Resume(bool gameStopped)

        public void Resume(bool gameStopped)
        {
            if (gameStopped)
            {
                _gamesRunning--;
            }
            StartClock();
        }

        #endregion

        #region Resume(string pauser)

        public void Resume(string pauser)
        {
            _pausers.Remove(pauser);
            StartClock();
        }

        #endregion

        #region SetVolume

        public void SetVolume(double volume)
        {
            if (_musicPlayer != null)
            {
                _musicPlayer.Volume = volume;
            }
        }

        #endregion

        #region Pause(bool gameStarted)

        public void Pause(bool gameStarted)
        {
            if (gameStarted)
            {
                _gamesRunning++;
            }
            PauseClock();
        }

        #endregion

        #region Pause(string pauser)

        public void Pause(string pauser)
        {
            _pausers.Add(pauser);
            PauseClock();
        }

        #endregion

        #region Close

        public void Close()
        {
            if (_musicPlayer?.Clock != null)
            {
                _errorHandler.Try(AttemptClose);
            }
        }

        private void AttemptClose()
        {
            _musicPlayer.Clock.Controller.Stop();
            _musicPlayer.Clock = null;
            _musicPlayer.Close();
        }

        #endregion

        #region Play

        public void Play(IEnumerable<Game> games)
        {
            if (ShouldPlayMusicOrClose()) switch (games?.Count())
            {
                case 1:
                    CurrentGame = games.First();
                    break;
                case 0:
                case null:
                    CurrentGame = null;
                    break;
            }
        }

        private bool ShouldPlayMusicOrClose()
        {
            var shouldPlayMusic = ShouldPlayMusic();
            if (!shouldPlayMusic)
            {
                Close();
            }

            return shouldPlayMusic;
        }

        #endregion

        #region Preview

        public void Preview()
        {
            var soundFile = _pathingService.GetSoundFiles().FirstOrDefault();

            if (soundFile != null)
            {
                _errorHandler.Try(() => AttemptPlay(soundFile));
            }
        }

        #endregion

        #region Helpers

        private void AttemptPlay(string musicFile)
        {
            Close();

            if (!string.IsNullOrWhiteSpace(musicFile))
            {
                LoadPlayer();

                _timeLine.Source = new Uri(musicFile);
                _musicPlayer.Volume = _settings.ActiveModeSettings.MusicVolume;
                _musicPlayer.Clock = _timeLine.CreateClock();
                _musicPlayer.Clock.Controller.Begin();
            }
        }

        private void LoadPlayer()
        {
            if (_musicPlayer is null)
            {
                // Defer loading until first use
                _musicPlayer = new MediaPlayer();
                _musicPlayer.MediaEnded += MediaEnded;
                _timeLine = new MediaTimeline();
            }
        }

        private void MediaEnded(object _, EventArgs __)
        {
            if (_settings.RandomizeOnMusicEnd)
            {
                PlayNextFile(true);
            }
            else if (_musicPlayer.Clock != null)
            {
                _musicPlayer.Clock.Controller.Stop();
                _musicPlayer.Clock.Controller.Begin();
            }
        }

        private bool ShouldPlayMusic() 
            => _pausers.Count is 0 
            && _gamesRunning is 0 
            && _settings.ActiveModeSettings.MusicEnabled 
            && !_settings.ActiveModeSettings.IsThemeControlled;

        private void PlayNextFile(bool musicEnded)
        {
            var files = GetFiles();
            var file = _musicFileSelector.SelectFile(files, _currentMusicFileName, musicEnded);
            CurrentMusicFile = file;
        }

        private string[] GetFiles()
        {
            string[] files;

            var musicType = EnumUtilities.SoundsSettingsToMusicType(_settings);

            switch (musicType)
            {
                case MusicType.Game:
                    files = _pathingService.GetGameMusicFiles(_currentGame);
                    break;
                case MusicType.Platform:
                    var newPlatform = _currentGame?.Platforms?.FirstOrDefault();
                    if (NewMusicSource(_currentPlatform?.Name, newPlatform?.Name))
                    {
                        _currentPlatform = newPlatform;
                        files = _pathingService.GetPlatformMusicFiles(_currentPlatform);
                    }
                    else
                    {
                        files = new string[] { _currentMusicFileName };
                    }
                    break;
                case MusicType.Filter:
                    var activeFilter = _mainView.GetActiveFilterPreset();
                    if (NewMusicSource(_currentFilterGuid, activeFilter))
                    {
                        _currentFilterGuid = activeFilter;
                        files = _pathingService.GeFilterMusicFiles(_mainView.GetActiveFilterPreset());
                    }
                    else
                    {
                        files = new string[] { _currentMusicFileName };
                    }
                    break;
                default:
                    files = _lastPlayedType != MusicType.Default || string.IsNullOrWhiteSpace(_currentMusicFileName)
                        ? _pathingService.GetDefaultMusicFiles()
                        : new string[] { _currentMusicFileName };
                    break;
            }

            var typePlayed = musicType;
            if (_settings.BackupMusicEnabled && !files.Any())
            {
                (files, typePlayed) = _musicFileSelector.GetBackupFiles();
            }

            _lastPlayedType = typePlayed;
            return files;
        }

        private bool NewMusicSource<T>(T expected, T actual)
            => _lastPlayedType != _settings.ActiveModeSettings.MainMusicType
            || !expected.Equals(actual)
            || string.IsNullOrWhiteSpace(_currentMusicFileName);

        private void PauseClock()
        {
            if (_musicPlayer?.Clock != null)
            {
                _errorHandler.Try(_musicPlayer.Clock.Controller.Pause);
            }
        }

        private void StartClock()
        {
            LoadPlayer();

            if (ShouldPlayMusic() && _musicPlayer.Clock != null)
            {
                _errorHandler.Try(_musicPlayer.Clock.Controller.Resume);
            }
        }

        #endregion

        #endregion
    }
}
