using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.UI;
using PlayniteSounds.Views.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PlayniteSounds.Views.Layouts
{
    public partial class SoundSettingsView : UserControl, IDisposable
    {
        private readonly TabItem _desktopTab;
        private readonly TabItem _fullscreenTab;

        public SoundSettingsView(ISoundManager soundManager, ISoundPlayer soundPlayer)
        {
            InitializeComponent();
            DataContextChanged += SetModeDataContext;

            Tabs.Items.Add(new TabItem
            {
                Header = "Default",
                Content = new DefaultSoundSettingsView(soundManager, soundPlayer)
            });

            _desktopTab = new TabItem
            {
                Header = "Desktop",
                Content = new SoundModeSettingsControl(soundPlayer) { IsDesktop = true }
            };
            Tabs.Items.Add(_desktopTab);

            _fullscreenTab = new TabItem
            {
                Header = "Fullscreen",
                Content = new SoundModeSettingsControl(soundPlayer)
            };
            Tabs.Items.Add(_fullscreenTab);
        }

        public void Dispose()
        {
            DataContextChanged -= SetModeDataContext;
        }

        public void SetModeDataContext(object sender, DependencyPropertyChangedEventArgs e)
        {
            var settingsModel = DataContext as PlayniteSoundsSettingsViewModel;
            _desktopTab.DataContext = settingsModel.DesktopSettingsModel;
            _fullscreenTab.DataContext = settingsModel.FullscreenSettingsModel;
        }
    }
}
