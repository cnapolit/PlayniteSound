using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using PlayniteSounds.Common.Imports;
using PlayniteSounds.Models;
using System;
using System.Diagnostics;
using System.Linq;

namespace PlayniteSounds.Services.Audio
{
    public abstract class BasePlayer
    {
        #region Infrastructure

        protected readonly PlayniteSoundsSettings _settings;
        protected readonly MixingSampleProvider _mixer;

        public BasePlayer(MixingSampleProvider mixer, PlayniteSoundsSettings settings)
        {
            _mixer = mixer;
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

            if (input.WaveFormat.Channels != _mixer.WaveFormat.Channels) /* Then */
            if (input.WaveFormat.Channels == 1)
            {
                input = new MonoToStereoSampleProvider(input);
            }
            else
            {
                //throw new NotImplementedException("Not yet implemented this channel count conversion");
                return null;
            }

            if (input.WaveFormat.SampleRate != _mixer.WaveFormat.SampleRate)
            {
                using (var resampler = new MediaFoundationResampler(input.ToWaveProvider(), _mixer.WaveFormat))
                {
                    input = resampler.ToSampleProvider();
                }
            }

            return input;
        }

        #endregion
    }
}
