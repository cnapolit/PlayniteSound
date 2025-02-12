using NAudio.Dsp;
using NAudio.Wave;
using System;

namespace PlayniteSounds.Models.Audio.SampleProviders;

internal class ControllableSampleProvider : ISampleProvider, IDisposable
{
    public enum AudioState
    {
        Disabled,
        FadingIn,
        Enabled,
        FadingOut
    }

    private readonly ISampleProvider _provider;
    private readonly IStreamReader   _source;
    private readonly BiQuadFilter[]  _filters;
    private readonly object          _lockObject  = new();
    private readonly float           _muffleCutOffFrequencyIncrement;
    private readonly float           _bandwidth;
    private readonly int             _volumeFadeSampleCount;
    private readonly int             _muffleFadeSampleCount;
    private readonly int             _upperBound;
    private readonly int             _lowerBound;
    private          AudioState      _muffledState;
    private          int             _volumeFadeSamplePosition;
    private          int             _muffleFadeSamplePosition;
    private          bool            _disposed;

    public WaveFormat WaveFormat => _provider.WaveFormat;
    public bool Stopped { get; private set; }
    public AudioState VolumeState { get; private set; } = AudioState.Enabled;
    public float Volume { get; set; }
    public long Position { get => _source.Position; set => _source.Position = value; }
    public long Length => _source.Length;
    public string FileName => _source.FileName;

    public TimeSpan CurrentTime => _source.CurrentTime;
    public TimeSpan TotalTime   => _source.TotalTime;

    public ControllableSampleProvider(
        ISampleProvider provider,
        IStreamReader source,
        float volume,
        float muffledBandwidth,
        int muffledUpperBound,
        int muffledLowerBound,
        int muffledFadeTime,
        int volumeFadeTime,
        bool muffled,
        bool fadeVolumeIn)
    {
        Volume = volume;
        _provider = provider;
        _source = source;
        _upperBound = muffledUpperBound; 
        _lowerBound = muffledLowerBound;
        _bandwidth = muffledBandwidth;
        _volumeFadeSampleCount = (int)(volumeFadeTime * WaveFormat.SampleRate / 1000.0);
        _muffleFadeSampleCount = (int)(muffledFadeTime * WaveFormat.SampleRate / 1000.0);
        _muffleCutOffFrequencyIncrement = (muffledUpperBound - muffledLowerBound) / (float)_muffleFadeSampleCount;

        _filters = new BiQuadFilter[provider.WaveFormat.Channels];
        var cuttoff = muffled ? muffledLowerBound : muffledUpperBound;
        for (var i = 0; i < _filters.Length; i++)
            /* Then */ _filters[i] = BiQuadFilter.LowPassFilter(WaveFormat.SampleRate, cuttoff, muffledBandwidth);

        if (muffled) /* Then */ _muffledState = AudioState.Enabled;

        if (fadeVolumeIn) /* Then */ VolumeState = AudioState.FadingIn;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        if (_disposed) /* Then */ return 0;
        lock (_lockObject)
        {
            var samplesRead = 0;

            if (_muffledState is AudioState.Enabled && VolumeState is AudioState.Enabled)
            {
                samplesRead = _provider.Read(buffer, offset, count);
                ApplyAcrossBuffer(Scaleform, buffer, offset, samplesRead);
                return samplesRead;
            }
                
            switch (VolumeState)
            {
                case AudioState.Enabled:
                    samplesRead = _provider.Read(buffer, offset, count);
                    if (Volume != 1) /* Then */ for (var i = 0; i < samplesRead; i++)
                    {
                        buffer[offset + i] *= Volume;
                    }
                    break;
                case AudioState.Disabled:
                    if (Stopped) /* Then */ return 0;
                    // Simulate pausing by returning silence
                    Array.Copy(new float[count], 0, buffer, offset, count);
                    return count;
                case AudioState.FadingOut:
                    samplesRead = _provider.Read(buffer, offset, count);
                    samplesRead = FadeVolumeOut(buffer, offset, samplesRead);
                    break;
                case AudioState.FadingIn:
                    samplesRead = _provider.Read(buffer, offset, count);
                    FadeVolumeIn(buffer, offset, samplesRead);
                    break;
            }

            switch (_muffledState)
            {
                case AudioState.Disabled: break;
                case AudioState.Enabled:
                    ApplyAcrossBuffer(Transform, buffer, offset, samplesRead);
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
        lock (_lockObject) /* Then */ if (!Stopped) 
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
        switch (VolumeState)
        {
            case AudioState.Disabled:
                return;
            case AudioState.Enabled:
                _volumeFadeSamplePosition = 0;
                break;
        }
        VolumeState = AudioState.FadingOut;
    }

    public void Resume()
    {
        lock (_lockObject)
        {
            switch (VolumeState)
            {
                case AudioState.Disabled:
                    if (Stopped) /* Then */ return;
                    _volumeFadeSamplePosition = 0;
                    break;
                case AudioState.Enabled:
                    return;
            }
                
            Stopped = false;
            VolumeState = AudioState.FadingIn;
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
        var sampleIndex = 0;
        while (sampleIndex < sourceSamplesRead)
        {
            var fade = Volume * (1f - (float)_volumeFadeSamplePosition / _volumeFadeSampleCount);
            for (var i = 0; i < _provider.WaveFormat.Channels; i++)
            {
                buffer[offset + sampleIndex++] *= fade;
            }

            _volumeFadeSamplePosition++;
            if (_volumeFadeSamplePosition > _volumeFadeSampleCount)
            {
                VolumeState = AudioState.Disabled;
                if (!Stopped)
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
        for (var i = 0; i < count; i++)
        {
            buffer[i + offset] = 0f;
        }
    }

    private void FadeVolumeIn(float[] buffer, int offset, int sourceSamplesRead)
    {
        var sampleIndex = 0;
        while (sampleIndex < sourceSamplesRead)
        {
            var fade = Volume * _volumeFadeSamplePosition / _volumeFadeSampleCount;
            for (var i = 0; i < _provider.WaveFormat.Channels; i++)
            {
                buffer[offset + sampleIndex++] *= fade;
            }

            _volumeFadeSamplePosition++;
            if (_volumeFadeSamplePosition > _volumeFadeSampleCount)
            {
                VolumeState = AudioState.Enabled;
                break;
            }
        }

        if    (Volume != 1)
        while (sampleIndex < sourceSamplesRead)
        {
            buffer[offset + sampleIndex++] *= Volume;
        }
    }

    private void FadeMuffleIn(float[] buffer, int offset, int sourceSamplesRead)
    {
        var sampleIndex = 0;
        while (sampleIndex < sourceSamplesRead)
        {
            var fadedCutOff = _upperBound - _muffleCutOffFrequencyIncrement * _muffleFadeSamplePosition;
            ApplyFadedMuffle(buffer, offset, fadedCutOff, ref sampleIndex);

            if (++_muffleFadeSamplePosition > _muffleFadeSampleCount)
            {
                _muffledState = AudioState.Enabled;
                break;
            }
        }

        ApplyAcrossBuffer(Transform, buffer, offset, sourceSamplesRead, sampleIndex);
    }

    private void FadeMuffleOut(float[] buffer, int offset, int sourceSamplesRead)
    {
        var sampleIndex = 0;
        while (sampleIndex < sourceSamplesRead)
        {
            var fadedCutOff = _lowerBound + _muffleCutOffFrequencyIncrement * _muffleFadeSamplePosition;
            ApplyFadedMuffle(buffer, offset, fadedCutOff, ref sampleIndex);

            if (++_muffleFadeSamplePosition > _muffleFadeSampleCount)
            {
                _muffledState = AudioState.Disabled;
                break;
            }
        }
    }

    private delegate void Mutate(ref float value, BiQuadFilter filter);

    private void ApplyAcrossBuffer(
        Mutate valueMutator, float[] buffer, int offset, int samplesRead, int sampleIndex = 0)
    {
        while   (sampleIndex < samplesRead)
        foreach (var filter in _filters)
        /* Then */ valueMutator(ref buffer[offset + sampleIndex++], filter);
    }

    private void ApplyFadedMuffle(float[] buffer, int offset, float fadedCutOff, ref int sampleIndex)
    {
        foreach (var filter in _filters)
        {
            filter.SetLowPassFilter(WaveFormat.SampleRate, fadedCutOff, _bandwidth);
            Transform(ref buffer[offset + sampleIndex++], filter);
        }
    }

    private static void Transform(ref float value, BiQuadFilter filter) => value = filter.Transform(value);

    private void Scaleform(ref float value, BiQuadFilter filter) => value = filter.Transform(value * Volume);

    public void Dispose()
    {
        lock (_lockObject)
        {
            if (_disposed) /* Then */ return;
            _disposed = true;

            if (_provider is IDisposable disposable) /* Then */ disposable.Dispose();
            _source.Dispose();
        }
    }
}