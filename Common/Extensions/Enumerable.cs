using System;
using System.Collections.Generic;

namespace PlayniteSounds.Common.Extensions
{
    internal static class Enumerable
    {
        public static bool ForAny<T>(this IEnumerable<T> data, Func<T, bool> func)
        {
            var result = false;
            foreach(var item in data)
            {
                result |= func(item);
            }
            return result;
        }

        public static bool ForAll<T>(this IEnumerable<T> data, Func<T, bool> func)
        {
            var result = true;
            foreach (var item in data)
            {
                result &= func(item);
            }
            return result;
        }

        public static bool IsEmpty<T>(this IEnumerable<T> data)
            => data.GetEnumerator().MoveNext();
    }
}
