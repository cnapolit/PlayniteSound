using System.Collections.Generic;

namespace PlayniteSounds.Models
{
    public class ModeSettings : ObservableObject
    {
        public double AppStartVolume { get; set; } = .5;
        public double AppStopVolume { get; set; } = .5;
        public double GameStartingVolume { get; set; } = .5;
        public double GameStartedVolume { get; set; } = .5;
        public double GameStoppedVolume { get; set; } = .5;
        public double LibraryUpdateVolume { get; set; } = .5;
        public double GameSelectedVolume { get; set; } = .5;
        public double GameInstalledVolume { get; set; } = .5;
        public double GameUninstalledVolume { get; set; } = .5;
        public bool PlayAppStart { get; set; } = true;
        public bool PlayAppStop { get; set; } = true;
        public bool PlayGameStarting { get; set; } = true;
        public bool PlayGameStarted { get; set; }
        public bool PlayGameStopped { get; set; } = true;
        public bool PlayLibraryUpdate { get; set; } = true;
        public bool PlayGameSelected { get; set; } = true;
        public bool PlayGameInstalled { get; set; } = true;
        public bool PlayGameUninstalled { get; set; } = true;
        public bool IsThemeControlled { get; set; } = false;

        private double _musicVolume = .25;
        public double MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = value;
                OnPropertyChanged();
            }
        }
    }
}
