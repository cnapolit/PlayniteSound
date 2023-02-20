using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Windows.Controls;
using Playnite.SDK.Events;
using PlayniteSounds.Common.Constants;
using PlayniteSounds.Models;
using PlayniteSounds.Services.UI;
using Castle.Windsor;
using System.Collections.Generic;
using PlayniteSounds.Views.Models;
using PlayniteSounds.Views.Layouts;
using PlayniteSounds.Services.State;
using PlayniteSounds.Services.Installers;

namespace PlayniteSounds
{
    public class PlayniteSounds : GenericPlugin
    {
        #region Infrastructure

        private readonly IPlayniteAPI _api;
        private readonly IWindsorContainer _container;

        private readonly Lazy<IPlayniteEventHandler> _playniteEventHandler;
        private IPlayniteEventHandler PlayniteEventHandler => _playniteEventHandler.Value;

        private readonly Lazy<IGameMenuFactory> _gameMenuFactory;
        private IGameMenuFactory GameMenuFactory => _gameMenuFactory.Value;

        private readonly Lazy<IMainMenuFactory> _mainMenuFactory;
        private IMainMenuFactory MainMenuFactory => _mainMenuFactory.Value;

        private readonly Lazy<IGameViewControlFactory> _gameViewFactory;
        private IGameViewControlFactory GameViewFactory => _gameViewFactory.Value;


        public PlayniteSounds(IPlayniteAPI api) : base(api)
        {
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };

            AddCustomElementSupport(new AddCustomElementSupportArgs
            {
                ElementList = new List<string> 
                { 
                    "Player_Default",
                    "Player_Filter",
                    "Player_Platform",
                    "Player_Game",
                    "Handler"
                },
                SourceName = "Sounds"
            });

            var settings = LoadPluginSettings<PlayniteSoundsSettings>() ?? new PlayniteSoundsSettings();

            // Set the 
            settings.DesktopSettings.IsDesktop = true;

            var isDesktop = api.ApplicationInfo.Mode is ApplicationMode.Desktop;
            settings. ActiveModeSettings = isDesktop ? settings.DesktopSettings : settings.FullscreenSettings;
            SoundFile. CurrentPrefix     = isDesktop ? SoundFile.DesktopPrefix : SoundFile.FullScreenPrefix;

            Localization.SetPluginLanguage(api.ApplicationSettings.Language);

            // Install services
            _container = Installation.RegisterInstallers(api, this, settings);

            _playniteEventHandler = LazyResolve<IPlayniteEventHandler>();
            _gameMenuFactory      = LazyResolve<IGameMenuFactory>();
            _mainMenuFactory      = LazyResolve<IMainMenuFactory>();
            _gameViewFactory      = LazyResolve<IGameViewControlFactory>();
        }

        public override void Dispose() => _container.Dispose();

        private Lazy<T> LazyResolve<T>() => new Lazy<T>(_container.Resolve<T>);

        #endregion

        #region Playnite Implementation

        public override Guid Id { get; } = Guid.Parse(App.AppGuid);

        public override ISettings GetSettings(bool firstRunSettings)
            => _container.Resolve<PlayniteSoundsSettingsViewModel>();

        public override UserControl GetSettingsView(bool firstRunSettings)
            => _container.Resolve<PlayniteSoundsSettingsView>();

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
            => _playniteEventHandler.Value.OnGameInstalled();

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
            => PlayniteEventHandler.OnGameUninstalled();

        public override void OnGameSelected(OnGameSelectedEventArgs args)
            => PlayniteEventHandler.OnGameSelected();

        public override void OnGameStarted(OnGameStartedEventArgs args)
            => PlayniteEventHandler.OnGameStarted();

        public override void OnGameStarting(OnGameStartingEventArgs args)
            => PlayniteEventHandler.OnGameStarting(args.Game);

        public override void OnGameStopped(OnGameStoppedEventArgs args)
            => PlayniteEventHandler.OnGameStopped();

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
            => PlayniteEventHandler.OnApplicationStarted(_api.Addons.Plugins);

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
            => PlayniteEventHandler.OnApplicationStopped();

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
            => PlayniteEventHandler.OnLibraryUpdated(SavePluginSettings);

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
            => MainMenuFactory.GetMainMenuItems(args);

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
            => GameMenuFactory.GetGameMenuItems(args);

        public override Control GetGameViewControl(GetGameViewControlArgs args)
            => GameViewFactory.GetGameViewControl(args);

        #endregion

    }
}
