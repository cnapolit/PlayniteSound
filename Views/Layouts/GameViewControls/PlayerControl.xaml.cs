using Playnite.SDK;
using Playnite.SDK.Controls;
using PlayniteSounds.Views.Models.GameViewControls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlayniteSounds.Views.Layouts.GameViewControls
{
    public partial class PlayerControl : PluginUserControl, INotifyPropertyChanged
    {
        public PlayerControl()
        {
            InitializeComponent();
            _model = new Lazy<PlayerControlModel>(() => DataContext as PlayerControlModel);
        }

        public RelayCommand<object> VideoPauseCommand => new RelayCommand<object>(_ =>
        {
            Player.Pause();
            Model.Pause = true;
        }, _ => !Model.Pause && Model.HasMusic);

        public RelayCommand<object> VideoPlayCommand => new RelayCommand<object>(_ =>
        {
            Player.Play();
            Model.Pause = false;
        }, _ => Model.Pause && Model.HasMusic);

        public RelayCommand<bool> VideoActionCommand => new RelayCommand<bool>(play =>
        {
            if (play)
            {
                Player.Play();
            }
            else
            {
                Player.Pause();
            }

            Model.Pause = !play;
        });

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }



        private bool _pause;
        public bool Pause
        {
            get => _pause;
            set
            {
                _pause = value;

                if (_pause)
                {
                    Player.Pause();
                }
                else
                {
                    Player.Play();
                }

                OnPropertyChanged();
            }
        }

        private readonly Lazy<PlayerControlModel> _model;
        public PlayerControlModel Model => _model.Value;
    }
}
