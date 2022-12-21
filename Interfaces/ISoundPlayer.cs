using System;

namespace PlayniteSounds.Services.Audio
{
    public interface ISoundPlayer
    {
        void PlayAppStarted(EventHandler mediaEndedHandler);
        void PlayAppStopped();
        void PlayGameSelected();
        void PlayGameInstalled();
        void PlayGameUnInstalled();
        void PlayGameStarting();
        void PlayGameStarted();
        void PlayGameStopped();
        void PlayLibraryUpdated();
        void Close();
    }
}
