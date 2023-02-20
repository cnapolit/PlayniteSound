using Playnite.SDK;
using PlayniteSounds.Common.Utilities;
using PlayniteSounds.Models;
using System;
using System.Linq;

namespace PlayniteSounds.Services.Files
{
    public class MusicFileSelector : IMusicFileSelector
    {
        private readonly PlayniteSoundsSettings _settings;
        private readonly IPathingService _pathingService;
        private readonly IMainViewAPI _mainView;

        public MusicFileSelector(IPathingService pathingService, IPlayniteAPI api, PlayniteSoundsSettings settings)
        {
            _pathingService= pathingService;
            _mainView = api.MainView;
            _settings= settings;
        }

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
            if (EnumUtilities.SoundsSettingsToMusicType(_settings) != MusicType.Default)
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
