using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteSounds.Common.Constants;
using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlayniteSounds.Services.UI
{
    public class MainMenuFactory : BaseMenuFactory, IMainMenuFactory
    {
        #region Infrastructure

        private readonly IGameDatabaseAPI         _gameDatabaseApi;
        private readonly IPathingService          _pathingService;
        private readonly ISoundPlayer             _soundPlayer;
        private readonly ISoundManager            _soundManager;
        private readonly Lazy<List<MainMenuItem>> _mainMenuItems;

        public MainMenuFactory(
            IMainViewAPI mainViewApi,
            IGameDatabaseAPI gameDatabaseApi,
            IPathingService pathingService,
            IFileManager fileManager,
            IMusicPlayer musicPlayer,
            ISoundPlayer audioPlayer,
            ISoundManager soundManager) : base(mainViewApi, fileManager, musicPlayer)
        {
            _gameDatabaseApi = gameDatabaseApi;
            _pathingService = pathingService;
            _soundPlayer = audioPlayer;
            _soundManager = soundManager;

            _mainMenuItems = new Lazy<List<MainMenuItem>>(() => new List<MainMenuItem>
            {
                ConstructMainMenuItem(Resource.ActionsOpenMusicFolder,     _soundManager.OpenMusicFolder),
                ConstructMainMenuItem(Resource.ActionsOpenSoundsFolder,    _soundManager.OpenSoundsFolder),
                ConstructMainMenuItem(Resource.ActionsHelp,                _soundManager.HelpMenu),
                new MainMenuItem { Description = "-", MenuSection = App.MainMenuName },
                ConstructMainMenuItem(Resource.ActionsCopySelectMusicFile, _fileManager.SelectMusicForDefault, "|" + Resource.ActionsDefault),
            });
        }

        #endregion

        #region Implementation

        #region GetMainMenuItems

        public IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs __)
        {
            var mainMenuItems = new List<MainMenuItem>(_mainMenuItems.Value);

            mainMenuItems.AddRange(CreateDirectoryMainMenuItems(
                _gameDatabaseApi.Platforms,
                Resource.ActionsPlatform,
                _fileManager.CreatePlatformDirectory,
                _fileManager.SelectMusicForPlatform));

            mainMenuItems.AddRange(CreateDirectoryMainMenuItems(
                _gameDatabaseApi.FilterPresets,
                Resource.ActionsFilter,
                _fileManager.CreateFilterDirectory,
                _fileManager.SelectMusicForFilter));

            var defaultSubMenu = $"|{Resource.ActionsDefault}";
            var defaultFiles = Directory.GetFiles(_pathingService.DefaultMusicPath);
            if (defaultFiles.Any())
            {
                mainMenuItems.Add(new MainMenuItem
                {
                    Description = "-",
                    MenuSection = App.MainMenuName + defaultSubMenu
                });
                mainMenuItems.AddRange(ConstructItems(ConstructMainMenuItem, defaultFiles, defaultSubMenu + "|"));
            }

            return mainMenuItems;
        }

        #endregion

        #region Helpers

        private IEnumerable<MainMenuItem> CreateDirectoryMainMenuItems<T>(
            IEnumerable<T> databaseObjects,
            string menuPath,
            Func<T, string> directoryConstructor,
            Action<T> musicSelector) where T : DatabaseObject
        {
            foreach (var databaseObject in databaseObjects.OrderBy(o => o.Name))
            {
                var directorySelect = $"|{menuPath}|{databaseObject.Name}";

                yield return ConstructMainMenuItem(
                    Resource.ActionsCopySelectMusicFile,
                    () => musicSelector(databaseObject),
                    directorySelect);

                var files = Directory.GetFiles(directoryConstructor(databaseObject));
                if (files.Any())
                {
                    yield return new MainMenuItem
                    {
                        Description = "-",
                        MenuSection = App.MainMenuName + directorySelect
                    };

                    foreach (var item in ConstructItems(ConstructMainMenuItem, files, directorySelect + "|"))
                    {
                        yield return item;
                    }
                }
            }
        }

        private static MainMenuItem ConstructMainMenuItem(string resource, Action action, string subMenu = "")
            => ConstructMainMenuItem(resource, _ => action(), subMenu);
        private static MainMenuItem ConstructMainMenuItem(
            string resource, Action<MainMenuItemActionArgs> action, string subMenu = "") => new MainMenuItem
        {
            MenuSection = App.MainMenuName + subMenu,
            Icon = SoundDirectory.IconPath,
            Description = resource,
            Action = action
        };

        #endregion

        #endregion
    }
}
