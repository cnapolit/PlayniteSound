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
using System.Windows.Data;
using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.State.FauxConverters;
using System.Linq;
using Microsoft.Win32;
using System.Windows;
using PlayniteSounds.Interfaces;

namespace PlayniteSounds
{
    public class PlayniteSoundsPlugin : GenericPlugin
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

        public PlayniteSoundsSettings Settings;

        public PlayniteSoundsPlugin(IPlayniteAPI api) : base(api)
        {
            #region Initialize settings

            Settings = LoadPluginSettings<PlayniteSoundsSettings>() ?? new PlayniteSoundsSettings();

            Settings.DesktopSettings.IsDesktop = true;

            var isDesktop = api.ApplicationInfo.Mode is ApplicationMode.Desktop;

            Settings.ActiveModeSettings = isDesktop ? Settings.DesktopSettings : Settings.FullscreenSettings;
            Settings.CurrentUIStateSettings = Settings.ActiveModeSettings.UIStatesToSettings[UIState.Main];

            #endregion

            #region Install services

            _container = Installation.RegisterInstallers(api, this, Settings);

            _container.Resolve<ISoundPlayer>();
            _container.Resolve<IUriHandler>();
            _playniteEventHandler = LazyResolve<IPlayniteEventHandler>();
            _gameMenuFactory = LazyResolve<IGameMenuFactory>();
            _mainMenuFactory = LazyResolve<IMainMenuFactory>();
            _gameViewFactory = LazyResolve<IGameViewControlFactory>();

            #endregion

            #region Initialize plugin

            _api = api;
            SoundFile.CurrentPrefix = isDesktop ? SoundFile.DesktopPrefix : SoundFile.FullScreenPrefix;

            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };

            var customElementArgs = new AddCustomElementSupportArgs
            {
                SourceName = App.SourceName,
                ElementList = new List<string>
                {
                    "Handler"
                }
            };
            AddCustomElementSupport(customElementArgs);

            var settingsArgs = new AddSettingsSupportArgs
            {
                SourceName = App.SourceName,
                SettingsRoot = nameof(Settings)
            };
            AddSettingsSupport(settingsArgs);

            var converterArgs = new AddConvertersSupportArgs
            {
                SourceName = App.SourceName,
                Converters = new List<IValueConverter>
                {
                    _container.Resolve<IFocusConverter>(),
                    _container.Resolve<IButtonConverter>(),
                    _container.Resolve<ILinkSoundConverter>(),
                    _container.Resolve<IButtonLoadConverter>(),
                    _container.Resolve<IVisibilityConverter>(),
                    _container.Resolve<ILostFocusTickConverter>()
                }
            };
            AddConvertersSupport(converterArgs);

            Localization.SetPluginLanguage(api.ApplicationSettings.Language);

            #endregion
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
            => PlayniteEventHandler.OnGameInstalled(args.Game);

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
            => PlayniteEventHandler.OnGameUninstalled(args.Game);

        public override void OnGameSelected(OnGameSelectedEventArgs args)
            => PlayniteEventHandler.OnGameSelected(args.NewValue);

        public override void OnGameStarting(OnGameStartingEventArgs args)
            => PlayniteEventHandler.OnGameStarting(args.Game);

        public override void OnGameStarted(OnGameStartedEventArgs args)
            => PlayniteEventHandler.OnGameStarted(args.Game);

        public override void OnGameStopped(OnGameStoppedEventArgs args)
            => PlayniteEventHandler.OnGameStopped(args.Game);

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            var handler = _container.Resolve<IItemCollectionChangedHandler>();
            handler.ExtraMetaDataPluginIsLoaded = _api.Addons.Plugins.Any(p => p.Id.ToString() is App.ExtraMetaGuid);

            var appStateChangeHandler = _container.Resolve<IAppStateChangeHandler>();
            SystemEvents.PowerModeChanged += appStateChangeHandler.OnPowerModeChanged;
            Application.Current.MainWindow.StateChanged += appStateChangeHandler.OnWindowStateChanged;
            Application.Current.Deactivated += appStateChangeHandler.OnApplicationDeactivate;
            Application.Current.Activated += appStateChangeHandler.OnApplicationActivate;

            PlayniteEventHandler.OnApplicationStarted();
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            var appStateChangeHandler = _container.Resolve<IAppStateChangeHandler>();

            SystemEvents.PowerModeChanged -= appStateChangeHandler.OnPowerModeChanged;
            Application.Current.Deactivated -= appStateChangeHandler.OnApplicationDeactivate;
            Application.Current.Activated -= appStateChangeHandler.OnApplicationActivate;

            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.StateChanged -= appStateChangeHandler.OnWindowStateChanged;
            }

            PlayniteEventHandler.OnApplicationStopped();
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
            => PlayniteEventHandler.OnLibraryUpdated();

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
            => MainMenuFactory.GetMainMenuItems(args);

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
            => GameMenuFactory.GetGameMenuItems(args);

        public override Control GetGameViewControl(GetGameViewControlArgs args)
            => GameViewFactory.GetGameViewControl(args);

        #endregion

    }
}
