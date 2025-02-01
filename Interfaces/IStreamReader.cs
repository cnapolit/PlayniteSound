using System;

namespace PlayniteSounds.Models.Audio.SampleProviders;

internal interface IStreamReader : IDisposable
{
    long            Position    { get; set; }
    long            Length      { get; }
    string          FileName    { get; }
    public TimeSpan CurrentTime { get; }
    public TimeSpan TotalTime   { get; }
}