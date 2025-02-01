using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PlayniteSounds.Common.Extensions;

public static class Collection
{
    public static IEnumerable<T> Select<T>(this ICollection collection, Func<object, T> func) =>
        from object item in collection select func(item);

    public static T Pop<T>(this ICollection<T> collection, int index)
    {
        var item = collection.ElementAt(index);
        collection.Remove(item);
        return item;
    }
}