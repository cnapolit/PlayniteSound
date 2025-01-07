using Playnite.SDK;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using PlayniteSounds.Common;
using PlayniteSounds.Models;
using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using PlayniteSounds.Common.Extensions;
using System.Threading.Tasks;
using Playnite.SDK.Models;
using PlayniteSounds.Models.Download;
using PlayniteSounds.Services.Files.Download.Downloaders;

namespace PlayniteSounds.Files.Download.Downloaders
{
    internal class KhDownloader : BaseDownloader, IDownloader
    {
        #region Infrastructure

        private const           string     BaseUrl = "https://downloads.khinsider.com";
        private        readonly ILogger    _logger;
        private        readonly HttpClient _httpClient;
        private        readonly HtmlWeb    _web;


        public KhDownloader(ILogger logger, HttpClient httpClient, HtmlWeb web)
        {
            _logger = logger;
            _httpClient = httpClient;
            _web = web;
        }

        #endregion

        #region Implementation

        public bool   SupportsBulkDownload => false;
        public string SourceLogo           => "KHInsider.jpg";
        public string SourceIcon           => "KHInsider.ico";

        public DownloadCapabilities GetCapabilities(DownloadItem item) => DownloadCapabilities.None;

        public DownloadCapabilities GetCapabilities() => DownloadCapabilities.None;

        public string GetItemUrl(DownloadItem item) => item is Album ? BaseUrl + item.Id : item.Id;

        public async IAsyncEnumerable<Album> GetAlbumsForGameAsync(Game game, string searchTerm,
            CancellationToken? token = null)
        {
            var htmlDoc = await _web.LoadFromWebAsync($"{BaseUrl}/search?search={searchTerm}");
            var tableRows = htmlDoc.DocumentNode.Descendants("tr").Skip(1);
            foreach (var row in tableRows)
            {
                var columnEntries = row.Descendants("td").ToList();

                var titleField = columnEntries.ElementAtOrDefault(1);
                if (titleField == null)
                {
                    _logger.Info($"Found album entry of game '{searchTerm}' without title field");
                    continue;
                }

                var htmlLink = titleField.Descendants("a").FirstOrDefault();
                if (htmlLink == null)
                {
                    _logger.Info($"Found entry for album entry of game '{searchTerm}' without title");
                    continue;
                }

                var albumName = htmlLink.InnerHtml;
                var albumPartialLink = htmlLink.GetAttributeValue("href", null);
                if (albumPartialLink == null)
                {
                    _logger.Info($"Found entry for album '{albumName}' of game '{searchTerm}' without link in title");
                    continue;
                }

                var iconUrl = columnEntries.FirstOrDefault()
                                          ?.Descendants("img")
                                           .FirstOrDefault()
                                          ?.GetAttributeValue("src", null);

                var platforms = columnEntries.ElementAtOrDefault(2)
                                            ?.Descendants("a")
                                             .Select(d => d.InnerHtml)
                                             .Where(StringExtensions.HasText)
                                             .ToList();

                var type = columnEntries.ElementAtOrDefault(3)?.Descendants("a").FirstOrDefault()?.InnerHtml;

                yield return new Album(GetSongsFromAlbumAsync)
                {
                    Name = StringUtilities.StripStrings(albumName),
                    Id = albumPartialLink,
                    Source = Source.KHInsider,
                    IconUri = iconUrl,
                    Platforms = platforms,
                    Types = type is null ? new List<string>() : new List<string> { type }
                };
            }
        }

        public IAsyncEnumerable<Song> SearchSongsAsync(Game game, string searchTerm, CancellationToken? token = null)
            => throw new NotImplementedException();

        public async Task GetAlbumInfoAsync(
            Album album, CancellationToken token, Func<Action<Song>, Song, Task> updateCallback)
        {
            var htmlDoc = await GetAlbumHtmlAsync(album.Id, token);
            if (token.IsCancellationRequested) /* Then */ return;

            var content = htmlDoc.GetElementbyId("pageContent");
            album.CoverUri = content.Descendants("div")
                                    .FirstOrDefault(d => d.HasClass("albumImage"))
                                   ?.Descendants("a")
                                    .FirstOrDefault()
                                   ?.GetAttributeValue("href", string.Empty);

            Task coverUriTask = null;
            if (album.CoverUri.HasText())
            {
                coverUriTask = updateCallback(null, null);
            }

            // Get Info; some albums have an alternative title in the way
            var infoPara = content
                          .Descendants("p")
                          .FirstOrDefault(p => !p.HasClass("albuminfoAlternativeTitles"));
            if (infoPara != null)
            {
                var hyperLinks = infoPara.Descendants("a").ToList();

                var developersFound = false;
                var publishersFound = false;
                var uploaderFound = false;
                foreach (var link in hyperLinks)
                {
                    var linkValue = link.GetAttributeValue("href", string.Empty);

                    if (!developersFound && linkValue.Contains("/developer/"))
                    {
                        album.Developers = new List<string> { link.InnerHtml };
                        developersFound = true;
                    }
                    else if (!publishersFound && linkValue.Contains("/publisher/"))
                    {
                        album.Publishers = new List<string> { link.InnerHtml };
                        publishersFound = true;
                    }
                    else if (!uploaderFound && linkValue.Contains("members/"))
                    {
                        album.Uploader = link.InnerHtml;
                        uploaderFound = true;
                    }

                    if (developersFound && publishersFound && uploaderFound) /* Then */ break;
                }

                var dateAddedRegex = new Regex(@"Date Added:\s*<b>([^<]*)<\/b>");
                var dateAddedMatch = dateAddedRegex.Match(infoPara.InnerHtml);
                if (dateAddedMatch.Success) /* Then */ album.CreationDate = dateAddedMatch.Groups[1].Value;
            }

            var tableRows = GetSongTable(htmlDoc);
            if (tableRows is null)
            {
                _logger.Info($"Unable to find table for album '{album.Name}'");
                return;
            }

            var footer = tableRows.Pop();

            var items = footer.Descendants("b").Skip(1).ToList();

            var length = items.FirstOrDefault()?.InnerHtml;
            if (TimeSpan.TryParseExact(length, "h\\h m\\m", null, out var result))
            {
                album.Length = result;
            }

            var mp3Size = items.LastOrDefault()?.InnerHtml;
            if (mp3Size != null) /* Then */ album.Sizes = new Dictionary<string, string> { ["MP3"] = mp3Size };

            if (coverUriTask != null) /* Then */ await coverUriTask;
            await updateCallback(null, null);

            await foreach(var song in GetSongsFromTable(album, tableRows, token))
            {
                await updateCallback(album.Songs.Add, song);
            }

            if (token.IsCancellationRequested) /* Then */ return;

            album.Count = (uint)album.Songs.Count;
            await updateCallback(null, null);
            album.HasExtraInfo = false;
        }

        private async Task<Stream> GetStreamAsync(string url)
        {
            var stream = await _httpClient.GetStreamAsync(url);
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return memoryStream;
        }

        public async IAsyncEnumerable<Song> GetSongsFromAlbumAsync(Album album, CancellationToken? token = null)
        {
            var cancellationToken = token ?? CancellationToken.None;
            var tableRows = GetSongTable(await GetAlbumHtmlAsync(album.Id, cancellationToken));

            // Remove footer
            tableRows.Pop();

            await foreach(var song in GetSongsFromTable(album, tableRows, cancellationToken))
            /* Then */ yield return song;
        }

        public async Task<bool> DownloadAsync(
            DownloadItem item, string path, IProgress<double> progress, CancellationToken token)
        {
            if (!(item is Song song))
            {
                _logger.Error("KH does not support bulk downloads");
                return false;
            }
            try
            {
                using (var httpMessage = await _httpClient.GetAsync(song.StreamUri, token))
                using (var fileStream = File.Create(path))
                if    (progress != null) /* Then */ await DownloadCommon.DownloadStreamAsync(await httpMessage.Content.ReadAsStreamAsync(), fileStream, progress, token);
                else                     /* Then */ await httpMessage.Content.CopyToAsync(fileStream);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Unable to download file from URL '{song.StreamUri}' for song '{song.Name}'");
                return false;
            }

            return true;
        }

        #region Helpers

        private Task<HtmlDocument> GetAlbumHtmlAsync(string id, CancellationToken token) 
            => _web.LoadFromWebAsync($"{BaseUrl}/{id}", token);

        private IList<HtmlNode> GetSongTable(HtmlDocument htmlDoc)
        {
            var headers = htmlDoc.GetElementbyId("songlist_header").Descendants("th").Select(n => n.InnerHtml);
            if (headers.All(h => !h.Contains("MP3")))
            {
                _logger.Info("No mp3 in album");
                return null;
            }

            var table = htmlDoc.GetElementbyId("songlist");

            // Get table and skip header
            var tableRows = table.Descendants("tr").Skip(1).ToList();
            if (tableRows.Count < 2)
            {
                _logger.Info("No songs in album");
                return null;
            }

            return tableRows;
        }

        
        private async IAsyncEnumerable<Song> GetSongsFromTable(
            Album album, IList<HtmlNode> tableRows, [EnumeratorCancellation] CancellationToken token)
        {
            foreach (var row in tableRows)
            {
                var rowEntries = row.Descendants("a").ToList();

                var songNameEntry = rowEntries.FirstOrDefault();
                if (songNameEntry is null)
                {
                    _logger.Info("Found entry without columns");
                    continue;
                }

                var songName = StringUtilities.StripStrings(songNameEntry.InnerHtml);

                var partialUrl = songNameEntry.GetAttributeValue("href", null);
                if (string.IsNullOrWhiteSpace(partialUrl))
                {
                    _logger.Info($"Found entry without link in title for song '{songName}'");
                    continue;
                }

                // Get Url for file from Song html page
                var fileHtmlDoc = await _web.LoadFromWebAsync($"{BaseUrl}/{partialUrl}", token);
                if (token.IsCancellationRequested) /* Then */ yield break;

                var fileUri = fileHtmlDoc.GetElementbyId("audio").GetAttributeValue("src", null);
                if (fileUri is null)
                {
                    _logger.Info($"Did not find file url for song '{songName}'");
                    continue;
                }

                var trackNumberStr = row.ChildNodes[1].InnerHtml;
                uint? trackNumber = null;
                if (uint.TryParse(trackNumberStr.Substring(0, trackNumberStr.Length - 1), out var number))
                /* Then */ trackNumber = number;

                var mp3Size = rowEntries.ElementAtOrDefault(2)?.InnerHtml;
                yield return new Song
                {
                    Name = songName,
                    Album = album.Name,
                    ParentAlbum = album,
                    Id = partialUrl,
                    Source = Source.KHInsider,
                    Length = StringUtilities.GetTimeSpan(rowEntries.ElementAtOrDefault(1)?.InnerHtml),
                    Sizes = mp3Size is null ? null : new Dictionary<string, string>
                    {
                        ["MP3"] = rowEntries.ElementAtOrDefault(2)?.InnerHtml
                    },
                    StreamUri = fileUri,
                    StreamFunc = _ => GetStreamAsync(fileUri),
                    TrackNumber = trackNumber
                };
            }
        }

        #endregion

        #endregion
    }
}
