using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Playnite.SDK;
using PlayniteSounds.Models;

namespace PlayniteSounds.Services.Installers
{
    internal static class Installation
    {
        internal static IWindsorContainer RegisterInstallers(
            IPlayniteAPI api, PlayniteSounds plugin, PlayniteSoundsSettings settings) 
            => new WindsorContainer().
                Install(FromAssembly.This(), new PlayniteInstaller(api)).
                Register(RegisterInstance(plugin), RegisterInstance(settings));

        internal static IRegistration RegisterInstance<T>(T instance) where T : class
            => Component.For<T>().Instance(instance).LifestyleSingleton();
    }
}
