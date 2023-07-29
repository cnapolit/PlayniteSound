using Playnite.SDK;
using PlayniteSounds.Models;
using System.Windows.Controls;

namespace PlayniteSounds.Services.State.FauxConverters
{
    public class VisibilityConverter : BaseUIStateFauxConverter<Control>, IVisibilityConverter
    {
        public VisibilityConverter(ILogger logger, IPlayniteEventHandler playniteEventHandler)
            : base(logger, playniteEventHandler) { }

        protected override void Link(Control cont, UIState state)
        {
            cont.IsVisibleChanged += (obj, args) =>
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
}
