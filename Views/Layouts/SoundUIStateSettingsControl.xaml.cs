using PlayniteSounds.Views.Models;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PlayniteSounds.Views.Layouts
{
    /// <summary>
    /// Interaction logic for SoundUIStateSettingsControl.xaml
    /// </summary>
    public partial class SoundUIStateSettingsControl : UserControl, IDisposable
    {
        public SoundUIStateSettingsControl()
        {
            InitializeComponent();
            DataContextChanged += SetDataContext;
        }

        public object Header
        {
            get => Expander.Header; 
            set => Expander.Header = value;
        }

        public void Dispose() => DataContextChanged -= SetDataContext;

        public void SetDataContext(object sender, DependencyPropertyChangedEventArgs e)
        {
            var settingsModel = DataContext as UIStateSettingsModel;
            foreach (var soundTypeToModel in settingsModel.SoundTypesToSettingsModels)
            {
                var control = new SoundTypeSettingsControl
                {
                    Header = soundTypeToModel.Key.ToString(),
                    DataContext = soundTypeToModel.Value
                };
                Stack.Children.Add(control);
            }
        }
    }
}
