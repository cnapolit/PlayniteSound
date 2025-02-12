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

namespace PlayniteSounds.Services.Audio;

public class SoundPlayer : BasePlayer, ISoundPlayer, IDisposable
{
    #region Infrastructure

    private readonly IMainViewAPI    _mainViewAPI;
    private readonly IPathingService _pathingService;
    private readonly IMusicFileSelector _fileSelector;
    private readonly IList<bool>     _activePlayers = (List<bool>) [..new bool[9]];
    private readonly PlayniteState   _playniteState;
    private          CachedSound     _cachedSelectedGameSound;
    private          UIStateSettings _uiStateSettings;
    private          Action          _playMusicCallback;
    private          string          _selectedSoundFilePath;
    private          bool            _firstSelectSound = true;
    private          bool            _disposed;

    public SoundPlayer(
        IMainViewAPI mainViewAPI,
        IPathingService pathingService,
        IMusicFileSelector fileSelector,
        IPlayniteEventHandler playniteEventHandler,
        IMusicPlayer musicPlayer,
        IWavePlayerManager mixer,
        PlayniteState playniteState,
        PlayniteSoundsSettings settings) : base(mixer, settings)
    {
        _mainViewAPI = mainViewAPI;
        _pathingService = pathingService;
        _fileSelector = fileSelector;
        _playniteState = playniteState;
        _playMusicCallback = musicPlayer.Initialize;
        playniteEventHandler.UIStateChanged += UIStateChanged;
        playniteEventHandler.PlayniteEventOccurred += PlayniteEventOccurred;
        _wavePlayerManager.Mixer.MixerInputEnded += SoundEnded;
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
            // allow time for last chunk of sound to play
            var timer = new System.Timers.Timer(1000)
            {
                Enabled = true,
                AutoReset = false
            };
            var callback = callBackSampleProvider.Callback;
            timer.Elapsed += (_, _) => callback();
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
        var sampler = GetSoundSampleProvider(settings, _playniteState.CurrentGame);
        if (sampler != null)
        {
            PlaySound(sampler, null);
        }

    }

    public void Play(SoundTypeSettings settings, Action callBack = null)
    {
        var sampler = ShouldPlaySound(settings)
            ? GetSoundSampleProvider(settings, _playniteState.CurrentGame) 
            : null;
        PlaySound(sampler, callBack);
    }

    public void Tick()
    {
        var settings = _uiStateSettings.TickSettings;
        if (ShouldPlaySound(settings))
        /* Then */ PlaySound(GetSelectSoundSampleProvider(settings), null);
    }

    public void Trigger(SoundType soundType)
    {
        var settings = _uiStateSettings.TickSettings;
        if (ShouldPlaySound(settings)) 
        /* Then */ PlaySound(GetSoundSampleProvider(settings.Source, soundType, settings.Volume, _playniteState.CurrentGame), null);
    }

    public void Play(SoundTypeSettings settings, Game game)
    {
        if (ShouldPlaySound(settings))
        /* Then */ PlaySound(GetSoundSampleProvider(settings, game), null);
    }

    #region Helpers

    private bool ShouldPlaySound(SoundTypeSettings settings) => settings.Enabled && (_playniteState.HasFocus || settings.SoundType is SoundType.Exit);

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
            return GetSoundSampleProvider(settings, _playniteState.CurrentGame);
        }

        object resource = null;
        switch (settings.Source)
        {
            case AudioSource.Platform:
            case AudioSource.Game:
                resource = _playniteState.CurrentGame;
                break;
            case AudioSource.Filter:
                resource = _mainViewAPI.GetActiveFilterPreset().ToString();
                break;
        }
        var filePath = _pathingService.GetSoundTypeFile(settings.Source, settings.SoundType, resource);

        if (_settings.BackupSoundEnabled && filePath is null)
            /* Then */ filePath = _fileSelector.GetBackupFiles(settings.Source, settings.SoundType, resource);
            
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
            _cachedSelectedGameSound = new CachedSound(filePath, settings.Volume * _settings.ActiveModeSettings.SoundMasterVolume);
        }

        return new CachedSoundSampleProvider(_cachedSelectedGameSound);
    }

    private ISampleProvider GetSoundSampleProvider(SoundTypeSettings settings, Game game)
        => GetSoundSampleProvider(settings.Source, settings.SoundType, settings.Volume, game);

    private ISampleProvider GetSoundSampleProvider(
        AudioSource source, SoundType soundType, float volume, Game game)
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
        var filePath = _pathingService.GetSoundTypeFile(source, soundType, resource);

        if (_settings.BackupSoundEnabled && filePath is null)
            /* Then */ filePath = _fileSelector.GetBackupFiles(
            source, soundType, _mainViewAPI.GetActiveFilterPreset().ToString());

        return filePath is null 
            ? null 
            : new AutoDisposeFileReader(filePath, volume * _settings.ActiveModeSettings.SoundMasterVolume);
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

        _wavePlayerManager.Mixer.AddMixerInput(reader);
    }

    #endregion

    #endregion
}