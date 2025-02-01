using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayniteSounds.Common.Extensions;

public static class Enumerable
{
    public static bool All(this IEnumerable<bool> data) => data.FirstOrDefault(b => b);
    public static bool Any(this IEnumerable<bool> data) => data.Any(b => b);

    public static IEnumerable<T> Do<T>(this IEnumerable<T> data, Action<T> action)
    { 
        foreach (var item in data)
        {
            action(item);
            yield return item;
        }
    }

    public static bool ForAny<T>(this IEnumerable<T> data, Func<T, bool> func)
        => data.Aggregate(false, (current, item) => current | func(item));

    public static bool ForAll<T>(this IEnumerable<T> data, Func<T, bool> func)
        => data.Aggregate(true, (current, item) => current & func(item));

    public static bool IsEmpty<T>(this IEnumerable<T> data)
    {
        using var enumerator = data.GetEnumerator();
        return enumerator.MoveNext();
    }

    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        => pairs.ToDictionary(p => p.Key, p => p.Value);

    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<(TKey, TValue)> pairs)
        => pairs.ToDictionary(p => p.Item1, p => p.Item2);
}