using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using PlayniteSounds.Common.Imports;
using PlayniteSounds.Models;
using System.Diagnostics;
using System.Linq;

namespace PlayniteSounds.Services.Audio
{
    public abstract class BasePlayer
    {
        #region Infrastructure

        protected readonly PlayniteSoundsSettings _settings;
        protected readonly IWavePlayerManager _WavePlayerManager;

        public BasePlayer(IWavePlayerManager wavePlayerManager, PlayniteSoundsSettings settings)
        {
            _WavePlayerManager = wavePlayerManager;
            _settings = settings;
        }

        #endregion

        #region Implementation

        protected static bool PlayniteIsInForeground()
        {
            var foregroundHandle = User32.GetForegroundWindow();

            return Process.
                GetProcesses().
                Where(p => p.ProcessName.Contains("Playnite")).
                Any(p => p.MainWindowHandle == foregroundHandle);
        }

        protected ISampleProvider ConvertProvider(ISampleProvider input)
        {
            if (input == null) /* Then */ return null;

            if (input.WaveFormat.Channels != _WavePlayerManager.Mixer.WaveFormat.Channels) /* Then */
            if (input.WaveFormat.Channels == 1)
            {
                input = new MonoToStereoSampleProvider(input);
            }
            else
            {
                return null;
            }

            if (input.WaveFormat.SampleRate != _WavePlayerManager.Mixer.WaveFormat.SampleRate)
            {
                using (var resampler = new MediaFoundationResampler(input.ToWaveProvider(), _WavePlayerManager.Mixer.WaveFormat))
                {
                    input = resampler.ToSampleProvider();
                }
            }

            return input;
        }

        #endregion
    }
}
