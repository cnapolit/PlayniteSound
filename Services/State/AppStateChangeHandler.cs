using Microsoft.Win32;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using System;
using System.Windows;

namespace PlayniteSounds.Services.State
{
    internal class AppStateChangeHandler : IAppStateChangeHandler
    {
        #region Infrastructure

        private readonly PlayniteSoundsSettings _settings;
        private readonly IMusicPlayer           _musicPlayer;
        private readonly ISoundPlayer           _soundPlayer;
        private readonly IErrorHandler          _errorHandler;

        public AppStateChangeHandler(
            IMusicPlayer musicPlayer,
            ISoundPlayer soundPlayer,
            IErrorHandler errorHandler,
            PlayniteSoundsSettings settings)
        {
            _musicPlayer = musicPlayer;
            _soundPlayer = soundPlayer;
            _errorHandler = errorHandler;
            _settings = settings;
        }

        #endregion

        #region Implementation

        #region OnPowerModeChanged

        public void OnPowerModeChanged(object sender, PowerModeChangedEventArgs args)
        {
            if (args.Mode is PowerModes.Resume)
            {
                //fix sounds not playing after system resume
                _errorHandler.Try(_soundPlayer.Close);

                if (ShouldRestartOnResume())
                {
                    _errorHandler.Try(() => _musicPlayer.Resume(false));
                }
            }
        }

        private bool ShouldRestartOnResume()
            => _settings.PauseOnDeactivate && Application.Current?.MainWindow?.WindowState != WindowState.Minimized;

        #endregion

        #region OnApplicationDeactivate

        public void OnApplicationDeactivate(object sender, EventArgs e)
        {
            if (_settings.PauseOnDeactivate)
            {
                _musicPlayer.Pause(false);
            }
        }

        #endregion

        #region OnApplicationActivate

        public void OnApplicationActivate(object sender, EventArgs e)
        {
            if (_settings.PauseOnDeactivate)
            {
                _musicPlayer.Resume(false);
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
                        _musicPlayer.Resume(false);
                        break;
                    case WindowState.Minimized:
                        _musicPlayer.Pause(false);
                        break;
                }
            }
        }

        #endregion

        #endregion
    }
}
