using PlayniteSounds.Views.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PlayniteSounds.Views.Layouts
{
    public partial class MusicSettingsView : UserControl, IDisposable
    {
        private readonly TabItem _desktopTab;
        private readonly TabItem _fullscreenTab;

        public MusicSettingsView()
        {
            InitializeComponent();
            DataContextChanged += SetModeDataContext;
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
