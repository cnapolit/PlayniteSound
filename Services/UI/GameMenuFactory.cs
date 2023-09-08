using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteSounds.Common.Constants;
using PlayniteSounds.Common.Extensions;
using PlayniteSounds.Files.Download;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlayniteSounds.Services.UI
{
    public class GameMenuFactory : BaseMenuFactory, IGameMenuFactory
    {
        #region Infrastructure

        private readonly Lazy<List<GameMenuItem>> _gameMenuItems;
        private readonly PlayniteSoundsSettings   _settings;
        private readonly IDownloadManager         _downloadManager;
        private readonly INormalizer              _normalizer;

        public GameMenuFactory(
            IMainViewAPI mainViewApi,
            IMusicPlayer musicPlayer,
            IFileManager fileManager,
            IDownloadManager downloadManager,
            INormalizer normalizer,
            PlayniteSoundsSettings settings) : base(mainViewApi, fileManager, musicPlayer)
        {
            _downloadManager = downloadManager;
            _normalizer = normalizer;
            _settings = settings;

            _gameMenuItems = new Lazy<List<GameMenuItem>>(() => new List<GameMenuItem>
            {
                ConstructGameMenuItem("All",                               _ => DownloadMusicForSelectedGames(Source.All), "|" + Resource.Actions_Download),
                ConstructGameMenuItem("KHInsider",                         _ => DownloadMusicForSelectedGames(Source.KHInsider), "|" + Resource.Actions_Download),
                ConstructGameMenuItem(Resource.Youtube,                    SelectedAction(DownloadMusicFromYouTube), "|" + Resource.Actions_Download),
                //ConstructGameMenuItem("Test Download",                     CreateDialog,                             "|" + Resource.Actions_Download),
                ConstructGameMenuItem(Resource.ActionsCopySelectMusicFile, SelectedAction(_fileManager.SelectMusicForGames)),
                ConstructGameMenuItem(Resource.ActionsOpenSelected,        SelectedAction(_fileManager.OpenGameDirectories)),
                ConstructGameMenuItem(Resource.ActionsDeleteSelected,      SelectedAction(_fileManager.DeleteMusicDirectories)),
                ConstructGameMenuItem(Resource.Actions_Normalize,          _normalizer.CreateNormalizationDialogue)
            });
        }

        #endregion

        #region Implmentation

        #region GetGameMenuItems

        public IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs __)
        {
            foreach (var item in _gameMenuItems.Value) yield return item;

            if (_mainViewApi.SingleGame())
            {
                var game = _mainViewApi.SelectedGames.First();

                yield return ConstructGameMenuItem(
                    "Select 'GameStarting' sound", _ => _fileManager.SelectStartSoundForGame(game));

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
            => () => action(_mainViewApi.SelectedGames);

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
            => _downloadManager.DownloadMusicForGames(Source.Youtube, games.ToList());

        private void DownloadMusicForSelectedGames(Source source)
            => _downloadManager.DownloadMusicForGames(source, _mainViewApi.SelectedGames.ToList());

        private void CreateDialog()
        {
            //var window = _mainViewApi.Dialogs.CreateWindow(new WindowCreationOptions
            //{
            //    ShowMinimizeButton = false
            //});

            //window.Height = 600;
            //window.Width = 840;
            //window.Title = "Download Dialog";
            //window.Content = new DownloadPrompt();
            //window.DataContext = new DownloadPromptModel(_mainViewApi, _downloadManager, _fileManager) 
            //{ 
            //    PromptForAlbum = true, 
            //    PromptForSong = true,
            //    Window = window
            //};
            //window.Owner = _mainViewApi.Dialogs.GetCurrentAppWindow();
            //window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            //window.ShowDialog();
        }

        #endregion

        #endregion
    }
}
