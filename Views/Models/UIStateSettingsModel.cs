using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using System.Collections.Generic;

namespace PlayniteSounds.Views.Models
{
    public class UIStateSettingsModel : BaseSettingsModel
    {
        private readonly IMusicPlayer _musicPlayer;
        public IDictionary<SoundType, SoundTypeSettingsModel> SoundTypesToSettingsModels { get; }

        private UIStateSettings _settings;
        public UIStateSettings Settings
        {
            get => _settings;
            set => UpdateSettings(ref _settings, value);
        }

        public int MusicVolumePercent
        {
            get => ConvertFromVolume(_settings.MusicVolume);
            set
            {
                _settings.MusicVolume = ConvertToVolume(value);
                OnPropertyChanged();
            }
        }

        public UIStateSettingsModel(
            IMusicPlayer musicPlayer,
            IDictionary<SoundType, SoundTypeSettingsModel> soundTypesToSettingsModels,
            UIStateSettings settings)
        {
            _musicPlayer = musicPlayer;
            _settings = settings;
            SoundTypesToSettingsModels = soundTypesToSettingsModels;
        }

        public void SetMusicVolume(double value)
            => _musicPlayer.SetVolume((float)value / 100f);
    }
}
