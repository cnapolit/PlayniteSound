using PlayniteSounds.Models;
using System.Windows;
using Playnite.SDK;

namespace PlayniteSounds.Services.State.FauxConverters;

public class FocusConverter(ILogger logger, IPlayniteEventHandler playniteEventHandler)
    : BaseUIStateFauxConverter<UIElement>(logger, playniteEventHandler), IFocusConverter
{
    protected override void Link(UIElement ui, UIState uiState)
    {
        ui.GotFocus += (_, _) =>
        {
            _playniteEventHandler.TriggerUIStateChanged(uiState);
        };
        //ui.LostFocus += (_, __) =>
        //{
        //    _playniteEventHandler.TriggerRevertUIStateChanged();
        //};
    }
}