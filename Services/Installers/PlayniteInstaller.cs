using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Playnite.SDK;
using static PlayniteSounds.Services.Installers.Installation;

namespace PlayniteSounds.Services.Installers
{
    internal class PlayniteInstaller : IWindsorInstaller
    {
        private readonly IPlayniteAPI _api;

        public PlayniteInstaller(IPlayniteAPI api) => _api = api;

        public void Install(IWindsorContainer container, IConfigurationStore store)
            => container.Register(
                RegisterInstance(_api.Addons),
                RegisterInstance(_api.Database),
                RegisterInstance(_api.Dialogs),
                RegisterInstance(_api.MainView),
                RegisterInstance(_api.Paths),
                RegisterInstance(_api.UriHandler),
                RegisterInstance(LogManager.GetLogger()));
    }
}
