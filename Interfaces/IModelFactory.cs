using PlayniteSounds.Models;
using PlayniteSounds.Models.UI;
using PlayniteSounds.Views.Models;
using System.Collections.Generic;

namespace PlayniteSounds.Services.UI;

public interface IModelFactory
{
    ModeSettingsModel CreateModeSettingsModel(ModeSettings settings);
    IDictionary<PlayniteEvent, SoundTypeSettingsModel> CreatePlayniteEventDictionary(ModeSettings settings);
    SoundTypeSettingsModel CreateSoundTypeSettingsModel(SoundTypeSettings settings, bool isDesktop);
    IDictionary<UIState, UIStateSettingsModel> CreateUIStateDictionary(ModeSettings settings);
}