using Playnite.SDK.Data;
using PlayniteSounds.Models.Audio;
using PlayniteSounds.Models.Audio.Sound;
using PlayniteSounds.Models.UI;
using System.Collections.Generic;

namespace PlayniteSounds.Models;

public class PlayniteSoundsSettings : ObservableObject
{
    public bool        BackupSoundEnabled      { get; set; } = true;
    public bool        ManualParallelDownload  { get; set; } = true;
    public bool        NormalizeMusic          { get; set; } = true;
    public bool        PauseOnDeactivate       { get; set; } = true;
    public bool        PlayTickOnGameSelect    { get; set; } = true;
    public bool        RandomizeOnMusicEnd     { get; set; } = true;
    public bool        SkipFirstSelectSound    { get; set; } = true;
    public bool        StopMusicOnGameStarting { get; set; } = true;
    public bool        YtPlaylists             { get; set; } = true;
    public bool        AutoDownload            { get; set; }
    public bool        AutoParallelDownload    { get; set; }
    public bool        BackupMusicEnabled      { get; set; }
    public bool        RandomizeOnEverySelect  { get; set; }
    public bool        TagMissingEntries       { get; set; }
    public bool        TagNormalizedGames      { get; set; }
    public int         AudioSampleRate         { get; set; } = 44100;
    public int         AudioChannels           { get; set; } = 2;
    public int         MuffledFadeLowerBound   { get; set; } = 800;
    public int         MuffledFadeUpperBound   { get; set; } = 2000;
    public int         MuffledFadeTimeMs       { get; set; } = 500;
    public int         VolumeFadeTimeMs        { get; set; } = 500;
    public float       MuffledFilterBandwidth  { get; set; } = 1;
    public AudioOutput AudioOutput             { get; set; }
    public string      FFmpegNormalizeArgs     { get; set; }
    public string      FFmpegNormalizePath     { get; set; }
    public string      FFmpegPath              { get; set; }
    public string      YoutubeSearchFormat     { get; set; }

    public ISet<Source> Downloaders { get; set; } = (HashSet<Source>)
        [Source.KHInsider, Source.SoundCloud, Source.Youtube];

    public ModeSettings DesktopSettings { get; set; } = new()
    {
        IsDesktop = true,
        UIStatesToSettings = new Dictionary<UIState, UIStateSettings>
        {
            [UIState.Main]                 = new() {                                 MusicMuffled = false },
            [UIState.GameDetails]          = new() { MusicSource = AudioSource.Game, MusicMuffled = false },
            [UIState.MainMenu]             = new(),
            [UIState.Filters]              = new(),
            [UIState.FilterPresets]        = new(),
            [UIState.Search]               = new(),
            [UIState.GameMenu]             = new(),
            [UIState.Settings]             = new(),
            [UIState.Notifications]        = new(),
            [UIState.GameMenu_GameDetails] = new()
        },
        PlayniteEventToSoundTypesSettings = new Dictionary<PlayniteEvent, SoundTypeSettings>
        {
            [PlayniteEvent.AppStarted]      = new(),
            [PlayniteEvent.AppStopped]      = new(),
            [PlayniteEvent.GameStarting]    = new(),
            [PlayniteEvent.GameSelected]    = new(),
            [PlayniteEvent.GameInstalled]   = new(),
            [PlayniteEvent.GameUninstalled] = new(),
            [PlayniteEvent.LibraryUpdated]  = new(),
            [PlayniteEvent.GameStarted]     = new(),
            [PlayniteEvent.GameStopped]     = new()
        }
    };

    public ModeSettings FullscreenSettings { get; set; } = new()
    {
        MusicEnabled = true,
        UIStatesToSettings = new Dictionary<UIState, UIStateSettings>
        {
            [UIState.Main] = new()
            {
                EnterSettings = new SoundTypeSettings { Enabled = true, SoundType = SoundType.Resume },
                ExitSettings  = new SoundTypeSettings { Enabled = true, SoundType = SoundType.Pause  },
                MusicMuffled  = false
            },
            [UIState.GameDetails] = new()
            {
                EnterSettings = new SoundTypeSettings { Enabled = true, SoundType = SoundType.Enter },
                ExitSettings  = new SoundTypeSettings { Enabled = true, SoundType = SoundType.Exit  },
                MusicSource   = AudioSource.Game, 
                MusicMuffled  = false
            },
            [UIState.MainMenu] = new()
            {
                EnterSettings = DefaultEnterSettings(),
                ExitSettings  = DefaultExitSettings()
            },
            [UIState.Filters] = new()
            {
                EnterSettings = DefaultEnterSettings(),
                ExitSettings  = DefaultExitSettings()
            },
            [UIState.FilterPresets] = new()
            {
                EnterSettings = DefaultEnterSettings(),
                ExitSettings  = DefaultExitSettings()
            },
            [UIState.Search] = new()
            {
                EnterSettings = DefaultEnterSettings(),
                ExitSettings  = DefaultExitSettings()
            },
            [UIState.GameMenu] = new()
            {
                EnterSettings = DefaultEnterSettings(),
                ExitSettings  = DefaultExitSettings()
            },
            [UIState.Settings] = new()
            {
                EnterSettings = DefaultEnterSettings(),
                ExitSettings  = DefaultExitSettings()
            },
            [UIState.Notifications] = new()
            {
                EnterSettings = DefaultEnterSettings(),
                ExitSettings = DefaultExitSettings()
            },
            [UIState.GameMenu_GameDetails] = new()
            {
                EnterSettings = DefaultEnterSettings(),
                ExitSettings  = DefaultExitSettings(),
                MusicSource   = AudioSource.Game
            }
        },
        PlayniteEventToSoundTypesSettings = new Dictionary<PlayniteEvent, SoundTypeSettings>
        {
            [PlayniteEvent.AppStarted] = new()
            { 
                Enabled = true,
                SoundType = SoundType.Start
            },
            [PlayniteEvent.AppStopped] = new()
            {
                Enabled = true,
                SoundType = SoundType.Stop 
            },
            [PlayniteEvent.GameStarting] = new()
            {
                Enabled = true,
                SoundType = SoundType.GameStart,
                Source = AudioSource.Game
            },
            [PlayniteEvent.GameSelected] = new()
            {
                Enabled = true,
                SoundType = SoundType.Tick,
            },
            [PlayniteEvent.GameInstalled] = new()
            {
                Enabled = true,
                SoundType = SoundType.Installed,
                Source = AudioSource.Game
            },
            [PlayniteEvent.GameUninstalled] = new()
            {
                Enabled = true,
                SoundType = SoundType.Uninstalled,
                Source = AudioSource.Game
            },
            [PlayniteEvent.LibraryUpdated] = new()
            {
                Enabled = true,
                SoundType = SoundType.Updated
            },
            [PlayniteEvent.GameStarted] = new(),
            [PlayniteEvent.GameStopped] = new()
        }
    };

    private static SoundTypeSettings DefaultEnterSettings() => new() { Enabled = true, SoundType = SoundType.Enter };
    private static SoundTypeSettings DefaultExitSettings() => new() { Enabled = true, SoundType = SoundType.Exit };

    [DontSerialize] public UIStateSettings CurrentUIStateSettings { get; set; }
    [DontSerialize] public ModeSettings ActiveModeSettings { get; set; }
}