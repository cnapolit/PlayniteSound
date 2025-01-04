using Playnite.SDK;
using PlayniteSounds.Models;
using System.Linq;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System;
using PlayniteSounds.Models.State;
using PlayniteSounds.Models.UI;

namespace PlayniteSounds.Services.State
{
    public class PlayniteEventHandler : IPlayniteEventHandler
    {
        #region Infrastructure

        private readonly IMainViewAPI           _mainViewAPI;
        private readonly PlayniteSoundsSettings _settings;
        private readonly PlayniteState _playniteState;
        private readonly object _stateLock = new object();

        public event EventHandler<UIStateChangedArgs>        UIStateChanged;
        public event EventHandler<PlayniteEventOccurredArgs> PlayniteEventOccurred;

        public PlayniteEventHandler(
            IMainViewAPI mainViewAPI, PlayniteState playniteState, PlayniteSoundsSettings settings)
        {
            _mainViewAPI = mainViewAPI;
            _playniteState = playniteState;
            _settings = settings;
        }

        #endregion

        #region Implementation

        public void OnApplicationStarted() => TriggerPlayniteEventOccurred(PlayniteEvent.AppStarted);
        public void OnApplicationStopped() => TriggerPlayniteEventOccurred(PlayniteEvent.AppStopped);
        public void OnLibraryUpdated()  => TriggerPlayniteEventOccurred(PlayniteEvent.LibraryUpdated);
        public void OnGameDetailsEntered() => TriggerUIStateChanged(UIState.GameDetails);
        public void OnMainViewEntered() => TriggerUIStateChanged(UIState.Main);
        public void OnSettingsEntered() => TriggerUIStateChanged(UIState.Settings);
        public void OnGameInstalled(Game game) => TriggerPlayniteEventOccurred(PlayniteEvent.GameInstalled, game);
        public void OnGameUninstalled(Game game) => TriggerPlayniteEventOccurred(PlayniteEvent.GameUninstalled, game);
        public void OnGameStarted(Game game) => TriggerPlayniteEventOccurred(PlayniteEvent.GameStarted, game);

        public void OnGameStarting(Game game)
        {
            // Only trigger if another plugin has not already informed of starting event
            if (_playniteState.GameIsStarting(game.Id)) /* Then */
                TriggerPlayniteEventOccurred(PlayniteEvent.GameStarting, game);
        }

        public void OnGameStopped(Game game)
        {
            if (_playniteState.GameHasEnded(game.Id)) /* Then */
                TriggerPlayniteEventOccurred(PlayniteEvent.GameStopped, game);
        }

        public void OnGameSelected(IList<Game> games)
            => TriggerPlayniteEventOccurred(PlayniteEvent.GameSelected, games ?? new List<Game>());

        public void TriggerUIStateChanged(UIState newState)
        {
            if (newState is UIState.GameMenu) /* Then */ newState |= _playniteState.CurrentUIState;

            if (newState == _playniteState.CurrentUIState) /* Then */ return;

            UIStateChangedArgs args;
            lock (_stateLock)
            {
                _playniteState.PreviousUIState = _playniteState.CurrentUIState;
                _playniteState.CurrentUIState = newState;
                args = CreateUIStateChangedArgs();
            }
            UIStateChanged(this, args);
        }

        public void TriggerRevertUIStateChanged()
        {
            UIStateChangedArgs args;
            lock (_stateLock)
            {
                // Swap
                (_playniteState.CurrentUIState, _playniteState.PreviousUIState)
                                       = (_playniteState.PreviousUIState, _playniteState.CurrentUIState);
                args = CreateUIStateChangedArgs();
            }
            UIStateChanged(this, args);
        }

        #region Helpers

        private UIStateChangedArgs CreateUIStateChangedArgs() => new UIStateChangedArgs
        {
            Game = _mainViewAPI.SelectedGames.FirstOrDefault(),
            OldState = _playniteState.PreviousUIState,
            NewState = _playniteState.CurrentUIState,
            OldSettings = _settings.ActiveModeSettings.UIStatesToSettings[_playniteState.PreviousUIState],
            NewSettings = _settings.ActiveModeSettings.UIStatesToSettings[_playniteState.CurrentUIState]
        };

        private void TriggerPlayniteEventOccurred(PlayniteEvent playniteEvent)
            => TriggerPlayniteEventOccurred(playniteEvent, _mainViewAPI.SelectedGames?.FirstOrDefault());

        private void TriggerPlayniteEventOccurred(PlayniteEvent playniteEvent, Game game)
            => TriggerPlayniteEventOccurred(playniteEvent, game is null ? new Game[] { } : new[] { game });


        private void TriggerPlayniteEventOccurred(PlayniteEvent playniteEvent, IList<Game> games)
        {
            PlayniteEventOccurredArgs args;
            lock (_stateLock)
            {
                _playniteState.PreviousGame = _playniteState.CurrentGame;
                _playniteState.CurrentGame = games.FirstOrDefault();
                args = new PlayniteEventOccurredArgs
                {
                    Event = playniteEvent,
                    SoundTypeSettings = _settings.ActiveModeSettings.PlayniteEventToSoundTypesSettings[playniteEvent],
                    Games = games
                };
            }
            PlayniteEventOccurred.Invoke(this, args);
        }   

        #endregion

        #endregion
    }
}
