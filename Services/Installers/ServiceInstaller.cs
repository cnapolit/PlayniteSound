using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using PlayniteSounds.Files.Download.Downloaders;
using System;
using System.Linq;
using System.Reflection;

namespace PlayniteSounds
{
    public class ServiceInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore _)
            => container.Register(Classes.
                FromThisAssembly().
                Where(ValidateClass).
                WithService.
                DefaultInterfaces());


        protected static readonly Assembly Assembly = Assembly.GetCallingAssembly();
        private static bool ValidateClass(Type classType)
        {
            // Only register classes with an interface defined by the plugin
            var interfaces = classType.GetInterfaces().Where(i => i.Assembly == Assembly);

            // DownloadManager will load downloaders depending on settings
            return interfaces.Any() && interfaces.All(i => i != typeof(IDownloader));
        }
    }
}
