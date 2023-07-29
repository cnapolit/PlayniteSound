using Playnite.SDK;
using PlayniteSounds.Models;
using System.Windows.Controls;

namespace PlayniteSounds.Services.State.FauxConverters
{


    public class ButtonConverter : BaseUIStateFauxConverter<Button>, IButtonConverter
    {
        public ButtonConverter(ILogger logger, IPlayniteEventHandler playniteEventHandler) 
            : base(logger, playniteEventHandler) { }

        protected override void Link(Button butt, UIState uiState)
        {
            butt.Click += (_, __) =>
            {
                _playniteEventHandler.TriggerUIStateChanged(uiState);
            };
            butt.GotFocus += (_, __) =>
            {

            };
        }
    }
}
