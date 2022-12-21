using PlayniteSounds.Common.Constants;
using PlayniteSounds.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Media;
using PlayniteSounds.Services.Files;

namespace PlayniteSounds.Services.Audio
{
    internal class SoundPlayer : BasePlayer, ISoundPlayer
    {
        #region Infrastructure

        private readonly IErrorHandler                   _errorHandler;
        private readonly IPathingService                 _pathingService;
        private          Dictionary<string, PlayerEntry> _players                  = new Dictionary<string, PlayerEntry>();
        private          bool                            _firstSelectSound         = true;

        public SoundPlayer(
            IErrorHandler errorHandler,
            IPathingService pathingService,
            PlayniteSoundsSettings settings,
            bool isDesktop) : base(settings, isDesktop)
        {
            _errorHandler = errorHandler;
            _pathingService = pathingService;
        }

        #endregion

        #region Implementation

        #region Close

        public void Close()
        {
            _players.Values.ForEach(p => _errorHandler.Try(() => CloseAudioFile(p)));
            _players = new Dictionary<string, PlayerEntry>();
        }

        private static void CloseAudioFile(PlayerEntry entry)
        {
            if (entry.MediaPlayer is null)
            {
                entry.SoundPlayer.Stop();
                entry.SoundPlayer = null;
            }
            else
            {
                entry.MediaPlayer.Stop();
                entry.MediaPlayer.Close();
                entry.MediaPlayer = null;

                var filename = entry.MediaPlayer?.Source.LocalPath;
                if (File.Exists(filename))
                {
                    var fileInfo = new FileInfo(filename);
                    for (var count = 0; IsFileLocked(fileInfo) && count < 100; count++)
                    {
                        Thread.Sleep(5);
                    }
                }
            }
        }

        private static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (var stream = file.Open(FileMode.Open, FileAccess.Write, FileShare.None))
                    stream.Close();

                //file is not locked
                return false;
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
        }

        #endregion

        #region PlayGameSelected

        public void PlayGameSelected()
        {
            if (!_firstSelectSound || !_settings.SkipFirstSelectSound)
            {
                PlaySound(SoundFile.GameSelectedSound);
            }
            _firstSelectSound = false;
        }

        #endregion

        #region PlayAppStarted

        public void PlayAppStarted(EventHandler mediaEndedHandler)
        {
            var playerEntry = CreatePlayerEntry(SoundFile.ApplicationStartedSound, false);
            if (playerEntry is null)
            {
                // Interpret missing file as 'instantly' ended
                mediaEndedHandler.Invoke(null, null);
            }
            else
            {
                playerEntry.MediaPlayer.MediaEnded += mediaEndedHandler;
                playerEntry.MediaPlayer.Play();
            }
        }

        #endregion

        public void PlayAppStopped() => PlaySound(SoundFile.ApplicationStoppedSound, true);

        public void PlayGameInstalled() => PlaySound(SoundFile.GameInstalledSound);

        public void PlayGameUnInstalled() => PlaySound(SoundFile.GameUninstalledSound);

        public void PlayGameStarting() => PlaySound(SoundFile.GameStartingSound);

        public void PlayGameStarted() => PlaySound(SoundFile.GameStartedSound, true);

        public void PlayGameStopped() => PlaySound(SoundFile.GameStoppedSound);

        public void PlayLibraryUpdated() => PlaySound(SoundFile.LibraryUpdatedSound);

        #region Helpers

        private void PlaySound(string fileName, bool useSoundPlayer = false)
            => _errorHandler.Try(() => AttemptPlay(fileName, useSoundPlayer));

        private void AttemptPlay(string fileName, bool useSoundPlayer)
        {
            _players.TryGetValue(fileName, out var entry);
            if (entry is null)
            {
                entry = CreatePlayerEntry(fileName, useSoundPlayer);
            }

            if (entry != null) /*Then*/
            if (useSoundPlayer)
            {
                entry.SoundPlayer.Stop();
                entry.SoundPlayer.PlaySync();
            }
            else
            {
                entry.MediaPlayer.Stop();
                entry.MediaPlayer.Play();
            }
        }

        private PlayerEntry CreatePlayerEntry(string fileName, bool useSoundPlayer)
        {
            var fullFileName = Path.Combine(_pathingService.ExtraMetaDataFolder, SoundDirectory.Sound, fileName);

            if (!File.Exists(fullFileName))
            {
                return null;
            }

            var entry = new PlayerEntry();
            if (useSoundPlayer)
            {
                entry.SoundPlayer = new System.Media.SoundPlayer { SoundLocation = fullFileName };
                entry.SoundPlayer.Load();
            }
            else
            {
                // MediaPlayer can play multiple sounds together from multiple instances, but the SoundPlayer can not
                entry.MediaPlayer = new MediaPlayer();
                entry.MediaPlayer.Open(new Uri(fullFileName));
            }

            return _players[fileName] = entry;
        }

        #endregion

        #endregion
    }
}
