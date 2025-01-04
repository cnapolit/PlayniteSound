using Playnite.SDK.Models;
using PlayniteSounds.Models;
using PlayniteSounds.Models.Audio.Sound;
using System;
using System.Collections.Generic;

namespace PlayniteSounds.Services.Files
{
    public interface IPathingService
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

        string[]              GetGameMusicFiles(Game game);
        string[]              GetPlatformMusicFiles(Platform platform);
        string[]              GeFilterMusicFiles(FilterPreset filter);
        string[]              GetDefaultMusicFiles();
        string[]              GeFilterMusicFiles(Guid filterId);
        string                GetFilterDirectoryPath(Guid filterId);
        string[]              GetSoundFiles();
        string                GetSoundTypeFile(AudioSource source, SoundType soundType, object resource = null);
        string[]              GetPlatformMusicFiles(string platform);
        string                GetLibraryFile(string fileSubpath);
        IEnumerable<SongFile> GetMusicFiles(Game game);
        IEnumerable<SongFile> GetAllMusicFiles(string dir);
        string                GetImageFile(string fileName);
    }
}
