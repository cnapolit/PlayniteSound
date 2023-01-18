using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteSounds.Common.Constants;
using System;
using System.IO;
using System.Linq;

namespace PlayniteSounds.Services.Files
{
    internal class PathingService : IPathingService
    {
        #region Infrastructure

        public string ExtraMetaDataFolder       { get; private set; }
        public string MusicFilesDataPath        { get; private set; }
        public string SoundFilesDataPath        { get; private set; }
        public string SoundManagerFilesDataPath { get; private set; }
        public string DefaultMusicPath          { get; private set; }
        public string GameMusicFilePath         { get; private set; }
        public string PlatformMusicFilePath     { get; private set; }
        public string FilterMusicFilePath       { get; private set; }

        public PathingService(IPlayniteAPI api)
        {
            ExtraMetaDataFolder       = Path.Combine(api.Paths.ConfigurationPath, SoundDirectory.ExtraMetaData);
            MusicFilesDataPath        = Path.Combine(        ExtraMetaDataFolder, SoundDirectory.Music);
            SoundFilesDataPath        = Path.Combine(        ExtraMetaDataFolder, SoundDirectory.Sound);
            SoundManagerFilesDataPath = Path.Combine(        ExtraMetaDataFolder, SoundDirectory.SoundManager);
            DefaultMusicPath          = Path.Combine(        ExtraMetaDataFolder, SoundDirectory.Default);
            PlatformMusicFilePath     = Path.Combine(        ExtraMetaDataFolder, SoundDirectory.Platform);
            FilterMusicFilePath       = Path.Combine(        ExtraMetaDataFolder, SoundDirectory.Filter);
            GameMusicFilePath         = Path.Combine(        ExtraMetaDataFolder, SoundDirectory.GamesFolder);
        }

        #endregion

        #region Implementation

        public string GetFilterDirectoryPath(FilterPreset filter)
            => Path.Combine(FilterMusicFilePath, filter.Id.ToString());

        public string GetFilterDirectoryPath(Guid filterId)
            => Path.Combine(FilterMusicFilePath, filterId.ToString());

        public string GetGameDirectoryPath(Game game)
            => Path.Combine(GameMusicFilePath, game.Id.ToString(), SoundDirectory.Music);

        public string GetPlatformDirectoryPath(Platform platform) 
            => Path.Combine(PlatformMusicFilePath, platform.Name ?? SoundDirectory.NoPlatform);

        public string[] GeFilterMusicFiles(FilterPreset filter)
            => GetDirectoryFiles(GetFilterDirectoryPath(filter));

        public string[] GeFilterMusicFiles(Guid filterId)
            => GetDirectoryFiles(GetFilterDirectoryPath(filterId));

        public string[] GetGameMusicFiles(Game game)
            => GetDirectoryFiles(GetGameDirectoryPath(game));

        public string GetGameStartSoundFile(Game game)
        {
            var gameStartSoundFileDirectory = Path.Combine(
                GetGameDirectoryPath(game), SoundDirectory.StartingSoundFolder);
            
            return Directory.GetFiles(gameStartSoundFileDirectory).FirstOrDefault();
        }

        public string[] GetPlatformMusicFiles(Platform platform)
            => GetDirectoryFiles(GetPlatformDirectoryPath(platform));

        public string[] GetDefaultMusicFiles()
            => GetDirectoryFiles(DefaultMusicPath);

        #region Helpers

        private string[] GetDirectoryFiles(string directory) 
            => Directory.Exists(directory) ? Directory.GetFiles(directory) : Array.Empty<string>();

        #endregion

        #endregion

    }
}
