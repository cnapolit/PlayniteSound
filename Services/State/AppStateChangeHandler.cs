using Microsoft.Win32;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using System;
using System.Windows;
using PlayniteSounds.Models.State;

namespace PlayniteSounds.Services.State
{
    public class AppStateChangeHandler : IAppStateChangeHandler
    {
        #region Infrastructure

        private readonly IWavePlayerManager     _wavePlayerManager;
        private readonly ISoundPlayer           _soundPlayer;
        private readonly IMusicPlayer           _musicPlayer;
        private readonly PlayniteState          _playniteState;
        private readonly PlayniteSoundsSettings _settings;

        public AppStateChangeHandler(
            IWavePlayerManager wavePlayerManager,
            ISoundPlayer soundPlayer,
            IMusicPlayer musicPlayer,
            PlayniteState playniteState,
            PlayniteSoundsSettings settings)
        {
            _wavePlayerManager = wavePlayerManager;
            _soundPlayer = soundPlayer;
            _musicPlayer = musicPlayer;
            _playniteState = playniteState;
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
                _playniteState.HasFocus = false;
                Pause();
            }
        }

        #endregion

        #region OnApplicationActivate

        public void OnApplicationActivate(object sender, EventArgs e)
        {
            if (_settings.PauseOnDeactivate && !_playniteState.HasFocus)
            {
                _playniteState.HasFocus = true;
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
                    if (!_playniteState.HasFocus)
                    {
                        _playniteState.HasFocus = true;
                        Resume();
                    }
                    break;
                case WindowState.Minimized:
                    _playniteState.HasFocus = false;
                    Pause();
                    break;
            }
        }

        #endregion

        #region Helpers

        private void Resume()
        {
            _wavePlayerManager.Init();
            if (_playniteState.GamesPlaying is 0)
                 /* Then */ _soundPlayer.Play(MainStateSettings().EnterSettings, _musicPlayer.Resume);
            else /* Then */ _musicPlayer.Resume();
        }

        private void Pause()
        {
            _musicPlayer.Pause();
            if (_playniteState.GamesPlaying is 0) /* Then */
                _soundPlayer.Play(MainStateSettings().ExitSettings, _wavePlayerManager.WavePlayer.Pause);
        }

        private UIStateSettings MainStateSettings() => _settings.ActiveModeSettings.UIStatesToSettings[UIState.Main];

        #endregion

        #endregion
    }
}
