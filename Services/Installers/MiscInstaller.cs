using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using static PlayniteSounds.Services.Installers.Installation;

namespace PlayniteSounds.Services.Installers
{
    public class MiscInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            var mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)) { ReadFully = true };
            IWavePlayer wavePlayer = new WaveOutEvent();
            wavePlayer.Init(mixer);
            wavePlayer.Play();
            container.Register(RegisterInstance(mixer), RegisterInstance(wavePlayer));
        }
    }
}
