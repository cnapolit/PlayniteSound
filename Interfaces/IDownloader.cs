using System.Collections.Generic;
using PlayniteSounds.Models;

namespace PlayniteSounds.Files.Download.Downloaders
{
    internal interface IDownloader
    {
        string BaseUrl();
        Source DownloadSource();
        IEnumerable<Album> GetAlbumsForGame(string gameName, bool auto = false);
        IEnumerable<Song> GetSongsFromAlbum(Album album);
        bool DownloadSong(Song song, string path);
    }
}
