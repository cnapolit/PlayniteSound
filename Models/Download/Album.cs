using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteSounds.Models
{
    public class Album : DownloadItem, IAsyncDisposable, IDisposable
    {

        private volatile bool                   _flag;
        private readonly object                 _lock = new object();
        private          IAsyncEnumerator<Song> _enumerator;

        private readonly Func<Album, CancellationToken?, IAsyncEnumerable<Song>> _enumerableFunc;

        public Album(Func<Album, CancellationToken?, IAsyncEnumerable<Song>> enumerableFunc = null)
        {
            _enumerableFunc = enumerableFunc;
            HasSongsToEnumerate = _enumerableFunc != null;
        }

        public bool                       HasExtraInfo        { get; set; } = true;
        public bool                       IsUnbound           { get; set; }
        public uint?                      Count               { get; set; }
        public IList<string>              Platforms           { get; set; }
        public ObservableCollection<Song> Songs               { get; set; } = [];
        public bool                       HasSongsToEnumerate { get; set; }
        public Song                       Current             => _enumerator?.Current;

        public bool Initialize(CancellationToken token)
        {
            if (!HasSongsToEnumerate)
            {
                return false;
            }

            _enumerator = _enumerator ?? _enumerableFunc(this, token).GetAsyncEnumerator(token);
            return true;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            lock (_lock)
            {
                if (_flag) /* Then */ return false;
                _flag = true;
            }

            if (HasSongsToEnumerate && (HasSongsToEnumerate &= await _enumerator.MoveNextAsync()))
            {
                Songs.Add(_enumerator.Current);
            }
            else
            {
                if (Count is null || (Count is 0 && Songs.Count != 0)) /* Then */ Count = (uint)Songs.Count;

                if (_enumerator != null) /* Then */ await _enumerator.DisposeAsync();
            }

            lock (_lock)
            {
                _flag = false;
            }

            return HasSongsToEnumerate;
        }

        private static readonly IEnumerable<string> Properties = new[]
        {
            nameof(Count),
            nameof(Platforms)
        };

        protected override IList<string> GetProperties()
        {
            var properties = new List<string>(base.GetProperties());
            properties.AddRange(Properties);
            return properties;
        }

        public override string ToString() => JoinWithBase(PropertiesToStrings(Properties));

        public async void Dispose() => await DisposeAsync();

        private bool _disposed;
        public async ValueTask DisposeAsync()
        {
            if (_disposed ) /* Then */ return;
            _disposed = true;

            var enumeratorTask = _enumerator?.DisposeAsync();
            Songs.OfType<IDisposable>().ForEach(s => s.Dispose());
            if (enumeratorTask != null) /* Then */ await enumeratorTask.Value;
        }

        public override string Summary => GetSummary();
        private string GetSummary()
        {
            var summaryList =  new List<string> { NameToValue(nameof(Name), Name) };

            if (Artists?.Any() ?? false) /* Then */ summaryList.Add(NameToValue(nameof(Artists), Artists));
            summaryList.Add(NameToValue(nameof(Count), Count));

            return JoinProperties(summaryList);
        }
    }
}
