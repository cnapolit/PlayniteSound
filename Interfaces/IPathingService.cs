using Playnite.SDK.Models;
using System;

namespace PlayniteSounds.Services.Files
{
    internal interface IPathingService
    {
        string ExtraMetaDataFolder { get; }
        string MusicFilesDataPath { get; }
        string SoundFilesDataPath { get; }
        string SoundManagerFilesDataPath { get; }
        string DefaultMusicPath { get; }
        string GameMusicFilePath { get; }
        string PlatformMusicFilePath { get; }
        string FilterMusicFilePath { get; }

        string GetGameDirectoryPath(Game game);
        string GetPlatformDirectoryPath(Platform platform);
        string GetFilterDirectoryPath(FilterPreset filter);

        string[] GetGameMusicFiles(Game game);
        string[] GetPlatformMusicFiles(Platform platform);
        string[] GeFilterMusicFiles(FilterPreset filter);
        string[] GetDefaultMusicFiles();
        string[] GeFilterMusicFiles(Guid filterId);
        string GetFilterDirectoryPath(Guid filterId);
        string GetGameStartSoundFile(Game game);
        string[] GetSoundFiles();
    }
}
