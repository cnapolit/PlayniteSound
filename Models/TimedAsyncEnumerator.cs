using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;

namespace PlayniteSounds.Models
{
    public class TimedAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly ILogger             _logger;
        private readonly IAsyncEnumerator<T> _enumerator;
        private readonly TimeSpan            _timeout;

        public TimedAsyncEnumerator(ILogger logger, IAsyncEnumerator<T> enumerator, TimeSpan timeout)
        {
            _logger = logger;
            _enumerator = enumerator;
            _timeout = timeout;
        }

        public T Current => _enumerator.Current;

        public ValueTask DisposeAsync() => _enumerator.DisposeAsync();

        public ValueTask<bool> MoveNextAsync() => MoveNextAsync(CancellationToken.None);

        public async ValueTask<bool> MoveNextAsync(CancellationToken token)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token)) /* Then */ try
            {
                cts.CancelAfter(_timeout);
                return await _enumerator.MoveNextAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to move next");
                return false;
            }
        }
    }
}