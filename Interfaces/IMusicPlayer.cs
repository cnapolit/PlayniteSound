using System;
using System.IO;

namespace PlayniteSounds.Services.Audio;

public interface IMusicPlayer
{
    long     Position          { get; set; }
    long     Length            { get; }
    long     LengthInSeconds   { get; }
    long     PositionInSeconds { get; }
    TimeSpan CurrentTime       { get; }
    TimeSpan TotalTime         { get; }

    void Initialize();
    void Pause(bool gameStarted);
    void Pause(string pauser);
    void Preview();
    void Resume(bool gameStopped);
    void Resume(string pauser);
    void SetVolume(float? volume = null);
    void Play(string filePath);
    void Toggle();
    void Play(Stream stream);
    void Stop();
    void Resume();
    void Pause();
}