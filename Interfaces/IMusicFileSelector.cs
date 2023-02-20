using PlayniteSounds.Models;

namespace PlayniteSounds.Services.Files
{
    public interface IMusicFileSelector
    {
        (string[], MusicType) GetBackupFiles();
        string SelectFile(string[] files, string previousMusicFile, bool musicEnded);
    }
}