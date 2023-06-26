using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace PlayniteSounds.Models
{
    public class PlayniteSoundsSettings : ObservableObject
    {
        public bool         ManualParallelDownload          { get; set; } = true;
        public bool         NormalizeMusic                  { get; set; } = true;
        public bool         PauseOnDeactivate               { get; set; } = true;
        public bool         RandomizeOnMusicEnd             { get; set; } = true;
        public bool         StopMusic                       { get; set; } = true;
        public bool         YtPlaylists                     { get; set; } = true;
        public bool         AutoDownload                    { get; set; }
        public bool         AutoParallelDownload            { get; set; }
        public bool         BackupMusicEnabled              { get; set; }
        public bool         BackupSoundEnabled              { get; set; }
        public bool         RandomizeOnEverySelect          { get; set; }
        public bool         SkipFirstSelectSound            { get; set; }
        public bool         TagMissingEntries               { get; set; }
        public bool         TagNormalizedGames              { get; set; }
        public string       FFmpegNormalizeArgs             { get; set; }
        public string       FFmpegNormalizePath             { get; set; }
        public string       FFmpegPath                      { get; set; }
        public DateTime     LastAutoLibUpdateAssetsDownload { get; set; } = DateTime.Now;
        public ISet<Source> Downloaders                     { get; set; } = new HashSet<Source> { Source.Youtube };

        public ModeSettings DesktopSettings { get; set; } = new ModeSettings 
        {
            IsDesktop = true,
            UIStatesToSettings = new Dictionary<UIState, UIStateSettings>
            {
                { UIState.Main,                 new UIStateSettings() },
                { UIState.GameDetails,          new UIStateSettings() },
                { UIState.MainMenu,             new UIStateSettings() },
                { UIState.Filters,              new UIStateSettings() },
                { UIState.FilterPresets,        new UIStateSettings() },
                { UIState.Search,               new UIStateSettings() },
                { UIState.GameMenu_Main,        new UIStateSettings() },
                { UIState.GameMenu_GameDetails, new UIStateSettings() },
                { UIState.Settings,             new UIStateSettings() }
            }
        };

        public ModeSettings FullscreenSettings { get; set; } = new ModeSettings
        {
            MusicEnabled = true,
            UIStatesToSettings = new Dictionary<UIState, UIStateSettings>
            {
                [UIState.Main] = new UIStateSettings
                { 
                    SoundTypesToSettings = UIStateSettings.DefaultFullscreenSoundTypeSettings(), 
                    MusicSource = AudioSource.Filter 
                },
                [UIState.GameDetails] = new UIStateSettings 
                {
                    SoundTypesToSettings = UIStateSettings.DefaultFullscreenSoundTypeSettings(),
                    MusicSource = AudioSource.Game 
                },
                [UIState.MainMenu] = new UIStateSettings
                {
                    SoundTypesToSettings = UIStateSettings.DefaultFullscreenSoundTypeSettings(),
                    MusicSource = AudioSource.Filter,
                    MusicMuffled = true 
                },
                [UIState.Filters] = new UIStateSettings
                {
                    SoundTypesToSettings = UIStateSettings.DefaultFullscreenSoundTypeSettings(), 
                    MusicSource = AudioSource.Filter,
                    MusicMuffled = true
                },
                [UIState.FilterPresets] = new UIStateSettings 
                {
                    SoundTypesToSettings = UIStateSettings.DefaultFullscreenSoundTypeSettings(),
                    MusicSource = AudioSource.Filter,
                    MusicMuffled = true
                },
                [UIState.Search] = new UIStateSettings 
                {
                    SoundTypesToSettings = UIStateSettings.DefaultFullscreenSoundTypeSettings(),
                    MusicSource = AudioSource.Filter,
                    MusicMuffled = true 
                },
                [UIState.GameMenu_Main] = new UIStateSettings 
                {
                    SoundTypesToSettings = UIStateSettings.DefaultFullscreenSoundTypeSettings(),
                    MusicSource = AudioSource.Filter,
                    MusicMuffled = true
                },
                [UIState.GameMenu_GameDetails] = new UIStateSettings 
                {
                    SoundTypesToSettings = UIStateSettings.DefaultFullscreenSoundTypeSettings(),
                    MusicSource = AudioSource.Filter,
                    MusicMuffled = true
                },
                [UIState.Settings] = new UIStateSettings
                {
                    SoundTypesToSettings = UIStateSettings.DefaultFullscreenSoundTypeSettings(),
                    MusicSource = AudioSource.Filter,
                    MusicMuffled = true
                }
            }
        };

        private UIState _uiState;
        [DontSerialize] public UIState UIState
        {
            get => _uiState; 
            set
            {
                if (_uiState != value)
                {
                    CurrentUIStateSettings = ActiveModeSettings.UIStatesToSettings[value];
                    _uiState = value;
                }
            }
        }

        [DontSerialize] public UIStateSettings CurrentUIStateSettings { get; set; }
        [DontSerialize] public ModeSettings ActiveModeSettings { get; set; }
    }
}
