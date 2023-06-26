using Playnite.SDK;
using Playnite.SDK.Controls;
using PlayniteSounds.Views.Models.GameViewControls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlayniteSounds.Views.Layouts.GameViewControls
{
    public partial class HandlerControl : PluginUserControl, INotifyPropertyChanged
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

        public RelayCommand<bool> VideoPlayingCommand
            => new RelayCommand<bool>(videoIsPaused => VideoIsPaused = videoIsPaused);

        public RelayCommand<bool> VideoMutedCommand 
            => new RelayCommand<bool>(videoIsMuted => VideoIsMuted = videoIsMuted);


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

        public bool VideoIsSilent => !_videoIsPlaying || _videoIsMuted;
    }
}
