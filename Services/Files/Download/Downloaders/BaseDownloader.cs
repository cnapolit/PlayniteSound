using System;
using Playnite.SDK.Models;
using PlayniteSounds.Models;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Playnite.SDK;
using PlayniteSounds.Files.Download;

namespace PlayniteSounds.Services.Files.Download.Downloaders
{ 
    internal abstract class BaseDownloader
    {
        protected ILogger    _logger;
        protected HttpClient _httpClient;

        public BaseDownloader(ILogger logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public virtual IAsyncEnumerable<IEnumerable<Album>> GetAlbumBatchesForGameAsync(Game game, string searchTerm,
            CancellationToken token)
            => throw new NotImplementedException();

        public virtual IAsyncEnumerable<IEnumerable<Song>> SearchSongBatchesAsync(Game game, string searchTerm,
            CancellationToken token)
            => throw new NotImplementedException();

        public virtual string GenerateSearchStr(string gameName) => gameName;

        protected async Task<Stream> GetStreamAsync(string url, CancellationToken token)
        {
            using (var stream = await _httpClient.GetStreamAsync(url))
            {
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, 81920, token);
                return memoryStream;
            }
        }

        protected async Task<MemoryStream> CopyToMemoryAsync(Stream stream, CancellationToken token)
        {
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, 81920, token);
            return memoryStream;
        }
        
        public async Task<bool> DownloadAsync(
            string streamUri, string path, IProgress<double> progress, CancellationToken token)
        {
            try
            {
                using (var httpMessage = await _httpClient.GetAsync(streamUri, token))
                using (var fileStream = File.Create(path))
                if    (progress != null) 
                using (var stream = await httpMessage.Content.ReadAsStreamAsync())
                     /* Then */ await DownloadCommon.DownloadStreamAsync(stream, fileStream, progress, token);
                else /* Then */ await httpMessage.Content.CopyToAsync(fileStream);
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Unable to download file from URL '{streamUri}' to path '{path}'");
                return false;
            }

            return true;
        }
    }
}
