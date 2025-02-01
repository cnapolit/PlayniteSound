using Playnite.SDK;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using System.Collections.Generic;
using System.Windows.Controls;

namespace PlayniteSounds.Services.State.FauxConverters;

public class ButtonLoadConverter(ILogger logger, IPlayniteEventHandler playniteEventHandler, ISoundPlayer soundPlayer)
    : BaseUIStateFauxConverter<Button>(logger, playniteEventHandler), IButtonLoadConverter
{
    private readonly IDictionary<UIState, bool> _linkedStates = new Dictionary<UIState, bool>
    {
        [UIState.GameMenu]      = false,
        [UIState.FilterPresets] = false
    };

    protected override void Link(Button butt, UIState uiState)
    {
        butt.LostFocus += (_, _) => soundPlayer.Tick();
        if (!_linkedStates[UIState.FilterPresets])
        {
            var settingsName = butt.DataContext?.
                GetType().GetProperty("Value")?.
                PropertyType.GetProperty("Settings")?.
                PropertyType.Name;
            if (settingsName != null && settingsName.StartsWith("FilterPresetSettings"))
            {
                _linkedStates[UIState.FilterPresets] = true;
                butt.Loaded += (_, _) =>
                {
                    _playniteEventHandler.TriggerUIStateChanged(UIState.FilterPresets);
                };
                butt.Unloaded += (_, _) =>
                {
                    _linkedStates[UIState.FilterPresets] = false;
                    _playniteEventHandler.TriggerRevertUIStateChanged();
                };
                return;
            }
        }

        if (!_linkedStates[UIState.GameMenu])
        {
            var dataContextName = butt.DataContext?.GetType().Name;
            if (dataContextName != null && dataContextName.StartsWith("GameActionItem"))
            {
                _linkedStates[UIState.GameMenu] = true;
                butt.Loaded += (_, _) =>
                {
                    _playniteEventHandler.TriggerUIStateChanged(UIState.GameMenu);
                };
                butt.Unloaded += (_, _) =>
                {
                    _linkedStates[UIState.GameMenu] = false;
                    _playniteEventHandler.TriggerRevertUIStateChanged();
                };
            }
        }
    }
}