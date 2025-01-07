using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteSounds.Common;
using PlayniteSounds.Common.Extensions;
using PlayniteSounds.Common.Constants;
using PlayniteSounds.Files.Download;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.Files;
using PlayniteSounds.Services.Play;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using PlayniteSounds.Models.Download;

namespace PlayniteSounds.Views.Models
{
    public class DownloadPromptModel : INotifyPropertyChanged, IDisposable, IAsyncDisposable
    {
        #region Infrastructure

        [Flags]
        private enum TaskType
        {
            Get = 1,
            Stream = 2,
            Download = 4,
            Song = 8,
            Album = 16,
            Info = 32,
            GetAlbum = Get | Album,
            GetAlbumInfo = GetAlbum | Info,
            DownloadAlbum = Download | Album,
            GetSong = Get | Song,
            GetSongInfo = GetSong | Info,
            StreamSong = Stream | Song,
            DownloadSong = Download | Song
        }

        private readonly PlayniteSoundsSettings  _settings;
        private readonly IWebViewFactory         _webViewFactory;
        private readonly IDownloadManager        _downloadManager;
        private readonly IFileManager            _fileManager;
        private readonly IPathingService         _pathingService;
        private readonly ITagger                 _tagger;
        private readonly IMusicPlayer            _musicPlayer;
        private readonly IList<Game>             _games;
        private readonly ISet<Game>              _downloadedGames                       = new HashSet<Game>();
        private readonly ISet<Game>              _removedGames                          = new HashSet<Game>();
        private readonly object                  _songLock                              = new object();
        private readonly object                  _albumLock                             = new object();
        private readonly object                  _songSearchLock                        = new object();
        private readonly object                  _downloadLock                          = new object();
        private readonly object                  _selectLock                            = new object();
        private readonly CancellationTokenSource _songSearchCancellationTokenSource = new CancellationTokenSource();
        private          int                     _gameIndex                             = -1;
        private          uint                    _downloadCount;
        private          string                  _currentSongSearchTerm;
        private volatile IAsyncEnumerator<IEnumerable<Song>>  _songSearchEnumerator;
        private volatile IAsyncEnumerator<IEnumerable<Album>> _albumEnumerator;
        private volatile bool                    _albumFlag;
        private volatile bool                    _songSearchFlag;

        private readonly Dictionary<TaskType, CancellationTokenSource> _taskTypesToTokens
            = new Dictionary<TaskType, CancellationTokenSource>();
        private readonly Dictionary<string, CancellationTokenSource> _songTasks =
            new Dictionary<string, CancellationTokenSource>();
        private readonly Dictionary<string, (ConfiguredTaskAwaitable, CancellationTokenSource)> _downloadTasks =
            new Dictionary<string, (ConfiguredTaskAwaitable, CancellationTokenSource)>();

        public Dispatcher Dispatcher { get; set; }

        public DownloadPromptModel(
            IMainViewAPI mainViewApi,
            IWebViewFactory webViewFactory,
            IDownloadManager downloadManager,
            IFileManager fileManager,
            IPathingService pathingService,
            ITagger tagger,
            IMusicPlayer musicPlayer,
            PlayniteSoundsSettings settings)
        {
            _games = mainViewApi.SelectedGames.ToList();
            _webViewFactory = webViewFactory;
            _downloadManager = downloadManager;
            _fileManager = fileManager;
            _pathingService = pathingService;
            _tagger = tagger;
            _musicPlayer = musicPlayer;
            _musicPlayer.Stop();
            _settings = settings;
            _songSearchItems = [];
            GetNextGame();
        }

        public async void Dispose() => await DisposeAsync();

        private bool _disposed;
        public async ValueTask DisposeAsync()
        {
            if (_disposed) /* Then */ return;
            _disposed = true;

            var enumeratorTask = _albumEnumerator?.DisposeAsync().ConfigureAwait(false);

            AlbumSearchItems = null;
            CancelTasks();
            await Task.WhenAll(_downloadTasks.Select(async t =>
            {
                t.Value.Item2.Cancel();
                await t.Value.Item1;
                t.Value.Item2.Dispose();
            }));

            if (_settings.TagMissingEntries)
            {
                _tagger.RemoveTag(_downloadedGames, Resource.MissingTag);
                _tagger.AddTag(_removedGames, Resource.MissingTag);
            }
            _musicPlayer.Stop();

            if (enumeratorTask.HasValue) /* Then */ await enumeratorTask.Value;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion

        #region Properties

        private string _downloadText;
        public string DownloadText
        {
            get => _downloadText;
            set
            {
                _downloadText = value;
                OnPropertyChanged();
            }
        }

        private Source _searchSource = Source.Youtube;

        // We modify the source to match offset of dropdown (particularly to skip Source.All)
        public  Source SearchSource
        {
            get => _searchSource - 1;
            set => _searchSource = value + 1;
        }

        private bool _itemCanBeStreamed;
        public bool ItemCanBeStreamed
        {
            get => _itemCanBeStreamed;
            set
            {
                if (_itemCanBeStreamed != value)
                {
                    _itemCanBeStreamed = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _hasNextGame;
        public bool HasNextGame
        {
            get => _hasNextGame;
            set
            {
                _hasNextGame = value;
                OnPropertyChanged();
            }
        }

        private bool _hasPreviousGame;
        public bool HasPreviousGame
        {
            get => _hasPreviousGame;
            set
            {
                _hasPreviousGame = value;
                OnPropertyChanged();
            }
        }

        private string _gameCoverImagePath;
        public string GameCoverImagePath
        {
            get => _gameCoverImagePath;
            set
            {
                _gameCoverImagePath = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<Tuple<Source, string>> Sources
            => ArrayExtensions.Where<Source>(s => s != Source.All).Select(
                s => new Tuple<Source, string>(
                    s,
                    _downloadManager.GetSourceIcon(s) is string str  ? _pathingService.GetImageFile(str) : null));


        public class SongFileItem
        {
            public Song    Song             { get; set; }
            public double? DownloadProgress { get; set; }
        }

        private ObservableCollection<SongFileItem> _files;
        public ObservableCollection<SongFileItem> Files
        {
            get => _files;
            set
            {
                _files = value;
                OnPropertyChanged();
            }
        }

        private string _playingMusicName;
        public string PlayingMusicName
        {
            get => _playingMusicName;
            set
            {
                _playingMusicName = value;
                OnPropertyChanged();
            }
        }

        private long _playingMusicLength;
        public long PlayingMusicLength
        {
            get => _playingMusicLength;
            set
            {
                _playingMusicLength = value;
                PlayingMusicLengthDisplay = PositionToString(_musicPlayer.LengthInSeconds);
                OnPropertyChanged();
            }
        }

        private string _playingMusicLengthDisplay;
        public string PlayingMusicLengthDisplay
        {
            get => _playingMusicLengthDisplay;
            set
            {
                _playingMusicLengthDisplay = value;
                OnPropertyChanged();
            }
        }

        public long PlayingMusicPosition
        {
            get => _musicPlayer.Position;
            set
            {
                _musicPlayer.Position = value;
                OnPropertyChanged();
            }
        }

        public string PlayingMusicPositionDisplay => PositionToString(_musicPlayer.PositionInSeconds);

        private string _albumSearchTerm;
        public string AlbumSearchTerm
        {
            get => _albumSearchTerm;
            set
            {
                _albumSearchTerm = value;
                OnPropertyChanged();
            }
        }

        private string _songSearchTerm;
        public string SongSearchTerm
        {
            get => _songSearchTerm;
            set
            {
                _songSearchTerm = value;
                OnPropertyChanged();
            }
        }

        private string _downloadStatus;
        public string DownloadStatus
        {
            get => _downloadStatus;
            set
            {
                _downloadStatus = value;
                OnPropertyChanged();
            }
        }

        private Album _selectedAlbum;
        public Album SelectedAlbum
        {
            get => _selectedAlbum;
            set
            {
                _selectedAlbum = value;
                OnPropertyChanged();
            }
        }

        private BaseItem _selectedItem;
        public BaseItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                lock (_selectLock)
                {
                    _selectedItem = value;
                    if (_selectedItem is Album album)
                    {
                        SongSearchItems = album.Songs;
                        SelectedAlbum = album;
                        if   (album.HasExtraInfo)
                        lock (_songLock)
                        if   (!_songTasks.ContainsKey(album.Id))
                        {
                            var tokenSource = new CancellationTokenSource();
                            _songTasks[album.Id] = tokenSource;
                            GetAlbumInfoAsync(album, tokenSource.Token).ConfigureAwait(false);
                        }
                    }
                    else /* Then */ ItemCanBeStreamed = true;

                    OnPropertyChanged();
                }
            }
        }

        private async Task GetAlbumInfoAsync(Album album, CancellationToken token)
        {
            await _downloadManager.GetAlbumInfoAsync(album, token, async (f, s) => await Dispatcher.InvokeAsync(() =>
            {
                lock (_selectLock)
                {
                    f?.Invoke(s);
                    if (_selectedAlbum == album)
                    {
                        OnPropertyChanged(nameof(SelectedItem));
                        if (f != null)
                        {
                            OnPropertyChanged(nameof(SongSearchItems));
                        }
                    }
                }
            }, DispatcherPriority.Send));

            lock (_songLock) /* Then */ _songTasks.Remove(album.Id);
        }

        private ObservableCollection<Album> _albumSearchItems = new ObservableCollection<Album>();
        public ObservableCollection<Album> AlbumSearchItems
        {
            get => _albumSearchItems;
            set
            {
                _albumSearchItems.OfType<IAsyncDisposable>().ForEach(d => d.DisposeAsync());
                _albumSearchItems = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Song> _songSearchItems;
        public ObservableCollection<Song> SongSearchItems
        {
            get => _songSearchItems;
            set
            {
                _songSearchItems = value;
                OnPropertyChanged();
            }
        }

        private string _previousGameStr;
        public string PreviousGameStr
        {
            get => _previousGameStr;
            set
            {
                _previousGameStr = value;
                OnPropertyChanged();
            }
        }

        private string _nextGameStr;
        public string NextGameStr
        {
            get => _nextGameStr;
            set
            {
                _nextGameStr = value;
                OnPropertyChanged();
            }
        }

        private Game _currentGame;
        public Game CurrentGame
        {
            get => _currentGame;
            set
            {
                _currentGame = value;

                HasPreviousGame = _gameIndex > 0;
                HasNextGame     = _gameIndex + 1 < _games.Count;

                PreviousGameStr = HasPreviousGame ? _games[_gameIndex - 1].Name : string.Empty;
                NextGameStr     = HasNextGame     ? _games[_gameIndex + 1].Name : string.Empty;

                var gameName = StringUtilities.StripStrings(value.Name);
                AlbumSearchTerm = _downloadManager.GenerateSearchStr(SearchSource, gameName);

                if (value.CoverImage != null)
                {
                    GameCoverImagePath = _pathingService.GetLibraryFile(value.CoverImage);
                }

                Files = _pathingService.GetMusicFiles(value).Select(f => new SongFileItem {Song = f }).ToObservable();

                DownloadStatus = $"{App.AppName} - {Resource.DialogMessageDownloadingFiles}\n\n{_gameIndex}/{_games.Count}\n{value.Name}";

                StopMusic();
                CancelTasks();
                SearchOnEnter();

                OnPropertyChanged();
            }
        }

        private double _volume;
        public double Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                _musicPlayer.SetVolume((float)value / 100f);
                OnPropertyChanged();
            }
        }

        private bool _noCancelingInProgress = true;
        public bool NoCancelingInProgress
        {
            get => _noCancelingInProgress;
            set
            {
                if (_noCancelingInProgress != value)
                {
                    _noCancelingInProgress = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CancelButtonEnabled));
                }
            }
        }

        public bool CancelButtonEnabled => _downloadTasks.Count > 0 && NoCancelingInProgress;

        #endregion

        #region Commands

        public RelayCommand SelectCommand => new RelayCommand(SelectItem);
        public void SelectItem()
        {
            switch (SelectedItem)
            {
                case SongFile file: Play(file, file.Id, _musicPlayer.Play); break;
                case Song     song: StreamMusic(song); break;
                case Album   album: if (album.Songs?.Any() ?? false)
                                    /* Then */ SongSearchItems = album.Songs.ToObservable();
                                    else if (album.HasExtraInfo && !_songTasks.ContainsKey(album.Id))
                                    /* Then */ GetAlbumInfoAsync(album).ConfigureAwait(false); break;
            }
        }

        private async Task GetAlbumInfoAsync(Album album)
        {
            CancellationTokenSource tokenSource = null;
            lock (_songLock)
            {
                if (_songTasks.ContainsKey(album.Id) || !album.HasExtraInfo) /* Then */ return;
                tokenSource = new CancellationTokenSource();
                _songTasks[album.Id] = tokenSource;
            }
            await GetAlbumInfoAsync(album, tokenSource.Token);
            _songTasks.Remove(album.Id);
            tokenSource.Dispose();
        }

        public void Download(DownloadItem item)
        {
            lock (_downloadLock)
            {
                if (!NoCancelingInProgress) /* Then */ return;
                if (_downloadTasks.TryGetValue(item.Id, out var task))
                {
                    if (task.Item2.IsCancellationRequested)
                    {
                        //should do something
                    }
                    return;
                }
                var tokenSource = new CancellationTokenSource();
                _downloadTasks[item.Id] = (DownloadAsync(item, tokenSource.Token).ConfigureAwait(false), tokenSource);

                DownloadText = $"Downloading {_downloadCount}/{_downloadTasks.Count}";
                if (_downloadTasks.Count is 1) /* Then */ OnPropertyChanged(nameof(CancelButtonEnabled));
            }
        }

        private async Task DownloadAsync(DownloadItem item, CancellationToken token)
        {
            var path = _fileManager.CreateMusicDirectory(CurrentGame);
            var songFileItem = new SongFileItem { Song = item as Song, DownloadProgress = 0 };
            if (item is Album album)
            {
                
            }
            else
            {
                path = Path.Combine(path, StringUtilities.Sanitize(item.Name) + (item.Types?.FirstOrDefault() ?? ".mp3"));
                Files.Add(songFileItem);
                //OnPropertyChanged(nameof(Files));
            }

            var success = await _downloadManager.DownloadAsync(
                CurrentGame, item, path, new Progress<double>(p => songFileItem.DownloadProgress = p), token);
            await Dispatcher.InvokeAsync(() => UpdateAfterDownload(item, success));
        }

        private void UpdateAfterDownload(DownloadItem item, bool success)
        {
            lock (_downloadLock)
            {
                if (success)
                {
                    _downloadedGames.Add(CurrentGame);
                    _removedGames.Remove(CurrentGame);
                }
                else
                {
                    var songFileItem = Files.FirstOrDefault(f => f.Song.Id == item.Id);
                    if (songFileItem != null) /* Then */ Files.Remove(songFileItem);
                }

                _downloadTasks.Remove(item.Id);
                if (_downloadTasks.Count is 0)
                {
                    _downloadCount = 0;
                    DownloadText = "Downloads Finished";
                    OnPropertyChanged(nameof(CancelButtonEnabled));
                }
                else /* Then */ DownloadText = $"Downloading {++_downloadCount}/{_downloadTasks.Count}; {SelectedItem.Name} completed";
            }
        }

        public RelayCommand CancelDownloadCommand => new RelayCommand(CancelDownload);
        private void CancelDownload()
        {
            lock (_downloadLock)
            {
                if (!NoCancelingInProgress) /* Then */ return;
                NoCancelingInProgress = false;
                foreach (var (_, tokenSource) in _downloadTasks.Values) /* Then */ tokenSource.Cancel();
                DownloadText = "Canceling Downloads...";
                WaitForTasksToCancelAsync().ConfigureAwait(false);
            }
        }

        private async Task WaitForTasksToCancelAsync()
        {
            await Task.WhenAll(_downloadTasks.Values.Select(async t => await t.Item1));
            _downloadTasks.Clear();
            await Dispatcher.InvokeAsync(() =>
            {
                lock (_downloadLock)
                {
                    _downloadCount = 0;
                    DownloadText = "Downloads Canceled";
                    NoCancelingInProgress = true;
                }
            });
        }


        public RelayCommand ReturnCommand => new RelayCommand(Return);
        private void Return() => CurrentGame = _games[--_gameIndex];

        public RelayCommand SkipCommand => new RelayCommand(GetNextGame);

        public  RelayCommand TogglePlayPreviewCommand => new RelayCommand(_musicPlayer.Toggle);

        #endregion

        #region Methods

        public void Play(SongFileItem item) => Play(item.Song, item.Song.Id, _musicPlayer.Play);
        public void Play<T>(Song song, T source, Action<T> playAction)
        {
            var artists = song.Artists?.ToList() ?? new List<string>();

            var songName = song.Name;
            switch (artists.Count)
            {
                case 0: break;
                case 1: songName += $" by {artists[0]}"; break;
                default: songName += $" by {string.Join(", ", artists.GetRange(0, artists.Count - 2))} & {artists.Last()}"; break;
            }

            PlayingMusicName = songName;
            playAction(source);
            PlayingMusicLength = _musicPlayer.Length;
            Volume = 50d;
        }

        public void PausePreview() => _musicPlayer.Pause(false);
        public void PlayPreview() => _musicPlayer.Resume(false);

        public void SearchOnEnter()
        {
            lock (_albumLock)
            {
                _albumFlag = true;
                if (_taskTypesToTokens.TryGetValue(TaskType.GetAlbum, out var tokenSource))
                {
                    _taskTypesToTokens.Remove(TaskType.GetAlbum);
                    tokenSource.Cancel();
                }

                AlbumSearchItems = new ObservableCollection<Album>();
                SongSearchItems = new ObservableCollection<Song>();

                if (_downloadManager.GetCapabilities(_searchSource).HasFlag(DownloadCapabilities.FlatSearch))
                {
                    var sourceStr = _searchSource.ToString();
                    var flatAlbum = new Album
                    {
                        Id = sourceStr,
                        Name = sourceStr,
                        CoverUri = _downloadManager.GetSourceLogo(_searchSource),
                        HasExtraInfo = false,
                        IsUnbound = true
                    };
                    AlbumSearchItems.Add(flatAlbum);
                }

                tokenSource = new CancellationTokenSource();
                _taskTypesToTokens[TaskType.GetAlbum] = tokenSource;
                GetAlbumBatchAsync(tokenSource.Token).ConfigureAwait(false);
            }
        }

        public void AddAlbums()
        {
            lock (_albumLock)
            {
                if (_albumFlag || !_taskTypesToTokens.ContainsKey(TaskType.GetAlbum)) /* Then */ return;
                _albumFlag = true;
            }
            GetBatchAsync(_albumEnumerator,
                          AlbumSearchItems,
                          nameof(AlbumSearchItems),
                          _albumLock,
                          () => _albumFlag = false,
                          CancellationToken.None)
               .ConfigureAwait(false);
        }

        public void AddSongs()
        {
            if (!SelectedAlbum.IsUnbound) /* Then */ return;

            lock (_songSearchLock)
            {
                if (_songSearchFlag) /* Then */ return;
                _songSearchFlag = true;
            }

            GetBatchAsync(_songSearchEnumerator,
                          SongSearchItems,
                          nameof(SongSearchItems),
                          _songSearchLock,
                          () => _songSearchFlag = false,
                          _songSearchCancellationTokenSource.Token)
               .ConfigureAwait(false);
        }

        private async Task GetAlbumBatchAsync(CancellationToken token)
        {
            if (_albumEnumerator != null) /* Then */ await _albumEnumerator.DisposeAsync();
            
            var albums = _downloadManager.GetAlbumsForGameAsync(CurrentGame, AlbumSearchTerm, _searchSource);
            _albumEnumerator = CreateBatchEnumerableAsync(albums, token).GetAsyncEnumerator(token);
            await GetBatchAsync(_albumEnumerator, AlbumSearchItems, nameof(AlbumSearchItems), _albumLock, () => _albumFlag = false, token);
        }

        private async Task GetSongBatchAsync()
        {
            SongSearchItems = new ObservableCollection<Song>();
            if (_currentSongSearchTerm.HasText())
            {
                _songSearchCancellationTokenSource.Cancel();
                _currentSongSearchTerm = SongSearchTerm;
                await _songSearchEnumerator.DisposeAsync();
            }

            var token = _songSearchCancellationTokenSource.Token;

            var enumerable = _downloadManager.GetCapabilities(_searchSource).HasFlag(DownloadCapabilities.Batching)
                ? _downloadManager.SearchSongBatchesAsync(CurrentGame, SongSearchTerm, _searchSource, token)
                : CreateBatchEnumerableAsync(_downloadManager.SearchSongsAsync(CurrentGame, SongSearchTerm, _searchSource, token), token);
            _songSearchEnumerator = enumerable.GetAsyncEnumerator(token);

            await GetBatchAsync(_songSearchEnumerator,
                                SongSearchItems,
                                nameof(SongSearchItems),
                                _songSearchLock,
                                () => _songSearchFlag = false,
                                token);
        }

        public void SearchSongsOnEnter()
        {
            if (!SongSearchTerm.HasText() || _currentSongSearchTerm == SongSearchTerm) /* Then */ return;

            if (SelectedAlbum.IsUnbound) /* Then */ lock (_songLock)
            {
                GetSongBatchAsync().ConfigureAwait(false);
                return;
            }

            SongSearchItems = SongSearchItems.OrderBy(s => s.Name.ToLower().Similarity(SongSearchTerm))
                                             .ToObservable();
        }

        public void RemoveFile(SongFileItem item)
        {
            _fileManager.DeleteMusicFile(item.Song.Id, item.Song.Name, CurrentGame);
            lock (_downloadLock)
            {
                Files.Remove(item);
                OnPropertyChanged(nameof(Files));
                if (Files.Count is 0)
                {
                    _removedGames.Add(CurrentGame);
                    _downloadedGames.Remove(CurrentGame);
                }
            }

        }

        public void Preview(BaseItem item)
        {
            switch (item)
            {
                case null: return;
                case SongFile _: _fileManager.OpenGameDirectories(new List<Game> { CurrentGame }); return;
            }

            using (var webView = _webViewFactory.CreateView(1280, 750))
            {
                var downloadItem = item as DownloadItem;
                // Age restricted videos can only be seen in the full version while logged in
                // Need to redirect to the full YouTube site to view them
                if (downloadItem.Source is Source.Youtube)
                {
                    webView.LoadingChanged += (_, __) =>
                    {
                        if (!webView.GetCurrentAddress().StartsWith("https://www.youtube.com/embed/")) /* Then */ return;

                        var source = webView.GetPageSource();
                        if (source.Contains("<div class=\"ytp-error-content-wrap\"><div class=\"ytp-error-content-wrap-reason\">"))
                        {
                            webView.Navigate($"https://www.youtube.com/watch?v={downloadItem.Id}");
                        }
                    };
                }

                var url = _downloadManager.GetItemUrl(downloadItem);
                var html = $@"
                    <head>
                        <title>Preview</title>
                        <meta http-equiv='refresh' content='0; url={url}'>
                    </head>
                    <body style='margin:0'>
                    </body>";

                webView.Navigate("data:text/html," + html);
                webView.OpenDialog();
            }
        }

        public void StreamMusic(Song song)
        {
            if (song.Stream is null)
            {
                if (_taskTypesToTokens.TryGetValue(TaskType.StreamSong, out var tokenSource))
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                    _taskTypesToTokens.Remove(TaskType.StreamSong);
                }

                tokenSource = new CancellationTokenSource();
                _taskTypesToTokens[TaskType.StreamSong] = tokenSource;
                StreamMusicAsync(song, tokenSource.Token).ConfigureAwait(false);
            }
            else /* Then */ Play(song, song.Stream, _musicPlayer.Play);
        }

        public async ValueTask StreamMusicAsync(Song song, CancellationToken token)
        {
            if (song.Stream is null) /* Then */ await song.GetStreamAsync(token);
            if (token.IsCancellationRequested || song.Stream is null) /* Then */ return;
            Play(song, song.Stream, _musicPlayer.Play);
        }

        #endregion

        #region Helpers

        private void GetNextGame()
        {
            CurrentGame = _games[++_gameIndex];
            SearchOnEnter();
        }

        private void StopMusic()
        {
            if (string.IsNullOrWhiteSpace(PlayingMusicName)) /* Then */ return;
            _musicPlayer.Stop();
            PlayingMusicName = string.Empty;
            PlayingMusicLength = 0;
        }

        private void CancelTasks()
        {
            _songTasks.ForEach(t => t.Value.Cancel());
            _taskTypesToTokens.ForEach(t => t.Value.Cancel());
            _songSearchCancellationTokenSource.Cancel();
        }

        private async IAsyncEnumerable<IEnumerable<T>> CreateBatchEnumerableAsync<T>(
            IAsyncEnumerable<T> items, [EnumeratorCancellation] CancellationToken token)
        {
            var enumerator = items.GetAsyncEnumerator(token);
            var hasItems = true;
            do
            {
                var batch = new List<T>();
                for (var i = 0; i < 10; i++)
                {
                    hasItems = await enumerator.MoveNextAsync(token);
                    if (!hasItems) /* Then */ break;
                    batch.Add(enumerator.Current);
                }
                yield return batch;
            } while (hasItems);
        }

        private async Task GetBatchAsync<T>(
            IAsyncEnumerator<IEnumerable<T>> itemsEnumerator, IList<T> list, string propertyName, object lockObj, Action flagAction, CancellationToken token)
        {
            if (await _albumEnumerator.MoveNextAsync(token)) /* Then */ await Dispatcher.InvokeAsync(() =>
            {
                lock (lockObj)
                {
                    foreach (var item in itemsEnumerator.Current) /* Then */ list.Add(item);
                    OnPropertyChanged(propertyName);
                }
            });

            lock (lockObj) /* Then */ flagAction();
        }

        private string PositionToString(long position)
        {
            const int hour = 3600;
            const int tenMinutes = 600;

            if (_musicPlayer.LengthInSeconds is 0) /* Then */ return null;

            var format = _musicPlayer.LengthInSeconds > hour       ? @"h\:mm\:ss" :
                         _musicPlayer.LengthInSeconds > tenMinutes ? @"mm\:ss"
                                                                   : @"m\:ss";
            return TimeSpan.FromSeconds(position).ToString(format);
        }

        #endregion
    }
}
