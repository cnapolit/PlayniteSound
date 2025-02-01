using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;

namespace PlayniteSounds.Views.Models;

public class SoundTypeSettingsModel(ISoundPlayer soundPlayer, SoundTypeSettings settings, bool isDesktop)
    : BaseSettingsModel
{
    private SoundTypeSettings _settings = settings;
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

    public void Preview() => soundPlayer.Preview(_settings, isDesktop);
}