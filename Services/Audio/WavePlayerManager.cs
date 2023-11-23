using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PlayniteSounds.Models;
using PlayniteSounds.Models.Audio;

namespace PlayniteSounds.Services.Audio
{
    public class WavePlayerManager : IWavePlayerManager
    {
        private readonly PlayniteSoundsSettings _settings;

        public MixingSampleProvider Mixer { get; private set; }
        public IWavePlayer WavePlayer { get; private set; }

        public WavePlayerManager(PlayniteSoundsSettings settings)
        {
            _settings = settings;
            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(settings.AudioSampleRate, settings.AudioChannels);
            Mixer = new MixingSampleProvider(waveFormat) { ReadFully = true };
            Init();
        }

        public void Init()
        {
            if (WavePlayer != null)
            {
                WavePlayer.Stop();
                WavePlayer.Dispose();
            }

            switch (_settings.AudioOutput)
            {
                default:                      WavePlayer = new WaveOutEvent();   break;
                case AudioOutput.Wasapi:      WavePlayer = new WasapiOut();      break;
                case AudioOutput.DirectSound: WavePlayer = new DirectSoundOut(); break;
                case AudioOutput.Asio:        WavePlayer = new AsioOut();        break;
            }
            
            WavePlayer.Init(Mixer);
            WavePlayer.Play();
        }

        public void Dispose() => WavePlayer?.Dispose();
    }
}