using NAudio.Wave;
using System;

namespace PlayniteSounds.Models.Audio.SampleProviders;

internal class CallBackSampleProvider(ISampleProvider sampleProvider, Action callback) : ISampleProvider, IDisposable
{
    public WaveFormat WaveFormat => sampleProvider.WaveFormat;
    public Action Callback { get; } = callback;

    public void Dispose()
    {
        if (sampleProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public int Read(float[] buffer, int offset, int count) => sampleProvider.Read(buffer, offset, count);
}