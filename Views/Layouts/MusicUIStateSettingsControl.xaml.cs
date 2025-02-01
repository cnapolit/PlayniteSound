using PlayniteSounds.Views.Models;
using System.Windows;
using System.Windows.Controls;

namespace PlayniteSounds.Views.Layouts;

/// <summary>
/// Interaction logic for SoundUIStateSettingsControl.xaml
/// </summary>
public partial class MusicUIStateSettingsControl
{
    public MusicUIStateSettingsControl() => InitializeComponent();

    public object Header
    {
        get => Expander.Header;
        set => Expander.Header = value;
    }

    private void LoadSlider_ValueChanged(object sender, RoutedEventArgs e)
        => Slider.ValueChanged += Slider_ValueChanged;

    public void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        => (DataContext as UIStateSettingsModel).SetMusicVolume(e.NewValue);
}