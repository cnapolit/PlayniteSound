using PlayniteSounds.Views.Models;
using System.Windows;
using System.Windows.Controls;

namespace PlayniteSounds.Views.Layouts;

/// <summary>
/// Interaction logic for SoundTypeSettingsControl.xaml
/// </summary>
public partial class SoundTypeSettingsControl
{
    public SoundTypeSettingsControl() => InitializeComponent();

    public string Header
    {
        get => Group.Header as string;
        set => Group.Header = value;
    }

    private void LoadSlider_ValueChanged(object sender, RoutedEventArgs e)
        => Slider.ValueChanged += Slider_ValueChanged;

    public void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> args)
        => (DataContext as SoundTypeSettingsModel).Preview();
}