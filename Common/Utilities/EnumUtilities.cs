using PlayniteSounds.Models;

namespace PlayniteSounds.Common.Utilities
{
    internal static class EnumUtilities
    {
        public static MusicType SoundsSettingsToMusicType(PlayniteSoundsSettings settings)
            => UIStateToMusicType(settings.UIState, settings.ActiveModeSettings);

        public static MusicType UIStateToMusicType(UIState uiState, ModeSettings modeSettings)
        {
            switch (uiState)
            {
                case UIState.GameDetails: return modeSettings.GameDetailsMusicType;
                case UIState.Settings:    return modeSettings.SettingsMusicType;
                default:                  return modeSettings.MainMusicType;
            }
        }
    }
}
