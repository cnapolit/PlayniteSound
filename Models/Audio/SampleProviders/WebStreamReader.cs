using System;
using NAudio.Wave;

namespace PlayniteSounds.Models.Audio.SampleProviders;

internal class WebStreamReader(WaveStream stream) : IStreamReader
{
    public TimeSpan CurrentTime => stream.CurrentTime;
    public TimeSpan TotalTime => stream.TotalTime;

    public long Position { get => stream.Position; set => stream.Position = value; }

    public long Length => stream.Length;

    public string FileName => null;

    public void Dispose() { /* stream should be disposed by the creator */ }
}