using Playnite.SDK;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using System.Windows;

namespace PlayniteSounds.Services.State.FauxConverters
{
    public class LostFocusTickConverter : BaseUIStateFauxConverter<UIElement>, ILostFocusTickConverter
    {
        private readonly ISoundPlayer _soundPlayer;
        public LostFocusTickConverter(
            ILogger logger,
            IPlayniteEventHandler playniteEventHandler,
            ISoundPlayer soundPlayer)
            : base(logger, playniteEventHandler) => _soundPlayer = soundPlayer;

        protected override void Link(UIElement ui, UIState uiState)
        {
            ui.LostFocus += (_, __) =>
            {
                if (PlayniteEventHandler.CurrentState == uiState)
                {
                    _soundPlayer.Tick();
                }
            };
        }
    }
}
