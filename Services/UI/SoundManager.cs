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

namespace PlayniteSounds.Services.UI;

public class SoundManager(
    IDialogsFactory dialogs,
    IErrorHandler errorHandler,
    IPathingService pathingService)
    : ISoundManager
{
    #region Infrastructure

    #endregion

    #region Implementation

    #region LoadSounds

    public void LoadSounds() => errorHandler.TryWithPrompt(AttemptLoadSounds);
    private void AttemptLoadSounds()
    {
        //just in case user deleted it
        Directory.CreateDirectory(pathingService.SoundManagerFilesDataPath);

        var dialog = new OpenFileDialog
        {
            Filter = "ZIP archive|*.zip",
            InitialDirectory = pathingService.SoundManagerFilesDataPath
        };

        var result = dialog.ShowDialog(dialogs.GetCurrentAppWindow());
        if (result == true)
        {
            var targetPath = dialog.FileName;
            //just in case user deleted it
            Directory.CreateDirectory(pathingService.SoundFilesDataPath);
            // Have to extract each file one at a time to enabled overwrites
            using (var archive = ZipFile.OpenRead(targetPath))
                foreach (var entry in archive.Entries.Where(e => !string.IsNullOrWhiteSpace(e.Name)))
                {
                    var entryDestination = Path.GetFullPath(Path.Combine(pathingService.SoundFilesDataPath, entry.Name));
                    entry.ExtractToFile(entryDestination, true);
                }
            dialogs.ShowMessage(
                $"{Resource.ManagerLoadConfirm} {Path.GetFileNameWithoutExtension(targetPath)}");
        }
    }

    #endregion

    #region SaveSounds

    public void SaveSounds()
    {
        var windowExtension = dialogs.CreateWindow(
            new WindowCreationOptions
            {
                ShowMinimizeButton = false,
                ShowMaximizeButton = false,
                ShowCloseButton = true
            });

        windowExtension.ShowInTaskbar = false;
        windowExtension.ResizeMode = ResizeMode.NoResize;
        windowExtension.Owner = dialogs.GetCurrentAppWindow();
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

        saveNameBox.KeyUp += (_, _) =>
        {
            // Only allow saving if filename is larger than 3 characters
            saveNameButton.IsEnabled = saveNameBox.Text.Trim().Length > 3;
        };

        saveNameButton.Click += (_, _) =>
        {
            // Create ZIP file in sound manager folder
            try
            {
                var soundPackName = saveNameBox.Text;
                //just in case user deleted it
                Directory.CreateDirectory(pathingService.SoundFilesDataPath);
                //just in case user deleted it
                Directory.CreateDirectory(pathingService.SoundManagerFilesDataPath);
                ZipFile.CreateFromDirectory(
                    pathingService.SoundFilesDataPath, Path.Combine(pathingService.SoundManagerFilesDataPath, soundPackName + ".zip"));
                dialogs.ShowMessage($"{Resource.ManagerSaveConfirm} {soundPackName}");
                windowExtension.Close();
            }
            catch (Exception e)
            {
                errorHandler.CreateExceptionPrompt(e);
            }
        };

        windowExtension.Content = stackPanel;
        windowExtension.SizeToContent = SizeToContent.WidthAndHeight;
        // Workaround for WPF bug which causes black sections to be displayed in the window
        windowExtension.ContentRendered += (_, _) => windowExtension.InvalidateMeasure();
        windowExtension.Loaded += (_, _) => saveNameBox.Focus();
        windowExtension.ShowDialog();
    }

    #endregion

    #region RemoveSounds

    public void RemoveSounds() => errorHandler.TryWithPrompt(AttemptRemoveSounds);
    private void AttemptRemoveSounds()
    {
        //just in case user deleted it
        Directory.CreateDirectory(pathingService.SoundManagerFilesDataPath);

        var dialog = new OpenFileDialog
        {
            Filter = "ZIP archive|*.zip",
            InitialDirectory = pathingService.SoundManagerFilesDataPath
        };

        if (dialog.ShowDialog(dialogs.GetCurrentAppWindow()) is true)
        {
            var targetPath = dialog.FileName;
            File.Delete(targetPath);
            dialogs.ShowMessage($"{Resource.ManagerDeleteConfirm} {Path.GetFileNameWithoutExtension(targetPath)}");
        }
    }

    #endregion

    #region ImportSounds

    public void ImportSounds()
    {
        var targetPaths = dialogs.SelectFiles("ZIP archive|*.zip");

        if (targetPaths.HasNonEmptyItems())
        {
            errorHandler.TryWithPrompt(() => AttemptImportSounds(targetPaths));
        }
    }

    private void AttemptImportSounds(IEnumerable<string> targetPaths)
    {
        //just in case user deleted it
        Directory.CreateDirectory(pathingService.SoundManagerFilesDataPath);
        foreach (var targetPath in targetPaths)
        {
            //just in case user selects a file from the soundManager location
            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (!targetDirectory.Equals(pathingService.SoundManagerFilesDataPath, StringComparison.OrdinalIgnoreCase))
            {
                var newTargetPath = Path.Combine(pathingService.SoundManagerFilesDataPath, Path.GetFileName(targetPath));
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
            Directory.CreateDirectory(pathingService.SoundManagerFilesDataPath);
            Process.Start(pathingService.SoundManagerFilesDataPath);
        }
        catch (Exception e)
        {
            errorHandler.CreateExceptionPrompt(e);
        }
    }

    #endregion

    #region OpenMusicFolder

    public void OpenMusicFolder() => OpenFolder(pathingService.MusicFilesDataPath);

    #endregion

    #region OpenSoundsFolder

    public void OpenSoundsFolder() => OpenFolder(pathingService.SoundFilesDataPath);

    #endregion

    #region HelpMenu

    public void HelpMenu() => dialogs.ShowMessage(App.HelpMessage.Value, App.AppName);

    #endregion

    #region Helpers

    private void OpenFolder(string folderPath) => errorHandler.TryWithPrompt(() => AttemptOpenFolder(folderPath));
    private void AttemptOpenFolder(string folderPath)
    {
        // just in case user deleted it
        Directory.CreateDirectory(folderPath);
        Process.Start(folderPath);
    }

    #endregion

    #endregion
}