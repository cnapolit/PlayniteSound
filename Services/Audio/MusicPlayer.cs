using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PlayniteSounds.Models.Audio.SampleProviders;

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
                    PlayNextFile();
                }
            }
        }

        // Prevents restarting same file
        private string _currentMusicFileName = string.Empty;
        public string CurrentMusicFile
        {
            set
            {
                if (_currentMusicFileName != value || _musicEnded)
                {
                    _currentMusicFileName = value;
                    if (ShouldPlayMusic())
                    {
                        if (_musicEnded || _currentSampleProvider is null)
                        {
                            _errorHandler.Try(AttemptPlay, _currentMusicFileName);
                        }
                        else
                        {
                            // stopping the current sample should trigger the relevant mixer event
                            _currentSampleProvider.Stop();
                        }
                    }
                }
            }
        }

        public bool StartSoundFinished { get; set; }

        private readonly IMainViewAPI _mainView;
        private readonly IErrorHandler _errorHandler;
        private readonly IPathingService _pathingService;
        private readonly IMusicFileSelector _musicFileSelector;
        private readonly ISet<string> _pausers = new HashSet<string>();
        private          ControllableSampleProvider _currentSampleProvider;
        private          Guid _currentFilterGuid;
        private          Platform _currentPlatform;
        private          AudioSource _lastPlayedType;
        private          uint _gamesRunning;
        private          bool _musicEnded;

        public MusicPlayer(
            IErrorHandler errorHandler,
            IPathingService pathingService,
            IMainViewAPI mainView,
            IMusicFileSelector musicFileSelector,
            MixingSampleProvider mixer,
            PlayniteSoundsSettings _settings) : base(mixer, _settings)
        {
            _errorHandler = errorHandler;
            _pathingService = pathingService;
            _mainView = mainView;
            _musicFileSelector = musicFileSelector;
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
                _currentSampleProvider.Volume = volume ?? _settings.CurrentUIStateSettings.MusicVolume;
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

        public void Play(IEnumerable<Game> games)
        {
            if (ShouldPlayMusic()) switch (games?.Count() ?? 0)
            {
                case 1: CurrentGame = games.First(); break;
                case 0: CurrentGame = null;          break;
            }
        }

        public void Preview()
        {
            var soundFile = _pathingService.GetSoundFiles().FirstOrDefault();

            if (soundFile != null)
            {
                _errorHandler.Try(AttemptPlay, soundFile);
            }
        }

        public void UIStateChanged()
        {
            // Update the volume in the event the file doesn't change
            SetVolume();
            if (ShouldPlayMusic())
            {
                PlayNextFile();
            }

            // Update after in case file did not change
            if (_currentSampleProvider != null)
            {
                _currentSampleProvider.Muffled = _settings.CurrentUIStateSettings.MusicMuffled;
            }
        }

        #region Helpers

        private void AttemptPlay(string musicFile)
        {
            if (!string.IsNullOrWhiteSpace(musicFile))
            {
                var baseProvider = ConvertProvider(new AutoDisposeFileReader(musicFile, 1));
                _currentSampleProvider = new ControllableSampleProvider(baseProvider)
                {
                    Volume = _settings.CurrentUIStateSettings.MusicVolume,
                    Muffled = _settings.CurrentUIStateSettings.MusicMuffled
                };

                _mixer.AddMixerInput(_currentSampleProvider);
            }
            _musicEnded = false;
        }

        private void MusicEnded(object _, SampleProviderEventArgs args)
        {
            // Mixer is shared with the SoundPlayer, so we must check if the finished sample is from the MusicPlayer
            if (args.SampleProvider == _currentSampleProvider)
            {
                _musicEnded = !_currentSampleProvider.Stopped;
                if (_settings.RandomizeOnMusicEnd && _musicEnded)
                {
                    PlayNextFile();
                }
                else
                {
                    _errorHandler.Try(AttemptPlay, _currentMusicFileName);
                }
            }
        }

        private bool ShouldPlayMusic() 
            => StartSoundFinished
            && _pausers.Count is 0 
            && _gamesRunning is 0
            && _settings.ActiveModeSettings.MusicEnabled;

        private void PlayNextFile()
        {
            string[] files;

            var musicSource = _settings.CurrentUIStateSettings.MusicSource;

            switch (musicSource)
            {
                case AudioSource.Game:
                    files = _pathingService.GetGameMusicFiles(_currentGame);
                    break;
                case AudioSource.Platform:
                    var newPlatform = _currentGame?.Platforms?.FirstOrDefault();
                    if (NewMusicSource(_currentPlatform?.Name, newPlatform?.Name, musicSource))
                    {
                        _currentPlatform = newPlatform;
                        files = _pathingService.GetPlatformMusicFiles(_currentPlatform);
                    }
                    else
                    {
                        files = new string[] { _currentMusicFileName };
                    }
                    break;
                case AudioSource.Filter:
                    var activeFilter = _mainView.GetActiveFilterPreset();
                    if (NewMusicSource(_currentFilterGuid, activeFilter, musicSource))
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
                    files = _lastPlayedType != AudioSource.Default || string.IsNullOrWhiteSpace(_currentMusicFileName)
                        ? _pathingService.GetDefaultMusicFiles()
                        : new string[] { _currentMusicFileName };
                    break;
            }

            var typePlayed = musicSource;
            if (_settings.BackupMusicEnabled && !files.Any())
            {
                (files, typePlayed) = _musicFileSelector.GetBackupFiles();
            }

            _lastPlayedType = typePlayed;
            CurrentMusicFile = _musicFileSelector.SelectFile(files, _currentMusicFileName, _musicEnded);
        }

        private bool NewMusicSource<T>(T expected, T actual, AudioSource desiredMusicType)
            => _lastPlayedType != desiredMusicType
            || !expected.Equals(actual)
            || string.IsNullOrWhiteSpace(_currentMusicFileName);

        private void Pause()
        {
            if (_currentSampleProvider is null) return;
            _currentSampleProvider.Paused = true;
        }

        private void Start()
        {
            if (ShouldPlayMusic() && _currentSampleProvider != null)
            {
                _currentSampleProvider.Paused = false;
            }
        }

        #endregion

        #endregion
    }
}
