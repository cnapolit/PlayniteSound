using Playnite.SDK.Models;
using System.Collections.Generic;

namespace PlayniteSounds.Services.Audio
{
    public interface IMusicPlayer
    {
        Game CurrentGame { get; set; }
        string CurrentMusicFile { set; }
        bool StartSoundFinished { get; set; }

        void Pause(bool gameStarted);
        void Pause(string pauser);
        void Play(IEnumerable<Game> games);
        void Preview();
        void Resume(bool gameStopped);
        void Resume(string pauser);
        void SetVolume(float? volume = null);
        void UIStateChanged();
    }
}