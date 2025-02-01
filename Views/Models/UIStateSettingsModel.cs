using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.UI;

namespace PlayniteSounds.Views.Models;

public class UIStateSettingsModel(
    bool isDesktop,
    IModelFactory modelFactory,
    IMusicPlayer musicPlayer,
    UIStateSettings settings)
    : BaseSettingsModel
{
    private UIStateSettings _settings = settings;
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

    public SoundTypeSettingsModel EnterSettingsModel { get; } = modelFactory.CreateSoundTypeSettingsModel(settings.EnterSettings, isDesktop);
    public SoundTypeSettingsModel ExitSettingsModel { get; } = modelFactory.CreateSoundTypeSettingsModel(settings.ExitSettings, isDesktop);
    public SoundTypeSettingsModel TickSettingsModel { get; } = modelFactory.CreateSoundTypeSettingsModel(settings.TickSettings, isDesktop);

    public void SetMusicVolume(double value)
        => musicPlayer.SetVolume((float)value / 100f);
}