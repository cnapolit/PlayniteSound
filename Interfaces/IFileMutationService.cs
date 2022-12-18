using Playnite.SDK.Models;
using PlayniteSounds.Models;
using System.Collections.Generic;

namespace PlayniteSounds.Services.Files
{
    internal interface IFileMutationService
    {
        void CreateDownloadDialogue(
            IEnumerable<Game> games,
            Source source,
            bool albumSelect = false,
            bool songSelect = false,
            bool overwriteSelect = false);

        void CreateNormalizationDialogue();
        void DeleteMusicDirectories(IEnumerable<Game> games);
        void DeleteMusicFile(string musicFile, string musicFileName, Game game);
        void DownloadMusicForGames(Source source, IList<Game> games);
        void SelectMusicForDefault();
        void SelectMusicForFilter(FilterPreset filter);
        void SelectMusicForGames(IEnumerable<Game> games);
        void SelectMusicForPlatform(Platform platform);
    }
}