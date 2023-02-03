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

namespace PlayniteSounds.Services.UI
{
    internal class GameViewControlFactory
    {
        private readonly IPlayniteAPI _api;
        private readonly IPathingService _pathingService;
        private readonly IMusicFileSelector _musicFileSelector;
        private readonly PlayniteSoundsSettings _settings;

        public GameViewControlFactory(IPlayniteAPI api, PlayniteSoundsSettings settings)
        {
            _api = api;
            _settings = settings;
        }

        // args: {FilePath,Player}_{Default,Filter,Platform,Game}
        public Control GetGameViewControl(GetGameViewControlArgs args)
        {
            var strArgs = args.Name.Split('_');

            if (strArgs.Length < 3)
            {
                throw new Exception($"Invalid number of args specified for GameViewControl: {args.Name}");
            }

            var controlType = strArgs[1];
            var musicTypeStr = strArgs[2];

            if (Enum.TryParse<MusicType>(musicTypeStr, true, out var musicType)) switch (controlType)
            {
                case "FilePath": return ConstructFilePathView (musicType);
                case   "Player": return ConstructPlayerView   (musicType);
                default: throw new Exception($"Unrecognized controlType '{controlType}' for request '{args.Name}'");
            }

            throw new Exception($"Unrecognized musicType '{musicTypeStr}' for request '{args.Name}'");
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

            var source = _api.ApplicationInfo.Mode is ApplicationMode.Desktop 
                ? _settings.DesktopSettings
                : _settings.FullscreenSettings; 
            
            var volumeBinding = new Binding("MusicVolume")
            {
                Mode = BindingMode.OneWay,
                Source = source
            };

            BindingOperations.SetBinding(userControl, MediaElement.VolumeProperty, volumeBinding);

            return userControl;
        }
    }
}
