using Playnite.SDK.Plugins;
using Playnite.SDK;
using System;
using System.Windows.Controls;
using PlayniteSounds.Models;
using Playnite.SDK.Controls;
using PlayniteSounds.Views.Layouts.GameViewControls;
using PlayniteSounds.Views.Models.GameViewControls;
using System.Windows.Data;
using PlayniteSounds.Services.Files;
using PlayniteSounds.Services.State;
using PlayniteSounds.Services.Audio;

namespace PlayniteSounds.Services.UI
{
    public class GameViewControlFactory : IGameViewControlFactory
    {
        private readonly IPlayniteAPI _api;
        private readonly IPathingService _pathingService;
        private readonly IMusicFileSelector _musicFileSelector;
        private readonly IPlayniteEventHandler _playniteEventHandler;
        private readonly IMusicPlayer _musicPlayer;
        private readonly PlayniteSoundsSettings _settings;

        public GameViewControlFactory(
            IPlayniteAPI api,
            IPathingService pathingService,
            IMusicFileSelector musicFileSelector,
            PlayniteSoundsSettings settings)
        {
            _api = api;
            _pathingService = pathingService;
            _musicFileSelector = musicFileSelector;
            _settings = settings;
        }

        // args: {FilePath,Player}_{Default,Filter,Platform,Game},Handler
        public Control GetGameViewControl(GetGameViewControlArgs args)
        {
            
            var strArgs = args.Name.Split('_');

            var controlType = strArgs[0];

            switch (controlType)
            {
                case "FilePath": return ConstructFilePathView(musicType);
                case "Player": return ConstructPlayerView(musicType);
                case "Handler": return ConstructHandlerView();
                default: throw new Exception($"Unrecognized controlType '{controlType}' for request '{args.Name}'");
            }
        }

        private PluginUserControl ConstructFilePathView(MusicType musicType)
        {
            return null;
        }

        private PluginUserControl ConstructPlayerView(MusicType musicType)
        {
            var userControl = new PlayerControl
            {
                DataContext = new PlayerControlModel(_api, _pathingService, _musicFileSelector, _settings)
                {
                    MusicType = musicType
                }
            };

            var volumeBinding = new Binding("MusicVolume")
            {
                Mode = BindingMode.OneWay,
                Source = _settings.IsDesktop ? _settings.DesktopSettings : _settings.FullscreenSettings
            };

            BindingOperations.SetBinding(userControl, MediaElement.VolumeProperty, volumeBinding);

            return userControl;
        }

        private static MusicType RetrieveMusicType(string[] strArgs)
        {
            var musicTypeStr = strArgs[1];
            if (Enum.TryParse<MusicType>(musicTypeStr, true, out var musicType))
            {
                return musicType;
            }


            throw new ArgumentException($"Unrecognized musicType '{musicTypeStr}'");
        }

        private PluginUserControl ConstructHandlerView() => new HandlerControl
        {
            DataContext = new HandlerControlModel(_playniteEventHandler, _musicPlayer)
        };
    }
}
