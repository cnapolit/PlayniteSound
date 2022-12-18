using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.UI;
using System.Windows;
using System.Windows.Controls;

namespace PlayniteSounds.Views.Layouts
{
    public partial class SoundSettingsView : UserControl
    {
        private readonly ISoundManager _soundManager;
        private readonly ISoundPlayer _soundPlayer;

        public SoundSettingsView(ISoundManager soundManager, ISoundPlayer soundPlayer)
        {
            _soundManager = soundManager;
            _soundPlayer = soundPlayer;
            InitializeComponent();
        }

        private void ButReloadAudio_Click(object sender, RoutedEventArgs e)
            => _soundPlayer.Close();

        private void ButOpenSoundsFolder_Click(object sender, RoutedEventArgs e)
            => _soundManager.OpenSoundsFolder();

        private void ButOpenMusicFolder_Click(object sender, RoutedEventArgs e)
            => _soundManager.OpenMusicFolder();

        private void ButOpenInfo_Click(object sender, RoutedEventArgs e)
            => _soundManager.HelpMenu();

        private void ButSaveSounds_Click(object sender, RoutedEventArgs e)
            => _soundManager.SaveSounds();

        private void ButLoadSounds_Click(object sender, RoutedEventArgs e)
            => _soundManager.LoadSounds();

        private void ButImportSounds_Click(object sender, RoutedEventArgs e)
            => _soundManager.ImportSounds();

        private void ButRemoveSounds_Click(object sender, RoutedEventArgs e)
            => _soundManager.RemoveSounds();

        private void ButOpenSoundManagerFolder_Click(object sender, RoutedEventArgs e)
            => _soundManager.OpenSoundManagerFolder();
    }
}
