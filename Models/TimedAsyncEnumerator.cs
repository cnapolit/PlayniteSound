using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;

namespace PlayniteSounds.Models;

public class TimedAsyncEnumerator<T>(ILogger logger, IAsyncEnumerator<T> enumerator, TimeSpan timeout)
    : IAsyncEnumerator<T>
{
    public T Current => enumerator.Current;

    public ValueTask DisposeAsync() => enumerator.DisposeAsync();

    public ValueTask<bool> MoveNextAsync() => MoveNextAsync(CancellationToken.None);

    public async ValueTask<bool> MoveNextAsync(CancellationToken token)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        try
        {
            cts.CancelAfter(timeout);
            return await enumerator.MoveNextAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception e)
        {
            logger.Error(e, "Failed to move next");
            return false;
        }
    }
}