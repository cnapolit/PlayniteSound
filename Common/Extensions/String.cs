using System;
using System.Linq;

namespace PlayniteSounds.Common.Extensions;

public static class StringExtensions
{
    public static int Similarity(this string s, string t)
    {
        var leftIsEmpty = string.IsNullOrEmpty(s);
        var rightIsEmpty = string.IsNullOrEmpty(t);
        return leftIsEmpty && rightIsEmpty ? 0
            : leftIsEmpty                 ? t.Length
            : rightIsEmpty                ? s.Length
            : CalculateSimilarity(s, t);
    }

    // based on https://stackoverflow.com/questions/6944056/compare-string-similarity
    private static int CalculateSimilarity(string s, string t)
    {
        var firstRow = new int[t.Length + 1];
        var secondRow = new int[t.Length + 1];

        // initialize very first row to 0, 1, 2, ...
        for (var i = 1; i <= t.Length; firstRow[i] = i++);

        for (var i = 1; i <= s.Length; i++)
        {
            secondRow[0] = i;
            for (var j = 1; j <= t.Length; j++)
            {
                var cost = t[j - 1] == s[i - 1] ? 0 : 1;
                var leftCell = firstRow[j] + 1;
                var topCell = secondRow[j - 1] + 1;
                var topLeftCell = firstRow[j - 1];
                secondRow[j] = Math.Min(Math.Min(leftCell, topCell), topLeftCell + cost);
            }
            (firstRow, secondRow) = (secondRow, firstRow);
        }

        return secondRow[t.Length];
    }

    public static bool EndsWithAny(this string str, params string[] suffixes)
        => EndsWithAny(str, StringComparison.OrdinalIgnoreCase, suffixes);

    public static bool EndsWithAny(this string str, StringComparison stringComparison, params string[] suffixes)
        => suffixes.Any(s => str.EndsWith(s, stringComparison));

    public static bool HasText(this string str) => !string.IsNullOrWhiteSpace(str);
}