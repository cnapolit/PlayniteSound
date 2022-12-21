using Microsoft.Win32;
using Playnite.SDK;
using PlayniteSounds.Models;
using System.Linq;
using PlayniteSounds.Services.Audio;
using System.Windows;
using PlayniteSounds.Services.Files;
using PlayniteSounds.Common.Extensions;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using PlayniteSounds.Common.Constants;
using System.Collections.Generic;
using System.IO;
using System;

namespace PlayniteSounds.Services.State
{
    internal class PlayniteEventHandler : IPlayniteEventHandler
    {
        #region Infrastructure

        private readonly IPlayniteAPI           _api;
        private readonly IMusicPlayer           _musicPlayer;
        private readonly ISoundPlayer           _soundPlayer;
        private readonly IAppStateChangeHandler _appStateChangeHandler;
        private readonly IFileMutationService   _fileMutationService;
        private readonly IFileManager           _fileManager;
        private readonly IPathingService        _pathingService;
        private readonly PlayniteSoundsSettings _settings;
        private          bool                   _extraMetaDataPluginIsLoaded;
        private          bool                   _appStartedCompleted;

        public PlayniteEventHandler(
            IPlayniteAPI api,
            IAppStateChangeHandler appStateChangeHandler,
            IMusicPlayer musicPlayer,
            ISoundPlayer audioPlayer,
            IFileMutationService fileMutationService,
            IFileManager fileManager,
            IPathingService pathingService,
            PlayniteSoundsSettings settings)
        {
            _api = api;
            _appStateChangeHandler = appStateChangeHandler;
            _musicPlayer = musicPlayer;
            _soundPlayer = audioPlayer;
            _fileMutationService = fileMutationService;
            _fileManager = fileManager;
            _pathingService = pathingService;
            _settings = settings;

            api.Database.Games.ItemCollectionChanged += UpdateGames;
            api.Database.Platforms.ItemCollectionChanged += UpdatePlatforms;
            api.Database.FilterPresets.ItemCollectionChanged += UpdateFilters;

            api.UriHandler.RegisterSource("Sounds", HandleUriEvent);
        }

        #endregion

        #region Implementation

        #region OnGameInstalled

        public void OnGameInstalled()
            => _soundPlayer.PlayGameInstalled();

        #endregion

        #region OnGameUninstalled

        public void OnGameUninstalled()
            => _soundPlayer.PlayGameUnInstalled();

        #endregion

        #region OnGameSelected

        public void OnGameSelected()
        {
            _soundPlayer.PlayGameSelected();

            if (_appStartedCompleted)
            {
                _musicPlayer.Play(_api.SelectedGames());
            }
        }

        #endregion

        #region OnGameStarted

        public void OnGameStarted()
        {
            if (_settings.StopMusic)
            {
                _musicPlayer.Pause(true);
            }

            _soundPlayer.PlayGameStarted();
        }

        #endregion

        #region OnGameStarting

        public void OnGameStarting()
        {
            if (!_settings.StopMusic)
            {
                _musicPlayer.Pause(true);
            }

            _soundPlayer.PlayGameStarting();
        }

        #endregion

        #region OnGameStopped

        public void OnGameStopped()
        {
            _soundPlayer.PlayGameStopped();
            _musicPlayer.Resume(true);
        }

        #endregion

        #region OnApplicationStarted

        public void OnApplicationStarted()
        {
            _fileManager.CopyAudioFiles();

            _soundPlayer.PlayAppStarted(AppStartedEnded);

            _extraMetaDataPluginIsLoaded = _api.Addons.Plugins.Any(p => p.Id.ToString() is App.ExtraMetaGuid);

            SystemEvents.PowerModeChanged += _appStateChangeHandler.OnPowerModeChanged;
            Application.Current.MainWindow.StateChanged += _appStateChangeHandler.OnWindowStateChanged;
            Application.Current.Deactivated += _appStateChangeHandler.OnApplicationDeactivate;
            Application.Current.Activated += _appStateChangeHandler.OnApplicationActivate;
        }

        private void AppStartedEnded(object _, EventArgs __)
        {
            _appStartedCompleted = true;
            _musicPlayer.Play(_api.SelectedGames());
        }

        #endregion

        #region OnApplicationStopped

        public void OnApplicationStopped()
        {
            SystemEvents.PowerModeChanged -= _appStateChangeHandler.OnPowerModeChanged;
            Application.Current.Deactivated -= _appStateChangeHandler.OnApplicationDeactivate;
            Application.Current.Activated -= _appStateChangeHandler.OnApplicationActivate;

            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.StateChanged -= _appStateChangeHandler.OnWindowStateChanged;
            }

            _musicPlayer.Dispose();
            _soundPlayer.PlayAppStopped();
            _soundPlayer.Close();
        }

        #region OnLibraryUpdated

        public void OnLibraryUpdated()
        {
            if (_settings.AutoDownload)
            {
                var games = _api.Database.Games.Where(
                    x => x.Added != null && x.Added > _settings.LastAutoLibUpdateAssetsDownload);
                _fileMutationService.CreateDownloadDialogue(games, Source.All);
            }

            _soundPlayer.PlayLibraryUpdated();
        }

        #endregion

        #endregion

        #region Helpers

        #region Callback Methods

        private void UpdateGames(object sender, ItemCollectionChangedEventArgs<Game> ItemCollectionChangedArgs)
        {
            // Let ExtraMetaDataLoader handle cleanup if it exists
            if (_extraMetaDataPluginIsLoaded)
            {
                return;
            }

            foreach (var removedItem in ItemCollectionChangedArgs.RemovedItems)
            {
                _fileManager.DeleteMusicDirectory(removedItem);
            }
        }

        private void UpdatePlatforms(object sender, ItemCollectionChangedEventArgs<Platform> ItemCollectionChangedArgs)
        {
            foreach (var addedItem in ItemCollectionChangedArgs.AddedItems)
            {
                _fileManager.CreatePlatformDirectory(addedItem);
            }

            DeleteDirectories(ItemCollectionChangedArgs.RemovedItems, _pathingService.GetPlatformDirectoryPath);
        }

        private void UpdateFilters(object _, ItemCollectionChangedEventArgs<FilterPreset> ItemCollectionChangedArgs)
        {
            foreach (var addedItem in ItemCollectionChangedArgs.AddedItems)
            {
                _fileManager.CreateFilterDirectory(addedItem);
            }

            DeleteDirectories(ItemCollectionChangedArgs.RemovedItems, _pathingService.GetFilterDirectoryPath);
        }

        private void DeleteDirectories<T>(IEnumerable<T> directoryLinks, Func<T, string> PathConstructor)
            => directoryLinks.
                Select(PathConstructor).
                Where(Directory.Exists).
                ForEach(f => Directory.Delete(f, true));

        // ex: playnite://Sounds/Play/someId
        // Sounds maintains a list of plugins who want the music paused and will only allow play when
        // no other plugins have paused.
        private void HandleUriEvent(PlayniteUriEventArgs args)
        {
            var action = args.Arguments[0];
            var senderId = args.Arguments[1];

            switch (action.ToLower())
            {
                case "play": _musicPlayer.Resume(senderId); break;
                case "pause": _musicPlayer.Pause(senderId); break;
            }
        }

        #endregion

        #endregion

        #endregion
    }
}
