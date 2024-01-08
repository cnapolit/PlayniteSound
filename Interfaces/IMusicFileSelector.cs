using PlayniteSounds.Models;
using PlayniteSounds.Models.Audio.Sound;

namespace PlayniteSounds.Services.Files
{
    public interface IMusicFileSelector
    {
        (string[], AudioSource) GetBackupFiles();
        string GetBackupFiles(AudioSource source, SoundType soundType, object resource = null);
        string SelectFile(string[] files, string previousMusicFile, bool musicEnded);
    }
}