using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using System;
using System.Collections.Generic;

namespace PlayniteSounds.Services.Installers
{
    public class ViewInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
            => container.Register(Classes.FromThisAssembly().Where(IsViewClass).LifestyleTransient());

        private static bool IsViewClass(Type classType)
            => classType.IsSubclassOf(typeof(ObservableObject)) 
             || classType.IsSubclassOf(typeof(System.Windows.Controls.UserControl));
    }
}
