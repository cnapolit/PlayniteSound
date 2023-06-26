using PlayniteSounds.Models;
using PlayniteSounds.Services.UI;
using System.Collections.Generic;

namespace PlayniteSounds.Views.Models
{
    public class ModeSettingsModel : BaseSettingsModel
    {
        public IDictionary<UIState, UIStateSettingsModel> UIStatesToSettingsModels { get; }

        private ModeSettings _settings;
        public ModeSettings Settings
        {
            get => _settings;
            set => UpdateSettings(ref _settings, value);
        }

        public ModeSettingsModel(IModelFactory uiStateModelFactory, ModeSettings settings)
        {
            UIStatesToSettingsModels = uiStateModelFactory.CreateUIStateDictionary(settings);
            _settings = settings;
        }
    }
}
