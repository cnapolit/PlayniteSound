using Playnite.SDK;
using PlayniteSounds.Models;
using PlayniteSounds.Models.Audio.Sound;
using System;
using System.Linq;

namespace PlayniteSounds.Services.Files;

public class MusicFileSelector(
    IPathingService pathingService,
    IMainViewAPI mainViewApi,
    PlayniteSoundsSettings settings)
    : IMusicFileSelector
{
    private static readonly Random RNG = new();
    public string SelectFile(string[] files, string previousMusicFile, bool musicEnded)
    {
        var musicFile = files.FirstOrDefault() ?? previousMusicFile;

        var shouldRandomize = settings.RandomizeOnEverySelect || (musicEnded && settings.RandomizeOnMusicEnd);
        if (files.Length > 1 && shouldRandomize) do
            {
                musicFile = files[RNG.Next(files.Length)];
            }
            while (previousMusicFile == musicFile);

        return musicFile;
    }

    // Backup order is game -> filter -> default
    public (string[], AudioSource) GetBackupFiles()
    {
        if (settings.CurrentUIStateSettings.MusicSource is AudioSource.Default)
            /* Then */ return ([], AudioSource.Default);

        if (settings.CurrentUIStateSettings.MusicSource is AudioSource.Game)
        {
            var filterFiles = pathingService.GeFilterMusicFiles(mainViewApi.GetActiveFilterPreset());
            if (filterFiles.Any()) /* Then */ return (filterFiles, AudioSource.Filter);
        }

        return (pathingService.GetDefaultMusicFiles(), AudioSource.Default);
    }

    // Backup order is game -> filter -> default
    public string GetBackupFiles(AudioSource source, SoundType soundType, object resource = null)
    {
        if (source is AudioSource.Default) /* Then */ return null;

        if (source is AudioSource.Game)
        {
            var file = pathingService.GetSoundTypeFile(AudioSource.Filter, soundType, resource);
            if (file != null)
            {
                return file;
            }
        }

        return pathingService.GetSoundTypeFile(AudioSource.Default, soundType, resource);
    }
}