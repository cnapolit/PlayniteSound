using PlayniteSounds.Models;
using System.Windows;
using Playnite.SDK;

namespace PlayniteSounds.Services.State.FauxConverters
{
    public class FocusConverter : BaseUIStateFauxConverter<UIElement>, IFocusConverter
    {
        public FocusConverter(ILogger logger, IPlayniteEventHandler playniteEventHandler) 
            : base(logger, playniteEventHandler) { }

        protected override void Link(UIElement ui, UIState uiState)
        {
            ui.GotFocus += (_, __) =>
            {
                _playniteEventHandler.TriggerUIStateChanged(uiState);
            };
            //ui.LostFocus += (_, __) =>
            //{
            //    _playniteEventHandler.TriggerRevertUIStateChanged();
            //};
        }
    }
}
