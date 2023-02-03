using PlayniteSounds.Models;

namespace PlayniteSounds.Services.Files
{
    internal interface IMusicFileSelector
    {
        (string[], MusicType) GetBackupFiles();
        string SelectFile(string[] files, string previousMusicFile, bool musicEnded);
    }
}