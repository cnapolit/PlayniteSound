using Playnite.SDK;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using System.Collections.Generic;
using System.Windows.Controls;

namespace PlayniteSounds.Services.State.FauxConverters
{
    public class ButtonLoadConverter : BaseUIStateFauxConverter<Button>, IButtonLoadConverter
    {
        private readonly ISoundPlayer _soundPlayer;
        private readonly IDictionary<UIState, bool> _linkedStates;

        public ButtonLoadConverter(
            ILogger logger, IPlayniteEventHandler playniteEventHandler, ISoundPlayer soundPlayer) 
            : base(logger, playniteEventHandler)
        {
            _soundPlayer = soundPlayer;
            _linkedStates = new Dictionary<UIState, bool>
            {
                [UIState.GameMenu]      = false,
                [UIState.FilterPresets] = false
            };
        }

        protected override void Link(Button butt, UIState uiState)
        {
            butt.LostFocus += (_, __) => _soundPlayer.Tick();
            if (!_linkedStates[UIState.FilterPresets])
            {
                var settingsName = butt.DataContext?.
                    GetType().GetProperty("Value")?.
                    PropertyType.GetProperty("Settings")?.
                    PropertyType.Name;
                if (settingsName != null && settingsName.StartsWith("FilterPresetSettings"))
                {
                    _linkedStates[UIState.FilterPresets] = true;
                    butt.Loaded += (_, __) =>
                    {
                        _playniteEventHandler.TriggerUIStateChanged(UIState.FilterPresets);
                    };
                    butt.Unloaded += (_, __) =>
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
                    butt.Loaded += (_, __) =>
                    {
                        _playniteEventHandler.TriggerUIStateChanged(UIState.GameMenu);
                    };
                    butt.Unloaded += (_, __) =>
                    {
                        _linkedStates[UIState.GameMenu] = false;
                        _playniteEventHandler.TriggerRevertUIStateChanged();
                    };
                }
            }
        }
    }
}
