using Playnite.SDK;
using PlayniteSounds.Models;
using System.Windows.Controls;

namespace PlayniteSounds.Services.State.FauxConverters;

public class ButtonConverter(ILogger logger, IPlayniteEventHandler playniteEventHandler)
    : BaseUIStateFauxConverter<Button>(logger, playniteEventHandler), IButtonConverter
{
    protected override void Link(Button butt, UIState uiState)
    {
        butt.Click += (_, _) =>
        {
            _playniteEventHandler.TriggerUIStateChanged(uiState);
        };
        butt.GotFocus += (_, _) =>
        {

        };
    }
}