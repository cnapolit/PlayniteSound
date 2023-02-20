using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace PlayniteSounds.Views.Layouts
{
    public partial class SoundModeSettingsControl : UserControl
    {
        private readonly ISoundPlayer _soundPlayer;
        public bool IsDesktop { get; set; }

        public SoundModeSettingsControl(ISoundPlayer soundPlayer)
        {
            _soundPlayer = soundPlayer;
            InitializeComponent();
        }

        public void PreviewAppStart(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.AppStarted, IsDesktop);
        public void PreviewAppStop(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.AppStopped, IsDesktop);
        public void PreviewGameStarting(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.GameStarting, IsDesktop);
        public void PreviewGameStarted(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.GameStarted, IsDesktop);
        public void PreviewGameStopped(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.GameStopped, IsDesktop);
        public void PreviewGameSelected(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.GameSelected, IsDesktop);
        public void PreviewGameInstalled(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.GameInstalled, IsDesktop);
        public void PreviewGameUninstalled(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.GameUninstalled, IsDesktop);
        public void PreviewLibaryUpdated(object sender, DragCompletedEventArgs e)
            => _soundPlayer.Preview(SoundType.LibraryUpdated, IsDesktop);
    }
}
