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
using System.Threading;
using System.Windows;
using PlayniteSounds.Services.State;

namespace PlayniteSounds.Services.UI
{
    public class GameMenuFactory : BaseMenuFactory, IGameMenuFactory
    {
        #region Infrastructure

        private readonly IDialogsFactory                       _dialogsFactory;
        private readonly IItemCollectionChangedHandler         _itemCollectionChangedHandler;
        private readonly IFactoryExecutor<DownloadPromptModel> _factoryExecutor;
        private readonly Lazy<List<GameMenuItem>>              _gameMenuItems;

        public GameMenuFactory(
            IMusicPlayer musicPlayer,
            IFileManager fileManager,
            INormalizer normalizer,
            IDialogsFactory dialogsFactory,
            IFactoryExecutor<DownloadPromptModel> factoryExecutor,
            IItemCollectionChangedHandler itemCollectionChangedHandler) : base(fileManager, musicPlayer)
        {
            _dialogsFactory = dialogsFactory;
            _factoryExecutor = factoryExecutor;
            _itemCollectionChangedHandler = itemCollectionChangedHandler;

            _gameMenuItems = new Lazy<List<GameMenuItem>>(() => new List<GameMenuItem>
            {
                ConstructGameMenuDownloadItem("Browse...",                 CreateDialog),
                ConstructGameMenuDownloadItem("Default",                   a => DownloadMusicForSelectedGames(a, null)),
                ConstructGameMenuDownloadItem("All",                       a => DownloadMusicForSelectedGames(a, Source.All)),
                ConstructGameMenuDownloadItem("KHInsider",                 a => DownloadMusicForSelectedGames(a, Source.KHInsider)),
                ConstructGameMenuDownloadItem("Sound Cloud",               a => DownloadMusicForSelectedGames(a, Source.SoundCloud)),
                ConstructGameMenuDownloadItem(Resource.Youtube,            a => DownloadMusicForSelectedGames(a, Source.Youtube)),
                ConstructGameMenuDownloadItem("Spotify",                   a => DownloadMusicForSelectedGames(a, Source.Spotify)),
                ConstructGameMenuItem(Resource.ActionsCopySelectMusicFile, SelectedAction(_fileManager.SelectMusicForGames)),
                ConstructGameMenuItem(Resource.ActionsOpenSelected,        SelectedAction(_fileManager.OpenGameDirectories)),
                ConstructGameMenuItem(Resource.ActionsDeleteSelected,      SelectedAction(_fileManager.DeleteMusicDirectories)),
                ConstructGameMenuItem(Resource.Actions_Normalize,          normalizer.CreateNormalizationDialogue),
                //ConstructGameMenuItem("Test Start Audio Selection",        CreateStartTest),
                //ConstructGameMenuItem("Test End Audio Selection",          CreateEndTest),
            });
        }

        #endregion

        #region Implmentation

        #region GetGameMenuItems

        public IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            foreach (var item in _gameMenuItems.Value) yield return item;

            if (args.Games.Count is 1)
            {
                var game = args.Games[0];

                //yield return ConstructGameMenuItem(
                //    "Select 'GameStarting' sound", _ => _fileManager.SelectStartSoundForGame(game));

                var files = Directory.GetFiles(_fileManager.CreateMusicDirectory(game));
                if (files.Any())
                {
                    yield return new GameMenuItem { Description = "-", MenuSection = App.AppName };
                    foreach (var item in ConstructItems<GameMenuItemActionArgs, GameMenuItem>(ConstructGameMenuItem, files, "|", game)) yield return item;
                }
            }
        }

        #endregion

        #region Helpers

        private static Action<GameMenuItemActionArgs> SelectedAction(Action<IEnumerable<Game>> action)
            => a => action(a.Games);

        private static GameMenuItem ConstructGameMenuDownloadItem(string resource, Action action)
            => ConstructGameMenuItem(resource, action, "|" + Resource.Actions_Download);

        private static GameMenuItem ConstructGameMenuItem(string resource, Action action, string subMenu = "")
            => ConstructGameMenuItem(resource, _ => action(), subMenu);

        private static GameMenuItem ConstructGameMenuDownloadItem(string resource, Action<GameMenuItemActionArgs> action)
            => ConstructGameMenuItem(resource, action, "|" + Resource.Actions_Download);

        private static GameMenuItem ConstructGameMenuItem(
            string resource, Action<GameMenuItemActionArgs> action, string subMenu = "") => new GameMenuItem
        {
            MenuSection = App.AppName + subMenu,
            Icon = SoundDirectory.IconPath,
            Description = resource,
            Action = action
        };

        private void DownloadMusicForSelectedGames(GameMenuItemActionArgs args, Source? source)
            => _itemCollectionChangedHandler.AddGames(args.Games, source);

        private void CreateStartTest()
        {
            var selector = new StartSoundSelector();
            var (start, end) = selector.SelectStartSound(@"C:\Users\bandg\Downloads\1-14. Run the Table.flac", StartSoundSelector.SelectStartAlgorithm.StartTrimSilence);
            _dialogsFactory.ShowMessage($"Start: {start}, End: {end}");
        }
        private void CreateEndTest()
        {
            var selector = new StartSoundSelector();
            var (start, end) = selector.SelectStartSound(@"C:\Users\bandg\Downloads\1-14. Run the Table.flac", StartSoundSelector.SelectStartAlgorithm.EndTrimSilence);
            _dialogsFactory.ShowMessage($"Start: {start}, End: {end}");
        }

        private void CreateDialog() => _factoryExecutor.Execute(CreateDownloadPrompt);

        private void CreateDownloadPrompt(DownloadPromptModel model)
        {
            var window = _dialogsFactory.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false
            });

            window.Height = 600;
            window.Width = 1000;
            window.Title = "Download Dialog";
            window.Content = new DownloadPrompt();


            window.DataContext = model;
            model.Dispatcher = window.Dispatcher;

            window.Owner = _dialogsFactory.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            window.ShowDialog();
        }

        #endregion

        #endregion
    }
}
