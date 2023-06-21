using NAudio.Wave;
using System;

namespace PlayniteSounds.Models.Audio.SampleProviders
{
    class CachedSoundSampleProvider : ISampleProvider
    {
        private long _position;
        public WaveFormat WaveFormat => _cachedSound.WaveFormat;
        private readonly CachedSound _cachedSound;

        public CachedSoundSampleProvider(CachedSound cachedSound)
        {
            _cachedSound = cachedSound;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = _cachedSound.AudioData.Length - _position;
            var samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(_cachedSound.AudioData, _position, buffer, offset, samplesToCopy);
            _position += samplesToCopy;
            return (int)samplesToCopy;
        }
    }
}
