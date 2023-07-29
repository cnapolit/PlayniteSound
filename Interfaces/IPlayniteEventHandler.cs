using Playnite.SDK.Models;
using PlayniteSounds.Models;
using PlayniteSounds.Models.State;
using System;
using System.Collections.Generic;

namespace PlayniteSounds.Services.State
{
    public interface IPlayniteEventHandler
    {
        event EventHandler<UIStateChangedArgs> UIStateChanged;
        event EventHandler<PlayniteEventOccurredArgs> PlayniteEventOccurred;

        void OnApplicationStarted();
        void OnApplicationStopped();
        void OnGameDetailsEntered();
        void OnGameInstalled(Game game);
        void OnGameSelected(IList<Game> games);
        void OnGameStarted(Game game);
        void OnGameStarting(Game game);
        void OnGameStopped(Game game);
        void OnGameUninstalled(Game game);
        void OnLibraryUpdated();
        void OnMainViewEntered();
        void OnSettingsEntered();
        void TriggerRevertUIStateChanged();
        void TriggerUIStateChanged(UIState newState);
    }
}