using Playnite.SDK.Data;

namespace PlayniteSounds.Models
{
    public class ModeSettings
    {
        public bool      AppStartSoundEnabled        { get; set; }
        public bool      AppStopSoundEnabled         { get; set; }
        public bool      GameInstalledSoundEnabled   { get; set; }
        public bool      GameSelectedSoundEnabled    { get; set; }
        public bool      GameStartedSoundEnabled     { get; set; }
        public bool      GameStartingSoundEnabled    { get; set; }
        public bool      GameStoppedSoundEnabled     { get; set; }
        public bool      GameUninstalledSoundEnabled { get; set; }
        public bool      IsThemeControlled           { get; set; }
        public bool      LibraryUpdateSoundEnabled   { get; set; }
        public bool      MusicEnabled                { get; set; }
        public double    AppStartVolume              { get; set; } = .5;
        public double    AppStopVolume               { get; set; } = .5;
        public double    GameInstalledVolume         { get; set; } = .5;
        public double    GameSelectedVolume          { get; set; } = .5;
        public double    GameStartedVolume           { get; set; } = .5;
        public double    GameStartingVolume          { get; set; } = .5;
        public double    GameStoppedVolume           { get; set; } = .5;
        public double    GameUninstalledVolume       { get; set; } = .5;
        public double    LibraryUpdateVolume         { get; set; } = .5;
        public double    MusicVolume                 { get; set; } = .25;
        public MusicType GameDetailsMusicType        { get; set; } = MusicType.Game;
        public MusicType MainMusicType               { get; set; } = MusicType.Filter;
        public MusicType SettingsMusicType           { get; set; }

        [DontSerialize]
        public bool IsDesktop { get; set; }
    }
}
