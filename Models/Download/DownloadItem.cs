using System.Collections.Generic;

namespace PlayniteSounds.Models;

public abstract class DownloadItem : BaseItem
{
    public ICollection<string> Developers { get; set; }
    public ICollection<string> Publishers { get; set; }
    public string              Uploader   { get; set; }

    private static readonly List<string> Properties =
    [
        nameof(Developers),
        nameof(Publishers),
        nameof(Uploader)
    ];

    protected override IList<string> GetProperties()
    {
        List<string> properties = [..base.GetProperties()];
        properties.AddRange(Properties);
        return properties;
    }

    public override string ToString() => JoinWithBase(PropertiesToStrings(Properties));

    protected string JoinWithBase(IEnumerable<string> strs)
        => JoinProperties([base.ToString(), JoinProperties(strs)]);
}