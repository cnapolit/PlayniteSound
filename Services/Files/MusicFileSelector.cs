using Playnite.SDK;
using PlayniteSounds.Models;
using System;
using System.Linq;

namespace PlayniteSounds.Services.Files
{
    internal class MusicFileSelector : IMusicFileSelector
    {
        private readonly PlayniteSoundsSettings _settings;
        private readonly IPathingService _pathingService;
        private readonly IMainViewAPI _mainView;

        private static readonly Random RNG = new Random();
        public string SelectFile(string[] files, string previousMusicFile, bool musicEnded)
        {
            var musicFile = files.FirstOrDefault() ?? string.Empty;

            var shouldRandomize = _settings.RandomizeOnEverySelect || (musicEnded && _settings.RandomizeOnMusicEnd);
            if (files.Length > 1 && shouldRandomize) do
            {
                musicFile = files[RNG.Next(files.Length)];
            }
            while (previousMusicFile == musicFile);

            return musicFile;
        }

        // Backup order is game -> filter -> default
        public (string[], MusicType) GetBackupFiles()
        {
            if (_settings.MusicType != MusicType.Default)
            {
                var filterFiles = _pathingService.GeFilterMusicFiles(_mainView.GetActiveFilterPreset());
                if (filterFiles.Any())
                {
                    return (filterFiles, MusicType.Filter);
                }
            }

            return (_pathingService.GetDefaultMusicFiles(), MusicType.Default);
        }
    }
}
