using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteSounds.Common.Constants;
using PlayniteSounds.Common.Extensions;
using PlayniteSounds.Files.Download;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.Files;
using PlayniteSounds.Views.Layouts;
using PlayniteSounds.Views.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace PlayniteSounds.Services.UI
{
    public class GameMenuFactory : BaseMenuFactory, IGameMenuFactory
    {
        #region Infrastructure

        private readonly Lazy<List<GameMenuItem>> _gameMenuItems;
        private readonly PlayniteSoundsSettings   _settings;
        private readonly IDownloadManager         _downloadManager;

        public GameMenuFactory(
            IPlayniteAPI api,
            IMusicPlayer musicPlayer,
            IFileMutationService fileMutationService,
            IFileManager fileManager,
            IDownloadManager downloadManager,
            PlayniteSoundsSettings settings) : base(api, fileMutationService, fileManager, musicPlayer)
        {
            _downloadManager = downloadManager;
            _settings = settings;

            _gameMenuItems = new Lazy<List<GameMenuItem>>(() => new List<GameMenuItem>
            {
                ConstructGameMenuItem(Resource.Youtube,                    SelectedAction(DownloadMusicFromYouTube), "|" + Resource.Actions_Download),
                ConstructGameMenuItem("Test Download",                     CreateDialog,                             "|" + Resource.Actions_Download),
                ConstructGameMenuItem(Resource.ActionsCopySelectMusicFile, SelectedAction(_fileMutationService.SelectMusicForGames)),
                ConstructGameMenuItem(Resource.ActionsOpenSelected,        SelectedAction(_fileManager.OpenGameDirectories)),
                ConstructGameMenuItem(Resource.ActionsDeleteSelected,      SelectedAction(_fileMutationService.DeleteMusicDirectories)),
                ConstructGameMenuItem(Resource.Actions_Normalize,          _fileMutationService.CreateNormalizationDialogue),
            });
        }

        #endregion

        #region Implmentation

        #region GetGameMenuItems

        public IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs __)
        {
            if (_settings.Downloaders.Contains(Source.KHInsider))
            {
                yield return ConstructGameMenuItem(
                    "All", _ => DownloadMusicForSelectedGames(Source.All), 
                    "|" + Resource.Actions_Download);
                yield return ConstructGameMenuItem(
                    "KHInsider",
                    _ => DownloadMusicForSelectedGames(Source.KHInsider),
                    "|" + Resource.Actions_Download);
            }

            foreach (var item in _gameMenuItems.Value) yield return item;

            if (_api.SingleGame())
            {
                var game = _api.SelectedGames().First();

                yield return ConstructGameMenuItem(
                    "Select 'GameStarting' sound", _ => _fileMutationService.SelectStartSoundForGame(game));

                var files = Directory.GetFiles(_fileManager.CreateMusicDirectory(game));
                if (files.Any())
                {
                    yield return new GameMenuItem { Description = "-", MenuSection = App.AppName };
                    foreach (var item in ConstructItems(ConstructGameMenuItem, files, "|", game)) yield return item;
                }
            }
        }

        #endregion

        #region Helpers

        private Action SelectedAction(Action<IEnumerable<Game>> action)
            => () => action(_api.SelectedGames());

        private static GameMenuItem ConstructGameMenuItem(string resource, Action action, string subMenu = "")
            => ConstructGameMenuItem(resource, _ => action(), subMenu);
        private static GameMenuItem ConstructGameMenuItem(
            string resource, Action<GameMenuItemActionArgs> action, string subMenu = "") => new GameMenuItem
        {
            MenuSection = App.AppName + subMenu,
            Icon = SoundDirectory.IconPath,
            Description = resource,
            Action = action
        };

        private void DownloadMusicFromYouTube(IEnumerable<Game> games)
            => _fileMutationService.DownloadMusicForGames(Source.Youtube, games.ToList());

        private void DownloadMusicForSelectedGames(Source source)
            => _fileMutationService.DownloadMusicForGames(source, _api.SelectedGames().ToList());

        private void CreateDialog()
        {
            var window = _api.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false
            });

            window.Height = 600;
            window.Width = 840;
            window.Title = "Download Dialog";
            window.Content = new DownloadPrompt();
            window.DataContext = new DownloadPromptModel(_api, _downloadManager, _fileManager) 
            { 
                PromptForAlbum = true, 
                PromptForSong = true,
                Window = window
            };
            window.Owner = _api.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            window.ShowDialog();

        }

        #endregion

        #endregion
    }
}
