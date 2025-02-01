using Playnite.SDK;
using PlayniteSounds.Models;
using PlayniteSounds.Models.Download;
using PlayniteSounds.Services.State;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK.Models;
using PlayniteSounds.Common.Extensions;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Playlists;

namespace PlayniteSounds.Files.Download.Downloaders;

internal class YtDownloader(
    ILogger logger,
    IAssemblyResolver assemblyResolver,
    HttpClient httpClient,
    PlayniteSoundsSettings settings)
    : IDownloader
{
    #region Infrastructure

    private const          Source                 DlSource    = Source.Youtube;
    private const          string                 BaseUrl     = "https://www.youtube.com/embed/";
    private const          string                 PlaylistUrl = BaseUrl + "videoseries?list=";
    private       readonly YoutubeClient          _youtubeClient = new(httpClient);

    #endregion

    #region Implementation

    public string SourceLogo => "YouTube.png";
    public string SourceIcon => "YouTube.ico";

    public string GetItemUrl(DownloadItem item) => (item is Album ? PlaylistUrl : BaseUrl) + item.Id;

    public DownloadCapabilities GetCapabilities()
        => DownloadCapabilities.Batching | DownloadCapabilities.Bulk | DownloadCapabilities.FlatSearch;

    public string GenerateSearchStr(string gameName)
    {
        var format = settings.YoutubeSearchFormat.HasText() ? settings.YoutubeSearchFormat : "{0} Soundtrack";
        return string.Format(format, gameName);
    }

    #region GetAlbums

    public IAsyncEnumerable<Album> GetAlbumsForGameAsync(Game game, string searchTerm, CancellationToken token)
        => GetAlbumBatchesForGameAsync(game, searchTerm, token).SelectMany(e => e.ToAsyncEnumerable());

    public IAsyncEnumerable<IEnumerable<Album>> GetAlbumBatchesForGameAsync(Game game, string searchTerm,
        CancellationToken token)
    {
        using (assemblyResolver.HandleAssemblies(
                   typeof(System.Text.Json.JsonSerializer), typeof(IAsyncDisposable), typeof(Memory<>), typeof(Unsafe)))
            /* Then */ return GetAlbumsFromExplodeApiAsync(searchTerm, token);
    }

    private IAsyncEnumerable<IEnumerable<Album>> GetAlbumsFromExplodeApiAsync(string gameName, CancellationToken token)
        => ValidateSettings()
            ? AsyncEnumerable.Empty<IEnumerable<Album>>() 
            :_youtubeClient.Search.GetResultBatchesAsync(gameName, SearchFilter.Playlist, token)
                .Select(b => b.Items.OfType<PlaylistSearchResult>().Select(PlaylistToAlbum));

    private static Album PlaylistToAlbum(PlaylistSearchResult playlist)
    {
        var (icon, cover) = GetAlbumImages(playlist.Thumbnails);
        return new Album
        {
            Name = playlist.Title,
            Id = playlist.Id,
            Source = DlSource,
            IconUri = icon,
            CoverUri = cover,
            Uploader = playlist.Author?.ChannelTitle
        };
    }

    private static (string Icon, string Cover) GetAlbumImages(IReadOnlyList<Thumbnail> thumbnails)
    {
        var thumbnailsBySize = thumbnails.OrderBy(t => t.Resolution.Area)
            .Select(t => t.Url)
            .ToList();
        return (thumbnailsBySize.FirstOrDefault(), thumbnailsBySize.LastOrDefault());
    }

    #endregion

    public IAsyncEnumerable<Song> SearchSongsAsync(Game game, string searchTerm, CancellationToken token)
        => SearchSongBatchesAsync(game, searchTerm, token).SelectMany(e => e.ToAsyncEnumerable());

    public IAsyncEnumerable<IEnumerable<Song>> SearchSongBatchesAsync(
        Game game, string searchTerm, CancellationToken token)
    {
        if (ValidateSettings()) /* Then */ return AsyncEnumerable.Empty<IEnumerable<Song>>();
        return _youtubeClient.Search.GetResultBatchesAsync(searchTerm, SearchFilter.Video, token)
            .Select(b => b.Items.OfType<VideoSearchResult>().Select(VideoToSong));
    }

    public async Task GetAlbumInfoAsync(
        Album album, CancellationToken token, Func<Action<Song>, Song, Task> updateCallback)
    {
        var youtubeAlbum = await _youtubeClient.Playlists.GetAsync(album.Id, token);
        album.Description = youtubeAlbum.Description;
        if (youtubeAlbum.Count is null)
        {
            throw new Exception("Infinite playlists are not supported");
        }

        album.Count = (uint)youtubeAlbum.Count;

        if (album.Songs.Count is 0 || album.Songs.Count < album.Count)
        {
            var length = TimeSpan.Zero;

            var tasks = new List<Task<double>>();
            await foreach (var videoBatch in _youtubeClient.Playlists.GetVideoBatchesAsync(album.Id, token))
            {
                foreach (var playlistVideo in videoBatch.Items)
                {
                    var (icon, cover) = GetAlbumImages(playlistVideo.Thumbnails);
                    var song = new Song
                    {
                        Name = playlistVideo.Title,
                        Id = playlistVideo.Id,
                        Album = album.Name,
                        ParentAlbum = album,
                        Length = playlistVideo.Duration,
                        Source = DlSource,
                        IconUri = icon,
                        CoverUri = cover
                    };
                    tasks.Add(UpdateSongAsync(song, playlistVideo, token));
                    await updateCallback(album.Songs.Add, song);

                    if (playlistVideo.Duration != null)
                        /* Then */ length = length.Add((TimeSpan)playlistVideo.Duration); 
                }
            }

            if (token.IsCancellationRequested) /* Then */ return;

            var sizes = await Task.WhenAll(tasks);

            if (token.IsCancellationRequested) /* Then */ return;

            album.Sizes = new Dictionary<string, string>
            {
                ["Size"] = $"{Math.Round(sizes.Sum(), 2)} MB"
            };
        }

        album.HasSongsToEnumerate = album.HasExtraInfo = token.IsCancellationRequested;
        await updateCallback(null, null);
    }

    private async Task<double> UpdateSongAsync(Song song, PlaylistVideo playlistVideo, CancellationToken token)
    {
        var manifestTask = _youtubeClient.Videos.Streams.GetManifestAsync(playlistVideo.Url, token);
        Video video;
        using (assemblyResolver.HandleAssemblies(typeof(System.Text.CodePagesEncodingProvider)))
            /* Then */ video = await _youtubeClient.Videos.GetAsync(playlistVideo.Url, token);
        song.CreationDate = video.UploadDate.ToLocalTime().ToString();
        song.Description = video.Description;

        var streamManifest = await manifestTask;
        var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        song.StreamFunc = (_, t) => CreateAudioStreamAsync(streamInfo, t);

        song.Sizes = new Dictionary<string, string>
        {
            [streamInfo.Container.Name] = $"{Math.Round(streamInfo.Size.MegaBytes)} MB"
        };
        return streamInfo.Size.MegaBytes;
    }

    #region GetSongsFromAlbumAsync

    public IAsyncEnumerable<Song> GetSongsFromAlbumAsync(Album album, CancellationToken token) 
        => _youtubeClient.Playlists.GetVideosAsync(album.Id, token).Select(VideoToSong);

    private async Task<Stream> CreateAudioStreamAsync(string url, CancellationToken token)
    {
        var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(url, token);
        var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        return await CreateAudioStreamAsync(streamInfo, token);
    }

    #endregion

    public IAsyncEnumerable<IEnumerable<Song>> GetSongBatchesFromAlbumAsync(Album album, CancellationToken token)
        => _youtubeClient.Playlists.GetVideoBatchesAsync(album.Id, token).Select(b => b.Items.Select(VideoToSong));

    #region DownloadStreamAsync

    public async Task<bool> DownloadAsync(
        DownloadItem item, string path, IProgress<double> progress, CancellationToken token)
    {
        using  (assemblyResolver.HandleAssemblies(typeof(CliWrap.Cli)))
            return await DownloadSongExplodeAsync(item as Song, path, progress, token);
    }

    private async Task<bool> DownloadSongExplodeAsync(
        Song song, string path, IProgress<double> progress, CancellationToken token)
    {
        try
        {
            await _youtubeClient.Videos.DownloadAsync(
                song.Id, path, o => o.SetFFmpegPath(settings.FFmpegPath), progress, token);
            return true;
        }
        catch (Exception e)
        {
            logger.Error(e, $"Something went wrong when attempting to download from Youtube with Id '{song.Id}' and Path '{path}'");
            return false;
        }
    }

    #endregion

    #region Helpers

    private Song VideoToSong(IVideo video) => new()
    {
        Name = video.Title,
        Id = video.Id,
        Length = video.Duration,
        Source = DlSource,
        CoverUri = video.Thumbnails.FirstOrDefault()?.Url,
        Uploader = video.Author.ChannelTitle,
        StreamUri = video.Url,
        StreamFunc = (s, t) => CreateAudioStreamAsync(s.StreamUri, t)
    };

    private async Task<Stream> CreateAudioStreamAsync(IStreamInfo streamInfo, CancellationToken token)
    {
        var memoryStream = new MemoryStream();
        await _youtubeClient.Videos.Streams.CopyToAsync(streamInfo, memoryStream, cancellationToken: token);

        if (!token.IsCancellationRequested) /* Then */ return memoryStream;

        memoryStream.Dispose();
        return null;
    }

    private bool ValidateSettings() => !string.IsNullOrWhiteSpace(settings.FFmpegPath);

    #endregion

    #endregion
}