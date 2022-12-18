using System;

namespace PlayniteSounds.Common.Constants
{
    public class SoundFile
    {
        public const string DefaultMusicName = "_music_.mp3";
        public const string LocalizationSource = "LocSource";
        public const string DefaultNormArgs = "-lrt 20 -c:a libmp3lame";

        public static string ApplicationStartedSound => _applicationStartedSound.Value;
        private static Lazy<string> _applicationStartedSound = new Lazy<string>(() => CurrentPrefix + BaseApplicationStartedSound);
        public static string ApplicationStoppedSound => _applicationStoppedSound.Value;
        private static Lazy<string> _applicationStoppedSound = new Lazy<string>(() => CurrentPrefix + BaseApplicationStoppedSound);
        public static string GameInstalledSound => _gameInstalledSound.Value;
        private static Lazy<string> _gameInstalledSound = new Lazy<string>(() => CurrentPrefix + BaseGameInstalledSound);
        public static string GameSelectedSound => _gameSelectedSound.Value;
        private static Lazy<string> _gameSelectedSound = new Lazy<string>(() => CurrentPrefix + BaseGameSelectedSound);
        public static string GameStartedSound => _gameStartedSound.Value;
        private static Lazy<string> _gameStartedSound = new Lazy<string>(() => CurrentPrefix + BaseGameStartedSound);
        public static string GameStartingSound => _gameStartingSound.Value;
        private static Lazy<string> _gameStartingSound = new Lazy<string>(() => CurrentPrefix + BaseGameStartingSound);
        public static string GameStoppedSound => _gameStoppedSound.Value;
        private static Lazy<string> _gameStoppedSound = new Lazy<string>(() => CurrentPrefix + BaseGameStoppedSound);
        public static string GameUninstalledSound => _gameUninstalledSound.Value;
        private static Lazy<string> _gameUninstalledSound = new Lazy<string>(() => CurrentPrefix + BaseGameUninstalledSound);
        public static string LibraryUpdatedSound => _libraryUpdatedSound.Value;
        private static Lazy<string> _libraryUpdatedSound = new Lazy<string>(() => CurrentPrefix + BaseLibraryUpdatedSound);

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
}
