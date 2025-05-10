using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteSounds.Common.Extensions;

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


    public static async Task<List<TSource>> AsListAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken token)
    {
        var list = new List<TSource>();

        await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
        {
            list.Add(item);
        }

        return list;
    }
}