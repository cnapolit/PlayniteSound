using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace PlayniteSounds.Services.Audio
{
    internal class MusicPlayer : BasePlayer, IMusicPlayer
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
                        _errorHandler.Try(() => AttemptPlay());
                    }
                }
            }
        }

        private readonly IMainViewAPI    _mainView;
        private readonly IErrorHandler   _errorHandler;
        private readonly IPathingService _pathingService;
        private readonly MediaTimeline   _timeLine;
        private readonly ISet<string>    _pausers            = new HashSet<string>();
        private          MediaPlayer     _musicPlayer;

        private          Guid            _currentFilterGuid;
        private          Platform        _currentPlatform;
        private          MusicType       _lastPlayedType;
        private          int             _gamesRunning;
        private          bool            _disposed           = false;

        public MusicPlayer(
            IErrorHandler errorHandler,
            IPathingService pathingService,
            IPlayniteAPI api,
            PlayniteSoundsSettings _settings,
            bool isDesktop) : base(_settings, isDesktop)
        {
            _errorHandler = errorHandler;
            _pathingService = pathingService;
            _mainView = api.MainView;
            _musicPlayer = new MediaPlayer();
            _musicPlayer.MediaEnded += MediaEnded;
            _timeLine = new MediaTimeline();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Close();
                _disposed = true;
                _musicPlayer.MediaEnded -= MediaEnded;
                _musicPlayer = null;
            }
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

        #region ResetVolume

        public void ResetVolume()
        {
            if (_musicPlayer != null)
            {
                _musicPlayer.Volume = _settings.MusicVolume / 100.0;
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
            if (_musicPlayer.Clock != null)
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

        #region Helpers

        private void AttemptPlay()
        {
            Close();
            if (!string.IsNullOrWhiteSpace(_currentMusicFileName))
            {
                _timeLine.Source = new Uri(_currentMusicFileName);
                _musicPlayer.Volume = _settings.MusicVolume / 100.0;
                _musicPlayer.Clock = _timeLine.CreateClock();
                _musicPlayer.Clock.Controller.Begin();
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
            => _pausers.Count is 0 && _gamesRunning is 0 && ShouldPlayAudio(_settings.MusicState);

        private void PlayNextFile(bool musicEnded)
        {
            var files = GetFiles();
            var file = SelectFile(files, musicEnded);
            CurrentMusicFile = file;
        }

        private string[] GetFiles()
        {
            string[] files;
            switch (_settings.MusicType)
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

            if (_settings.PlayBackupMusic && !files.Any())
            {
                return GetBackupFiles();
            }

            // Playing intended type
            _lastPlayedType = _settings.MusicType;
            return files;
        }

        private bool NewMusicSource<T>(T expected, T actual)
            => _lastPlayedType != _settings.MusicType
            || !expected.Equals(actual) 
            || string.IsNullOrWhiteSpace(_currentMusicFileName);

        // Backup order is game -> filter -> default
        private string[] GetBackupFiles()
        {
            if (_settings.MusicType != MusicType.Default)
            {
                var filterFiles = _pathingService.GeFilterMusicFiles(_mainView.GetActiveFilterPreset());
                if (filterFiles.Any())
                {
                    _lastPlayedType = MusicType.Filter;
                    return filterFiles;
                }
            }

            _lastPlayedType = MusicType.Default;
            return _pathingService.GetDefaultMusicFiles();
        }

        private static readonly Random RNG = new Random();
        private string SelectFile(string[] files, bool musicEnded)
        {
            var musicFile = files.FirstOrDefault() ?? string.Empty;

            var shouldRandomize = _settings.RandomizeOnEverySelect || (musicEnded && _settings.RandomizeOnMusicEnd);
            if (files.Length > 1 && shouldRandomize) do
            {
                musicFile = files[RNG.Next(files.Length)];
            }
            while (_currentMusicFileName == musicFile);

            return musicFile;
        }

        private void PauseClock()
        {
            if (_musicPlayer.Clock != null)
            {
                _errorHandler.Try(_musicPlayer.Clock.Controller.Pause);
            }
        }

        private void StartClock()
        {
            if (ShouldPlayMusic() && _musicPlayer.Clock != null)
            {
                _errorHandler.Try(_musicPlayer.Clock.Controller.Resume);
            }
        }

        #endregion

        #endregion
    }
}
