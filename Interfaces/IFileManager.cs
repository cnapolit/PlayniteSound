using Playnite.SDK.Models;
using System.Collections.Generic;

namespace PlayniteSounds.Services.Files
{
    public interface IFileManager
    {
        void CopyAudioFiles();
        string CreateFilterDirectory(FilterPreset filter);
        string CreateMusicDirectory(Game game);
        string CreatePlatformDirectory(Platform platform);
        string CreatePlatformDirectoryPathFromGame(Game game);
        bool DeleteMusicDirectory(Game game);
        void DeleteMusicFile(string musicFile, string musicFileName, Game game);
        void OpenGameDirectories(IEnumerable<Game> games);
        void SelectMusicForDefault();
        void SelectMusicForFilter(FilterPreset filter);
        void SelectMusicForPlatform(Platform platform);
        void SelectMusicForGames(IEnumerable<Game> games);
        string SelectStartSoundForGame(Game game);
        void DeleteMusicDirectories(IEnumerable<Game> games);
    }
}
