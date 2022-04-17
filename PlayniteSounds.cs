﻿using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Media;
using Playnite.SDK.Events;
using System.Windows.Media.Animation;
using System.Diagnostics;
using Microsoft.Win32;
using System.Windows;
using System.IO.Compression;
using System.Threading;
using System.Net.Http;
using HtmlAgilityPack;

namespace PlayniteSounds
{
    public class PlayniteSounds : GenericPlugin
    {
        private static readonly IResourceProvider resources = new ResourceProvider();
        private static readonly ILogger logger = LogManager.GetLogger();
        public bool MusicNeedsReload { get; set; } = false;
        private PlayniteSoundsSettingsViewModel Settings { get; set; }
        private string prevmusicfilename = "";
        private MediaPlayer musicplayer; 
        private readonly MediaTimeline timeLine;
        private static readonly HttpClient httpClient = new HttpClient();

        public static string pluginFolder;

        public override Guid Id { get; } = Guid.Parse("9c960604-b8bc-4407-a4e4-e291c6097c7d");

        private Dictionary<string, PlayerEntry> players = new Dictionary<string, PlayerEntry>();
        private bool closeaudiofilesnextplay = false;
        private bool gamerunning = false;
        private bool firstselectsound = true;

        private const string KHInsiderBaseUrl = @"https://downloads.khinsider.com/";

        protected virtual bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }

        public PlayniteSounds(IPlayniteAPI api) : base(api)
        {
            try
            {
                Settings = new PlayniteSoundsSettingsViewModel(this);
                Properties = new GenericPluginProperties
                {
                    HasSettings = true
                };

                pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                Localization.SetPluginLanguage(pluginFolder, api.ApplicationSettings.Language);
                musicplayer = new MediaPlayer();
                timeLine = new MediaTimeline
                {
                    RepeatBehavior = RepeatBehavior.Forever
                };
            }
            catch (Exception E)
            {
                logger.Error(E, "PlayniteSounds");
                PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
            }
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            // Add code to be executed when game is finished installing.
            PlayFileName("GameInstalled.wav");
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            // Add code to be executed when game is started running.
            if (Settings.Settings.StopMusic == 1)
            {
                PauseMusic();
                gamerunning = true;
            }
            PlayFileName("GameStarted.wav", true);
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
            if (Settings.Settings.StopMusic == 0)
            {
                PauseMusic();
                gamerunning = true;
            }
            PlayFileName("GameStarting.wav");
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            gamerunning = false;
            // Add code to be executed when game is preparing to be started.
            PlayFileName("GameStopped.wav");
            ResumeMusic();
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            // Add code to be executed when game is uninstalled.
            PlayFileName("GameUninstalled.wav");
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // Add code to be executed when Playnite is initialized.
            PlayFileName("ApplicationStarted.wav");
            SystemEvents.PowerModeChanged += OnPowerMode_Changed;
            Application.Current.Deactivated += onApplicationDeactivate;
            Application.Current.Activated += onApplicationActivate;
            Application.Current.MainWindow.StateChanged += onWindowStateChanged;
        }

        private void onWindowStateChanged(object sender, EventArgs e)
        {
            if (Settings.Settings.PauseOnDeactivate)
            {
                switch (Application.Current?.MainWindow?.WindowState)
                {
                    case WindowState.Maximized:
                        ResumeMusic();
                        break;
                    case WindowState.Minimized:
                        PauseMusic();
                        break;
                    case WindowState.Normal:
                        ResumeMusic();
                        break;
                }
            }
        }

        public void onApplicationDeactivate(object sender, EventArgs e)
        {
            if (Settings.Settings.PauseOnDeactivate)
            {
                PauseMusic();
            }
        }

        public void onApplicationActivate(object sender, EventArgs e)
        {
            if (Settings.Settings.PauseOnDeactivate)
            {
                ResumeMusic();
            }
        }

        //fix sounds not playing after system resume
        public void OnPowerMode_Changed(object sender, PowerModeChangedEventArgs e)
        {
            try
            { 
                if (e.Mode == PowerModes.Resume)
                {
                    closeaudiofilesnextplay = true;
                    MusicNeedsReload = true;
                    //Restart music:
                    ReplayMusic();
                }
            }
            catch (Exception E)
            {
                logger.Error(E, "OnPowerMode_Changed");
                PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
            }
        }

        public void ResetMusicVolume()
        {
            musicplayer.Volume = (double)Settings.Settings.MusicVolume / 100;
        }

        public void ReplayMusic()
        {
            if (gamerunning)
            {
                return;
            }
            
            if (PlayniteApi.MainView.SelectedGames.Count() == 1)
            {
                foreach (Game game in PlayniteApi.MainView.SelectedGames)
                {
                    Platform platform = game.Platforms.FirstOrDefault(o => o != null);
                    if (Settings.Settings.MusicType == 2)
                    {
                        PlayMusic(game.Name, platform == null ? "No Platform" : platform.Name);
                    }
                    else
                    {
                        if (Settings.Settings.MusicType == 1)
                        {
                            PlayMusic("_music_", platform == null ? "No Platform" : platform.Name);
                        }
                        else
                        {
                            PlayMusic("_music_", "");
                        }
                    }
                }
            }
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
            SystemEvents.PowerModeChanged -= OnPowerMode_Changed;
            Application.Current.Deactivated -= onApplicationDeactivate;
            Application.Current.Activated -= onApplicationActivate;
            Application.Current.MainWindow.StateChanged -= onWindowStateChanged;
            PlayFileName("ApplicationStopped.wav", true);
            CloseAudioFiles();
            CloseMusic();
            musicplayer = null;
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            // Add code to be executed when library is updated.
            PlayFileName("LibraryUpdated.wav");
        }

        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {
            if (firstselectsound)
            {
                firstselectsound = false;
                if (!Settings.Settings.SkipFirstSelectSound)
                {
                    PlayFileName("GameSelected.wav");
                }
            }
            else
            {
                PlayFileName("GameSelected.wav");
            }

            if (args.NewValue.Count == 1) 
            {
                foreach(Game game in args.NewValue)
                {
                    Platform platform = game.Platforms.FirstOrDefault(o => o != null);
                    if (Settings.Settings.MusicType == 2)
                    {
                        PlayMusic(game.Name, platform == null ? "No Platform" : platform.Name);
                    }
                    else
                    {
                        if (Settings.Settings.MusicType == 1)
                        {
                            PlayMusic("_music_", platform == null ? "No Platform" : platform.Name);
                        }
                        else
                        {
                            PlayMusic("_music_", "");
                        }
                    }
                    
                }
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new PlayniteSoundsSettingsView(this);
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            List<GameMenuItem> MainMenuItems = new List<GameMenuItem>
            {
                new GameMenuItem {
                    MenuSection = "Playnite Sounds",
                    Icon = Path.Combine(pluginFolder, "icon.png"),
                    Description = resources.GetString("LOC_PLAYNITESOUNDS_ActionsShowMusicFilename"),
                    Action = (MainMenuItem) =>
                    {
                        ShowMusicFilename();
                    }
                },
                new GameMenuItem {
                    MenuSection = "Playnite Sounds",
                    Icon = Path.Combine(pluginFolder, "icon.png"),
                    Description = resources.GetString("LOC_PLAYNITESOUNDS_ActionsCopySelectMusicFile"),
                    Action = (MainMenuItem) =>
                    {
                        SelectMusicFilename();
                    }
                }
             };
            return MainMenuItems;
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            List<MainMenuItem> MainMenuItems = new List<MainMenuItem>
            {                
                new MainMenuItem {
                    MenuSection = "@Playnite Sounds",
                    Icon = Path.Combine(pluginFolder, "icon.png"),
                    Description = resources.GetString("LOC_PLAYNITESOUNDS_ActionsShowMusicFilename"),
                    Action = (MainMenuItem) =>
                    {
                        ShowMusicFilename();
                    }
                },
                new MainMenuItem {
                    MenuSection = "@Playnite Sounds",
                    Icon = Path.Combine(pluginFolder, "icon.png"),
                    Description = resources.GetString("LOC_PLAYNITESOUNDS_ActionsCopySelectMusicFile"),
                    Action = (MainMenuItem) =>
                    {
                        SelectMusicFilename();
                    }
                },
                new MainMenuItem {
                    MenuSection = "@Playnite Sounds",
                    Icon = Path.Combine(pluginFolder, "icon.png"),
                    Description = resources.GetString("LOC_PLAYNITESOUNDS_ActionsDownloadSelectMusicFile"),
                    Action = (MainMenuItem) =>
                    {
                        DownloadMusicFilename();
                    }
                },
                new MainMenuItem {
                    MenuSection = "@Playnite Sounds",
                    Icon = Path.Combine(pluginFolder, "icon.png"),
                    Description = resources.GetString("LOC_PLAYNITESOUNDS_ActionsOpenMusicFolder"),
                    Action = (MainMenuItem) =>
                    {
                        OpenMusicFolder();
                    }
                },
                new MainMenuItem {
                    MenuSection = "@Playnite Sounds",
                    Icon = Path.Combine(pluginFolder, "icon.png"),
                    Description = resources.GetString("LOC_PLAYNITESOUNDS_ActionsOpenSoundsFolder"),
                    Action = (MainMenuItem) =>
                    {
                        OpenSoundsFolder();
                    }
                },
                new MainMenuItem {
                    MenuSection = "@Playnite Sounds",
                    Icon = Path.Combine(pluginFolder, "icon.png"),
                    Description = resources.GetString("LOC_PLAYNITESOUNDS_ActionsReloadAudioFiles"),
                    Action = (MainMenuItem) =>
                    {
                        ReloadAudioFiles();
                    }
                },
                new MainMenuItem {
                    MenuSection = "@Playnite Sounds",
                    Icon = Path.Combine(pluginFolder, "icon.png"),
                    Description = resources.GetString("LOC_PLAYNITESOUNDS_ActionsHelp"),
                    Action = (MainMenuItem) =>
                    {
                        HelpMenu();
                    }
                }
             };
            return MainMenuItems;
        }

        public void HelpMenu()
        {
            PlayniteApi.Dialogs.ShowMessage(resources.GetString("LOC_PLAYNITESOUNDS_MsgHelp1") + "\n\n" +
                resources.GetString("LOC_PLAYNITESOUNDS_MsgHelp2") + "\n\n" +
                resources.GetString("LOC_PLAYNITESOUNDS_MsgHelp3") + " " +
                resources.GetString("LOC_PLAYNITESOUNDS_MsgHelp4") + " " +
                resources.GetString("LOC_PLAYNITESOUNDS_MsgHelp5") + "\n\n" +
                resources.GetString("LOC_PLAYNITESOUNDS_MsgHelp6") + "\n\n" +
                "D_ApplicationStarted.wav - F_ApplicationStarted.wav\n" +
                "D_ApplicationStopped.wav - F_ApplicationStopped.wav\n" +
                "D_GameInstalled.wav - F_GameInstalled.wav\n" +
                "D_GameSelected.wav - F_GameSelected.wav\n" +
                "D_GameStarted.wav - F_GameStarted.wav\n" +
                "D_GameStarting.wav - F_GameStarting.wav\n" +
                "D_GameStopped.wav - F_GameStopped.wav\n" +
                "D_GameUninstalled.wav - F_GameUninstalled.wav\n" +
                "D_LibraryUpdated.wav - F_LibraryUpdated.wav\n\n" +
                resources.GetString("LOC_PLAYNITESOUNDS_MsgHelp7"), Constants.AppName);
        }

        public string GetMusicFilename(string gamename, string platform)
        {
            try
            { 
                string musisdir = Path.Combine(GetPluginUserDataPath(), "Music Files", platform);
                Directory.CreateDirectory(musisdir);
                string invalidChars = new string(Path.GetInvalidFileNameChars());
                Regex r = new Regex(string.Format("[{0}]", Regex.Escape(invalidChars)));
                string sanitizedgamename = r.Replace(gamename, "") + ".mp3";
                return Path.Combine(musisdir, sanitizedgamename);
            }
            catch (Exception E)
            {
                logger.Error(E, "GetMusicFilename");
                PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
                return "";
            }
        }

        public void SelectMusicFilename()
        {

            foreach (Game game in PlayniteApi.MainView.SelectedGames)
            {
                string MusicFileName = GetMusicFileNameFromPlatform(game);
                string NewMusicFileName = PlayniteApi.Dialogs.SelectFile("MP3 File|*.mp3");
                File.Copy(NewMusicFileName, MusicFileName, true);
            }

            MusicNeedsReload = true;
            CloseMusic();
            ReplayMusic();
        }

        public void DownloadMusicFilename()
        {
            var albumSelect = PromptForAlbumSelect();
            var songSelect = PromptForSongSelect();

            foreach (Game game in PlayniteApi.MainView.SelectedGames)
            {

                var albumsToPartialUrls = GetAlbumsForGame(game.Name);
                if (!albumsToPartialUrls.Any())
                {
                    logger.Warn($"Did not find any albums for game '{game.Name}'");
                }

                var albumToPartialUrl = albumSelect 
                    ? PromptForAlbum(albumsToPartialUrls) 
                    : albumsToPartialUrls.FirstOrDefault();

                var songsToPartialUrls = GetSongsFromAlbum(albumToPartialUrl);
                if (!songsToPartialUrls.Any())
                {
                    logger.Warn($"Did not find any songs for album '{albumToPartialUrl.Key}'");
                }

                var songToPartialUrl = songSelect
                    ? PromptForSongUrl(songsToPartialUrls)
                    : songsToPartialUrls.FirstOrDefault();

                var platform = game.Platforms.FirstOrDefault(o => o != null);
                var MusicFileName = GetMusicFilename(game.Name, platform == null ? "No Platform" : platform.Name);

                DownloadSongFromUrlToPath(songToPartialUrl, MusicFileName);

            }

            MusicNeedsReload = true;
            CloseMusic();
            ReplayMusic();
        }

        private bool PromptForAlbumSelect()
        {
            return true;
        }

        private bool PromptForSongSelect()
        {
            return true;
        }

        private static List<KeyValuePair<string, string>> GetAlbumsForGame(string gameName)
        {
            var web = new HtmlWeb();

            var htmlDoc = web.Load($"{KHInsiderBaseUrl}search?{gameName}");
            
            var tableRows = htmlDoc.DocumentNode.Descendants("tr");
            var albumsToPartialUrls = new List<KeyValuePair<string, string>>();
            foreach (var row in tableRows)
            {
                var titleField = row.Descendants("td").Skip(1).FirstOrDefault();
                if (titleField == null)
                {
                    logger.Info($"Found album entry of game '{gameName}' without title field");
                    continue;
                }

                var htmlLink = titleField.Descendants("a").FirstOrDefault();
                if (htmlLink == null)
                {
                    logger.Info($"Found entry for album entry of game '{gameName}' without title");
                    continue;
                }

                var albumName = htmlLink.InnerHtml;
                var albumPartialLink = htmlLink.GetAttributeValue("href", null);
                if (albumPartialLink == null)
                {
                    logger.Info($"Found entry for album '{albumName}' of game '{gameName}' without link in title");
                    continue;
                }

                albumsToPartialUrls.Add(new KeyValuePair<string, string>(albumName, albumPartialLink));
            }

            return albumsToPartialUrls;
        }

        private static KeyValuePair<string, string> PromptForAlbum(IEnumerable<KeyValuePair<string, string>> albumsToPartialUrls)
        {
            return albumsToPartialUrls.FirstOrDefault();
        }

        private static List<KeyValuePair<string, string>> GetSongsFromAlbum(KeyValuePair<string, string> albumtoPartialUrl)
        {
            var songsToPartialUrls = new List<KeyValuePair<string, string>>();

            var web = new HtmlWeb();

            var htmlDoc = web.Load($"{KHInsiderBaseUrl}{albumtoPartialUrl.Value}");

            // Validate Html
            var headerRow = htmlDoc.GetElementbyId("songlist_header");
            var headers = headerRow.Descendants("th").Select(n => n.InnerHtml);
            if (headers.All(h => !h.Contains("MP3")))
            {
                logger.Warn($"No mp3 in album '{albumtoPartialUrl.Key}'");
                return songsToPartialUrls;
            }

            var table = htmlDoc.GetElementbyId("songlist");

            // Get table and skip header
            var tableRows = table.Descendants("tr").Skip(1).ToList();
            if (tableRows.Count < 2)
            {
                // Throw no songs in album
                logger.Warn($"No songs in album '{albumtoPartialUrl.Key}'");
                return songsToPartialUrls;
            }

            // Remove footer
            tableRows.RemoveAt(tableRows.Count - 1);

            foreach(var row in tableRows)
            {
                var songNameEntry = row.Descendants("a").Select(
                    r => new KeyValuePair<string, string>(r.InnerHtml, r.GetAttributeValue("href", null)))
                    .FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.Value));

                songsToPartialUrls.Add(songNameEntry);
            }

            return songsToPartialUrls;
        }

        private static KeyValuePair<string, string> PromptForSongUrl(List<KeyValuePair<string, string>> songs)
        {
            return songs.FirstOrDefault();
        }

        private static void DownloadSongFromUrlToPath(KeyValuePair<string, string> songToPartialUrl, string path)
        {
            // Get Url to file from Song html page
            var web = new HtmlWeb();
            var htmlDoc = web.Load($"{KHInsiderBaseUrl}{songToPartialUrl.Value}");

            var fileUrl = htmlDoc.GetElementbyId("audio").GetAttributeValue("href", null);
            if (fileUrl == null)
            {
                logger.Warn($"Did not find file url for song '{songToPartialUrl.Key}'");
            }

            HttpResponseMessage httpMesage = httpClient.GetAsync(fileUrl).Result;
            using (FileStream fs = File.Create(path))
            {
                httpMesage.Content.CopyToAsync(fs).Wait();
            }
        }

        private string GetMusicFileNameFromPlatform(Game game)
        {
            string musicFileName;
            Platform platform = game.Platforms.FirstOrDefault(o => o != null);

            switch (Settings.Settings.MusicType)
            {
                case 1:
                    musicFileName = GetMusicFilename("_music_", platform == null ? "No Platform" : platform.Name);
                    break;
                case 2:
                    musicFileName = GetMusicFilename(game.Name, platform == null ? "No Platform" : platform.Name);
                    break;
                default:
                    musicFileName = GetMusicFilename("_music_", "");
                    break;

            };

            return musicFileName;
        }

        public void ShowMusicFilename()
        {
            if (PlayniteApi.MainView.SelectedGames.Count() == 1)
            {
                foreach (Game game in PlayniteApi.MainView.SelectedGames)
                {
                    string MusicFileName;
                    Platform platform = game.Platforms.FirstOrDefault(o => o != null);
                    if (Settings.Settings.MusicType == 2)
                    {
                        MusicFileName = GetMusicFilename(game.Name, platform == null ? "No Platform" : platform.Name);
                    }
                    else
                    {
                        if (Settings.Settings.MusicType == 1)
                        {
                            MusicFileName = GetMusicFilename("_music_", platform == null ? "No Platform" : platform.Name);
                        }
                        else
                        {
                            MusicFileName = GetMusicFilename("_music_", "");
                        }
                    }
                    PlayniteApi.Dialogs.ShowMessage(MusicFileName, Constants.AppName);
                }
            }
            else
            {
                PlayniteApi.Dialogs.ShowMessage(resources.GetString("LOC_PLAYNITESOUNDS_MsgSelectSingleGame"), Constants.AppName);
            }
        }

        public void PlayFileName(string FileName, bool UseSoundPlayer = false)
        {
            try
            { 
                InitialCopyAudioFiles();

                if (closeaudiofilesnextplay)
                {
                    CloseAudioFiles();
                    closeaudiofilesnextplay = false;
                }

                bool DesktopMode = PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop;
                bool DoPlay = (DesktopMode && ((Settings.Settings.SoundWhere == 1) || (Settings.Settings.SoundWhere == 3))) ||
                    (!DesktopMode && ((Settings.Settings.SoundWhere == 2) || (Settings.Settings.SoundWhere == 3)));

                if (DoPlay)
                {
                    PlayerEntry Entry;
                    if (players.ContainsKey(FileName))
                    {
                        Entry = players[FileName];
                    }
                    else
                    {
                        string Prefix = PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop ? "D_" : "F_";

                        string FullFileName = Path.Combine(GetPluginUserDataPath(), "Sound Files", Prefix + FileName);

                        //MediaPlayer can play multiple sounds together from mulitple instances SoundPlayer can not
                        if (UseSoundPlayer)
                        {
                            Entry = new PlayerEntry(File.Exists(FullFileName), null, new SoundPlayer(), 0);
                        }
                        else
                        {
                            Entry = new PlayerEntry(File.Exists(FullFileName), new MediaPlayer(), null, 1);
                        }

                        if (Entry.FileExists)
                        {
                            if (Entry.TypePlayer == 1)
                            {
                                Entry.MediaPlayer.Open(new Uri(FullFileName));
                            }
                            else
                            {
                                Entry.SoundPlayer.SoundLocation = FullFileName;
                                Entry.SoundPlayer.Load();
                            }
                        }
                        players[FileName] = Entry;
                    }

                    if (Entry.FileExists)
                    {
                        if (Entry.TypePlayer == 1)
                        {
                            Entry.MediaPlayer.Stop();
                            Entry.MediaPlayer.Play();
                        }
                        else
                        {
                            Entry.SoundPlayer.Stop();
                            Entry.SoundPlayer.PlaySync();
                        }
                    }
                }
            }
            catch (Exception E)
            {
                logger.Error(E, "PlayFileName");
                PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
            }
        }


        public void CloseAudioFiles()
        {
            try
            {
                foreach (string keyname in players.Keys)
                {
                    PlayerEntry Entry = players[keyname];
                    if (Entry.FileExists)
                    {
                        if (Entry.TypePlayer == 1)
                        {
                            string filename = "";
                            if (Entry.MediaPlayer.Source != null)
                            {
                                filename = Entry.MediaPlayer.Source.LocalPath;
                            }
                            Entry.MediaPlayer.Stop();
                            Entry.MediaPlayer.Close();
                            Entry.MediaPlayer = null;
                            if (File.Exists(filename))
                            {
                                int count = 0;
                                while (IsFileLocked(new FileInfo(filename)))
                                {
                                    Thread.Sleep(5);
                                    count += 5;
                                    if (count > 500)
                                        break;
                                }
                            }
                        }
                        else
                        {
                            Entry.SoundPlayer.Stop();
                            Entry.SoundPlayer = null;
                        }
                    }
                }
                players.Clear();
            }
            catch (Exception E)
            {
                logger.Error(E, "CloseAudioFiles");
                PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
            }
        }

        public void ReloadAudioFiles()
        {
            CloseAudioFiles();
            PlayniteApi.Dialogs.ShowMessage(resources.GetString("LOC_PLAYNITESOUNDS_MsgAudioFilesReloaded"), Constants.AppName);
        }

        public void InitialCopyAudioFiles()
        {
            try
            { 
                string SoundFilesInstallPath = Path.Combine(pluginFolder, "Sound Files");
                string SoundFilesDataPath = Path.Combine(GetPluginUserDataPath(), "Sound Files");

                if (!Directory.Exists(SoundFilesDataPath))
                {
                    if (Directory.Exists(SoundFilesInstallPath))
                    {
                        CloseAudioFiles();

                        Directory.CreateDirectory(SoundFilesDataPath);
                        string[] files = Directory.GetFiles(SoundFilesInstallPath);
                        foreach (string file in files)
                        {                        
                            string DestPath = Path.Combine(SoundFilesDataPath, Path.GetFileName(file));
                            File.Copy(file, DestPath, true);
                        }
                    }
                }
            }
            catch (Exception E)
            {
                logger.Error(E, "InitialCopyAudioFiles");
                PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
            }
        }

        public void ResumeMusic()
        {
            try
            {
                if (gamerunning)
                {
                    return;
                }

                if (musicplayer.Clock != null)
                {
                    musicplayer.Clock.Controller.Resume();
                }
            }
            catch (Exception E)
            {
                logger.Error(E, "ResumeMusic");
                PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
            }
        }

        public void PauseMusic()
        {
            try
            { 
                if (gamerunning)
                {
                    return;
                }

                if (musicplayer.Clock != null)
                {
                    musicplayer.Clock.Controller.Pause();
                }
            }
            catch (Exception E)
            {
                logger.Error(E, "PauseMusic");
                PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
            }
        }

        public void  CloseMusic()
        {
            try
            {
                if (musicplayer.Clock != null)
                {
                    musicplayer.Clock.Controller.Stop();
                    musicplayer.Clock = null;
                    musicplayer.Close();
                }
            }
            catch (Exception E)
            {
                logger.Error(E, "CloseMusic");
                PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
            }
}

        public void PlayMusic(string gamename, string platform)
        {
            try
            { 
                bool DesktopMode = PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop;
                bool DoPlay = (!gamerunning) && ((DesktopMode && ((Settings.Settings.MusicWhere == 1) || (Settings.Settings.MusicWhere == 3))) ||
                    (!DesktopMode && ((Settings.Settings.MusicWhere == 2) || (Settings.Settings.MusicWhere == 3))));

                if (DoPlay)
                {
                    string MusicFileName = GetMusicFilename(gamename, platform);
                    if (MusicNeedsReload || (MusicFileName != prevmusicfilename))
                    {
                        CloseMusic();
                        MusicNeedsReload = false;
                        prevmusicfilename = "";
                        if (File.Exists(MusicFileName))
                        {
                            prevmusicfilename = MusicFileName;
                            timeLine.Source = new Uri(MusicFileName);
                            musicplayer.Volume = (double)Settings.Settings.MusicVolume / 100;
                            musicplayer.Clock = timeLine.CreateClock();
                            musicplayer.Clock.Controller.Begin();
                            
                        }
                    }
                }
                else 
                { 
                    CloseMusic();
                }
            }
            catch (Exception E)
            {
                logger.Error(E, "PlayMusic");
                PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
            }
        }

        public void OpenSoundsFolder()
        {
            try
            { 
                //need to release them otherwise explorer can't overwrite files even though you can delete them
                CloseAudioFiles();
                string SoundFilesDataPath = Path.Combine(GetPluginUserDataPath(), "Sound Files");
                // just in case user deleted it
                Directory.CreateDirectory(SoundFilesDataPath);
                Process.Start(SoundFilesDataPath);
            }
            catch (Exception E)
            {
                logger.Error(E, "OpenSoundsFolder");
                PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
            }
        }

        public void OpenMusicFolder()
        {
            try
            {
                //need to release them otherwise explorer can't overwrite files even though you can delete them
                CloseMusic();
                string SoundFilesDataPath = Path.Combine(GetPluginUserDataPath(), "Music Files");
                //just in case user deleted it
                Directory.CreateDirectory(SoundFilesDataPath);
                Process.Start(SoundFilesDataPath);
                MusicNeedsReload = true;
            }
            catch (Exception E)
            {
                logger.Error(E, "OpenMusicFolder");
                PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
            }
        }

        public void SaveSounds()
        {
            Window windowExtension = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false,
                ShowMaximizeButton = false,
                ShowCloseButton = true
            });

            windowExtension.ShowInTaskbar = false;
            windowExtension.ResizeMode = ResizeMode.NoResize;
            windowExtension.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            windowExtension.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            TextBox saveNameBox = new TextBox
            {
                Margin = new Thickness(5, 5, 10, 5),
                Width = 200
            };
            stackPanel.Children.Add(saveNameBox);

            Button saveNameButton = new Button
            {
                Margin = new Thickness(0, 5, 5, 5)
            };
            saveNameButton.SetResourceReference(Button.ContentProperty, "LOC_PLAYNITESOUNDS_ManagerSave");
            saveNameButton.IsEnabled = false;
            saveNameButton.IsDefault = true;
            stackPanel.Children.Add(saveNameButton);

            saveNameBox.KeyUp += (object sender, System.Windows.Input.KeyEventArgs e) =>
            {
                // Only allow saving if filename is larger than 3 characters
                saveNameButton.IsEnabled = saveNameBox.Text.Trim().Length > 3;
            };

            saveNameButton.Click += (object sender, RoutedEventArgs e) =>
            {
                // Create ZIP file in sound manager folder
                try
                {
                    string soundPackName = saveNameBox.Text;
                    string SoundFilesDataPath = Path.Combine(GetPluginUserDataPath(), "Sound Files");
                    //just in case user deleted it
                    Directory.CreateDirectory(SoundFilesDataPath);
                    string SoundManagerFilesDataPath = Path.Combine(GetPluginUserDataPath(), "Sound Manager");
                    //just in case user deleted it
                    Directory.CreateDirectory(SoundManagerFilesDataPath);
                    ZipFile.CreateFromDirectory(SoundFilesDataPath, Path.Combine(SoundManagerFilesDataPath, soundPackName + ".zip"));
                    PlayniteApi.Dialogs.ShowMessage(Application.Current.FindResource("LOC_PLAYNITESOUNDS_ManagerSaveConfirm").ToString() + " " + soundPackName);
                    windowExtension.Close();
                }
                catch (Exception E)
                {
                    logger.Error(E, "SaveSounds");
                    PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
                }
            };

            windowExtension.Content = stackPanel;
            windowExtension.SizeToContent = SizeToContent.WidthAndHeight;
            // Workaround for WPF bug which causes black sections to be displayed in the window
            windowExtension.ContentRendered += (s, e) => windowExtension.InvalidateMeasure();
            windowExtension.Loaded += (s, e) => saveNameBox.Focus();
            windowExtension.ShowDialog();
        }

        public void LoadSounds()
        {
            try
            {
                string SoundManagerFilesDataPath = Path.Combine(GetPluginUserDataPath(), "Sound Manager");
                //just in case user deleted it
                Directory.CreateDirectory(SoundManagerFilesDataPath);

                OpenFileDialog dialog = new OpenFileDialog
                {
                    Filter = "ZIP archive|*.zip",
                    InitialDirectory = SoundManagerFilesDataPath
                };
                bool? result = dialog.ShowDialog(PlayniteApi.Dialogs.GetCurrentAppWindow());
                if (result == true)
                {
                    CloseAudioFiles();
                    string targetPath = dialog.FileName;
                    string SoundFilesDataPath = Path.Combine(GetPluginUserDataPath(), "Sound Files");
                    //just in case user deleted it
                    Directory.CreateDirectory(SoundFilesDataPath);
                    // Have to extract each file one at a time to enabled overwrites
                    using (ZipArchive archive = ZipFile.OpenRead(targetPath))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            // If it's a directory, it doesn't have a "Name".
                            if (!String.IsNullOrEmpty(entry.Name))
                            {
                                string entryDestination = Path.GetFullPath(Path.Combine(SoundFilesDataPath, entry.Name));
                                entry.ExtractToFile(entryDestination, true);
                            }
                        }
                    }
                    PlayniteApi.Dialogs.ShowMessage(Application.Current.FindResource("LOC_PLAYNITESOUNDS_ManagerLoadConfirm").ToString() + " " + Path.GetFileNameWithoutExtension(targetPath));
                }
            }
            catch (Exception E)
            {
                logger.Error(E, "LoadSounds");
                PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
            }


        }

        public void ImportSounds()
        {
            List<string> targetPaths = PlayniteApi.Dialogs.SelectFiles("ZIP archive|*.zip");

            if (targetPaths.HasNonEmptyItems())
            {
                try
                {
                    string SoundManagerFilesDataPath = Path.Combine(GetPluginUserDataPath(), "Sound Manager");
                    //just in case user deleted it
                    Directory.CreateDirectory(SoundManagerFilesDataPath);
                    foreach (string targetPath in targetPaths)
                    {
                        //just in case user selects a file from the soundmanager location
                        if (! Path.GetDirectoryName(targetPath).Equals(SoundManagerFilesDataPath, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Copy(targetPath, Path.Combine(SoundManagerFilesDataPath, Path.GetFileName(targetPath)), true);
                        }
                    }
                }
                catch (Exception E)
                {
                    logger.Error(E, "ImportSounds");
                    PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
                }
            }
        }

        public void RemoveSounds()
        {
            try
            {
                string SoundManagerFilesDataPath = Path.Combine(GetPluginUserDataPath(), "Sound Manager");
                //just in case user deleted it
                Directory.CreateDirectory(SoundManagerFilesDataPath);

                OpenFileDialog dialog = new OpenFileDialog
                {
                    Filter = "ZIP archive|*.zip",
                    InitialDirectory = SoundManagerFilesDataPath
                };
                bool? result = dialog.ShowDialog(PlayniteApi.Dialogs.GetCurrentAppWindow());
                if (result == true)
                {
                    string targetPath = dialog.FileName;
                    File.Delete(targetPath);
                    PlayniteApi.Dialogs.ShowMessage(Application.Current.FindResource("LOC_PLAYNITESOUNDS_ManagerDeleteConfirm").ToString() + " " + Path.GetFileNameWithoutExtension(targetPath));
                }
            }
            catch (Exception E)
            {
                logger.Error(E, "RemoveSounds");
                PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
            }
        }

        public void OpenSoundManagerFolder()
        {
            try
            {
                string SoundManagerFilesDataPath = Path.Combine(GetPluginUserDataPath(), "Sound Manager");
                //just in case user deleted it
                Directory.CreateDirectory(SoundManagerFilesDataPath);
                Process.Start(SoundManagerFilesDataPath);
            }
            catch (Exception E)
            {
                logger.Error(E, "OpenSoundManagerFolder");
                PlayniteApi.Dialogs.ShowErrorMessage(E.Message, Constants.AppName);
            }
        }
    }
}