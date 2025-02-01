using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using PlayniteSounds.Views.Layouts;
using PlayniteSounds.Views.Models;
using System;

namespace PlayniteSounds.Services.Installers;

public class ViewInstaller : IWindsorInstaller
{
    public void Install(IWindsorContainer container, IConfigurationStore store)
        => container.Register(RegisterComponent<PlayniteSoundsSettingsViewModel>(container),
            RegisterComponent<PlayniteSoundsSettingsView>(container));

    private ComponentRegistration<T> RegisterComponent<T>(IWindsorContainer container) where T : class
        => Component.For<T>().LifestyleTransient().DependsOn(
            Dependency.OnValue("containerReleaseMethod", new Action<object>(container.Release)));
}