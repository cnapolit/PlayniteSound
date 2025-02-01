using Playnite.SDK;
using Playnite.SDK.Controls;
using PlayniteSounds.Views.Models.GameViewControls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PlayniteSounds.Views.Layouts.GameViewControls;

public partial class HandlerControl : INotifyPropertyChanged
{
    public HandlerControl()
    {
        InitializeComponent();
        _model = new Lazy<HandlerControlModel>(() => DataContext as HandlerControlModel);
    }

    private readonly Lazy<HandlerControlModel> _model;
    public HandlerControlModel Model => _model.Value;

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public RelayCommand<bool> VideoPlayingCommand => new(videoIsPaused => VideoIsPaused = videoIsPaused);

    public RelayCommand<bool> VideoMutedCommand => new(videoIsMuted => VideoIsMuted = videoIsMuted);


    private bool _videoIsPlaying = true;
    public bool VideoIsPaused
    {
        get => _videoIsPlaying;
        set
        {
            _videoIsPlaying = value;
            PlayIfNoVideoAudio();
            OnPropertyChanged();
        }
    }

    private bool _videoIsMuted = true;
    public bool VideoIsMuted
    {
        get => _videoIsMuted;
        set
        {
            _videoIsMuted = value;
            PlayIfNoVideoAudio();
            OnPropertyChanged();
        }
    }

    private void PlayIfNoVideoAudio()
    {
        if (_paused || VideoIsSilent)
        {
            Model.Play();
        }
        else
        {
            Model.Pause();
        }
    }

    private bool _paused;
    public bool Paused
    {
        get => _paused;
        set
        {
            if (_paused != value)
            {
                if (value)
                {
                    Model.Pause();
                }
                else if (VideoIsSilent)
                {
                    Model.Play();
                }

                _paused = value;
                OnPropertyChanged();
            }
        }
    }

    //private bool _gameDetailsIsVisible;
    public bool GameDetailsIsVisible
    {
        get => (bool)GetValue(GameDetailsIsVisibleProperty);//_gameDetailsIsVisible;
        set
        {
            if (GameDetailsIsVisible != value)
            {
                if (value)
                {
                    Model.PlayniteEventHandler.OnGameDetailsEntered();
                }
                else
                {
                    Model.PlayniteEventHandler.TriggerRevertUIStateChanged();
                }
                SetValue(GameDetailsIsVisibleProperty, value);
                //_gameDetailsIsVisible = value;
                //OnPropertyChanged();
            }
        }
    }
    public static DependencyProperty GameDetailsIsVisibleProperty
        = DependencyProperty.Register(nameof(GameDetailsIsVisible), typeof(bool), typeof(HandlerControl), new PropertyMetadata(false));

    private bool _mainMenuIsVisible;

    public bool MainMenuIsVisible
    {
        get => _mainMenuIsVisible;
        set
        {
            if (_mainMenuIsVisible != value)
            {
                if (value)
                {
                    Model.PlayniteEventHandler.OnSettingsEntered();
                }
                else
                {
                    Model.PlayniteEventHandler.TriggerRevertUIStateChanged();
                }

                _mainMenuIsVisible = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _settingsIsVisible;
    public bool SettingsIsVisible
    {
        get => _settingsIsVisible;
        set
        {
            if (_settingsIsVisible != value)
            {
                if (value)
                {
                    Model.PlayniteEventHandler.OnSettingsEntered();
                }
                else
                {
                    Model.PlayniteEventHandler.TriggerRevertUIStateChanged();
                }

                _settingsIsVisible = value;
                OnPropertyChanged();
            }
        }
    }

    public bool VideoIsSilent => !_videoIsPlaying || _videoIsMuted;
}