﻿using Playnite.SDK.Models;
using PlayniteSounds.Common.Constants;
using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.Files;
using System;
using System.Collections.Generic;
using System.IO;

namespace PlayniteSounds.Services.UI;

public abstract class BaseMenuFactory(IFileManager fileManager, IMusicPlayer musicPlayer)
{
    #region Infrastructure

    protected readonly IFileManager _fileManager = fileManager;

    #endregion

    #region Implementation

    protected IEnumerable<TMenuItem> ConstructItems<TArgs, TMenuItem>(
        Func<string, Action<TArgs>, string, TMenuItem> menuItemConstructor,
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
                _ => musicPlayer.Play(file),
                songSubMenu);
            yield return menuItemConstructor(
                Resource.ActionsCopyDeleteMusicFile,
                _ => _fileManager.DeleteMusicFile(file, songName, game), 
                songSubMenu);
        }
    }

    #endregion
}