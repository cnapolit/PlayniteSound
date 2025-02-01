using Playnite.SDK;
using Playnite.SDK.Data;
using PlayniteSounds.Common;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PlayniteSounds.Views.Models;

public class PlayniteSoundsSettingsViewModel(
    IModelFactory modelFactory,
    IMusicPlayer musicPlayer,
    ISoundManager soundManager,
    PlayniteSoundsPlugin plugin,
    PlayniteSoundsSettings settings,
    Action<object> containerReleaseMethod)
    : BaseSettingsModel, ISettings, IDisposable
{
    private PlayniteSoundsSettings EditingClone { get; set; }

    private bool _disposed;

    public PlayniteSoundsSettings Settings
    {
        get => settings;
        set
        {
            // Copying allows changes to propagate across the plugin due to this instance being singleton
            // The volume still needs to be manually updated for the change to be immediate
            settings.Copy(value);

            // The Serialized edit clone does not define properties with the 'DontSerialize' flag
            settings.ActiveModeSettings = settings.DesktopSettings;
            settings.CurrentUIStateSettings = settings.DesktopSettings.UIStatesToSettings[UIState.MainMenu];
            OnPropertyChanged();

            musicPlayer.SetVolume(settings.CurrentUIStateSettings.MusicVolume);
            musicPlayer.Pause(false);
            musicPlayer.Resume(false);
        }
    }

    public RelayCommand<object> BrowseForFFmpegFile => new(_ =>
    {
        var filePath = plugin.PlayniteApi.Dialogs.SelectFile(string.Empty);
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            Settings.FFmpegPath = filePath;
        }
    });

    public RelayCommand<object> BrowseForFFmpegNormalizeFile => new(_ =>
    {
        var filePath = plugin.PlayniteApi.Dialogs.SelectFile(string.Empty);
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            Settings.FFmpegNormalizePath = filePath;
        }
    });

    public RelayCommand<object> NavigateUrlCommand => new(url => Process.Start((url as Uri).AbsoluteUri));

    public ModeSettingsModel DesktopSettingsModel { get; private set; } = modelFactory.CreateModeSettingsModel(settings.DesktopSettings);
    public ModeSettingsModel FullscreenSettingsModel { get; private set; } = modelFactory.CreateModeSettingsModel(settings.FullscreenSettings);

    // Use serialization for a deep copy of Settings
    public void BeginEdit() => EditingClone = Serialization.GetClone(Settings);

    // Implicitly use Copy extension method for shallow copy
    public void CancelEdit() => Settings = EditingClone;

    public void EndEdit()
    {
        plugin.SavePluginSettings(Settings);

        settings.CurrentUIStateSettings = settings.DesktopSettings.UIStatesToSettings[UIState.Main];
        musicPlayer.SetVolume(settings.CurrentUIStateSettings.MusicVolume);
        musicPlayer.Pause(false);
        musicPlayer.Resume(false);
    }

    public bool VerifySettings(out List<string> errors)
    {
        errors = [];
        var outcome = true;

        if (!string.IsNullOrWhiteSpace(Settings.FFmpegPath) && !File.Exists(Settings.FFmpegPath))
        {
            errors.Add($"The path to FFmpeg '{Settings.FFmpegPath}' is invalid");
            outcome = false;
        }

        if (!string.IsNullOrEmpty(Settings.YoutubeSearchFormat) && !Settings.YoutubeSearchFormat.Contains("{0}"))
        {
            errors.Add("The YouTube search format string does not contain the game insertion sub-string '{0}'");
            outcome = false;
        }

        return outcome;
    }

    public RelayCommand<object> ButOpenSoundsFolder_Click => new(_ => soundManager.OpenSoundsFolder());

    public RelayCommand<object> ButOpenMusicFolder_Click => new(_ => soundManager.OpenMusicFolder());

    public RelayCommand<object> ButOpenInfo_Click => new(_ => soundManager.HelpMenu());

    public RelayCommand<object> ButSaveSounds_Click => new(_ => soundManager.SaveSounds());

    public RelayCommand<object> ButLoadSounds_Click => new(_ => soundManager.LoadSounds());

    public RelayCommand<object> ButImportSounds_Click => new(_ => soundManager.ImportSounds());

    public RelayCommand<object> ButRemoveSounds_Click => new(_ => soundManager.RemoveSounds());

    public RelayCommand<object> ButOpensoundManagerFolder_Click => new(_ => soundManager.OpenSoundManagerFolder());

    public void Dispose()
    {
        if (_disposed) return;
        containerReleaseMethod(this);
        _disposed = true;
    }
}