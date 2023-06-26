using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteSounds.Models;
using System;
using System.Collections.Generic;

namespace PlayniteSounds.Services.State
{
    public interface IPlayniteEventHandler
    {
        void OnApplicationStarted(List<Plugin> plugins);
        void OnApplicationStopped();
        void OnGameDetailsEntered();
        void OnGameInstalled();
        void OnGameSelected();
        void OnGameStarted();
        void OnGameStarting(Game game);
        void OnGameStopped();
        void OnGameUninstalled();
        void OnLibraryUpdated(Action<PlayniteSoundsSettings> saveAction);
        void OnMainViewEntered();
        void OnSettingsEntered();
    }
}