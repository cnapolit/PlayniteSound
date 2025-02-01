using Playnite.SDK;
using PlayniteSounds.Views.Models.GameViewControls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PlayniteSounds.Views.Layouts.GameViewControls;

public partial class MusicControl : INotifyPropertyChanged
{
    static MusicControl()
    {
        TagProperty.OverrideMetadata(typeof(MusicControl), new FrameworkPropertyMetadata(-1, OnTagChanged));
    }

    public MusicControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    private static void OnTagChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        (d as MusicControl).VideoIsPlaying = Convert.ToBoolean(e.NewValue);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public RelayCommand<bool> VideoPlayingCommand => new(videoIsPlaying => VideoIsPlaying = videoIsPlaying);

    public string CurrentMusicName { get; set; }

    public bool VideoIsPlaying { get; set; }

    public void OnSettingsChanged(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(VideoIsPlaying))
        {
            OnPropertyChanged(nameof(VideoPlayingCommand));
            OnPropertyChanged(nameof(VideoIsPlaying));
        }
        else if (args.PropertyName == nameof(CurrentMusicName))
        {
            OnPropertyChanged(nameof(CurrentMusicName));
        }
    }
}