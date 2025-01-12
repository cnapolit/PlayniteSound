using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlayniteSounds.Models;

namespace PlayniteSounds.Files.Download
{
    public interface IDownloadBase
    {
        string                 GetItemUrl(DownloadItem item);
        Task                   GetAlbumInfoAsync(Album album, CancellationToken token, Func<Action<Song>, Song, Task> updateCallback);
        IAsyncEnumerable<Song> GetSongsFromAlbumAsync(Album album, CancellationToken token);
    }
}