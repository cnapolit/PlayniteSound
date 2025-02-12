using Playnite.SDK;
using PlayniteSounds.Models;
using System.Linq;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System;
using PlayniteSounds.Models.State;
using PlayniteSounds.Models.UI;

namespace PlayniteSounds.Services.State;

public class PlayniteEventHandler(
    IMainViewAPI mainViewApi,
    PlayniteState playniteState,
    PlayniteSoundsSettings settings)
    : IPlayniteEventHandler
{
    #region Infrastructure

    private readonly object _stateLock = new();

    public event EventHandler<UIStateChangedArgs>        UIStateChanged;
    public event EventHandler<PlayniteEventOccurredArgs> PlayniteEventOccurred;

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
        if (playniteState.GameIsStarting(game.Id))
        /* Then */ TriggerPlayniteEventOccurred(PlayniteEvent.GameStarting, game);
    }

    public void OnGameStopped(Game game)
    {
        if (playniteState.GameHasEnded(game.Id))
        /* Then */ TriggerPlayniteEventOccurred(PlayniteEvent.GameStopped, game);
    }

    public void OnGameSelected(IList<Game> games)
        => TriggerPlayniteEventOccurred(PlayniteEvent.GameSelected, games ?? []);

    public void TriggerUIStateChanged(UIState newState)
    {
        if (newState is UIState.GameMenu) /* Then */ newState |= playniteState.CurrentUIState;

        if (newState == playniteState.CurrentUIState) /* Then */ return;

        UIStateChangedArgs args;
        lock (_stateLock)
        {
            playniteState.PreviousUIState = playniteState.CurrentUIState;
            playniteState.CurrentUIState = newState;
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
            (playniteState.CurrentUIState, playniteState.PreviousUIState)
                = (playniteState.PreviousUIState, playniteState.CurrentUIState);
            args = CreateUIStateChangedArgs();
        }
        UIStateChanged(this, args);
    }

    #region Helpers

    private UIStateChangedArgs CreateUIStateChangedArgs() => new()
    {
        Game = mainViewApi.SelectedGames.FirstOrDefault(),
        OldState = playniteState.PreviousUIState,
        NewState = playniteState.CurrentUIState,
        OldSettings = settings.ActiveModeSettings.UIStatesToSettings[playniteState.PreviousUIState],
        NewSettings = settings.ActiveModeSettings.UIStatesToSettings[playniteState.CurrentUIState]
    };

    private void TriggerPlayniteEventOccurred(PlayniteEvent playniteEvent)
        => TriggerPlayniteEventOccurred(playniteEvent, mainViewApi.SelectedGames?.FirstOrDefault());

    private void TriggerPlayniteEventOccurred(PlayniteEvent playniteEvent, Game game)
        => TriggerPlayniteEventOccurred(playniteEvent, game is null ? new Game[] { } : new[] { game });


    private void TriggerPlayniteEventOccurred(PlayniteEvent playniteEvent, IList<Game> games)
    {
        PlayniteEventOccurredArgs args;
        lock (_stateLock)
        {
            playniteState.PreviousGame = playniteState.CurrentGame;
            playniteState.CurrentGame = games.FirstOrDefault();
            args = new PlayniteEventOccurredArgs
            {
                Event = playniteEvent,
                SoundTypeSettings = settings.ActiveModeSettings.PlayniteEventToSoundTypesSettings[playniteEvent],
                Games = games
            };
        }
        PlayniteEventOccurred.Invoke(this, args);
    }   

    #endregion

    #endregion
}