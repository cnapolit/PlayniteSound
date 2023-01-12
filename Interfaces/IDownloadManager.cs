using System.Collections.Generic;
using PlayniteSounds.Models;

namespace PlayniteSounds.Files.Download
{
    internal interface IDownloadManager
    {
        Album BestAlbumPick(IEnumerable<Album> albums, string gameName, string regexGameName);
        Song BestSongPick(IEnumerable<Song> songs, string regexGameName);
        bool DownloadSong(Song song, string path);
        IEnumerable<Album> GetAlbumsForGame(string gameName, Source source, bool auto = false);
        string GetItemUrl(DownloadItem item);
        IEnumerable<Song> GetSongsFromAlbum(Album album);
    }
}
