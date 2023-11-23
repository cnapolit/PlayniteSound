using Microsoft.Win32;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using System;
using System.Windows;

namespace PlayniteSounds.Services.State
{
    public class AppStateChangeHandler : IAppStateChangeHandler
    {
        #region Infrastructure

        private readonly IWavePlayerManager     _wavePlayerManager;
        private readonly ISoundPlayer           _soundPlayer;
        private readonly IMusicPlayer           _musicPlayer;
        private readonly PlayniteSoundsSettings _settings;

        public AppStateChangeHandler(
            IWavePlayerManager wavePlayerManager,
            ISoundPlayer soundPlayer,
            IMusicPlayer musicPlayer,
            PlayniteSoundsSettings settings)
        {
            _wavePlayerManager = wavePlayerManager;
            _soundPlayer = soundPlayer;
            _musicPlayer = musicPlayer;
            _settings = settings;
        }

        #endregion

        #region Implementation

        #region OnPowerModeChanged

        public void OnPowerModeChanged(object sender, PowerModeChangedEventArgs args)
        {
            if (args.Mode is PowerModes.Resume)
            {
                _wavePlayerManager.Init();
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
            _wavePlayerManager.Init();
            _soundPlayer.Play(MainStateSettings().EnterSettings, _musicPlayer.Resume);
        }

        private void Pause()
        {
            _musicPlayer.Pause();
            _soundPlayer.Play(MainStateSettings().ExitSettings, _wavePlayerManager.WavePlayer.Pause);
        }

        private UIStateSettings MainStateSettings() => _settings.ActiveModeSettings.UIStatesToSettings[UIState.Main];

        #endregion

        #endregion
    }
}
