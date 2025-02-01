using PlayniteSounds.Views.Models;
using System;
using System.Windows;

namespace PlayniteSounds.Views.Layouts;

public partial class PlayniteSoundsSettingsView : IDisposable
{
    private readonly Action<object> _containerReleaseMethod;

    public PlayniteSoundsSettingsView(Action<object> containerReleaseMethod)
    {
        InitializeComponent();
        DataContextChanged += SetModeDataContext;
        _containerReleaseMethod = containerReleaseMethod;
    }

    public void Dispose()
    {
        DataContextChanged -= SetModeDataContext;

        DesktopMusic.Dispose();
        FullscreenMusic.Dispose();

        _containerReleaseMethod(this);
    }

    public void SetModeDataContext(object sender, DependencyPropertyChangedEventArgs e)
    {
        var settingsModel = DataContext as PlayniteSoundsSettingsViewModel;

        General.DataContext = settingsModel;
        GeneralMusic.DataContext = settingsModel;
        DesktopSound.DataContext = settingsModel.DesktopSettingsModel;
        FullscreenSound.DataContext = settingsModel.FullscreenSettingsModel;
        DesktopMusic.DataContext = settingsModel.DesktopSettingsModel;
        FullscreenMusic.DataContext = settingsModel.FullscreenSettingsModel;
    }
}