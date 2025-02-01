using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PlayniteSounds.Models;

public class Album : DownloadItem, IDisposable, IAsyncEnumerator<IEnumerable<Song>>
{
    public bool                       HasExtraInfo        { get; set; } = true;
    public bool                       IsUnbound           { get; set; }
    public uint?                      Count               { get; set; }
    public IList<string>              Platforms           { get; set; }
    public ObservableCollection<Song> Songs               { get; set; } = [];
    public bool                       HasSongsToEnumerate { get; set; }
    public Dispatcher Dispatcher                          { private get; set; }

    public           IEnumerable<Song>                   Current => _enumerator.Current;
    private volatile bool                                _flag;
    private readonly object                              _lock = new();
    private          IAsyncEnumerator<IEnumerable<Song>> _enumerator;
    private          CancellationToken                   _token;

    public bool Initialize(IAsyncEnumerator<IEnumerable<Song>> enumerator, CancellationToken token)
    {
        _enumerator = enumerator;
        _token = token;
        HasSongsToEnumerate = true;
        return true;
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        lock (_lock)
        {
            if (_flag || _enumerator is null) /* Then */ return false;
            _flag = true;
        }

        if      (HasSongsToEnumerate &= await _enumerator.MoveNextAsync(_token))
        foreach (var song in _enumerator.Current) /* Then */ await Dispatcher.InvokeAsync(() => Songs.Add(song));
        else
        {
            if (Count is null || (Count is 0 && Songs.Count != 0)) /* Then */ Count = (uint)Songs.Count;
            if (!Length.HasValue) /* Then */ Length = TimeSpan.FromTicks(Songs.Where(s => s.Length.HasValue).Sum(s => s.Length.Value.Ticks));
        }

        lock (_lock) /* Then */ _flag = false;

        return HasSongsToEnumerate;
    }

    private static readonly IEnumerable<string> Properties =
    [
        nameof(Count),
        nameof(Platforms)
    ];

    protected override IList<string> GetProperties()
    {
        List<string> properties = [..base.GetProperties()];
        properties.AddRange(Properties);
        return properties;
    }

    public override string ToString() => JoinWithBase(PropertiesToStrings(Properties));

    public async void Dispose() => await DisposeAsync();

    private bool _disposed;
    public async ValueTask DisposeAsync()
    {
        if (_disposed) /* Then */ return;
        _disposed = true;

        var enumeratorTask = _enumerator?.DisposeAsync();
        Songs.OfType<IDisposable>().ForEach(s => s.Dispose());
        if (enumeratorTask != null) /* Then */ await enumeratorTask.Value;
    }

    public override string Summary => GetSummary();
    private string GetSummary()
    {
        List<string> summaryList = [NameToValue(nameof(Name), Name)];

        if (Artists?.Any() ?? false) /* Then */ summaryList.Add(NameToValue(nameof(Artists), Artists));
        summaryList.Add(NameToValue(nameof(Count), Count));

        return JoinProperties(summaryList);
    }
}