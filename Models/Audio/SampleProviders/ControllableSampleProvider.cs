using NAudio.Dsp;
using NAudio.Wave;
using System;

namespace PlayniteSounds.Models.Audio.SampleProviders
{
    internal class ControllableSampleProvider : ISampleProvider
    {
        public WaveFormat WaveFormat => _source.WaveFormat;
        private BiQuadFilter _filter;

        public bool Paused { get; set; }
        public bool Stopped { get; private set; }
        private Mutate _audioMutator;
        private bool _muffled;
        public bool Muffled
        {
            get => _muffled;
            set
            {
                if (_muffled != value)
                {
                    _muffled = value;
                    if (value)
                        _audioMutator = Transform;
                    else
                        _audioMutator = Scale;
                }
            }
        }
        public float Volume { get; set; }

        private readonly ISampleProvider _source;

        public ControllableSampleProvider(ISampleProvider source)
        {
            _source = source;
            _filter = BiQuadFilter.LowPassFilter(WaveFormat.SampleRate, 300, 1);
            _audioMutator = Scale;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (Stopped)
                return 0;

            if (Paused)
            {
                // Simulate pausing by returning silence
                Array.Copy(new float[count], 0, buffer, offset, count);
                return count;
            }

            var samplesRead = _source.Read(buffer, offset, count);

            if (!Muffled && Volume is 1)
                return samplesRead;

            for (var n = 0; n < samplesRead; n++)
                _audioMutator(ref buffer[offset + n]);

            return samplesRead;
        }

        public void Stop()
        {
            if (Stopped) return;
            Stopped = true;
            if (_source is IDisposable disposable)
                disposable.Dispose();
        }

        private delegate void Mutate(ref float value);
        private Mutate Transform => (ref float value) => value = _filter.Transform(value) * Volume;
        private Mutate Scale => (ref float value) => value *= Volume;
    }
}
