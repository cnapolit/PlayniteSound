using Playnite.SDK;
using PlayniteSounds.Models;
using PlayniteSounds.Models.State;
using PlayniteSounds.Services.Audio;
using System.Windows;

namespace PlayniteSounds.Services.State.FauxConverters
{
    public class LostFocusTickConverter : BaseUIStateFauxConverter<UIElement>, ILostFocusTickConverter
    {
        private readonly ISoundPlayer _soundPlayer;
        private readonly PlayniteState _playniteState;
        public LostFocusTickConverter(
            ILogger logger,
            IPlayniteEventHandler playniteEventHandler,
            ISoundPlayer soundPlayer,
            PlayniteState playniteState)
            : base(logger, playniteEventHandler)
        {
            _soundPlayer = soundPlayer;
            _playniteState = playniteState;
        }

        protected override void Link(UIElement ui, UIState uiState)
        {
            ui.LostFocus += (_, __) =>
            {
                if (_playniteState.CurrentUIState == uiState)
                {
                    _soundPlayer.Tick();
                }
            };
        }
    }
}
