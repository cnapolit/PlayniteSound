﻿using System.IO;
using System.Reflection;

namespace PlayniteSounds.Common.Constants;

public static class SoundDirectory
{
    public const string NoPlatform = "No Platform";
    public const string Music = "Music Files";
    public const string Platform = "Platform";
    public const string Filter = "Filter";
    public const string Default = "Default";
    public const string Sound = "Sound Files";
    public const string SoundManager = "Sound Manager";
    public const string Localization = "Localization";
    public const string Orphans = "Orphans";
    public const string ExtraMetaData = "ExtraMetadata";
    public const string GamesFolder = "Games";

    public const string SoundsFolder = "Sounds";

    public const string GameStartingSoundFolder = SoundsFolder + "GameStarting";
    public const string GameStartedSoundFolder = SoundsFolder + "GameStarted";
    public const string AppStartedSoundFolder = SoundsFolder + "StartingSound";
    public const string AppStartingSoundFolder = SoundsFolder + "StartingSound";
    public const string StartingSoundFolder = SoundsFolder + "StartingSound";

    public static readonly string PluginFolder   = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    public static readonly string ResourceFolder = Path.Combine(PluginFolder,   "Resources");
    public static readonly string ImagesFolder   = Path.Combine(ResourceFolder, "Images");
    public static readonly string AudioFolder    = Path.Combine(ResourceFolder, "Audio");
    public static readonly string IconPath       = Path.Combine(ImagesFolder,   "icon.png");
}