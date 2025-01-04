using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PlayniteSounds.Common.Extensions
{
    public static class Dictionary
    {
        public static IDictionary<TKey, TResult> ToDict<TKey, TValue, TResult>(
            this Dictionary<TKey, TValue> dict, Func<KeyValuePair<TKey, TValue>, TResult> valueSelector) 
            => dict.ToDictionary(p => p.Key, valueSelector);

        public static IEnumerable<T> Select<T>(this IDictionary dictionary, Func<DictionaryEntry, T> func)
        {
            var enumerator = dictionary.GetEnumerator();
            while (enumerator.MoveNext()) /* Then */ yield return func(enumerator.Entry);
            if (enumerator is IDisposable disposable) /* Then */ disposable.Dispose();
        }
    }
}