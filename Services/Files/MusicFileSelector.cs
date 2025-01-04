using Playnite.SDK;
using PlayniteSounds.Models;
using PlayniteSounds.Models.Audio.Sound;
using System;
using System.Linq;

namespace PlayniteSounds.Services.Files
{
    public class MusicFileSelector : IMusicFileSelector
    {
        private readonly PlayniteSoundsSettings _settings;
        private readonly IPathingService _pathingService;
        private readonly IMainViewAPI _mainView;

        public MusicFileSelector(IPathingService pathingService, IMainViewAPI mainViewApi, PlayniteSoundsSettings settings)
        {
            _pathingService = pathingService;
            _mainView = mainViewApi;
            _settings = settings;
        }

        private static readonly Random RNG = new Random();
        public string SelectFile(string[] files, string previousMusicFile, bool musicEnded)
        {
            var musicFile = files.FirstOrDefault() ?? previousMusicFile;

            var shouldRandomize = _settings.RandomizeOnEverySelect || (musicEnded && _settings.RandomizeOnMusicEnd);
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
            if (_settings.CurrentUIStateSettings.MusicSource is AudioSource.Default)
                /* Then */ return (Array.Empty<string>(), AudioSource.Default);

            if (_settings.CurrentUIStateSettings.MusicSource is AudioSource.Game)
            {
                var filterFiles = _pathingService.GeFilterMusicFiles(_mainView.GetActiveFilterPreset());
                if (filterFiles.Any()) /* Then */ return (filterFiles, AudioSource.Filter);
            }

            return (_pathingService.GetDefaultMusicFiles(), AudioSource.Default);
        }

        // Backup order is game -> filter -> default
        public string GetBackupFiles(AudioSource source, SoundType soundType, object resource = null)
        {
            if (source is AudioSource.Default) /* Then */ return null;

            if (source is AudioSource.Game)
            {
                var file = _pathingService.GetSoundTypeFile(AudioSource.Filter, soundType, resource);
                if (file != null)
                {
                    return file;
                }
            }

            return _pathingService.GetSoundTypeFile(AudioSource.Default, soundType, resource);
        }
    }
}
