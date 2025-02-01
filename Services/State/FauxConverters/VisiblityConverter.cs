using Playnite.SDK;
using PlayniteSounds.Models;
using System.Windows.Controls;

namespace PlayniteSounds.Services.State.FauxConverters;

public class VisibilityConverter(ILogger logger, IPlayniteEventHandler playniteEventHandler)
    : BaseUIStateFauxConverter<Control>(logger, playniteEventHandler), IVisibilityConverter
{
    protected override void Link(Control cont, UIState state)
    {
        cont.IsVisibleChanged += (_, args) =>
        {
            if (args.NewValue is true)
            {
                _playniteEventHandler.TriggerUIStateChanged(state);
            }
            else
            {
                _playniteEventHandler.TriggerRevertUIStateChanged();
            }
        };
    }
}