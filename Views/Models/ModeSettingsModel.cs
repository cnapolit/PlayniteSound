using PlayniteSounds.Common;
using PlayniteSounds.Models;
using System.Collections.Generic;

namespace PlayniteSounds.Views.Models
{
    public class ModeSettingsModel : ObservableObject
    {
        private          ModeSettings _settings;
        public ModeSettings Settings
        {
            get => _settings;
            set
            {
                if (_settings is null)
                {
                    // Allow main model to pass in value
                    _settings = value;
                }
                else
                {
                    // Copying allows changes to propagate across the plugin due to the settings being singleton
                    _settings.Copy(value);
                }

                OnPropertyChanged();
            }
        }

        public int GameUninstalledPercent
        {
            get => (int)(_settings.GameUninstalledVolume * 100);
            set
            {
                _settings.GameUninstalledVolume = value / 100.0;
                OnPropertyChanged();
            }
        }

        public int LibraryUpdatedPercent
        {
            get => (int)(_settings.LibraryUpdateVolume * 100);
            set
            {
                _settings.LibraryUpdateVolume = value / 100.0;
                OnPropertyChanged();
            }
        }

        public int GameInstalledPercent
        {
            get => (int)(_settings.GameInstalledVolume * 100);
            set
            {
                _settings.GameInstalledVolume = value / 100.0;
                OnPropertyChanged();
            }
        }

        public int GameSelectedPercent
        {
            get => (int)(_settings.GameSelectedVolume * 100);
            set
            {
                _settings.GameSelectedVolume = value / 100.0;
                OnPropertyChanged();
            }
        }

        public int GameStoppedPercent
        {
            get => (int)(_settings.GameStoppedVolume * 100);
            set
            {
                _settings.GameStoppedVolume = value / 100.0;
                OnPropertyChanged();
            }
        }

        public int GameStartedPercent
        {
            get => (int)(_settings.GameStartedVolume * 100);
            set
            {
                _settings.GameStartedVolume = value / 100.0;
                OnPropertyChanged();
            }
        }

        public int GameStartingPercent
        {
            get => (int)(_settings.GameStartingVolume * 100);
            set
            {
                _settings.GameStartingVolume = value / 100.0;
                OnPropertyChanged();
            }
        }

        public int AppStartPercent
        {
            get => (int)(_settings.AppStartVolume * 100);
            set
            {
                _settings.AppStartVolume = value / 100.0;
                OnPropertyChanged();
            }
        }

        public int AppStopPercent
        {
            get => (int)(_settings.AppStopVolume * 100);
            set
            {
                _settings.AppStopVolume = value / 100.0;
                OnPropertyChanged();
            }
        }

        public int MusicPercent
        {
            get => (int)(_settings.MusicVolume * 100);
            set
            {
                _settings.MusicVolume = value / 100.0;
                OnPropertyChanged();
            }
        }
    }
}
