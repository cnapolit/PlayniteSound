using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteSounds.Common;
using PlayniteSounds.Common.Constants;
using PlayniteSounds.Files.Download;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Files;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PlayniteSounds.Views.Models
{

    public class DownloadPromptModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public IDownloadManager DownloadManager { get; set; }
        public IFileManager FileManager { get; set; }

        public Window Window { get; set; }

        public IEnumerator<Game> Games { get; set; }

        public Source Source { get; set; } = Source.All;

        private IEnumerable<DownloadItem> searchItems;
        public IEnumerable<DownloadItem> SearchItems
        {
            get => searchItems;
            set
            {
                searchItems = value;
                OnPropertyChanged();
            }
        }

        public bool PromptForAlbum { get; set; } = true;
        public bool PromptForSong { get; set; } = true;

        private bool searchSong;

        private bool isItemSelected;
        public bool IsItemSelected
        {
            get => isItemSelected;
            set
            {
                isItemSelected = value;
                OnPropertyChanged();
            }
        }

        private DownloadItem selectedItem;
        public DownloadItem SelectedItem
        {
            get => selectedItem;
            set
            {
                selectedItem = value;
                if (selectedItem != null)
                {
                    IsItemSelected = true;
                }
                else
                {
                    IsItemSelected = false;
                }
                OnPropertyChanged();
            }
        }

        private string searchTerm;
        public string SearchTerm
        {
            get => searchTerm;
            set
            {
                searchTerm = value;
                OnPropertyChanged();
            }
        }

        public int DownloadTotal { get; set; }

        private int downloadCount = 0;
        public int DownloadCount
        {
            get => downloadCount;
            set
            {
                downloadCount = value;
            }
        }

        private string downloadStatus;
        public string DownloadStatus
        {
            get => downloadStatus;
            set
            {
                downloadStatus = value;
                OnPropertyChanged();
            }
        }

        public IPlayniteAPI PlayniteAPI { get; set; }

        public RelayCommand<object> SearchCommand => new RelayCommand<object>(a =>
        {
            if (searchSong)
            {
                CreateDownloadProgress(
                    "Fetching Songs...",
                    () => SearchItems = SearchItems.OrderByDescending(s => s.Name.StartsWith(SearchTerm)));
                return;
            }

            CreateDownloadProgress(
                "Fetching Albums...",
                () => SearchItems = DownloadManager.GetAlbumsForGame(Games.Current.Name, Source));
        });

        public RelayCommand<object> SelectCommand => new RelayCommand<object>(_ => SelectItem());

        public void DoubleClickSelect(object _, RoutedEventArgs __) => SelectItem();
        
        private void SelectItem()
        {
            if (SelectedItem is Song song)
            {
                DownloadSong(song);
                return;
            }

            GetSongs(SelectedItem as Album);
        }

        public RelayCommand<object> PreviewCommand => new RelayCommand<object>(a => 
        {
            var url = DownloadManager.GetItemUrl(selectedItem);
            var html = string.Format(@"
                    <head>
                        <title>Extra Metadata</title>
                        <meta http-equiv='refresh' content='0; url={0}'>
                    </head>
                    <body style='margin:0'>
                    </body>", url);
            var webView = PlayniteAPI.WebViews.CreateView(1280, 750);

            // Age restricted videos can only be seen in the full version while logged in
            // so it's needed to redirect to the full YouTube site to view them
            var embedLoaded = false;
            webView.LoadingChanged += async (s, e) =>
            {
                if (!embedLoaded)
                {
                    if (webView.GetCurrentAddress().StartsWith(@"https://www.youtube.com/embed/"))
                    {
                        var source = await webView.GetPageSourceAsync();
                        if (source.Contains("<div class=\"ytp-error-content-wrap\"><div class=\"ytp-error-content-wrap-reason\">"))
                        {
                            webView.Navigate($"https://www.youtube.com/watch?v={selectedItem.Id}");
                        }
                        embedLoaded = true;
                    }
                }
            };

            webView.Navigate("data:text/html," + html);
            webView.OpenDialog();
            webView.Dispose();
        });

        public RelayCommand<object> ReturnCommand => new RelayCommand<object>(a => 
        {
            var gameName = StringUtilities.StripStrings(Games.Current.Name);
            DownloadStatus = $"{App.AppName} - {Resource.DialogMessageDownloadingFiles}\n\n{downloadCount}/{DownloadTotal}\n{gameName}";
            GetAlbums(gameName);
        });

        public RelayCommand<object> SkipCommand => new RelayCommand<object>(_ => GetNextGame());

        public DownloadPromptModel(IPlayniteAPI api, IDownloadManager downloadManager, IFileManager fileManager)
        {
            PlayniteAPI = api;
            DownloadManager = downloadManager;
            FileManager = fileManager;
            Games = api.SelectedGames().GetEnumerator();
            GetNextGame();
        }

        private void GetNextGame()
        {
            if (!Games.MoveNext())
            {
                Window.Close();
                return;
            }

            var gameName = StringUtilities.StripStrings(Games.Current.Name);
            DownloadCount++;
            DownloadStatus = $"{App.AppName} - {Resource.DialogMessageDownloadingFiles}\n\n{downloadCount}/{DownloadTotal}\n{gameName}";

            if (PromptForAlbum)
            {
                GetAlbums(gameName);
                return;
            }

            var albums = DownloadManager.GetAlbumsForGame(gameName, Source);
            GetSongs(DownloadManager.BestAlbumPick(albums, gameName, StringUtilities.Sanitize(gameName)));
        }

        private void GetAlbums(string gameName)
        {
            searchSong = false;
            CreateDownloadProgress(
                $"Fetching Albums for '{gameName}...",
                () => DownloadManager.GetAlbumsForGame(gameName, Source));
        }

        private void GetSongs(Album album)
        {
            CreateDownloadProgress(
                $"Fetching Songs for album '{album.Name}' from '{album.Source}'...",
                () => DownloadManager.GetSongsFromAlbum(album));

            if (PromptForSong)
            {
                searchSong = true;
                DownloadStatus += $" - {album.Name} Songs";
                return;
            }

            var song = DownloadManager.BestSongPick(
                SearchItems as IEnumerable<Song>, StringUtilities.Sanitize(Games.Current.Name));
            DownloadSong(song);
        }

        private void DownloadSong(Song song)
        {
            bool result;

            var gameDirectory = FileManager.CreateMusicDirectory(Games.Current);
            var gameName = StringUtilities.Sanitize(Games.Current.Name);
            var newFilePath = Path.Combine(gameDirectory, gameName);
            CreateDownloadProgress(
                $"Downloading song '{song.Name}'...",
                () => result = DownloadManager.DownloadSong(song, newFilePath));
            GetNextGame();
        }

        private void CreateDownloadProgress(string caption, Func<IEnumerable<DownloadItem>> downloadFunc)
        {
            PlayniteAPI.Dialogs.ActivateGlobalProgress(
                _ => SearchItems = downloadFunc().ToList(),
                new GlobalProgressOptions(caption, true) { IsIndeterminate = false });
        }

        private void CreateDownloadProgress(string caption, Action downloadFunc) 
            => PlayniteAPI.Dialogs.ActivateGlobalProgress(
                _ => downloadFunc(),
                new GlobalProgressOptions(caption, true) { IsIndeterminate = false });

        public RelayCommand<object> CancelCommand => new RelayCommand<object>(_ => Window.Close());
    }
}
