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

        private string _currentMusicFileName = string.Empty;  //used to prevent same file being restarted
        public string CurrentMusicFile
        {
            set
            {
                _currentMusicFileName = value;
                if (ShouldPlayMusic())
                {
                    _errorHandler.Try(() => AttemptPlay());
                }
            }
        }

        private readonly IMainViewAPI    _mainView;
        private readonly IErrorHandler   _errorHandler;
        private readonly IPathingService _pathingService;
        private readonly MediaTimeline   _timeLine;
        private readonly ISet<string>    _pausers         = new HashSet<string>();
        private          MediaPlayer     _musicPlayer;
        private          int             _gamesRunning;
        private          bool            _disposed        = false;

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
                _disposed = true;
                _musicPlayer.MediaEnded -= MediaEnded;
                _musicPlayer = null;
            }
        }

        #endregion

        #region Implementation

        #region Resume

        public void Resume(bool gameStopped)
        {
            if (gameStopped)
            {
                _gamesRunning--;
            }

            if (ShouldPlayMusic() && _musicPlayer.Clock != null)
            {
                _errorHandler.Try(_musicPlayer.Clock.Controller.Resume);
            }
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

        #region Resume

        public void Resume(string pauser)
        {
            _pausers.Remove(pauser);

            if (ShouldPlayMusic() && _musicPlayer.Clock != null)
            {
                _errorHandler.Try(_musicPlayer.Clock.Controller.Resume);
            }
        }

        #endregion

        #region Pause

        public void Pause(bool gameStarted)
        {
            if (gameStarted)
            {
                _gamesRunning++;
            }

            if (_musicPlayer.Clock != null)
            {
                _errorHandler.Try(_musicPlayer.Clock.Controller.Pause);
            }
        }

        #endregion

        #region Pause

        public void Pause(string pauser)
        {
            _pausers.Add(pauser);

            if (_musicPlayer.Clock != null)
            {
                _errorHandler.Try(_musicPlayer.Clock.Controller.Pause);
            }
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

        #region AttemptPlay

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

            if (file != _currentMusicFileName)
            {
                _currentMusicFileName = file;
                if (ShouldPlayMusic())
                {
                    _errorHandler.Try(() => AttemptPlay());
                }
            }
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
                    files = _pathingService.GetPlatformMusicFiles(_currentGame.Platforms.FirstOrDefault());
                    break;
                case MusicType.Filter:
                    files = _pathingService.GeFilterMusicFiles(_mainView.GetActiveFilterPreset());
                    break;
                default:
                    files = _pathingService.GetDefaultMusicFiles();
                    break;
            }

            return _settings.PlayBackupMusic && !files.Any() ? GetBackupFiles() : files;
        }

        // Backup order is game -> filter -> default
        private string[] GetBackupFiles()
        {
            if (_settings.MusicType != MusicType.Default)
            {
                var filterFiles = _pathingService.GeFilterMusicFiles(_mainView.GetActiveFilterPreset());
                if (filterFiles.Any())
                {
                    return filterFiles;
                }
            }

            return _pathingService.GetDefaultMusicFiles();
        }

        private static readonly Random RNG = new Random();
        private string SelectFile(string[] files, bool musicEnded)
        {
            var musicFile = files.FirstOrDefault() ?? string.Empty;

            var shouldRandomize = _settings.RandomizeOnEverySelect || musicEnded && _settings.RandomizeOnMusicEnd;
            if (files.Length > 1 && shouldRandomize) do
            {
                musicFile = files[RNG.Next(files.Length)];
            }
            while (_currentMusicFileName == musicFile);

            return musicFile;
        }

        #endregion

        #endregion
    }
}
