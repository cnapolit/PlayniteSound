using PlayniteSounds.Models;
using PlayniteSounds.Models.UI;
using PlayniteSounds.Services.Files;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PlayniteSounds.Models.Audio.SampleProviders;
using PlayniteSounds.Models.Audio;
using PlayniteSounds.Services.State;
using PlayniteSounds.Models.State;
using Playnite.SDK.Models;
using Playnite.SDK;
using PlayniteSounds.Models.Audio.Sound;

namespace PlayniteSounds.Services.Audio
{
    public class SoundPlayer : BasePlayer, ISoundPlayer, IDisposable
    {
        #region Infrastructure

        private readonly IMainViewAPI    _mainViewAPI;
        private readonly IPathingService _pathingService;
        private readonly IList<bool>     _activePlayers = new List<bool>(new bool[9]);
        private          Game            _currentGame;
        private          CachedSound     _cachedSelectedGameSound;
        private          UIStateSettings _uiStateSettings;
        private          Action          _playMusicCallback;
        private          string          _selectedSoundFilePath;
        private          bool            _firstSelectSound = true;
        private          bool            _disposed;

        public SoundPlayer(
            IMainViewAPI mainViewAPI,
            IPathingService pathingService,
            IPlayniteEventHandler playniteEventHandler,
            IMusicPlayer musicPlayer,
            MixingSampleProvider mixer,
            PlayniteSoundsSettings settings) : base(mixer, settings)
        {
            _mainViewAPI = mainViewAPI;
            _pathingService = pathingService;
            _playMusicCallback = musicPlayer.Initialize;
            playniteEventHandler.UIStateChanged += UIStateChanged;
            playniteEventHandler.PlayniteEventOccurred += PlayniteEventOccurred;
            _mixer.MixerInputEnded += SoundEnded;
            _uiStateSettings = settings.ActiveModeSettings.UIStatesToSettings[UIState.Main];
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            // Give AppStopped and any other sounds 5 seconds to finish
            for (var i = 0; _activePlayers.Any(p => p) && i < 50; i++)
            {
                Thread.Sleep(100);
            }
        }

        private static void SoundEnded(object mixer, SampleProviderEventArgs args)
        {
            if (args.SampleProvider is CallBackSampleProvider callBackSampleProvider)
            {
                var timer = new System.Timers.Timer(300)
                {
                    Enabled = true,
                    AutoReset = false
                };
                var callback = callBackSampleProvider.Callback;
                timer.Elapsed += (_, __) => callback();
            }
        }

        #endregion

        #region Implementation

        #region PlaySound

        private void PlayniteEventOccurred(object sender, PlayniteEventOccurredArgs args)
        {
            ISampleProvider sampleProvider = null;
            Action onSoundEndCallback = _playMusicCallback;
            if (args.SoundTypeSettings.Enabled) switch (args.Event)
            {
                case PlayniteEvent.GameSelected:
                    if (_settings.PlayTickOnGameSelect)
                    {
                        _currentGame = args.Games.FirstOrDefault();
                        sampleProvider = GetSelectSoundSampleProvider(args.SoundTypeSettings);
                    }
                    break;
                case PlayniteEvent.AppStarted:
                    _playMusicCallback = null;
                    goto default;
                case PlayniteEvent.GameStarting:
                case PlayniteEvent.AppStopped:
                        _activePlayers[(int)args.Event] = true;
                        onSoundEndCallback = () => _activePlayers[(int)args.Event] = false;
                    goto default;
                default:
                    sampleProvider = GetSoundSampleProvider(args.SoundTypeSettings, args.Games.FirstOrDefault());
                    break;
            }

            if (sampleProvider is null)
            {
                onSoundEndCallback?.Invoke();
                return;
            }

            PlaySound(sampleProvider, onSoundEndCallback);
        }

        #endregion

        private void UIStateChanged(object sender, UIStateChangedArgs args)
        {
            SoundTypeSettings settings = null;
            _uiStateSettings = args.NewSettings;

            switch (args.NewState)
            {
                case UIState.Settings:
                case UIState.GameMenu:
                case UIState.GameMenu_GameDetails:
                case UIState.Search:
                case UIState.FilterPresets:
                case UIState.Filters:
                    settings = args.NewSettings.EnterSettings;
                    break;
            }

            if (settings is null) /* Then */ switch (args.OldState)
            {
                case UIState.Settings:
                case UIState.GameMenu:
                case UIState.GameMenu_GameDetails:
                case UIState.Search:
                case UIState.FilterPresets:
                case UIState.Filters:
                    settings = args.NewSettings.EnterSettings;
                    break;
            }

            if (settings is null) /* Then */ switch (args.NewState)
            {
                case UIState.MainMenu:
                    settings = args.NewSettings.EnterSettings;
                    break;
            }

            if (settings is null)
            {
                settings = args.OldState != UIState.Main 
                    ? args.OldSettings.ExitSettings
                    : args.NewSettings.EnterSettings;
            }

            Play(settings, args.Game);
        }

        public void Preview(SoundTypeSettings settings, bool isDesktop)
        {
            var sampler = GetSoundSampleProvider(settings, _currentGame);
            if (sampler != null)
            {
                PlaySound(sampler, null);
            }

        }

        public void Play(SoundTypeSettings settings, Action callBack = null)
        {
            var sampler = settings.Enabled ? GetSoundSampleProvider(settings, _currentGame) : null;
            PlaySound(sampler, callBack);
        }

        public void Tick()
        {
            var settings = _uiStateSettings.TickSettings;
            if (settings.Enabled) /* Then */ PlaySound(GetSelectSoundSampleProvider(settings), null);
        }

        public void Trigger(SoundType soundType)
        {
            var settings = _uiStateSettings.TickSettings;
            if (settings.Enabled) /* Then */
            PlaySound(GetSoundSampleProvider(settings.Source, soundType, settings.Volume, _currentGame), null);
        }

        public void Play(SoundTypeSettings settings, Game game)
        {
            if (!settings.Enabled) /* Then */ return;
            PlaySound(GetSoundSampleProvider(settings, game), null);
        }

        #region Helpers

        private ISampleProvider GetSelectSoundSampleProvider(SoundTypeSettings settings)
        {
            if (_firstSelectSound && _settings.SkipFirstSelectSound)
            {
                _firstSelectSound = false;
                return null;
            }

            if (settings.Source is AudioSource.Game)
            {
                // Don't bother caching since the source changes so frequently 
                return GetSoundSampleProvider(settings, _currentGame);
            }

            object resource = null;
            switch (settings.Source)
            {
                case AudioSource.Platform:
                case AudioSource.Game:
                    resource = _currentGame;
                    break;
                case AudioSource.Filter:
                    resource = _mainViewAPI.GetActiveFilterPreset().ToString();
                    break;
            }
            var filePath = _pathingService.GetSoundTypeFile(settings.Source, settings.SoundType, resource);
            if (filePath is null)
            {
                return null;
            }

            if (_selectedSoundFilePath != filePath || _cachedSelectedGameSound?.Volume != settings.Volume)
            {
                // We must reload the cached sound to inherit the new audio file or update the volume
                _cachedSelectedGameSound = null;
                _selectedSoundFilePath = filePath;
            }

            if (_cachedSelectedGameSound is null)
            {
                _cachedSelectedGameSound = new CachedSound(filePath, settings.Volume);
            }

            return new CachedSoundSampleProvider(_cachedSelectedGameSound);
        }

        private ISampleProvider GetSoundSampleProvider(SoundTypeSettings settings, Game game)
            => GetSoundSampleProvider(settings.Source, settings.SoundType, settings.Volume, game);

        private ISampleProvider GetSoundSampleProvider(
            AudioSource source, SoundType soundType, float Volume, Game game)
        {
            object resource = null;
            switch (source)
            {
                case AudioSource.Platform:
                case AudioSource.Game:
                    resource = game;
                    break;
                case AudioSource.Filter:
                    resource = _mainViewAPI.GetActiveFilterPreset().ToString();
                    break;
            }
            string filePath = _pathingService.GetSoundTypeFile(source, soundType, resource);
            return filePath is null ? null : new AutoDisposeFileReader(filePath, Volume);
        }

        private void PlaySound(ISampleProvider reader, Action callBack)
        {
            reader = ConvertProvider(reader);
            if (reader is null)
            {
                callBack?.Invoke();
                return;
            }

            if (callBack != null)
            {
                reader = new CallBackSampleProvider(reader, callBack);
            }

            _mixer.AddMixerInput(reader);
        }

        #endregion

        #endregion
    }
}
