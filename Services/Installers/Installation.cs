using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Playnite.SDK;
using PlayniteSounds.Models;
using PlayniteSounds.Models.State;

namespace PlayniteSounds.Services.Installers
{
    internal static class Installation
    {
        internal static IWindsorContainer RegisterInstallers(
            IPlayniteAPI api, PlayniteSoundsPlugin plugin, PlayniteSoundsSettings settings) 
            => new WindsorContainer().Register(
                                        RegisterInstance(new PlayniteState()),
                                        RegisterInstance(api), 
                                        RegisterInstance(plugin),
                                        RegisterInstance(settings))
                                     .Install(FromAssembly.This());

        internal static IRegistration RegisterInstance<T>(T instance) where T : class
            => Component.For<T>().Instance(instance);
    }
}
