using Playnite.SDK.Models;
using System.Collections.Generic;

namespace PlayniteSounds.Services.Audio
{
    public interface IMusicPlayer
    {
        void Initialize();
        void Pause(bool gameStarted);
        void Pause(string pauser);
        void Preview();
        void Resume(bool gameStopped);
        void Resume(string pauser);
        void SetVolume(float? volume = null);
        void SetMusicFile(string filePath);
        void Resume();
        void Pause();
    }
}