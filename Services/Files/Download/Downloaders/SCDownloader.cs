using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteSounds.Files.Download.Downloaders;
using PlayniteSounds.Models;
using PlayniteSounds.Models.Download;
using PlayniteSounds.Services.State;
using SoundCloudExplode;
using SoundCloudExplode.Search;
using SoundCloudExplode.Tracks;

namespace PlayniteSounds.Services.Files.Download.Downloaders;

internal class SCDownloader(ILogger logger, IAssemblyResolver assemblyResolver, HttpClient client)
    : BaseDownloader(logger, client), IDownloader
{
    private const string BaseUrl = "https://soundcloud.com/";

    private readonly SoundCloudClient  _client = new(client);

    public string SourceLogo => "SoundCloud.jpg";
    public string SourceIcon => "SoundCloud.ico";

    public DownloadCapabilities GetCapabilities()
        => DownloadCapabilities.Album | DownloadCapabilities.Batching | DownloadCapabilities.FlatSearch;

    public string GetItemUrl(DownloadItem item) => BaseUrl + item.Id;

    public async Task GetAlbumInfoAsync(
        Album album, CancellationToken token, Func<Action<Song>, Song, Task> updateCallback)
    {
        var playlist = album.SourceObject as PlaylistSearchResult;
        var tracks = _client.Playlists.GetTracksAsync(playlist.Url, album.Songs.Count, cancellationToken: token);
        await foreach (var track in tracks)
            /* Then */ await updateCallback(album.Songs.Add, TrackToSong(track));
    }

    public IAsyncEnumerable<Song> GetSongsFromAlbumAsync(Album album, CancellationToken token)
    {
        var playlist = album.SourceObject as PlaylistSearchResult;
        return _client.Playlists.GetTracksAsync(playlist.Url, album.Songs.Count, cancellationToken: token)
            .Select(TrackToSong);
    }

    public async IAsyncEnumerable<IEnumerable<Song>> GetSongBatchesFromAlbumAsync(
        Album album, [EnumeratorCancellation] CancellationToken token)
    {
        var playlist = album.SourceObject as PlaylistSearchResult;
        var batches = _client.Playlists.GetTrackBatchesAsync(playlist.Url, album.Songs.Count, cancellationToken: token);
        await foreach (var batch in batches)
            /* Then */ yield return batch.Items.Select(TrackToSong);
    }

    public IAsyncEnumerable<Album> GetAlbumsForGameAsync(Game game, string searchTerm, CancellationToken token) 
        => GetAlbumBatchesForGameAsync(game, searchTerm, token).SelectMany(b => b.ToAsyncEnumerable());

    public override async IAsyncEnumerable<IEnumerable<Album>> GetAlbumBatchesForGameAsync(
        Game game, string searchTerm, [EnumeratorCancellation] CancellationToken token)
    {
        using (CreateAssemblyHandler())
        {
            if (!_client.IsInitialized) /* Then */ await _client.InitializeAsync(token);

            var batches = _client.Search.GetResultBatchesAsync(
                searchTerm, SearchFilter.Album, cancellationToken: token);
            await foreach (var batch in batches)
                /* Then */ yield return batch.Items.OfType<PlaylistSearchResult>().Select(PlaylistSearchResultToAlbum);
        }
    }

    private async Task<Stream> GetStreamAsync(Song song, CancellationToken token)
    {
        song.StreamUri = await _client.Tracks.GetDownloadUrlAsync(song.SourceObject as Track, token);

        var request = new HttpRequestMessage(HttpMethod.Get, song.StreamUri);

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

        if (!response.IsSuccessStatusCode) /* Then */ return null;

        var totalLength = response.Content.Headers.ContentLength ?? 0;
        if (totalLength != 0) /* Then */ song.Sizes = new Dictionary<string, string> { { "MP3", (totalLength / 1e6).ToString() } };

        using var stream = await response.Content.ReadAsStreamAsync();
        return await CopyToMemoryAsync(stream, token);
    }

    private Album PlaylistSearchResultToAlbum(PlaylistSearchResult result)
    {
        uint? count = null;
        if (result.TrackCount.HasValue)
        {
            count = (uint)result.TrackCount.Value;
        }

        return new Album
        {
            Id = $"{result.User?.Username}/sets/{result.Permalink}",
            SourceObject = result,
            Name = result.Title,
            CreationDate = result.DisplayDate.ToString(),
            Description = result.Description,
            CoverUri = result.ArtworkUrl?.AbsoluteUri,
            IconUri = result.ArtworkUrl?.AbsoluteUri,
            Songs = result.Tracks.Where(t => t.Downloadable).Select(TrackToSong).ToObservable(),
            Source = Source.SoundCloud,
            Uploader = result.User?.Username,
            HasExtraInfo = false,
            Count = count
        };
    }

    private Song TrackToSong(Track track) => new()
    {
        Id = $"{track.User?.Username}/{track.Permalink}",
        SourceObject = track,
        Name = track.Title,
        Album = track.PlaylistName,
        StreamFunc = GetStreamAsync,
        CoverUri = track.ArtworkUrl?.AbsoluteUri,
        IconUri = track.ArtworkUrl?.AbsoluteUri,
        Source = Source.SoundCloud,
        CreationDate = track.DisplayDate.ToString(),
        Description = track.Description,
        Length = track.Duration.HasValue ? TimeSpan.FromMilliseconds(track.Duration.Value) : null,
        Uploader = track.User?.Username
    };

    public IAsyncEnumerable<Song> SearchSongsAsync(Game game, string searchTerm, CancellationToken token)
        => SearchSongBatchesAsync(game, searchTerm, token).SelectMany(b => b.ToAsyncEnumerable());

    public override IAsyncEnumerable<IEnumerable<Song>> SearchSongBatchesAsync(
        Game game, string searchTerm, CancellationToken token)
    {
        using (CreateAssemblyHandler())
            /* Then */ return _client.Search.GetResultBatchesAsync(searchTerm, SearchFilter.Track, cancellationToken: token)
            .Select(b => b.Items.OfType<Track>().Select(TrackToSong));
    }

    private IDisposable CreateAssemblyHandler() 
        => assemblyResolver.HandleAssemblies(typeof(Unsafe), typeof(System.Text.Encodings.Web.HtmlEncoder));

    public async Task<bool> DownloadAsync(
        DownloadItem item, string path, IProgress<double> progress, CancellationToken token)
    {
        var song = item as Song;

        try
        {
            await _client.DownloadAsync(song.SourceObject as Track, path, progress, cancellationToken: token);
        }
        catch (Exception)
        {
            return false;
        }
        return true;
    }
}