using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.State;
using System;
using System.Collections.Generic;

namespace PlayniteSounds.Views.Models.GameViewControls
{
    public class HandlerControlModel : ObservableObject
    {
        private readonly IPlayniteEventHandler _playniteEventHandler;
        private readonly IMusicPlayer _musicPlayer;

        public HandlerControlModel(IPlayniteEventHandler playniteEventHandler, IMusicPlayer musicPlayer)
        {
            _playniteEventHandler = playniteEventHandler;
            _musicPlayer = musicPlayer;
        }

        public void TriggerGameDetailsEnteredEvent(object sender, EventArgs e)
            => _playniteEventHandler.OnGameDetailsEntered();

        public void TriggerSettingsEnteredEvent(object sender, EventArgs e)
            => _playniteEventHandler.OnSettingsEntered();

        public void TriggerMainViewEnteredEvent(object sender, EventArgs e)
            => _playniteEventHandler.OnMainViewEntered();

        private double _musicVolume;
        public double MusicVolume
        { 
            get => _musicVolume; 
            set
            { 
                _musicVolume = value;
                OnPropertyChanged();
                _musicPlayer.SetVolume(value);
            }
        }

        public void Pause(object sender, EventArgs e) => _musicPlayer.Pause("Theme");
        public void Play(object sender, EventArgs e)  => _musicPlayer.Resume("Theme");
    }
}
