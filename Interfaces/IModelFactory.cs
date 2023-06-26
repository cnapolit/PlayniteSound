using PlayniteSounds.Models;
using PlayniteSounds.Views.Models;
using System.Collections.Generic;

namespace PlayniteSounds.Services.UI
{
    public interface IModelFactory
    {
        ModeSettingsModel CreateModeSettingsModel(ModeSettings settings);
        IDictionary<UIState, UIStateSettingsModel> CreateUIStateDictionary(ModeSettings settings);
    }
}