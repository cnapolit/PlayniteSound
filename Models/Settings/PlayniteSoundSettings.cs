using Playnite.SDK.Data;
using PlayniteSounds.Models.Audio;
using PlayniteSounds.Models.Audio.Sound;
using PlayniteSounds.Models.UI;
using System.Collections.Generic;

namespace PlayniteSounds.Models
{
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

        public ISet<Source> Downloaders { get; set; } = new HashSet<Source>
        {
            Source.KHInsider, Source.Youtube
        };

        public ModeSettings DesktopSettings { get; set; } = new ModeSettings 
        {
            IsDesktop = true,
            UIStatesToSettings = new Dictionary<UIState, UIStateSettings>
            {
                [UIState.Main]                 = new UIStateSettings {                                 MusicMuffled = false },
                [UIState.GameDetails]          = new UIStateSettings { MusicSource = AudioSource.Game, MusicMuffled = false },
                [UIState.MainMenu]             = new UIStateSettings(),
                [UIState.Filters]              = new UIStateSettings(),
                [UIState.FilterPresets]        = new UIStateSettings(),
                [UIState.Search]               = new UIStateSettings(),
                [UIState.GameMenu]             = new UIStateSettings(),
                [UIState.Settings]             = new UIStateSettings(),
                [UIState.Notifications]        = new UIStateSettings(),
                [UIState.GameMenu_GameDetails] = new UIStateSettings()
            },
            PlayniteEventToSoundTypesSettings = new Dictionary<PlayniteEvent, SoundTypeSettings>
            {
                [PlayniteEvent.AppStarted]      = new SoundTypeSettings(),
                [PlayniteEvent.AppStopped]      = new SoundTypeSettings(),
                [PlayniteEvent.GameStarting]    = new SoundTypeSettings(),
                [PlayniteEvent.GameSelected]    = new SoundTypeSettings(),
                [PlayniteEvent.GameInstalled]   = new SoundTypeSettings(),
                [PlayniteEvent.GameUninstalled] = new SoundTypeSettings(),
                [PlayniteEvent.LibraryUpdated]  = new SoundTypeSettings(),
                [PlayniteEvent.GameStarted]     = new SoundTypeSettings(),
                [PlayniteEvent.GameStopped]     = new SoundTypeSettings()
            }
        };

        public ModeSettings FullscreenSettings { get; set; } = new ModeSettings
        {
            MusicEnabled = true,
            UIStatesToSettings = new Dictionary<UIState, UIStateSettings>
            {
                [UIState.Main] = new UIStateSettings 
                {
                    EnterSettings = new SoundTypeSettings { Enabled = true, SoundType = SoundType.Resume },
                    ExitSettings  = new SoundTypeSettings { Enabled = true, SoundType = SoundType.Pause  },
                    MusicMuffled  = false
                },
                [UIState.GameDetails] = new UIStateSettings
                {
                    EnterSettings = new SoundTypeSettings { Enabled = true, SoundType = SoundType.Enter },
                    ExitSettings  = new SoundTypeSettings { Enabled = true, SoundType = SoundType.Exit  },
                    MusicSource   = AudioSource.Game, 
                    MusicMuffled  = false
                },
                [UIState.MainMenu] = new UIStateSettings
                {
                    EnterSettings = DefaultEnterSettings(),
                    ExitSettings  = DefaultExitSettings()
                },
                [UIState.Filters] = new UIStateSettings
                {
                    EnterSettings = DefaultEnterSettings(),
                    ExitSettings  = DefaultExitSettings()
                },
                [UIState.FilterPresets] = new UIStateSettings
                {
                    EnterSettings = DefaultEnterSettings(),
                    ExitSettings  = DefaultExitSettings()
                },
                [UIState.Search] = new UIStateSettings
                {
                    EnterSettings = DefaultEnterSettings(),
                    ExitSettings  = DefaultExitSettings()
                },
                [UIState.GameMenu] = new UIStateSettings
                {
                    EnterSettings = DefaultEnterSettings(),
                    ExitSettings  = DefaultExitSettings()
                },
                [UIState.Settings] = new UIStateSettings
                {
                    EnterSettings = DefaultEnterSettings(),
                    ExitSettings  = DefaultExitSettings()
                },
                [UIState.Notifications] = new UIStateSettings
                {
                    EnterSettings = DefaultEnterSettings(),
                    ExitSettings = DefaultExitSettings()
                },
                [UIState.GameMenu_GameDetails] = new UIStateSettings
                {
                    EnterSettings = DefaultEnterSettings(),
                    ExitSettings  = DefaultExitSettings(),
                    MusicSource   = AudioSource.Game
                }
            },
            PlayniteEventToSoundTypesSettings = new Dictionary<PlayniteEvent, SoundTypeSettings>
            {
                [PlayniteEvent.AppStarted] = new SoundTypeSettings 
                { 
                    Enabled = true,
                    SoundType = SoundType.Start
                },
                [PlayniteEvent.AppStopped] = new SoundTypeSettings 
                {
                    Enabled = true,
                    SoundType = SoundType.Stop 
                },
                [PlayniteEvent.GameStarting] = new SoundTypeSettings
                {
                    Enabled = true,
                    SoundType = SoundType.GameStart,
                    Source = AudioSource.Game
                },
                [PlayniteEvent.GameSelected] = new SoundTypeSettings
                {
                    Enabled = true,
                    SoundType = SoundType.Tick,
                },
                [PlayniteEvent.GameInstalled] = new SoundTypeSettings
                {
                    Enabled = true,
                    SoundType = SoundType.Installed,
                    Source = AudioSource.Game
                },
                [PlayniteEvent.GameUninstalled] = new SoundTypeSettings
                {
                    Enabled = true,
                    SoundType = SoundType.Uninstalled,
                    Source = AudioSource.Game
                },
                [PlayniteEvent.LibraryUpdated] = new SoundTypeSettings
                {
                    Enabled = true,
                    SoundType = SoundType.Updated
                },
                [PlayniteEvent.GameStarted] = new SoundTypeSettings(),
                [PlayniteEvent.GameStopped] = new SoundTypeSettings()
            }
        };

        private static SoundTypeSettings DefaultEnterSettings()
            => new SoundTypeSettings { Enabled = true, SoundType = SoundType.Enter };
        private static SoundTypeSettings DefaultExitSettings()
            => new SoundTypeSettings { Enabled = true, SoundType = SoundType.Exit };

        [DontSerialize] public UIStateSettings CurrentUIStateSettings { get; set; }
        [DontSerialize] public ModeSettings ActiveModeSettings { get; set; }
    }
}
