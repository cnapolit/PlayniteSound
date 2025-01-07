using System;
using Playnite.SDK.Models;
using PlayniteSounds.Models;
using System.Collections.Generic;
using System.Threading;

namespace PlayniteSounds.Services.Files.Download.Downloaders
{ 
    internal abstract class BaseDownloader
    {
        public virtual IAsyncEnumerable<IEnumerable<Album>> GetAlbumBatchesForGameAsync(
            Game game, string searchTerm, CancellationToken? token = null)
            => throw new NotImplementedException();

        public IAsyncEnumerable<IEnumerable<Song>> SearchSongBatchesAsync(Game game, string searchTerm, CancellationToken? token = null)
            => throw new NotImplementedException();

        public virtual string GenerateSearchStr(string gameName) => gameName;
    }
}
