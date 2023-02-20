using PlayniteSounds.Common.Imports;
using PlayniteSounds.Models;
using System;
using System.Diagnostics;
using System.Linq;

namespace PlayniteSounds.Services.Audio
{
    public abstract class BasePlayer : IDisposable
    {
        #region Infrastructure

        protected readonly PlayniteSoundsSettings _settings;
        protected          bool                   _disposed;

        public BasePlayer(PlayniteSoundsSettings settings)
        {
            _settings = settings;
        }

        public abstract void Dispose();

        #endregion

        #region Implementation

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
