using PlayniteSounds.Views.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PlayniteSounds.Views.Layouts;

public partial class MusicModeSettingsView : IDisposable
{
    public bool IsDesktop { get; set; }

    public MusicModeSettingsView()
    {
        InitializeComponent();
        DataContextChanged += SetDataContext;
    }

    public void Dispose() => DataContextChanged -= SetDataContext;

    private void SetDataContext(object sender, DependencyPropertyChangedEventArgs e)
    {
        var settingsModel = DataContext as ModeSettingsModel;
        foreach (var stateToModel in settingsModel.UIStatesToSettingsModels)
        {
            var control = new MusicUIStateSettingsControl 
            {
                Header = stateToModel.Key,
                DataContext = stateToModel.Value
            };
            Stack.Children.Add(control);
        }
    }
}