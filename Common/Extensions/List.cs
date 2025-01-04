using System;
using System.Collections.Generic;

namespace PlayniteSounds.Common.Extensions
{
    public static class List
    {
        public static TItem Pop<TItem>(this IList<TItem> list)
        {
            if (list.Count <= 0) /* Then */ return default;

            var index = list.Count - 1;
            var item = list[index];
            list.RemoveAt(index);
            return item;

        }

        public static void ForEach<TItem, TIgnore>(this IList<TItem> list, Func<TItem, TIgnore> methodAction)
        {
            void Action(TItem i) => methodAction(i);
            list.ForEach(Action);
        }

        public static bool AddNotNull<T>(this IList<T> list, T item)
        {
            if (item == null) /* Then */ return false;
            list.Add(item);
            return true;

        }

        public static bool AddNotNull<T>(this IList<T> list, T? item) where T : struct
        {
            if (item is null) /* Then */ return false;
            list.Add(item.Value);
            return true;

        }
    }
}
