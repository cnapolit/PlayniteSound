using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PlayniteSounds.Common.Extensions;

namespace PlayniteSounds.Common;

public class StringUtilities
{
    private static readonly string[] StringsToRemove = ["-", ":"];
    private static readonly IDictionary<string, string> StringsToReplace
        = new Dictionary<string, string> { { " & ", " (&|and) " } };
    private static readonly IDictionary<string, string> ReplaceExtraWhitespace
        = new Dictionary<string, string> { { "  ", " " } };
    private static readonly string InvalidCharacters = new(Path.GetInvalidFileNameChars());
    private static readonly Regex InvalidCharsRegex = new($"[{Regex.Escape(InvalidCharacters)}]");

    public static string StripStrings(string stringToStrip, string[] stringsToRemove = null)
    {
        stringsToRemove ??= StringsToRemove;
        stringToStrip = stringsToRemove.Aggregate(stringToStrip, (current, str) => current.Replace(str, ""));
        return ReplaceStrings(stringToStrip, ReplaceExtraWhitespace);
    }

    public static string ReplaceStrings(string stringToSub, IDictionary<string, string> stringsToReplace = null)
    {
        stringsToReplace ??= StringsToReplace;
        return stringsToReplace.Aggregate(stringToSub, (current, stringToReplace) 
            => current.Replace(stringToReplace.Key, stringToReplace.Value));
    }

    public static string Sanitize(string str) => InvalidCharsRegex.Replace(str, string.Empty);

    public static TimeSpan? GetTimeSpan(string time)
    {
        if (string.IsNullOrWhiteSpace(time)) return null;

        var times = time.Split(':').ToList();

        var seconds = PopToInt(times);
        var minutes = PopToInt(times);
        var hours = PopToInt(times);

        return new TimeSpan(hours, minutes, seconds);
    }

    private static int PopToInt(IList<string> strings)
    {
        var str = strings.Pop();
        return string.IsNullOrWhiteSpace(str) ? 0 : int.Parse(str);
    }
}