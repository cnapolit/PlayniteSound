using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.UI;

namespace PlayniteSounds.Views.Models
{
    public class UIStateSettingsModel : BaseSettingsModel
    {
        private readonly IMusicPlayer _musicPlayer;

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

        public SoundTypeSettingsModel EnterSettingsModel { get; }
        public SoundTypeSettingsModel ExitSettingsModel { get; }
        public SoundTypeSettingsModel TickSettingsModel { get; }

        public UIStateSettingsModel(
            bool isDesktop,
            IModelFactory modelFactory,
            IMusicPlayer musicPlayer,
            UIStateSettings settings)
        {
            EnterSettingsModel = modelFactory.CreateSoundTypeSettingsModel(settings.EnterSettings, isDesktop);
            ExitSettingsModel = modelFactory.CreateSoundTypeSettingsModel(settings.ExitSettings, isDesktop);
            TickSettingsModel = modelFactory.CreateSoundTypeSettingsModel(settings.TickSettings, isDesktop);
            _musicPlayer = musicPlayer;
            _settings = settings;
        }

        public void SetMusicVolume(double value)
            => _musicPlayer.SetVolume((float)value / 100f);
    }
}
