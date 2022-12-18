using Playnite.SDK;
using System.Windows.Controls;

namespace PlayniteSounds.Views.Layouts
{
    public partial class PlayniteSoundsSettingsView
    {
        public PlayniteSoundsSettingsView(SoundSettingsView soundSettingsView, MusicSettingsView musicSettingsView)
        {
            InitializeComponent();
            Tabs.Items.Add(new TabItem
            { 
                Header = ResourceProvider.GetString("LOC_PLAYNITESOUNDS_Sound"), 
                Content = soundSettingsView 
            });
            Tabs.Items.Add(new TabItem
            {
                Header = ResourceProvider.GetString("LOC_PLAYNITESOUNDS_Music"),
                Content = musicSettingsView
            });
        }
    }
}