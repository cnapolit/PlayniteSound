using Playnite.SDK.Models;
using PlayniteSounds.Models;
using System.Collections.Generic;

namespace PlayniteSounds.Services.Audio
{
    public interface IMusicPlayer
    {
        Game CurrentGame { get; set; }
        string CurrentMusicFile { set; }
        UIState UIState { get; set; }

        void Close();
        void Pause(bool gameStarted);
        void Pause(string pauser);
        void Play(IEnumerable<Game> games);
        void Resume(bool gameStopped);
        void Resume(string pauser);
        void SetVolume(double volume);
    }
}