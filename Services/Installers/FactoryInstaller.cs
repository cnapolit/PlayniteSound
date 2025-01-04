using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using PlayniteSounds.GeneratedFactories;
using PlayniteSounds.Views.Models;

namespace PlayniteSounds.Services.Installers
{
    public class FactoryInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.AddFacility<TypedFactoryFacility>();
            container.Register(Component.For<DownloadPromptModel>().LifestyleTransient(), Component.For(typeof(IFactory<>)).AsFactory());
        }
    }
}
