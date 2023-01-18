using Playnite.SDK.Models;
using System.Collections.Generic;

namespace PlayniteSounds.Services.Files
{
    internal interface IFileManager
    {
        void CopyAudioFiles();
        string CreateFilterDirectory(FilterPreset filter);
        string CreateMusicDirectory(Game game);
        string CreatePlatformDirectory(Platform platform);
        string CreatePlatformDirectoryPathFromGame(Game game);
        IEnumerable<Game> DeleteMusicDirectories(IEnumerable<Game> games);
        bool DeleteMusicDirectory(Game game);
        void DeleteMusicFile(string musicFile, string musicFileName, Game game);
        void OpenGameDirectories(IEnumerable<Game> games);
        IEnumerable<string> SelectMusicForDefault(IEnumerable<string> files);
        IEnumerable<string> SelectMusicForFilter(FilterPreset filter, IEnumerable<string> files);
        IEnumerable<string> SelectMusicForGame(Game game, IEnumerable<string> files);
        string SelectStartSoundForGame(Game game, string file);
        IEnumerable<string> SelectMusicForPlatform(Platform platform, IEnumerable<string> files);
    }
}
