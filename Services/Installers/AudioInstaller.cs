using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PlayniteSounds.Models;
using PlayniteSounds.Models.Audio;
using static PlayniteSounds.Services.Installers.Installation;

namespace PlayniteSounds.Services.Installers
{
    public class AudioInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            var settings = container.Resolve<PlayniteSoundsSettings>();

            IWavePlayer wavePlayer;
            switch (settings.AudioOutput)
            {
                default:                      wavePlayer = new WaveOutEvent();   break;
                case AudioOutput.Wasapi:      wavePlayer = new WasapiOut();      break;
                case AudioOutput.DirectSound: wavePlayer = new DirectSoundOut(); break;
                case AudioOutput.Asio:        wavePlayer = new AsioOut();        break;
            }

            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(settings.AudioSampleRate, settings.AudioChannels);
            var mixer = new MixingSampleProvider(waveFormat) { ReadFully = true };
            wavePlayer.Init(mixer);
            wavePlayer.Play();

            container.Register(RegisterInstance(mixer), RegisterInstance(wavePlayer));
        }
    }
}
