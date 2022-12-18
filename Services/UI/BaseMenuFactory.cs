using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteSounds.Common.Constants;
using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.Files;
using System;
using System.Collections.Generic;
using System.IO;

namespace PlayniteSounds.Services.UI
{
    internal abstract class BaseMenuFactory
    {
        #region Infrastructure

        protected readonly IPlayniteAPI         _api;
        protected readonly IFileMutationService _fileMutationService;
        protected readonly IFileManager         _fileManager;
        private   readonly IMusicPlayer         _musicPlayer;

        public BaseMenuFactory(
            IPlayniteAPI api,
            IFileMutationService fileMutationService,
            IFileManager fileManager,
            IMusicPlayer musicPlayer)
        {
            _api = api;
            _fileMutationService = fileMutationService;
            _fileManager = fileManager;
            _musicPlayer = musicPlayer;
        }

        #endregion

        #region Implementation

        protected IEnumerable<TMenuItem> ConstructItems<TMenuItem>(
            Func<string, Action, string, TMenuItem> menuItemConstructor,
            string[] files,
            string subMenu,
            Game game = null)
        {
            foreach (var file in files)
            {
                var songName = Path.GetFileNameWithoutExtension(file);
                var songSubMenu = subMenu + songName;

                yield return menuItemConstructor(
                    Resource.ActionsCopyPlayMusicFile,
                    () => _musicPlayer.CurrentMusicFile = file,
                    songSubMenu);
                yield return menuItemConstructor(
                    Resource.ActionsCopyDeleteMusicFile,
                    () => _fileMutationService.DeleteMusicFile(file, songName, game), 
                    songSubMenu);
            }
        }

        #endregion
    }
}
