using NAudio.Wave;
using System;

namespace PlayniteSounds.Models.Audio.SampleProviders;

class CachedSoundSampleProvider(CachedSound cachedSound) : ISampleProvider
{
    private long _position;
    public WaveFormat WaveFormat => cachedSound.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        var availableSamples = cachedSound.AudioData.Length - _position;
        var samplesToCopy = Math.Min(availableSamples, count);
        Array.Copy(cachedSound.AudioData, _position, buffer, offset, samplesToCopy);
        _position += samplesToCopy;
        return (int)samplesToCopy;
    }
}