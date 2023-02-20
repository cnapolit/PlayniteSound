using Playnite.SDK;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace PlayniteSounds.Views.Models.GameViewControls
{
    public class PlayerControlModel : ObservableObject
    {
        public IPlayniteAPI PlayniteAPI { get; set; }
        public IMusicFileSelector MusicFileSelector { get; set; }
        public IPathingService PathingService { get; set; }
        public PlayniteSoundsSettings Settings { get; set; }

        private double _volume;
        public double Volume 
        {
            get => _volume;
            set 
            { 
                _volume= value;
                OnPropertyChanged();
            }
        }

        private bool _pause;
        public bool Pause
        {
            get => _pause;
            set
            {
                _pause = value;
                OnPropertyChanged();
            }
        }

        private bool _mute;
        public bool Mute
        {
            get => _mute;
            set
            {
                _mute = value;
                OnPropertyChanged();
            }
        }

        private string _musicFilePath;
        public string MusicFilePath 
        { 
            get => _musicFilePath;
            set
            { 
                _musicFilePath = value;
                OnPropertyChanged();
            }
        }
        public bool HasMusic => !string.IsNullOrWhiteSpace(MusicFilePath);

        private MusicType _musicType;
        public MusicType MusicType
        {
            get => _musicType; 
            set 
            {
                _musicType = value;
                UpdateMusic();
            } 
        }

        public PlayerControlModel(
            IPlayniteAPI api, 
            IPathingService pathingService, 
            IMusicFileSelector musicFileSelector,
            PlayniteSoundsSettings settings)
        {
            PlayniteAPI = api;
            PathingService = pathingService;
            MusicFileSelector = musicFileSelector;
            Settings = settings;
        }

        public void Play(object sender, EventArgs e)
        {
            if (sender is MediaElement player)
            {
                player.Play();
                Pause = false;
            }
        }

        public void OnEnd(object sender, EventArgs e)
        {
            if (!Pause && sender is MediaElement player)
            {
                player.Position = TimeSpan.Zero;

                if (Settings.RandomizeOnMusicEnd) 
                {
                    UpdateMusic();
                }

                player.Play();
            }
        }

        public void UpdateMusic() 
            => MusicFilePath = MusicFileSelector.SelectFile(MusicTypeToFiles(), MusicFilePath, true);

        private string[] MusicTypeToFiles()
        {
            var game = PlayniteAPI.SelectedGames().FirstOrDefault();

            switch (MusicType)
            {
                case MusicType.Game:
                    return PathingService.GetGameMusicFiles(game);
                case MusicType.Platform:
                    return PathingService.GetPlatformMusicFiles(game?.Platforms?.FirstOrDefault());
                case MusicType.Filter:
                    return PathingService.GeFilterMusicFiles(PlayniteAPI.MainView.GetActiveFilterPreset());
                default:
                    return PathingService.GetDefaultMusicFiles();
            }
        }
    }
}
