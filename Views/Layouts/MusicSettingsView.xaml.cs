using PlayniteSounds.Services.Audio;
using System.Windows;
using System.Windows.Controls;

namespace PlayniteSounds.Views.Layouts
{
    public partial class MusicSettingsView : UserControl
    {
        private readonly IMusicPlayer _musicPlayer;

        public MusicSettingsView(IMusicPlayer musicPlayer)
        {
            _musicPlayer = musicPlayer;
            InitializeComponent();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => _musicPlayer.SetVolume();
    }
}
