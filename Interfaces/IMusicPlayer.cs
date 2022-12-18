using Playnite.SDK.Models;
using System;
using System.Collections.Generic;

namespace PlayniteSounds.Services.Audio
{
    public interface IMusicPlayer : IDisposable
    {
        Game CurrentGame { get; set; }
        string CurrentMusicFile { set; }

        void Close();
        void Pause(bool gameStarted);
        void Pause(string pauser);
        void Play(IEnumerable<Game> games);
        void Resume(bool gameStopped);
        void Resume(string pauser);
        void ResetVolume();
    }
}