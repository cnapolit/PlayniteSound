using Playnite.SDK.Models;

namespace PlayniteSounds.Services.State
{
    internal interface IPlayniteEventHandler
    {
        void OnApplicationStarted();
        void OnApplicationStopped();
        void OnGameInstalled();
        void OnGameSelected();
        void OnGameStarted();
        void OnGameStarting(Game game);
        void OnGameStopped();
        void OnGameUninstalled();
        void OnLibraryUpdated();
    }
}