using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using PlayniteSounds.Models;

namespace PlayniteSounds.Services.Audio;

public abstract class BasePlayer(IWavePlayerManager wavePlayerManager, PlayniteSoundsSettings settings)
{
    #region Infrastructure

    protected readonly PlayniteSoundsSettings _settings = settings;
    protected readonly IWavePlayerManager _wavePlayerManager = wavePlayerManager;

    #endregion

    #region Implementation

    protected ISampleProvider ConvertProvider(ISampleProvider input)
    {
        if (input == null) /* Then */ return null;

        if (input.WaveFormat.Channels != _wavePlayerManager.Mixer.WaveFormat.Channels)
        if (input.WaveFormat.Channels == 1)
        {
            input = new MonoToStereoSampleProvider(input);
        }
        else
        {
            return null;
        }

        if (input.WaveFormat.SampleRate != _wavePlayerManager.Mixer.WaveFormat.SampleRate)
        {
            using var resampler = new MediaFoundationResampler(input.ToWaveProvider(), _wavePlayerManager.Mixer.WaveFormat);
            input = resampler.ToSampleProvider();
        }

        return input;
    }

    #endregion
}