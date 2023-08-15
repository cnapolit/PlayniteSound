using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Playnite.SDK;
using static PlayniteSounds.Services.Installers.Installation;

namespace PlayniteSounds.Services.Installers
{
    public class PlayniteInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            var api = container.Resolve<IPlayniteAPI>();
            container.Register(
                        RegisterInstance(api.Addons),
                        RegisterInstance(api.Database),
                        RegisterInstance(api.Dialogs),
                        RegisterInstance(api.MainView),
                        RegisterInstance(api.Paths),
                        RegisterInstance(api.UriHandler),
                        RegisterInstance(LogManager.GetLogger()));
        }
    }
}
