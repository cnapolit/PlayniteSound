using PlayniteSounds.Views.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PlayniteSounds.Views.Layouts;

/// <summary>
/// Interaction logic for SoundUIStateSettingsControl.xaml
/// </summary>
public partial class SoundUIStateSettingsControl : IDisposable
{
    public SoundUIStateSettingsControl()
    {
        InitializeComponent();
        DataContextChanged += SetDataContext;
    }

    public object Header
    {
        get => Expander.Header; 
        set => Expander.Header = value;
    }

    public void Dispose() => DataContextChanged -= SetDataContext;

    public void SetDataContext(object sender, DependencyPropertyChangedEventArgs e)
    {
        var settingsModel = DataContext as UIStateSettingsModel;
        Enter.DataContext = settingsModel.EnterSettingsModel;
        Exit.DataContext = settingsModel.ExitSettingsModel;
        Tick.DataContext = settingsModel.TickSettingsModel;
    }
}