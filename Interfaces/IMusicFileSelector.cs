using PlayniteSounds.Models;

namespace PlayniteSounds.Services.Files
{
    public interface IMusicFileSelector
    {
        (string[], AudioSource) GetBackupFiles();
        string SelectFile(string[] files, string previousMusicFile, bool musicEnded);
    }
}