namespace PlayniteSounds.Common.Constants;

public class SoundFile
{
    public const string DefaultMusicName = "_music_.mp3";
    public const string LocalizationSource = "LocSource";
    public const string DefaultNormArgs = "-lrt 20 -c:a libmp3lame";

    // Defined upon plugin creation
    public static string CurrentPrefix;

    public const string DesktopPrefix = "D_";
    public const string FullScreenPrefix = "F_";

    public const string BaseApplicationStartedSound = "ApplicationStarted.wav";
    public const string BaseApplicationStoppedSound = "ApplicationStopped.wav";
    public const string BaseGameInstalledSound      = "GameInstalled.wav";
    public const string BaseGameSelectedSound       = "GameSelected.wav";
    public const string BaseGameStartedSound        = "GameStarted.wav";
    public const string BaseGameStartingSound       = "GameStarting.wav";
    public const string BaseGameStoppedSound        = "GameStopped.wav";
    public const string BaseGameUninstalledSound    = "GameUninstalled.wav";
    public const string BaseLibraryUpdatedSound     = "LibraryUpdated.wav";
}