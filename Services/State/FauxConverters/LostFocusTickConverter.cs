using Playnite.SDK;
using PlayniteSounds.Models;
using PlayniteSounds.Models.State;
using PlayniteSounds.Services.Audio;
using System.Windows;

namespace PlayniteSounds.Services.State.FauxConverters;

public class LostFocusTickConverter(
    ILogger logger,
    IPlayniteEventHandler playniteEventHandler,
    ISoundPlayer soundPlayer,
    PlayniteState playniteState)
    : BaseUIStateFauxConverter<UIElement>(logger, playniteEventHandler), ILostFocusTickConverter
{
    protected override void Link(UIElement ui, UIState uiState)
    {
        ui.LostFocus += (_, _) =>
        {
            if (playniteState.CurrentUIState == uiState)
            {
                soundPlayer.Tick();
            }
        };
    }
}