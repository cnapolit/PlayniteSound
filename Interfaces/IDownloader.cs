using PlayniteSounds.Models;
using PlayniteSounds.Models.Download;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK.Models;
using System;

namespace PlayniteSounds.Files.Download.Downloaders
{
    public interface IDownloader : IDownloadBase
    {
        bool                    SupportsBulkDownload { get; }
        string                  SourceLogo           { get; }
        string                  SourceIcon           { get; }
        IAsyncEnumerable<Album> GetAlbumsForGameAsync(Game game, string searchTerm, bool auto, CancellationToken? token = null);
        IAsyncEnumerable<Song>  SearchSongsAsync(Game game, string searchTerm, CancellationToken? token = null);
        DownloadCapabilities    GetCapabilities();
        Task<bool>              DownloadAsync(DownloadItem item, string path, IProgress<double> progress, CancellationToken token);
    }
}
