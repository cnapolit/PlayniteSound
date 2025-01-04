using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteSounds.Common.Extensions
{
    public static class AsyncEnumerableExtensions
    {
        public static async Task<bool> ForAllAsync<T>(this IAsyncEnumerable<T> data, Func<T, Task<bool>> func)
        {
            var dataEnumerator = data.GetAsyncEnumerator();
            var tasks = new List<Task<bool>>();
            while (await dataEnumerator.MoveNextAsync())
            {
                tasks.Add(func(dataEnumerator.Current));
            }
            var results = await Task.WhenAll(tasks);

            return results.All();
        }

        public static async Task<ObservableCollection<T>> ToObservableCollectionAsync<T>(
            this IAsyncEnumerable<T> data, CancellationToken token)
            => new(await data.ToListAsync(token));
    }
}
