using Microsoft.Win32;
using NAudio.Wave;
using PlayniteSounds.Models;
using System;
using System.Windows;

namespace PlayniteSounds.Services.State
{
    public class AppStateChangeHandler : IAppStateChangeHandler
    {
        #region Infrastructure

        private readonly PlayniteSoundsSettings _settings;
        private readonly IWavePlayer            _wavePlayer;

        public AppStateChangeHandler(
            IWavePlayer waveplayer,
            PlayniteSoundsSettings settings)
        {
            _wavePlayer = waveplayer;
            _settings = settings;
        }

        #endregion

        #region Implementation

        #region OnPowerModeChanged

        public void OnPowerModeChanged(object sender, PowerModeChangedEventArgs args)
        {
            if (args.Mode is PowerModes.Resume)
            {
                _wavePlayer.Play();
            }
        }

        #endregion

        #region OnApplicationDeactivate

        public void OnApplicationDeactivate(object sender, EventArgs e)
        {
            if (_settings.PauseOnDeactivate)
            {
                _wavePlayer.Pause();
            }
        }

        #endregion

        #region OnApplicationActivate

        public void OnApplicationActivate(object sender, EventArgs e)
        {
            if (_settings.PauseOnDeactivate)
            {
                _wavePlayer.Play();
            }
        }

        #endregion

        #region OnWindowStateChanged

        public void OnWindowStateChanged(object sender, EventArgs e)
        {
            if (_settings.PauseOnDeactivate)
            {
                switch (Application.Current?.MainWindow?.WindowState)
                {
                    case WindowState.Normal:
                    case WindowState.Maximized:
                        _wavePlayer.Play();
                        break;
                    case WindowState.Minimized:
                        _wavePlayer.Pause();
                        break;
                }
            }
        }

        #endregion

        #endregion
    }
}
