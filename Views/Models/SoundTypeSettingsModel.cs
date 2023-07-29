using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;

namespace PlayniteSounds.Views.Models
{
    public class SoundTypeSettingsModel : BaseSettingsModel
    {
        private readonly ISoundPlayer _soundPlayer;
        private readonly bool _isDesktop;

        private SoundTypeSettings _settings;
        public SoundTypeSettings Settings
        {
            get => _settings;
            set => UpdateSettings(ref _settings, value);
        }

        public int VolumePercent
        {
            get => ConvertFromVolume(_settings.Volume);
            set
            {
                _settings.Volume = ConvertToVolume(value);
                OnPropertyChanged();
            }
        }

        public SoundTypeSettingsModel(ISoundPlayer soundPlayer, SoundTypeSettings settings, bool isDesktop)
        {
            _soundPlayer = soundPlayer;
            _settings = settings;
            _isDesktop = isDesktop;
        }

        public void Preview() => _soundPlayer.Preview(_settings, _isDesktop);
    }
}
