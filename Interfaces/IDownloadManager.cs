using System.Collections.Generic;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteSounds.Models;

namespace PlayniteSounds.Files.Download
{
    public interface IDownloadManager
    {
        Album BestAlbumPick(IEnumerable<Album> albums, string gameName, string regexGameName);
        Song BestSongPick(IEnumerable<Song> songs, string regexGameName);
        void CreateDownloadDialogue(IEnumerable<Game> games, Source source, bool albumSelect = false, bool songSelect = false, bool overwriteSelect = false);
        void DownloadMusicForGames(Source source, IList<Game> games);
        bool DownloadSong(Song song, string path);
        IEnumerable<Album> GetAlbumsForGame(string gameName, Source source, bool auto = false);
        string GetItemUrl(DownloadItem item);
        IEnumerable<Song> GetSongsFromAlbum(Album album);
        void StartDownload(GlobalProgressActionArgs args, List<Game> games, Source source, string progressTitle, bool albumSelect, bool songSelect, bool overwrite);
    }
}
