using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Windows.Controls;
using Playnite.SDK.Events;
using PlayniteSounds.Common.Constants;
using PlayniteSounds.Models;
using PlayniteSounds.Services;
using PlayniteSounds.Services.UI;
using Castle.Windsor;
using Castle.MicroKernel.Registration;
using PlayniteSounds.Services.Files;
using PlayniteSounds.Services.Audio;
using PlayniteSounds.Common;
using System.Collections.Generic;
using PlayniteSounds.Views.Models;
using PlayniteSounds.Views.Layouts;
using PlayniteSounds.Services.State;
using PlayniteSounds.Services.Play;
using PlayniteSounds.Files.Download;

namespace PlayniteSounds
{
    public class PlayniteSounds : GenericPlugin
    {
        #region Infrastructure

        private readonly IWindsorContainer               _container;
        private readonly IPlayniteEventHandler           _playniteEventHandler;
        private readonly IGameMenuFactory                _gameMenuFactory;
        private readonly IMainMenuFactory                _mainMenuFactory;
        private readonly PlayniteSoundsSettingsViewModel _settingsModel;

        public PlayniteSounds(IPlayniteAPI api) : base(api)
        {
            var isDesktop = api.ApplicationInfo.Mode is ApplicationMode.Desktop;
            SoundFile.CurrentPrefix = isDesktop ? SoundFile.DesktopPrefix : SoundFile.FullScreenPrefix;
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };

            Localization.SetPluginLanguage(api.ApplicationSettings.Language);

            _container = new WindsorContainer();

            var isDesktopDepedency = Dependency.OnValue("isDesktop", isDesktop);
            var settings = LoadPluginSettings<PlayniteSoundsSettings>() ?? new PlayniteSoundsSettings();

            _container.Register(
                RegisterSingleton<IAppStateChangeHandler, AppStateChangeHandler>(),
                RegisterSingleton<IPlayniteEventHandler, PlayniteEventHandler>(),
                RegisterSingleton<IGameMenuFactory, GameMenuFactory>(),
                RegisterSingleton<IMainMenuFactory, MainMenuFactory>(),
                RegisterSingleton<IFileMutationService, FileMutationService>(),
                RegisterSingleton<IFileManager, FileManager>(),
                RegisterSingleton<IPathingService, PathingService>(),
                RegisterSingleton<INormalizer, Normalizer>(),
                RegisterSingleton<IDownloadManager, DownloadManager>(),
                RegisterSingleton<IPromptFactory, PromptFactory>(),
                RegisterSingleton<IErrorHandler, ErrorHandler>(),
                RegisterSingleton<ITagger, Tagger>(),
                RegisterSingleton<ISoundManager, SoundManager>(),
                RegisterInstance(this),
                RegisterInstance(api),
                RegisterInstance(settings),
                Component.For<PlayniteSoundsSettingsViewModel>().LifestyleSingleton(),
                Component.For<SoundSettingsView>(),
                Component.For<MusicSettingsView>(),
                Component.For<PlayniteSoundsSettingsView>(),
                Component.For<IMusicPlayer>().
                    ImplementedBy<MusicPlayer>().
                    DependsOn(isDesktopDepedency).
                    LifestyleSingleton(),
                Component.For<ISoundPlayer>().
                    ImplementedBy<SoundPlayer>().
                    DependsOn(isDesktopDepedency).
                    LifestyleSingleton());

            _settingsModel = _container.Resolve<PlayniteSoundsSettingsViewModel>();
            _playniteEventHandler = _container.Resolve<IPlayniteEventHandler>();
            _gameMenuFactory = _container.Resolve<IGameMenuFactory>();
            _mainMenuFactory = _container.Resolve<IMainMenuFactory>();
        }

        private static IRegistration RegisterSingleton<TInterface, TImplementation>() 
          where TInterface : class
          where TImplementation : TInterface
            => Component.For<TInterface>().ImplementedBy<TImplementation>().LifestyleSingleton();
        private static IRegistration RegisterInstance<T>(T instance) where T : class
            => Component.For<T>().Instance(instance).LifestyleSingleton();

        #endregion

        #region Playnite Implementation

        public override Guid Id { get; } = Guid.Parse(App.AppGuid);

        public override ISettings GetSettings(bool firstRunSettings) => _settingsModel;

        public override UserControl GetSettingsView(bool firstRunSettings)
            => _container.Resolve<PlayniteSoundsSettingsView>();

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
            => _playniteEventHandler.OnGameInstalled();

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
            => _playniteEventHandler.OnGameUninstalled();

        public override void OnGameSelected(OnGameSelectedEventArgs args)
            => _playniteEventHandler.OnGameSelected();

        public override void OnGameStarted(OnGameStartedEventArgs args)
            => _playniteEventHandler.OnGameStarted();

        public override void OnGameStarting(OnGameStartingEventArgs args)
            => _playniteEventHandler.OnGameStarting();

        public override void OnGameStopped(OnGameStoppedEventArgs args)
            => _playniteEventHandler.OnGameStopped();

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
            => _playniteEventHandler.OnApplicationStarted();

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
            => _playniteEventHandler.OnApplicationStopped();

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            _settingsModel.Settings.LastAutoLibUpdateAssetsDownload = DateTime.Now;
            SavePluginSettings(_settingsModel.Settings);

            _playniteEventHandler.OnLibraryUpdated();
        }

        public void UpdateSettings(PlayniteSoundsSettings settings) 
            => _container.Resolve<PlayniteSoundsSettings>().Copy(settings);

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
            => _mainMenuFactory.GetMainMenuItems(args);

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
            => _gameMenuFactory.GetGameMenuItems(args);

        #endregion

    }
}
