using Playnite.SDK;
using PlayniteSounds.Common.Extensions;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace PlayniteSounds.Views.Models.GameViewControls
{
    internal class PlayerControlModel : ObservableObject
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

        public string MusicFilePath { get; set; }
        public bool HasMusic => !string.IsNullOrWhiteSpace(MusicFilePath);
        public MusicType MusicType { get; set; }

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

        public void OnEnd(object sender, EventArgs e)
        {
            var media = sender as MediaElement;
            media.Position = TimeSpan.Zero;

            if (Settings.RandomizeOnMusicEnd) 
            {
                MusicFilePath = MusicFileSelector.SelectFile(MusicTypeToFiles(), MusicFilePath, true);
            }

            media.Play();
        }

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
