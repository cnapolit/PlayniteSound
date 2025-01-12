
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteSounds.Models;
using PlayniteSounds.Models.Download;
using PlayniteSounds.Services.Files.Download.Downloaders;
using PlayniteSounds.Services.State;
using SoundCloudExplode;
using SpotifyExplode;
using SpotifyExplode.Common;
using SpotifyExplode.Search;
using SpotifyExplode.Tracks;

namespace PlayniteSounds.Files.Download.Downloaders
{
    internal class SpDownloader : BaseDownloader, IDownloader
    {
        private const string BaseUrl = "https://open.spotify.com/";

        private readonly SpotifyClient     _client;
        private readonly IAssemblyResolver _assemblyResolver;

        public SpDownloader(ILogger logger, IAssemblyResolver assemblyResolver, HttpClient client)
            : base(logger, client)
        {
            _client = new SpotifyClient(client);
            _assemblyResolver = assemblyResolver;
        }

        public string SourceLogo => "Spotify.png";
        public string SourceIcon => "Spotify.ico";

        public string GetItemUrl(DownloadItem item)
        {
            var type = item is Song                           ? "track"
                     : item.SourceObject is AlbumSearchResult ? "album"
                                                              : "playlist";
            return $"{BaseUrl}{type}/{item.Id}";
        }

        private Album PlaylistResultToAlbum(PlaylistSearchResult result)
        {
            var album = new Album
            {
                Id = result.Id,
                SourceObject = result,
                Name = result.Title,
                Types = new List<string> { "MP3" },
                Description = result.Description,
                Uploader = result.Owner.DisplayName,
                Source = Source.Spotify,
                Count = (uint)result.Total
            };

            var hasAllTracks = result.Total == result.Tracks.Count;
            UpdateAlbum(album, hasAllTracks, result.Images, result.Tracks);

            var artists = new HashSet<string>();
            foreach (var song in album.Songs)
            {
                foreach(var artist in song.Artists) /* Then */ artists.Add(artist);
                song.Uploader = result.Items.Find(i => i.Track.Id == song.Id)?.AddedBy?.DisplayName;
            }
            album.Artists = artists.ToList();

            return album;
        }

        private Album AlbumResultToAlbum(AlbumSearchResult result)
        {
            var album = new Album
            {
                Id = result.Id,
                SourceObject = result,
                Name = result.Title,
                Artists = result.Artists.Select(a => a.Name).ToList(),
                Types = new List<string> { "mp3" },
                CreationDate = result.ReleaseDateStr,
                Source = Source.Spotify,
                HasExtraInfo = false,
                Count = (uint)result.TotalTracks
            };

            result.Tracks = result.Tracks ?? new List<Track>();
            var hasAllTracks = result.TotalTracks == result.Tracks.Count;
            UpdateAlbum(album, hasAllTracks, result.Images, result.Tracks);
            return album;
        }

        private void UpdateAlbum(Album album, bool hasAllTracks, List<Image> images, List<Track> tracks)
        {
            var length = 0L;
            var songs = new List<Song>();
            var (icon, cover) = GetImages(images);


            foreach (var track in tracks)
            {
                if (hasAllTracks) /* Then */ length += track.DurationMs;
                var song = TrackToSong(track);
                song.IconUri = icon;
                song.CoverUri = cover;
                songs.Add(song);
            }

            album.Songs = songs.ToObservable();
            album.Length = hasAllTracks ? TimeSpan.FromMilliseconds(length) : null;
            album.CoverUri = cover;
            album.IconUri = icon;
        }

        private static (string Icon, string Cover) GetImages(List<Image> thumbnails)
        {
            var thumbnailsBySize = thumbnails.OrderBy(t => (t.Height ?? t.Width) is null ? 0 : t.Height * t.Width)
                                             .Select(t => t.Url)
                                             .ToList();
            return (thumbnailsBySize.FirstOrDefault(), thumbnailsBySize.LastOrDefault());
        }

        private Song TrackToSong(Track track) => new Song
        {
            Id = track.Id,
            SourceObject = track,
            Name = track.Title,
            Artists = track.Artists.Select(a => a.Name).ToList(),
            Types = new List<string> { "MP3" },
            TrackNumber = (uint)track.TrackNumber,
            StreamUri = track.PreviewUrl,
            StreamFunc = (s, t) => GetStreamAsync(s.StreamUri, t),
            Length = TimeSpan.FromMilliseconds(track.DurationMs),
            Source = Source.Spotify
        };

        private Song TrackResultToSong(TrackSearchResult track)
        {
            var (icon, cover) = GetImages(track.Album.Images);
            return new Song
            {
                Id = track.Id,
                SourceObject = track,
                Name = track.Title,
                Artists = track.Artists.Select(a => a.Name).ToList(),
                Types = new List<string> { "MP3" },
                TrackNumber = (uint)track.TrackNumber,
                StreamUri = track.PreviewUrl,
                StreamFunc = (s,t) => GetStreamAsync(s.StreamUri, t),
                Length = TimeSpan.FromMilliseconds(track.DurationMs),
                CoverUri = cover,
                IconUri = icon,
                Source = Source.Spotify
            };
        }

        public override IAsyncEnumerable<IEnumerable<Album>> GetAlbumBatchesForGameAsync(
            Game game, string searchTerm, CancellationToken token)
        {
            using (_assemblyResolver.HandleAssemblies(typeof(IAsyncDisposable)))
            /* Then */ return GetAlbumBatchesAsync(searchTerm, token);
        }

        private async IAsyncEnumerable<IEnumerable<Album>> GetAlbumBatchesAsync(
            string searchTerm, [EnumeratorCancellation] CancellationToken token)
        {
            using (_assemblyResolver.HandleAssemblies(typeof(System.Text.Encodings.Web.HtmlEncoder), typeof(Memory<>)))
            {
                var albumBatches    = GetBatchesAsync<AlbumSearchResult,    Album>(searchTerm, SearchFilter.Album,    AlbumResultToAlbum, token);
                var playlistBatches = GetBatchesAsync<PlaylistSearchResult, Album>(searchTerm, SearchFilter.Album, PlaylistResultToAlbum, token);

                var moreAlbums = true;
                var morePlaylists = true;
                await using (var albumEnumerator = albumBatches.GetAsyncEnumerator(token))
                await using (var playlistEnumerator = playlistBatches.GetAsyncEnumerator(token))
                while (moreAlbums || morePlaylists)
                {
                    if (moreAlbums && (moreAlbums = await albumEnumerator.MoveNextAsync()))
                    /* Then */ yield return albumEnumerator.Current;
                    if (morePlaylists && (morePlaylists = await playlistEnumerator.MoveNextAsync()))
                    /* Then */ yield return playlistEnumerator.Current;
                }
            }
        }

        public async IAsyncEnumerable<Song> SearchSongsAsync(
            Game game, string searchTerm, [EnumeratorCancellation] CancellationToken token)
        {
            var offset = 0;
            while (!token.IsCancellationRequested)
            {
                var songs = await _client.Search.GetResultsAsync(searchTerm, offset: offset, cancellationToken: token);
                foreach (TrackSearchResult song in songs) /* Then */ yield return TrackResultToSong(song);
                if (songs.Count < Constants.MaxLimit) /* Then */ yield break;

                offset += Constants.MaxLimit;
            }

        }

        private async IAsyncEnumerable<IEnumerable<TItem>> GetBatchesAsync<TSearchResult, TItem>(
            string searchTerm,
            SearchFilter filter,
            Func<TSearchResult, TItem> selector,
            [EnumeratorCancellation] CancellationToken token,
            int offset = 0)
            where TSearchResult : ISearchResult
        {
            while (!token.IsCancellationRequested)
            {
                var batch = await _client.Search.GetResultsAsync(searchTerm, filter, offset, cancellationToken: token);
                yield return batch.OfType<TSearchResult>().Select(selector);
                if (batch.Count < Constants.MaxLimit) /* Then */ yield break;

                offset += Constants.MaxLimit;
            }
        }

        public DownloadCapabilities GetCapabilities() 
            => DownloadCapabilities.Bulk | DownloadCapabilities.FlatSearch | DownloadCapabilities.Batching;

        public async Task<bool> DownloadAsync(
            DownloadItem item, string path, IProgress<double> progress, CancellationToken token)
        {
            string url;
            using (_assemblyResolver.HandleAssemblies(typeof(HtmlAgilityPack.HtmlAttribute))) /* Then */ try
            {
                url = await _client.Tracks.GetDownloadUrlAsync(item.Id, token);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Failed to acquire download url for song '{item.Id}'");
                return false;
            }

            return await DownloadAsync(url, path, progress, token);
        }

        public override IAsyncEnumerable<IEnumerable<Song>> SearchSongBatchesAsync(
            Game game, string searchTerm, CancellationToken token)
            => GetBatchesAsync<TrackSearchResult, Song>(searchTerm, SearchFilter.Track, TrackResultToSong, token);

        public async IAsyncEnumerable<IEnumerable<Song>> GetSongBatchesFromAlbumAsync(
            Album album, [EnumeratorCancellation] CancellationToken token)
        {
            var offset = album.Songs.Count;
            while (!token.IsCancellationRequested)
            {
                var batch = await _client.Albums.GetTracksAsync(album.Id, offset, cancellationToken: token);
                yield return batch.Select(TrackToSong);
                if (batch.Count < Constants.MaxLimit) /* Then */ yield break;

                offset += Constants.MaxLimit;
            }
        }

        public Task GetAlbumInfoAsync(Album album, CancellationToken token, Func<Action<Song>, Song, Task> updateCallback)
            => throw new NotImplementedException();

        public IAsyncEnumerable<Song> GetSongsFromAlbumAsync(Album album, CancellationToken token)
            => throw new NotImplementedException();

        public IAsyncEnumerable<Album> GetAlbumsForGameAsync(
            Game game, string searchTerm, CancellationToken token)
            => throw new NotImplementedException();
    }
}