using Playnite.SDK;
using PlayniteSounds.Models;
using System.Linq;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using PlayniteSounds.Common.Constants;
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
        public  static   UIState                CurrentState = UIState.Main;
        private static   UIState                _oldState     = UIState.Main;

        public event EventHandler<UIStateChangedArgs>        UIStateChanged;
        public event EventHandler<PlayniteEventOccurredArgs> PlayniteEventOccurred;

        public PlayniteEventHandler(
            IMainViewAPI mainViewAPI,
            IUriHandlerAPI uriHandlerAPI,
            PlayniteSoundsSettings settings)
        {
            _mainViewAPI = mainViewAPI;
            _settings = settings;
            uriHandlerAPI.RegisterSource(App.SourceName, HandleUriEvent);
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
        public void OnGameStarting(Game game) => TriggerPlayniteEventOccurred(PlayniteEvent.GameStarting, game);
        public void OnGameStopped(Game game) => TriggerPlayniteEventOccurred(PlayniteEvent.GameStopped, game);
        public void OnGameSelected(IList<Game> games)
            => TriggerPlayniteEventOccurred(PlayniteEvent.GameSelected, games ?? new List<Game> { });

        public void TriggerUIStateChanged(UIState newState)
        {
            if (newState is UIState.GameMenu) /* Then */ newState |= CurrentState;

            if (newState == CurrentState) /* Then */ return;

            _oldState = CurrentState;
            CurrentState = newState;
            var args = new UIStateChangedArgs
            {
                Game = _mainViewAPI.SelectedGames.FirstOrDefault(),
                OldState = _oldState,
                NewState = CurrentState,
                OldSettings = _settings.ActiveModeSettings.UIStatesToSettings[_oldState],
                NewSettings = _settings.ActiveModeSettings.UIStatesToSettings[CurrentState]
            };
            UIStateChanged(this, args);
        }

        public void TriggerRevertUIStateChanged()
        {
            var args = new UIStateChangedArgs
            {
                Game = _mainViewAPI.SelectedGames.FirstOrDefault(),
                OldState = CurrentState,
                NewState = _oldState,
                OldSettings = _settings.ActiveModeSettings.UIStatesToSettings[CurrentState],
                NewSettings = _settings.ActiveModeSettings.UIStatesToSettings[_oldState]
            };
            CurrentState = _oldState;
            UIStateChanged(this, args);
        }

        #region Helpers

        #region Callback Methods

        // ex: playnite://Sounds/Play/someId
        // Sounds maintains a list of plugins who want the music paused and will only allow play when
        // no other plugins have paused.
        private void HandleUriEvent(PlayniteUriEventArgs args)
        {
            var action = args.Arguments[0];
            var senderId = args.Arguments[1];

            switch (action.ToLower())
            {
                //case "play": _musicPlayer.Resume(senderId); break;
                //case "pause": _musicPlayer.Pause(senderId); break;
            }
        }

        #endregion

        private void TriggerPlayniteEventOccurred(PlayniteEvent playniteEvent)
            => TriggerPlayniteEventOccurred(playniteEvent, _mainViewAPI.SelectedGames?.FirstOrDefault());

        private void TriggerPlayniteEventOccurred(PlayniteEvent playniteEvent, Game game)
            => TriggerPlayniteEventOccurred(playniteEvent, game is null ? new Game[] { } : new[] { game });


        private void TriggerPlayniteEventOccurred(PlayniteEvent playniteEvent, IList<Game> games)
        {
            var args = new PlayniteEventOccurredArgs
            {
                Event = playniteEvent,
                SoundTypeSettings = _settings.ActiveModeSettings.PlayniteEventToSoundTypesSettings[playniteEvent],
                Games = games
            };
            PlayniteEventOccurred.Invoke(this, args);
        }   

        #endregion

        #endregion
    }
}
