using NAudio.Dsp;
using NAudio.Wave;
using System;

namespace PlayniteSounds.Models.Audio.SampleProviders
{
    internal class ControllableSampleProvider : ISampleProvider
    {
        private enum AudioState
        {
            Disabled,
            FadingIn,
            Enabled,
            FadingOut
        }

        private const    float           MuffleCutoffFrequencyIncrement = (UpperBound - LowerBound) / (float)MuffleFadeSampleCount;
        private const    int             VolumeFadeSampleCount          = (int)(500 * 44100 / 1000.0);
        private const    int             MuffleFadeSampleCount          = (int)(500 * 44100 / 1000.0);
        private const    int             UpperBound                     = 2000;
        private const    int             LowerBound                     = 800;
        private readonly ISampleProvider _source;
        private readonly BiQuadFilter    _filter;
        private readonly object          _lockObject                    = new object();
        private          AudioState      _volumeState                   = AudioState.Enabled;
        private          AudioState      _muffledState;
        private          int             _volumeFadeSamplePosition;
        private          int             _muffleFadeSamplePosition;
        public WaveFormat WaveFormat => _source.WaveFormat;
        public bool Stopped { get; private set; }
        public float Volume { get; set; } = 1;

        public ControllableSampleProvider(ISampleProvider source, float volume, bool muffled)
        {
            _source = source;
            Volume = volume;
            if (muffled)
            {
                _muffledState = AudioState.Enabled;
            }
            _filter = BiQuadFilter.LowPassFilter(WaveFormat.SampleRate, LowerBound, 1);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            lock (_lockObject)
            {
                var samplesRead = 0;

                if (_muffledState is AudioState.Enabled && _volumeState is AudioState.Enabled)
                {
                    samplesRead = _source.Read(buffer, offset, count);
                    for (var i = 0; i < samplesRead; i++)
                    {
                        Scaleform(ref buffer[offset + i]);
                    }
                    return samplesRead;
                }

                switch (_volumeState)
                {
                    case AudioState.Enabled:
                        samplesRead = _source.Read(buffer, offset, count);
                        if (Volume != 1) /* Then */ for (var i = 0; i < samplesRead; i++)
                        {
                            buffer[offset + i] *= Volume;
                        }
                        break;
                    case AudioState.Disabled:
                        if (Stopped)
                        {
                            return 0;
                        }
                        else
                        {
                            // Simulate pausing by returning silence
                            Array.Copy(new float[count], 0, buffer, offset, count);
                            return count;
                        }
                    case AudioState.FadingOut:
                        samplesRead = _source.Read(buffer, offset, count);
                        samplesRead = FadeVolumeOut(buffer, offset, samplesRead);
                        break;
                    case AudioState.FadingIn:
                        samplesRead = _source.Read(buffer, offset, count);
                        FadeVolumeIn(buffer, offset, samplesRead);
                        break;
                }

                switch (_muffledState)
                {
                    case AudioState.Disabled: break;
                    case AudioState.Enabled:
                        for (var i = 0; i < samplesRead; i++)
                        {
                            Transform(ref buffer[offset + i]);
                        }
                        break;
                    case AudioState.FadingIn:
                        FadeMuffleIn(buffer, offset, samplesRead);
                        break;
                    case AudioState.FadingOut:
                        FadeMuffleOut(buffer, offset, samplesRead);
                        break;
                }

                return samplesRead;
            }
        }

        public void Stop()
        {
            if (!Stopped) lock (_lockObject)
            {
                Stopped = true;
                FadeOut();
            }
        }

        public void Pause()
        {
            lock (_lockObject)
            {
                FadeOut();
            }
        }

        private void FadeOut()
        {
            switch (_volumeState)
            {
                case AudioState.Disabled:
                    return;
                case AudioState.Enabled:
                    _volumeFadeSamplePosition = 0;
                    break;
            }
            _volumeState = AudioState.FadingOut;
        }

        public void Resume()
        {
            lock (_lockObject)
            {
                switch (_volumeState)
                {
                    case AudioState.Disabled:
                        if (Stopped)
                        {
                            return;
                        }
                        _volumeFadeSamplePosition = 0;
                        break;
                    case AudioState.Enabled:
                        return;
                }

                _volumeState = AudioState.FadingIn;
            }
        }

        public void Muffle()
        {
            lock (_lockObject)
            {
                switch (_muffledState)
                {
                    case AudioState.Disabled:
                        _muffleFadeSamplePosition = 0;
                        break;
                    case AudioState.Enabled:
                        return;
                }
                _muffledState = AudioState.FadingIn;
            }
        }

        public void UnMuffle()
        {
            lock (_lockObject)
            {
                switch (_muffledState)
                {
                    case AudioState.Disabled:
                        return;
                    case AudioState.Enabled:
                        _muffleFadeSamplePosition = 0;
                        break;
                }

                _muffledState = AudioState.FadingOut;
            }
        }

        private int FadeVolumeOut(float[] buffer, int offset, int sourceSamplesRead)
        {
            int sampleIndex = 0;
            while (sampleIndex < sourceSamplesRead)
            {
                float num2 = Volume * (1f - (float)_volumeFadeSamplePosition / VolumeFadeSampleCount);
                for (int i = 0; i < _source.WaveFormat.Channels; i++)
                {
                    buffer[offset + sampleIndex++] *= num2;
                }

                _volumeFadeSamplePosition++;
                if (_volumeFadeSamplePosition > VolumeFadeSampleCount)
                {
                    _volumeState = AudioState.Disabled;
                    if (Stopped)
                    {
                        if (_source is IDisposable disposable) /* Then */ disposable.Dispose();
                    }
                    else
                    {
                        ClearBuffer(buffer, offset + sampleIndex, sourceSamplesRead - sampleIndex);
                        return sourceSamplesRead;
                    }    
                    break;
                }
            }
            return sampleIndex;
        }

        private static void ClearBuffer(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[i + offset] = 0f;
            }
        }

        private void FadeVolumeIn(float[] buffer, int offset, int sourceSamplesRead)
        {
            var sampleIndex = 0;
            while (sampleIndex < sourceSamplesRead)
            {
                var fade = Volume * _volumeFadeSamplePosition / VolumeFadeSampleCount;
                for (var i = 0; i < _source.WaveFormat.Channels; i++)
                {
                    buffer[offset + sampleIndex++] *= fade;
                }

                _volumeFadeSamplePosition++;
                if (_volumeFadeSamplePosition > VolumeFadeSampleCount)
                {
                    _volumeState = AudioState.Enabled;
                    break;
                }
            }

            if (Volume != 1) /* Then */ while (sampleIndex < sourceSamplesRead)
            {
                buffer[offset + sampleIndex++] *= Volume;
            }
        }

        private void FadeMuffleIn(float[] buffer, int offset, int sourceSamplesRead)
        {
            var sampleIndex = 0;
            while (sampleIndex < sourceSamplesRead)
            {
                //var filter = _filters[_muffleFadeSamplePosition];
                _filter.SetLowPassFilter(44100, UpperBound - MuffleCutoffFrequencyIncrement * _muffleFadeSamplePosition, 1);
                for (var i = 0; i < _source.WaveFormat.Channels; i++)
                {
                    Transform(ref buffer[offset + sampleIndex++]);
                }

                _muffleFadeSamplePosition++;
                if (_muffleFadeSamplePosition > MuffleFadeSampleCount)
                {
                    _muffledState = AudioState.Enabled;
                    break;
                }
            }

            while (sampleIndex < sourceSamplesRead)
            {
                Transform(ref buffer[offset + sampleIndex++]);
            }
        }

        private void FadeMuffleOut(float[] buffer, int offset, int sourceSamplesRead)
        {
            var sampleIndex = 0;
            while (sampleIndex < sourceSamplesRead)
            {
                var fadedCutoff = LowerBound + MuffleCutoffFrequencyIncrement * _muffleFadeSamplePosition;
                _filter.SetLowPassFilter(44100, fadedCutoff, 1);
                for (var i = 0; i < _source.WaveFormat.Channels; i++)
                {
                    Transform(ref buffer[offset + sampleIndex++]);
                }

                _muffleFadeSamplePosition++;
                if (_muffleFadeSamplePosition > MuffleFadeSampleCount)
                {
                    _muffledState = AudioState.Disabled;
                    break;
                }
            }
        }

        private void Transform(ref float value) => value = _filter.Transform(value);

        private void Scaleform(ref float value) => value = _filter.Transform(value) * Volume;
    }
}
