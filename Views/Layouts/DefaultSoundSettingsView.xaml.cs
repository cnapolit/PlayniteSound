using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PlayniteSounds.Views.Layouts
{
    public partial class DefaultSoundSettingsView : UserControl
    {
        private readonly ISoundManager _soundManager;
        private readonly ISoundPlayer _soundPlayer;

        public DefaultSoundSettingsView(ISoundManager soundManager, ISoundPlayer soundPlayer)
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
