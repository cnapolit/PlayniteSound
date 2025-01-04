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
using NAudio.Wave;
using System.IO;

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
        private          UIState                    _currentUIState;
        private          AudioSource                _lastPlayedAudioSource;
        private          string                     _currentMusicFileName;
        private          string                     _mainFileName;
        private          long                       _mainPosition;
        private          uint                       _gamesRunning;
        private          bool                       _startSoundFinished;
        private          bool                       _playing;
        private          bool                       _appIsNotStopping = true;

        public MusicPlayer(
            IErrorHandler errorHandler,
            IPathingService pathingService,
            IMainViewAPI mainView,
            IPlayniteEventHandler playniteEventHandler,
            IMusicFileSelector musicFileSelector,
            IWavePlayerManager wavePlayerManager,
            PlayniteSoundsSettings settings) : base(wavePlayerManager, settings)
        {
            _errorHandler = errorHandler;
            _pathingService = pathingService;
            _mainView = mainView;
            _musicFileSelector = musicFileSelector;
            _uiStateSettings = settings.CurrentUIStateSettings;
            playniteEventHandler.UIStateChanged += UIStateChanged;
            playniteEventHandler.PlayniteEventOccurred += PlayniteEventOccurred;
            wavePlayerManager.Mixer.MixerInputEnded += MusicEnded;
        }

        #endregion

        #region Implementation

        #region Public

        public long Position 
        {
            get => _currentSampleProvider?.Position ?? 0;
            set => _currentSampleProvider.Position = value;
        }

        public long PositionInSeconds => _currentSampleProvider is null
            ? 0 : _currentSampleProvider.Position / _currentSampleProvider.WaveFormat.AverageBytesPerSecond;

        public long Length => _currentSampleProvider?.Length ?? 0;

        public long LengthInSeconds => _currentSampleProvider is null
            ? 0 : _currentSampleProvider.Length / _currentSampleProvider.WaveFormat.AverageBytesPerSecond;

        public void Toggle()
        {
            if (_playing) /* Then */ Pause(false);
            else if (_currentSampleProvider != null) /* Then */ lock (_gameLock)
            {
                _currentSampleProvider.Resume();
                _playing = true;
            } 
        }

        public void Resume(bool gameStopped)
        {
            if (gameStopped)
            {
                _gamesRunning--;
            }
            Resume();
        }

        public void Resume(string pauser)
        {
            _pausers.Remove(pauser);
            Resume();
        }

        public void Resume()
        {
            lock (_gameLock) /* Then */ if (_currentSampleProvider != null)
            {
                _currentSampleProvider.Resume();
                _playing = true;
            }
        }

        public void SetVolume(float? volume = null)
        {
            if (_currentSampleProvider != null)
            {
                var subVolume = volume ?? _uiStateSettings.MusicVolume;
                _currentSampleProvider.Volume = subVolume * _settings.ActiveModeSettings.MusicMasterVolume;
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

        public void Pause()
        {
            lock (_gameLock) /* Then */ if (_currentSampleProvider != null) 
            {
                _currentSampleProvider.Pause();
                _playing = false;
            }
        }

        public void Preview()
        {
            var soundFile = _pathingService.GetSoundFiles().FirstOrDefault();
            if (soundFile != null)
            {
                _errorHandler.Try(() => PlayFile(soundFile));
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

        public void Play(string filePath)
        {
            lock (_gameLock)
            {
                StopInternal();
                _errorHandler.TryWithPrompt(() => PlayFile(filePath));
            }
        }

        public void Play(Stream stream)
        {
            lock (_gameLock)
            {
                StopInternal();
                _errorHandler.TryWithPrompt(() => PlayStream(stream));
            }
        }

        public void Stop()
        {
            lock (_gameLock)
            {
                StopInternal();
            }
        }

        private void StopInternal()
        {
            var oldProvider = _currentSampleProvider;
            _currentSampleProvider = null;
            oldProvider?.Stop();
            oldProvider?.Dispose();
        }

        #endregion

        #region Event Handlers

        private void UIStateChanged(object sender, UIStateChangedArgs args)
        {
            _currentUIState = args.NewState;
            _uiStateSettings = args.NewSettings;

            lock (_gameLock) /* Then */ if (_uiStateSettings.MusicSource != _lastPlayedAudioSource)
            {
                if (args.OldState is UIState.Main)
                { 
                    _mainPosition = _currentSampleProvider?.Position ?? _mainPosition;
                    _mainFileName = _currentMusicFileName;
                }
                PlayNextFile();
            }
            else if (_currentSampleProvider != null)
            {
                var volume = _uiStateSettings.MusicVolume * _settings.ActiveModeSettings.MusicMasterVolume;
                _currentSampleProvider.Volume = volume;
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
                        _appIsNotStopping = false;
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
                        _currentSampleProvider?.Resume();
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
            _lastPlayedAudioSource = _uiStateSettings.MusicSource;
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
                (files, _lastPlayedAudioSource) = _musicFileSelector.GetBackupFiles();
            }

            return files;

        }

        private void MusicEnded(object _, SampleProviderEventArgs args)
        {
            // Mixer is shared with the SoundPlayer, so we must check if the finished sample is from the MusicPlayer
            if (_appIsNotStopping && args.SampleProvider == _currentSampleProvider)
            /* Then */ lock (_gameLock)
            /* Then */ if ((!_settings.RandomizeOnMusicEnd && !_currentSampleProvider.Stopped)
                           || _currentMusicFileName == _currentSampleProvider.FileName)
            {
                _currentSampleProvider.Position = 0;
                _WavePlayerManager.Mixer.AddMixerInput(_currentSampleProvider);
            }
            else
            {
                PlayFile(_currentMusicFileName);
            }
        }

        #endregion

        #region Helpers

        private bool PlayFile(string musicFile)
        {
            if (string.IsNullOrWhiteSpace(musicFile))
            {
                return false;
            }

            var source =  new AudioFileReader(musicFile) { Volume = 1 };
            var baseProvider = ConvertProvider(source);
            if (baseProvider is null)
            {
                source.Dispose();
                return false;
            }

            _currentSampleProvider?.Dispose();

            _currentGame = _incomingGame;
            _currentFilterGuid = _incomingFilterGuid;
            _currentMusicFileName = musicFile;

            var currentIsMain = _currentUIState is UIState.Main;
            var mainFileIsTheSame = _mainFileName == _currentMusicFileName;
            _currentSampleProvider = new ControllableSampleProvider(
                baseProvider,
                new AudioFileStreamReader(source),
                _uiStateSettings.MusicVolume * _settings.ActiveModeSettings.MusicMasterVolume,
                _settings.MuffledFilterBandwidth,
                _settings.MuffledFadeUpperBound,
                _settings.MuffledFadeLowerBound,
                _settings.MuffledFadeTimeMs,
                _settings.VolumeFadeTimeMs,
                _uiStateSettings.MusicMuffled,
                currentIsMain && mainFileIsTheSame);

            if (currentIsMain) /* Then */ if (mainFileIsTheSame)
            {
                _currentSampleProvider.Position = _mainPosition;
            }
            else
            {
                _mainPosition = 0;
                _mainFileName = _currentMusicFileName;
            }

            _WavePlayerManager.Mixer.AddMixerInput(_currentSampleProvider);
            _playing = true;

            return true;
        }

        private bool PlayStream(Stream stream)
        {
            var streamReader = new StreamMediaFoundationReader(stream);
            var waveProvider = new Wave16ToFloatProvider(streamReader);
            var streamProvider = new WaveToSampleProvider(waveProvider);
            var baseProvider = ConvertProvider(streamProvider);
            if (baseProvider is null)
            {
                streamReader.Dispose();
                return false;
            }

            _currentSampleProvider?.Dispose();

            var currentIsMain = _currentUIState is UIState.Main;
            var mainFileIsTheSame = _mainFileName == _currentMusicFileName;
            _currentSampleProvider = new ControllableSampleProvider(
                baseProvider,
                new WebStreamReader(streamReader),
                _uiStateSettings.MusicVolume * _settings.ActiveModeSettings.MusicMasterVolume,
                _settings.MuffledFilterBandwidth,
                _settings.MuffledFadeUpperBound,
                _settings.MuffledFadeLowerBound,
                _settings.MuffledFadeTimeMs,
                _settings.VolumeFadeTimeMs,
                _uiStateSettings.MusicMuffled,
                currentIsMain && mainFileIsTheSame);

            _WavePlayerManager.Mixer.AddMixerInput(_currentSampleProvider);
            _playing = true;

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

            var newAudioSource = _uiStateSettings.MusicSource;
            if (_settings.BackupMusicEnabled && files.IsEmpty())
            {
                (files, newAudioSource) = _musicFileSelector.GetBackupFiles();
            }

            var noSampleProvider = _currentSampleProvider is null;
            var musicEnded = !noSampleProvider && _currentSampleProvider.Stopped;
            var noMusicIsPlaying = noSampleProvider || _currentSampleProvider.Stopped;

            var file = _musicFileSelector.SelectFile(files, _currentMusicFileName, musicEnded);
            if (ShouldPlayMusic()) /* Then */ if (noMusicIsPlaying)
            {
                if (PlayFile(file)) /* Then */ _lastPlayedAudioSource = newAudioSource;
            }
            else if (_currentMusicFileName != file)
            {
                _lastPlayedAudioSource = newAudioSource;
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
                    if (_lastPlayedAudioSource != AudioSource.Default || string.IsNullOrWhiteSpace(_currentMusicFileName))
                    {
                        return _pathingService.GetDefaultMusicFiles();
                    }
                    break;
            }

            return new[] { _currentMusicFileName };
        }

        private bool NewMusicSource<T>(T expected, T actual, AudioSource desiredMusicType)
            => _lastPlayedAudioSource != desiredMusicType
            || !Equals(expected, actual)
            || string.IsNullOrWhiteSpace(_currentMusicFileName);

        #endregion

        #endregion
    }
}
