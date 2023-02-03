using PlayniteSounds.Common.Imports;
using PlayniteSounds.Models;
using System.Diagnostics;
using System.Linq;

namespace PlayniteSounds.Services.Audio
{
    internal abstract class BasePlayer
    {
        #region Infrastructure

        protected readonly bool                   _isDesktop;
        protected readonly PlayniteSoundsSettings _settings;
        protected readonly ModeSettings           _modeSettings;

        public BasePlayer(PlayniteSoundsSettings settings, bool isDekstop)
        {
            _settings = settings;
            _isDesktop = isDekstop;
            _modeSettings = isDekstop ? settings.DesktopSettings : settings.FullscreenSettings;
        }

        #endregion

        #region Implementation

        protected bool ShouldPlayAudio(AudioState state)
        {
            var playOnFullScreen = !_isDesktop && state == AudioState.Fullscreen;
            var playOnBoth = state == AudioState.Always;
            var playOnDesktop = _isDesktop && state == AudioState.Desktop;

            return playOnFullScreen || playOnBoth || playOnDesktop;
        }

        protected static bool PlayniteIsInForeground()
        {
            var foregroundHandle = User32.GetForegroundWindow();

            return Process.
                GetProcesses().
                Where(p => p.ProcessName.Contains("Playnite")).
                Any(p => p.MainWindowHandle == foregroundHandle);
        }

        #endregion
    }
}
