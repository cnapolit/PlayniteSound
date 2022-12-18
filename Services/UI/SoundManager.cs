using Microsoft.Win32;
using Playnite.SDK;
using PlayniteSounds.Common.Constants;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using PlayniteSounds.Services.Files;
using PlayniteSounds.Services.Audio;

namespace PlayniteSounds.Services.UI
{
    internal class SoundManager : ISoundManager
    {
        #region Infrastructure

        private readonly IErrorHandler _errorHandler;
        private readonly IPathingService _pathingService;
        private readonly ISoundPlayer _soundPlayer;
        private readonly IDialogsFactory _dialogs;

        public SoundManager(
            IErrorHandler errorHandler,
            IPathingService pathingService,
            ISoundPlayer audioPlayer,
            IPlayniteAPI api)
        {
            _errorHandler = errorHandler;
            _pathingService = pathingService;
            _soundPlayer = audioPlayer;
            _dialogs = api.Dialogs;
        }

        #endregion

        #region Implementation

        #region LoadSounds

        public void LoadSounds() => _errorHandler.Try(AttemptLoadSounds);
        private void AttemptLoadSounds()
        {
            //just in case user deleted it
            Directory.CreateDirectory(_pathingService.SoundManagerFilesDataPath);

            var dialog = new OpenFileDialog
            {
                Filter = "ZIP archive|*.zip",
                InitialDirectory = _pathingService.SoundManagerFilesDataPath
            };

            var result = dialog.ShowDialog(_dialogs.GetCurrentAppWindow());
            if (result == true)
            {
                _soundPlayer.Close();
                var targetPath = dialog.FileName;
                //just in case user deleted it
                Directory.CreateDirectory(_pathingService.SoundFilesDataPath);
                // Have to extract each file one at a time to enabled overwrites
                using (var archive = ZipFile.OpenRead(targetPath))
                    foreach (var entry in archive.Entries.Where(e => !string.IsNullOrWhiteSpace(e.Name)))
                    {
                        var entryDestination = Path.GetFullPath(Path.Combine(_pathingService.SoundFilesDataPath, entry.Name));
                        entry.ExtractToFile(entryDestination, true);
                    }
                _dialogs.ShowMessage(
                    $"{Resource.ManagerLoadConfirm} {Path.GetFileNameWithoutExtension(targetPath)}");
            }
        }

        #endregion

        #region SaveSounds

        public void SaveSounds()
        {
            var windowExtension = _dialogs.CreateWindow(
                new WindowCreationOptions
                {
                    ShowMinimizeButton = false,
                    ShowMaximizeButton = false,
                    ShowCloseButton = true
                });

            windowExtension.ShowInTaskbar = false;
            windowExtension.ResizeMode = ResizeMode.NoResize;
            windowExtension.Owner = _dialogs.GetCurrentAppWindow();
            windowExtension.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var saveNameBox = new TextBox
            {
                Margin = new Thickness(5, 5, 10, 5),
                Width = 200
            };
            stackPanel.Children.Add(saveNameBox);

            var saveNameButton = new Button
            {
                Margin = new Thickness(0, 5, 5, 5),
                Content = Resource.ManagerSave,
                IsEnabled = false,
                IsDefault = true
            };
            stackPanel.Children.Add(saveNameButton);

            saveNameBox.KeyUp += (sender, _) =>
            {
                // Only allow saving if filename is larger than 3 characters
                saveNameButton.IsEnabled = saveNameBox.Text.Trim().Length > 3;
            };

            saveNameButton.Click += (sender, _) =>
            {
                // Create ZIP file in sound manager folder
                try
                {
                    var soundPackName = saveNameBox.Text;
                    //just in case user deleted it
                    Directory.CreateDirectory(_pathingService.SoundFilesDataPath);
                    //just in case user deleted it
                    Directory.CreateDirectory(_pathingService.SoundManagerFilesDataPath);
                    ZipFile.CreateFromDirectory(
                        _pathingService.SoundFilesDataPath, Path.Combine(_pathingService.SoundManagerFilesDataPath, soundPackName + ".zip"));
                    _dialogs.ShowMessage($"{Resource.ManagerSaveConfirm} {soundPackName}");
                    windowExtension.Close();
                }
                catch (Exception e)
                {
                    _errorHandler.HandleException(e);
                }
            };

            windowExtension.Content = stackPanel;
            windowExtension.SizeToContent = SizeToContent.WidthAndHeight;
            // Workaround for WPF bug which causes black sections to be displayed in the window
            windowExtension.ContentRendered += (s, e) => windowExtension.InvalidateMeasure();
            windowExtension.Loaded += (s, e) => saveNameBox.Focus();
            windowExtension.ShowDialog();
        }

        #endregion

        #region RemoveSounds

        public void RemoveSounds() => _errorHandler.Try(AttemptRemoveSounds);
        private void AttemptRemoveSounds()
        {
            //just in case user deleted it
            Directory.CreateDirectory(_pathingService.SoundManagerFilesDataPath);

            var dialog = new OpenFileDialog
            {
                Filter = "ZIP archive|*.zip",
                InitialDirectory = _pathingService.SoundManagerFilesDataPath
            };

            var result = dialog.ShowDialog(_dialogs.GetCurrentAppWindow());
            if (result == true)
            {
                var targetPath = dialog.FileName;
                File.Delete(targetPath);
                _dialogs.ShowMessage(
                    $"{Resource.ManagerDeleteConfirm} {Path.GetFileNameWithoutExtension(targetPath)}");
            }
        }

        #endregion

        #region ImportSounds

        public void ImportSounds()
        {
            var targetPaths = _dialogs.SelectFiles("ZIP archive|*.zip");

            if (targetPaths.HasNonEmptyItems())
            {
                _errorHandler.Try(() => AttemptImportSounds(targetPaths));
            }
        }

        private void AttemptImportSounds(IEnumerable<string> targetPaths)
        {
            //just in case user deleted it
            Directory.CreateDirectory(_pathingService.SoundManagerFilesDataPath);
            foreach (var targetPath in targetPaths)
            {
                //just in case user selects a file from the soundManager location
                var targetDirectory = Path.GetDirectoryName(targetPath);
                if (!targetDirectory.Equals(_pathingService.SoundManagerFilesDataPath, StringComparison.OrdinalIgnoreCase))
                {
                    var newTargetPath = Path.Combine(_pathingService.SoundManagerFilesDataPath, Path.GetFileName(targetPath));
                    File.Copy(targetPath, newTargetPath, true);
                }
            }
        }

        #endregion

        #region OpenSoundManagerFolder

        public void OpenSoundManagerFolder()
        {
            try
            {
                //just in case user deleted it
                Directory.CreateDirectory(_pathingService.SoundManagerFilesDataPath);
                Process.Start(_pathingService.SoundManagerFilesDataPath);
            }
            catch (Exception e)
            {
                _errorHandler.HandleException(e);
            }
        }

        #endregion

        #region OpenMusicFolder

        public void OpenMusicFolder() => OpenFolder(_pathingService.MusicFilesDataPath);

        #endregion

        #region OpenSoundsFolder

        public void OpenSoundsFolder() => OpenFolder(_pathingService.SoundFilesDataPath);

        #endregion

        #region HelpMenu

        public void HelpMenu() => _dialogs.ShowMessage(App.HelpMessage.Value, App.AppName);

        #endregion

        #region Helpers

        private void OpenFolder(string folderPath) => _errorHandler.Try(() => AttemptOpenFolder(folderPath));
        private void AttemptOpenFolder(string folderPath)
        {
            //need to release them otherwise explorer can't overwrite files even though you can delete them
            _soundPlayer.Close();
            // just in case user deleted it
            Directory.CreateDirectory(folderPath);
            Process.Start(folderPath);
        }

        #endregion

        #endregion
    }
}
