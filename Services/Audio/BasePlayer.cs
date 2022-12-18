using PlayniteSounds.Common.Imports;
using PlayniteSounds.Models;
using System.Diagnostics;
using System.Linq;

namespace PlayniteSounds.Services.Audio
{
    internal abstract class BasePlayer
    {
        #region Infrastructure

        private   readonly bool                   _isDesktop;
        protected          PlayniteSoundsSettings _settings;

        public BasePlayer(PlayniteSoundsSettings settings, bool isDekstop)
        {
            _settings = settings;
            _isDesktop = isDekstop;
        }

        #endregion

        #region Implementation

        protected bool ShouldPlayAudio(AudioState state)
        {
            var playOnFullScreen = !_isDesktop && state == AudioState.Fullscreen;
            var playOnBoth = state == AudioState.Always;
            var playOnDesktop = _isDesktop && state == AudioState.Desktop;

            var isInForeground = !_settings.PauseOnDeactivate || PlayniteIsInForeground();

            return (playOnFullScreen || playOnBoth || playOnDesktop) && isInForeground;
        }

        private static bool PlayniteIsInForeground()
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
