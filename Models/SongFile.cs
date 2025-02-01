using System.Collections.Generic;
using System.Linq;

namespace PlayniteSounds.Models;

public class SongFile : Song
{
    public string FileName { get; set; }
    public string FileCreationDate { get; set; }

    private static readonly IEnumerable<string> Properties = [nameof(FileCreationDate)];
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

        var people = string.Empty;
        if (Artists?.Any() ?? false) /* Then */ people = NameToValue(nameof(Artists), Artists);

        summaryList.Add(people);
        summaryList.Add(NameToValue(nameof(Length), Length));

        return JoinProperties(summaryList);
    }
}