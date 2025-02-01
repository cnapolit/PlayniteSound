using System;
using NAudio.Wave;

namespace PlayniteSounds.Models.Audio.SampleProviders;

internal class AudioFileStreamReader(AudioFileReader reader) : IStreamReader
{
    public long Position { get => reader.Position; set => reader.Position = value; }

    public long Length => reader.Length;

    public string FileName => reader.FileName;

    public TimeSpan CurrentTime => reader.CurrentTime;
    public TimeSpan TotalTime   => reader.TotalTime;

    public void Dispose() => reader.Dispose();
}