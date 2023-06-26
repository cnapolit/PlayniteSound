using System.Collections.Generic;

namespace PlayniteSounds.Common.Extensions
{
    public static class List
    {
        public static TItem Pop<TItem>(this IList<TItem> list)
        {
            if (list.Count > 0)
            {
                var index = list.Count - 1;
                var item = list[index];
                list.RemoveAt(index);
                return item;
            }

            return default;
        }
    }
}
