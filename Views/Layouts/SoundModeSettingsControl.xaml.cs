using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace PlayniteSounds.Views.Layouts
{
    public partial class SoundModeSettingsControl : UserControl
    {
        private readonly ISoundPlayer _soundPlayer;
        private readonly bool _isDesktop;

        public SoundModeSettingsControl(ISoundPlayer soundPlayer, bool isDesktop)
        {
            _soundPlayer = soundPlayer;
            _isDesktop = isDesktop;
            InitializeComponent();
        }

        private void PreviewAppStart(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.AppStarted);
        private void PreviewAppStop(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.AppStopped);
        private void PreviewGameStarting(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.GameStarting);
        private void PreviewGameStarted(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.GameStarted);
        private void PreviewGameStopped(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.GameStopped);
        private void PreviewGameSelected(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.GameSelected);
        private void PreviewGameInstalled(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.GameInstalled);
        private void PreviewGameUninstalled(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.GameUninstalled);
        private void PreviewLibaryUpdated(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.LibraryUpdated);
    }
}
