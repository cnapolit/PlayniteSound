using NAudio.Wave;
using System;
using System.Timers;

namespace PlayniteSounds.Models.Audio.SampleProviders
{
    internal class CallBackSampleProvider : ISampleProvider, IDisposable
    {
        public WaveFormat WaveFormat => _source.WaveFormat;
        public Action Callback { get; }
        private readonly ISampleProvider _source;

        public CallBackSampleProvider(ISampleProvider sampleProvider, Action callback)
        {
            _source = sampleProvider;
            Callback = callback;
        }

        public void Dispose()
        {
            if (_source is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public int Read(float[] buffer, int offset, int count) => _source.Read(buffer, offset, count);
    }
}
