using Microsoft.Win32;
using NAudio.Wave;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using System;
using System.Windows;

namespace PlayniteSounds.Services.State
{
    public class AppStateChangeHandler : IAppStateChangeHandler
    {
        #region Infrastructure

        private readonly IWavePlayer            _wavePlayer;
        private readonly ISoundPlayer           _soundPlayer;
        private readonly PlayniteSoundsSettings _settings;

        public AppStateChangeHandler(
            IWavePlayer waveplayer,
            ISoundPlayer soundPlayer,
            PlayniteSoundsSettings settings)
        {
            _wavePlayer = waveplayer;
            _soundPlayer = soundPlayer;
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
                Pause();
            }
        }

        #endregion

        #region OnApplicationActivate

        public void OnApplicationActivate(object sender, EventArgs e)
        {
            if (_settings.PauseOnDeactivate)
            {
                Resume();
            }
        }

        #endregion

        #region OnWindowStateChanged

        public void OnWindowStateChanged(object sender, EventArgs e)
        {
            if (_settings.PauseOnDeactivate) /* Then */ switch (Application.Current?.MainWindow?.WindowState)
            {
                case WindowState.Normal:
                case WindowState.Maximized:
                    Resume();
                    break;
                case WindowState.Minimized:
                    Pause();
                    break;
            }
        }

        private void Resume()
        {
            _soundPlayer.Play(MainStateSettings().EnterSettings);
            _wavePlayer.Play();
        }

        private void Pause() => _soundPlayer.Play(MainStateSettings().ExitSettings, _wavePlayer.Pause);

        private UIStateSettings MainStateSettings() => _settings.ActiveModeSettings.UIStatesToSettings[UIState.Main];

        #endregion

        #endregion
    }
}
