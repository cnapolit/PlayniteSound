namespace PlayniteSounds.Services.State
{
    internal interface IPlayniteEventHandler
    {
        void OnApplicationStarted();
        void OnApplicationStopped();
        void OnGameInstalled();
        void OnGameSelected();
        void OnGameStarted();
        void OnGameStarting();
        void OnGameStopped();
        void OnGameUninstalled();
        void OnLibraryUpdated();
    }
}