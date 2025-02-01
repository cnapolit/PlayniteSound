using PlayniteSounds.Models;
using PlayniteSounds.Models.UI;
using PlayniteSounds.Services.UI;
using System.Collections.Generic;

namespace PlayniteSounds.Views.Models;

public class ModeSettingsModel(IModelFactory modelFactory, ModeSettings settings) : BaseSettingsModel
{
    public IDictionary<UIState, UIStateSettingsModel> UIStatesToSettingsModels { get; } = modelFactory.CreateUIStateDictionary(settings);
    public IDictionary<PlayniteEvent, SoundTypeSettingsModel> PlayniteEventsToSettingsModel { get; } = modelFactory.CreatePlayniteEventDictionary(settings);

    private ModeSettings _settings = settings;
    public ModeSettings Settings
    {
        get => _settings;
        set => UpdateSettings(ref _settings, value);
    }

    public int MusicMasterVolumePercent
    {
        get => ConvertFromVolume(_settings.MusicMasterVolume);
        set
        {
            _settings.MusicMasterVolume = ConvertToVolume(value);
            OnPropertyChanged();
        }
    }

    public int SoundMasterVolumePercent
    {
        get => ConvertFromVolume(_settings.SoundMasterVolume);
        set
        {
            _settings.SoundMasterVolume = ConvertToVolume(value);
            OnPropertyChanged();
        }
    }
}