using Playnite.SDK.Models;
using PlayniteSounds.Files.Download.Downloaders;
using PlayniteSounds.Models;
using PlayniteSounds.Models.Download;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;
using PlayniteSounds.Common.Utilities;
using PlayniteSounds.Common.Extensions;
using PlayniteSounds.Files.Download;

namespace PlayniteSounds.Services.Files.Download.Downloaders
{
    internal class LocalDownloader : BaseDownloader, IDownloader
    {
        private readonly ILogger         _logger;
        private readonly IPathingService _pathingService;

        public LocalDownloader(ILogger logger, IPathingService pathingService)
        {
            _logger = logger;
            _pathingService = pathingService;
        }

        public bool   SupportsBulkDownload => true;
        public string SourceLogo           => "local.png";
        public string SourceIcon           => "local.ico";

        public async Task<bool> DownloadAsync(
            DownloadItem item, string path, IProgress<double> progress, CancellationToken token)
        {
            try
            {
                     if (item is Album album)                        album.Songs.ForEach(s => File.Copy(s.Id, Path.Combine(path, s.Name))); 
                else if (progress is null)                           File.Copy(item.Id, path);
                else using (var sourceStream = File.Create(item.Id)) await DownloadCommon.DownloadStreamAsync(sourceStream, path, progress, token);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Failed to copy '{item.Id}' to '{path}'");
                return false;
            }

            return true;
        }

        public Task GetAlbumInfoAsync(
            Album album, CancellationToken token, Func<Action<Song>, Song, Task> updateCallback)
            => throw new NotSupportedException();

        public async IAsyncEnumerable<Album> GetAlbumsForGameAsync(Game game, string searchTerm,
            CancellationToken? token = null)
        {
            if (game.InstallationStatus != InstallationStatus.Installed) /* Then */ yield break;

            var files = _pathingService.GetAllMusicFiles(game.InstallDirectory);
            files = searchTerm.HasText() ? files.Where(f => f.Name.Similarity(searchTerm) < 3) : files;
            var filesByDir = files.GroupBy(f => UriUtilities.GetParent(f.Id));

            foreach (var dirToFiles in filesByDir)
            {
                var album = new Album
                {
                    Id = dirToFiles.Key,
                    Name = dirToFiles.Key.Substring(game.InstallDirectory.Length),
                    Artists = new HashSet<string>(),
                    Types = new HashSet<string>(),
                    Length = TimeSpan.Zero,
                    IconUri = dirToFiles.First().IconUri,
                    CoverUri = dirToFiles.First().CoverUri,
                    Source = Source.Local,
                    Developers = new HashSet<string>(),
                    Publishers = new HashSet<string>(),
                    HasExtraInfo = false,
                    Songs = new ObservableCollection<Song>()
                };

                var size = 0;
                foreach (var file in dirToFiles)
                {
                    foreach (var artist in file.Artists)       album.Artists.   Add(artist);
                    foreach (var developer in file.Developers) album.Developers.Add(developer);
                    foreach (var publisher in file.Publishers) album.Publishers.Add(publisher);

                    album.Types.Add(file.Types.First());
                    album.Length += file.Length.Value;
                    size += int.Parse(file.Sizes.First().Value);
                    file.ParentAlbum = album;
                    album.Songs.Add(file);
                }

                album.Sizes = new Dictionary<string, string> { ["All"] = size.ToString() };
                album.Count = (uint)album.Songs.Count;
                yield return album;
            }
        }

        public DownloadCapabilities GetCapabilities()
            => DownloadCapabilities.Album | DownloadCapabilities.Bulk | DownloadCapabilities.FlatSearch;

        public DownloadCapabilities GetCapabilities(DownloadItem item) => GetCapabilities();

        public string GetItemUrl(DownloadItem item) => item.Id;

        public IAsyncEnumerable<Song> GetSongsFromAlbumAsync(Album album, CancellationToken? token = null)
            => album.Songs.ToAsyncEnumerable();

        public IAsyncEnumerable<Song> SearchSongsAsync(Game game, string searchTerm, CancellationToken? token = null)
        {
            if (game.InstallationStatus != InstallationStatus.Installed) /* Then */ return AsyncEnumerable.Empty<Song>();

            var files = _pathingService.GetAllMusicFiles(game.InstallDirectory);
            files = searchTerm.HasText() ? files.Where(f => f.Name.Similarity(searchTerm) < 3) : files;
            return files.ToAsyncEnumerable();
        }
    }
}
