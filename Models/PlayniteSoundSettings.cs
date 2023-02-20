using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace PlayniteSounds.Models
{
    public class PlayniteSoundsSettings
    {
        public bool         ManualParallelDownload          { get; set; } = true;
        public bool         NormalizeMusic                  { get; set; } = true;
        public bool         PauseOnDeactivate               { get; set; } = true;
        public bool         PerGameStartSound               { get; set; } = true;
        public bool         RandomizeOnMusicEnd             { get; set; } = true;
        public bool         StopMusic                       { get; set; } = true;
        public bool         YtPlaylists                     { get; set; } = true;
        public bool         AutoDownload                    { get; set; }
        public bool         AutoParallelDownload            { get; set; }
        public bool         BackupMusicEnabled              { get; set; }
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
            IsDesktop = true
        };

        public ModeSettings FullscreenSettings { get; set; } = new ModeSettings
        {
            AppStartSoundEnabled = true,
            AppStopSoundEnabled = true,
            GameInstalledSoundEnabled = true,
            GameUninstalledSoundEnabled = true,
            GameStartingSoundEnabled = true,
            LibraryUpdateSoundEnabled = true,
            MusicEnabled = true
        };


        [DontSerialize] public UIState      UIState            { get; set; }
        [DontSerialize] public ModeSettings ActiveModeSettings { get; set; }
        [DontSerialize] public bool IsDesktop => ActiveModeSettings.IsDesktop;
    }
}
