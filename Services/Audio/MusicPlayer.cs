using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave.SampleProviders;
using PlayniteSounds.Models.Audio.SampleProviders;
using PlayniteSounds.Services.State;
using PlayniteSounds.Models.State;
using PlayniteSounds.Models.UI;
using PlayniteSounds.Common.Extensions;

namespace PlayniteSounds.Services.Audio
{
    public class MusicPlayer : BasePlayer, IMusicPlayer
    {
        #region Infrastructure

        private readonly IMainViewAPI               _mainView;
        private readonly IErrorHandler              _errorHandler;
        private readonly IPathingService            _pathingService;
        private readonly IMusicFileSelector         _musicFileSelector;
        private readonly ISet<string>               _pausers = new HashSet<string>();
        private readonly object                     _gameLock = new object();
        private          Game                       _currentGame;
        private          Game                       _incomingGame;
        private          ControllableSampleProvider _currentSampleProvider;
        private          UIStateSettings            _uiStateSettings;
        private          Guid                       _currentFilterGuid;
        private          Guid                       _incomingFilterGuid;
        private          AudioSource                _lastPlayedType;
        private          string                     _currentMusicFileName;
        private          uint                       _gamesRunning;
        private          bool                       _startSoundFinished;
        private          bool                       _playing = true;

        public MusicPlayer(
            IErrorHandler errorHandler,
            IPathingService pathingService,
            IMainViewAPI mainView,
            IPlayniteEventHandler playniteEventHandler,
            IMusicFileSelector musicFileSelector,
            MixingSampleProvider mixer,
            PlayniteSoundsSettings settings) : base(mixer, settings)
        {
            _errorHandler = errorHandler;
            _pathingService = pathingService;
            _mainView = mainView;
            _musicFileSelector = musicFileSelector;
            _uiStateSettings = settings.CurrentUIStateSettings;
            playniteEventHandler.UIStateChanged += UIStateChanged;
            playniteEventHandler.PlayniteEventOccurred += PlayniteEventOccurred;
            mixer.MixerInputEnded += MusicEnded;
        }

        #endregion

        #region Implementation

        public void Resume(bool gameStopped)
        {
            if (gameStopped)
            {
                _gamesRunning--;
            }
            Start();
        }

        public void Resume(string pauser)
        {
            _pausers.Remove(pauser);
            Start();
        }

        public void SetVolume(float? volume = null)
        {
            if (_currentSampleProvider != null)
            { 
                _currentSampleProvider.Volume = volume ?? _uiStateSettings.MusicVolume;
            }
        }

        public void Pause(bool gameStarted)
        {
            if (gameStarted)
            {
                _gamesRunning++;
            }
            Pause();
        }

        public void Pause(string pauser)
        {
            _pausers.Add(pauser);
            Pause();
        }

        public void Preview()
        {
            var soundFile = _pathingService.GetSoundFiles().FirstOrDefault();
            if (soundFile != null)
            {
                _errorHandler.Try(() =>Play(soundFile));
            }
        }

        public void Initialize()
        {
            _startSoundFinished = true;
            lock (_gameLock) /* Then */ if (_incomingGame != null || _currentMusicFileName != null)
            {
                PlayNextFile();
            }
        }

        public void SetMusicFile(string filePath)
        {
            lock (_gameLock)
            {
                var oldProvider = _currentSampleProvider;
                _errorHandler.Try(() => Play(filePath));
                oldProvider?.Stop();
            }
        }

        #region Event Handlers

        public void UIStateChanged(object sender, UIStateChangedArgs args)
        {
            _uiStateSettings = args.NewSettings;

            lock (_gameLock) /* Then */ if (_uiStateSettings.MusicSource != _lastPlayedType)
            {
                PlayNextFile();
            }
            else if (_currentSampleProvider != null)
            {
                _currentSampleProvider.Volume = _uiStateSettings.MusicVolume;
                if (_uiStateSettings.MusicMuffled)
                {
                    _currentSampleProvider.Muffle();
                }
                else
                {
                    _currentSampleProvider.UnMuffle();
                }
            }
        }

        private void PlayniteEventOccurred(object sender, PlayniteEventOccurredArgs args)
        {
            lock (_gameLock) /* Then */ switch (args.Event)
            {
                case PlayniteEvent.AppStarted:
                    if (_startSoundFinished)
                    {
                       _incomingGame = args.Games.FirstOrDefault();
                       PlayNextFile();
                    }
                    else
                    {
                        _incomingGame = args.Games.FirstOrDefault();
                        _currentMusicFileName = GetMusicFilesForStateChange().FirstOrDefault();
                    }
                    break;
                case PlayniteEvent.AppStopped:
                        _playing = false;
                        _currentSampleProvider?.Stop();
                    break;
                case PlayniteEvent.GameStarting:
                    if (_settings.StopMusicOnGameStarting)
                    {
                        Pause(true);
                    }
                    break;
                case PlayniteEvent.GameStarted:
                    if (!_settings.StopMusicOnGameStarting)
                    {
                        Pause(true);
                    }
                    break;
                case PlayniteEvent.GameStopped:
                    Resume(true);
                    break;
                case PlayniteEvent.GameSelected:
                    _incomingGame = args.Games.FirstOrDefault();
                    if (_currentGame == _incomingGame)
                    {
                        _currentSampleProvider.Resume();
                    }
                    else
                    {
                        _incomingFilterGuid = _mainView.GetActiveFilterPreset();
                        PlayNextFile();
                    }
                    break;
            }
        }

        // Assumes Game/Platform/Filter states haven't changed 
        private string[] GetMusicFilesForStateChange()
        {
            string[] files;
            _lastPlayedType = _uiStateSettings.MusicSource;
            switch (_uiStateSettings.MusicSource)
            {
                case AudioSource.Game:
                    files = _pathingService.GetGameMusicFiles(_incomingGame);
                    break;
                case AudioSource.Platform:
                    files = _pathingService.GetPlatformMusicFiles(_incomingGame?.Platforms?.FirstOrDefault());
                    break;
                case AudioSource.Filter:
                    files = _pathingService.GeFilterMusicFiles(_mainView.GetActiveFilterPreset());
                    break;
                default: return _pathingService.GetDefaultMusicFiles();
            }

            if (files.IsEmpty() && _settings.BackupMusicEnabled)
            {
                (files, _lastPlayedType) = _musicFileSelector.GetBackupFiles();
            }

            return files;

        }

        private void MusicEnded(object _, SampleProviderEventArgs args)
        {
            // Mixer is shared with the SoundPlayer, so we must check if the finished sample is from the MusicPlayer
            if (_playing && args.SampleProvider == _currentSampleProvider)
            /* Then */ if (!_settings.RandomizeOnMusicEnd && !_currentSampleProvider.Stopped)
            {
                _mixer.AddMixerInput(_currentSampleProvider);
            }
            else
            {
                Play(_currentMusicFileName);
            }
        }

        #endregion

        #region Helpers

        private bool Play(string musicFile)
        {
            if (string.IsNullOrWhiteSpace(musicFile))
            {
                return false;
            }

            var baseProvider = ConvertProvider(new AutoDisposeFileReader(musicFile, 1));
            if (baseProvider is null)
            {
                return false;
            }

            _currentGame = _incomingGame;
            _currentFilterGuid = _incomingFilterGuid;
            _currentMusicFileName = musicFile;

            _currentSampleProvider = new ControllableSampleProvider(
                baseProvider, _uiStateSettings.MusicVolume, _uiStateSettings.MusicMuffled);
            _mixer.AddMixerInput(_currentSampleProvider);
            return true;
        }

        private bool ShouldPlayMusic()
            => _startSoundFinished
            && _pausers.Count is 0 
            && _gamesRunning is 0
            && _settings.ActiveModeSettings.MusicEnabled;

        private void PlayNextFile()
        {
            var files = GetFiles(_uiStateSettings.MusicSource);

            var lastPlayedType = _uiStateSettings.MusicSource;
            if (_settings.BackupMusicEnabled && files.IsEmpty())
            {
                (files, lastPlayedType) = _musicFileSelector.GetBackupFiles();
            }

            var noSampleProvider = _currentSampleProvider is null;
            var musicEnded = !noSampleProvider && _currentSampleProvider.Stopped;
            var noMusicIsPlaying = noSampleProvider || _currentSampleProvider.Stopped;

            var file = _musicFileSelector.SelectFile(files, _currentMusicFileName, musicEnded);
            if (ShouldPlayMusic()) /* Then */ if (noMusicIsPlaying)
            {
                if (Play(file)) /* Then */ _lastPlayedType = lastPlayedType;
            }
            else if (_currentMusicFileName != file && ShouldPlayMusic())
            {
                // stopping the current sample should trigger the relevant mixer event
                _lastPlayedType = lastPlayedType;
                _currentMusicFileName = file;
                _currentSampleProvider.Stop();
            }
        }

        private string[] GetFiles(AudioSource musicSource)
        {
            switch (musicSource)
            {
                case AudioSource.Game:
                    if (NewMusicSource(_currentGame, _incomingGame, musicSource))
                    {
                        return _pathingService.GetGameMusicFiles(_incomingGame);
                    }
                    break;
                case AudioSource.Platform:
                    var currentPlatform = _currentGame.Platforms?.FirstOrDefault()?.Name;
                    var newPlatform = _incomingGame.Platforms?.FirstOrDefault()?.Name;
                    if (NewMusicSource(currentPlatform, newPlatform, musicSource))
                    {
                        return _pathingService.GetPlatformMusicFiles(currentPlatform);
                    }
                    break;
                case AudioSource.Filter:
                    var activeFilterGuid = _mainView.GetActiveFilterPreset();
                    if (NewMusicSource(_currentFilterGuid, activeFilterGuid, musicSource))
                    {
                        return _pathingService.GeFilterMusicFiles(_mainView.GetActiveFilterPreset());
                    }
                    break;
                default:
                    if (_lastPlayedType != AudioSource.Default || string.IsNullOrWhiteSpace(_currentMusicFileName))
                    {
                        return _pathingService.GetDefaultMusicFiles();
                    }
                    break;
            }

            return new string[] { _currentMusicFileName };
        }

        private bool NewMusicSource<T>(T expected, T actual, AudioSource desiredMusicType)
            => _lastPlayedType != desiredMusicType
            || !Equals(expected, actual)
            || string.IsNullOrWhiteSpace(_currentMusicFileName);

        private void Pause()
        {
            if (_currentSampleProvider is null) return;
            lock(_gameLock)
            {
                _currentSampleProvider.Pause();
            }
        }

        private void Start()
        {
            if (ShouldPlayMusic() && _currentSampleProvider != null)  lock (_gameLock)
            {
                _currentSampleProvider.Resume();
            }
        }

        #endregion

        #endregion
    }
}
