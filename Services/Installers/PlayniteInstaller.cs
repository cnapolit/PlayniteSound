using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Playnite.SDK;

namespace PlayniteSounds.Services.Installers
{
    public class PlayniteInstaller : IWindsorInstaller
    {
        private readonly IPlayniteAPI _api;

        public PlayniteInstaller(IPlayniteAPI api) => _api = api;

        public void Install(IWindsorContainer container, IConfigurationStore store)
            => container.Register(
                Installation.RegisterInstance(_api.MainView),
                Installation.RegisterInstance(_api.Database),
                Installation.RegisterInstance(_api.Dialogs),
                Installation.RegisterInstance(_api.UriHandler),
                Installation.RegisterInstance(_api.Addons),
                Installation.RegisterInstance(LogManager.GetLogger()));
    }
}
