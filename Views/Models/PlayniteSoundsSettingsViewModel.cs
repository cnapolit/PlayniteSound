using Playnite.SDK;
using Playnite.SDK.Data;
using PlayniteSounds.Common;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PlayniteSounds.Views.Models
{
    public class PlayniteSoundsSettingsViewModel : ObservableObject, ISettings
    {
        private PlayniteSoundsSettings EditingClone { get; set; }
        private readonly IMusicPlayer _musicPlayer;
        private readonly PlayniteSoundsSettings _settings;
        public PlayniteSoundsSettings Settings
        {
            get => _settings;
            set
            {
                // Copying allows changes to propagate across the plugin due to this instance being singleton
                // We still need to manually update the volume for the change to be immediate
                _settings.Copy(value);
                OnPropertyChanged();

                var volume = _settings.IsDesktop 
                    ? _settings.DesktopSettings.MusicVolume 
                    : _settings.FullscreenSettings.MusicVolume;

                _musicPlayer.SetVolume(volume);
                _musicPlayer.Pause(false);
                _musicPlayer.Resume(false);
            }
        }

        public RelayCommand<object> BrowseForFFmpegFile => new RelayCommand<object>(a =>
        {
            var filePath = _plugin.PlayniteApi.Dialogs.SelectFile(string.Empty);
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                Settings.FFmpegPath = filePath;
            }
        });

        public RelayCommand<object> BrowseForFFmpegNormalizeFile => new RelayCommand<object>(a =>
        {
            var filePath = _plugin.PlayniteApi.Dialogs.SelectFile(string.Empty);
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                Settings.FFmpegNormalizePath = filePath;
            }
        });

        public RelayCommand<object> NavigateUrlCommand 
            => new RelayCommand<object>(url => Process.Start((url as Uri).AbsoluteUri));

        private readonly PlayniteSounds _plugin;
        public ModeSettingsModel DesktopSettingsModel { get; private set; }
        public ModeSettingsModel FullscreenSettingsModel { get; private set; }

        public PlayniteSoundsSettingsViewModel(
            PlayniteSounds plugin, 
            PlayniteSoundsSettings settings,
            ModeSettingsModel desktopSettingsModel, 
            ModeSettingsModel fullscreenSettingsModel, 
            IMusicPlayer musicPlayer)
        {
            _plugin = plugin;
            _musicPlayer = musicPlayer;
            _settings = settings;

            DesktopSettingsModel = desktopSettingsModel;
            DesktopSettingsModel.Settings = settings.DesktopSettings;

            FullscreenSettingsModel = fullscreenSettingsModel;
            FullscreenSettingsModel.Settings = settings.FullscreenSettings;
        }

        // Use serialization for a deep copy of Settings
        public void BeginEdit() => EditingClone = Serialization.GetClone(Settings);

        // Implicitly use Copy extension method for shallow copy
        public void CancelEdit() => Settings = EditingClone;

        public void EndEdit() => _plugin.SavePluginSettings(Settings);

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            var outcome = true;

            if (!string.IsNullOrWhiteSpace(Settings.FFmpegPath) && !File.Exists(Settings.FFmpegPath))
            {
                errors.Add($"The path to FFmpeg '{Settings.FFmpegPath}' is invalid");
                outcome = false;
            }

            return outcome;
        }
    }
}