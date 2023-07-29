using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteSounds.Common.Constants;
using PlayniteSounds.Models;
using PlayniteSounds.Models.Audio.Sound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlayniteSounds.Services.Files
{
    public class PathingService : IPathingService
    {
        #region Infrastructure

        public string ExtraMetaDataFolder       { get; }
        public string MusicFilesDataPath        { get; }
        public string SoundFilesDataPath        { get; }
        public string SoundManagerFilesDataPath { get; }
        public string DefaultMusicPath          { get; }
        public string GameMusicFilePath         { get; }
        public string PlatformMusicFilePath     { get; }
        public string FilterMusicFilePath       { get; }

        public PathingService(IPlaynitePathsAPI pathsApi)
        {
            ExtraMetaDataFolder       = Path.Combine(pathsApi.ConfigurationPath, SoundDirectory.ExtraMetaData);
            MusicFilesDataPath        = Path.Combine(       ExtraMetaDataFolder, SoundDirectory.Music);
            SoundFilesDataPath        = Path.Combine(       ExtraMetaDataFolder, SoundDirectory.Sound);
            SoundManagerFilesDataPath = Path.Combine(       ExtraMetaDataFolder, SoundDirectory.SoundManager);
            DefaultMusicPath          = Path.Combine(       ExtraMetaDataFolder, SoundDirectory.Default);
            PlatformMusicFilePath     = Path.Combine(       ExtraMetaDataFolder, SoundDirectory.Platform);
            FilterMusicFilePath       = Path.Combine(       ExtraMetaDataFolder, SoundDirectory.Filter);
            GameMusicFilePath         = Path.Combine(       ExtraMetaDataFolder, SoundDirectory.GamesFolder);
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
            => Path.Combine(PlatformMusicFilePath, platform?.Name ?? SoundDirectory.NoPlatform);

        public string GetPlatformDirectoryPath(string platform)
            => Path.Combine(PlatformMusicFilePath, platform ?? SoundDirectory.NoPlatform);

        public string[] GeFilterMusicFiles(FilterPreset filter)
            => GetDirectoryFiles(GetFilterDirectoryPath(filter));

        public string[] GeFilterMusicFiles(Guid filterId)
            => GetDirectoryFiles(GetFilterDirectoryPath(filterId));

        public string[] GetGameMusicFiles(Game game)
            => game is null ? new string[] { } : GetDirectoryFiles(GetGameDirectoryPath(game));

        public string GetGameStartSoundFile(Game game)
        {
            var gameStartSoundFileDirectory = Path.Combine(
                GetGameDirectoryPath(game), SoundDirectory.StartingSoundFolder);
            
            return Directory.GetFiles(gameStartSoundFileDirectory).FirstOrDefault();
        }

        public string[] GetPlatformMusicFiles(Platform platform)
            => GetDirectoryFiles(GetPlatformDirectoryPath(platform));

        public string[] GetPlatformMusicFiles(string platform)
            => GetDirectoryFiles(GetPlatformDirectoryPath(platform));

        public string[] GetDefaultMusicFiles()
            => GetDirectoryFiles(DefaultMusicPath);

        public string[] GetSoundFiles()
            => GetDirectoryFiles(SoundDirectory.Sound);

        public string GetSoundTypeFile(AudioSource source, SoundType soundType, object resource = null)
        {
            var path = new List<string> { ExtraMetaDataFolder };

            switch (source)
            {
                case AudioSource.Default:
                    path.Add(SoundDirectory.Default);
                    break;
                case AudioSource.Filter:
                    path.Add(SoundDirectory.Filter);
                    path.Add(resource as string);
                    break;
                case AudioSource.Platform:
                    var platform = (resource as Game).Platforms?.FirstOrDefault().Name;
                    path.Add(SoundDirectory.Platform);
                    path.Add(platform ?? SoundDirectory.NoPlatform);
                    break;
                case AudioSource.Game:
                    var game = resource as Game;
                    path.Add(SoundDirectory.Game);
                    path.Add(game.Id.ToString());
                    break;
            }

            path.Add(SoundDirectory.Sound);

            path.Add(soundType.ToString());

            return GetDirectoryFiles(Path.Combine(path.ToArray())).FirstOrDefault();
        }

        #region Helpers

        private string[] GetDirectoryFiles(string directory) 
            => Directory.Exists(directory) ? Directory.GetFiles(directory) : Array.Empty<string>();

        #endregion

        #endregion

    }
}
