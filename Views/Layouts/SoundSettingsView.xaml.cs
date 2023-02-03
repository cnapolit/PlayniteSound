using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.UI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace PlayniteSounds.Views.Layouts
{
    public partial class SoundSettingsView : UserControl
    {

        public SoundSettingsView(ISoundManager soundManager, ISoundPlayer soundPlayer)
        {
            InitializeComponent();
        }
    }
}
