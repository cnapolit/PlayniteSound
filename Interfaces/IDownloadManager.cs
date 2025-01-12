using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK.Models;
using PlayniteSounds.Models;
using PlayniteSounds.Models.Download;

namespace PlayniteSounds.Files.Download
{
    public interface IDownloadManager : IDownloadBase
    {
        string                  GetSourceIcon(Source source);
        string                  GetSourceLogo(Source source);
        IAsyncEnumerable<Album> GetAlbumsForGameAsync(Game game, string searchTerm, Source source,
            CancellationToken token);
        IAsyncEnumerable<Song>  SearchSongsAsync(Game game, string searchTerm, Source source, CancellationToken token);
        DownloadCapabilities    GetCapabilities(Source source);
        Task<bool>              DownloadAsync(Game game, DownloadItem item, string path, IProgress<double> progress, CancellationToken token);
        Task<DownloadStatus>    DownloadAsync(Game game, CancellationToken token);

        IAsyncEnumerable<IEnumerable<Album>> GetAlbumBatchesForGameAsync(Game game, string searchTerm, Source source,
            CancellationToken token);

        string GenerateSearchStr(Source source, string gameName);

        IAsyncEnumerable<IEnumerable<Song>> SearchSongBatchesAsync(Game game, string searchTerm, Source source,
            CancellationToken token);

        IAsyncEnumerable<IEnumerable<Song>> GetSongBatchesFromAlbumAsync(Album album, CancellationToken token);
    }
}
