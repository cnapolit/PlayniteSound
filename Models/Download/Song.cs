using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteSounds.Models;

public class Song : DownloadItem, IDisposable
{
    public string Album       { get; set; }
    public Stream Stream      { get; set; }
    public Album  ParentAlbum { get; set; }
    public string StreamUri   { get; set; }

    public Func<Song, CancellationToken, Task<Stream>> StreamFunc { private get; set; }

    public async Task GetStreamAsync(CancellationToken token) => Stream = await StreamFunc(this, token);

    private static readonly IEnumerable<string> Properties =
    [
        nameof(Album)
    ];

    protected override IList<string> GetProperties()
    {
        List<string> properties = [..base.GetProperties()];
        properties.AddRange(Properties);
        return properties;
    }

    public override string Summary => GetSummary();
    private string GetSummary()
    {
        List<string> summaryList = [NameToValue(nameof(Name), Name)];

        var people = Artists?.Any() ?? false              ? NameToValue(nameof(Artists), Artists) :
            !string.IsNullOrWhiteSpace(Uploader) ? NameToValue(nameof(Uploader), Uploader)
            : string.Empty;

        summaryList.Add(people);
        summaryList.Add(NameToValue(nameof(Length), Length));

        return JoinProperties(summaryList);
    }

    public override string ToString() => JoinWithBase(PropertiesToStrings(Properties));

    public void Dispose() => Stream?.Dispose();
}