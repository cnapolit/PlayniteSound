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

namespace PlayniteSounds.Views.Models
{
    public class PlayniteSoundsSettingsViewModel : BaseSettingsModel, ISettings, IDisposable
    {
        private PlayniteSoundsSettings EditingClone { get; set; }
        private readonly IMusicPlayer _musicPlayer;

        private readonly PlayniteSoundsSettings _settings;
        private readonly Action<object> _containerReleaseMethod;
        private bool _disposed;

        public PlayniteSoundsSettings Settings
        {
            get => _settings;
            set
            {
                // Copying allows changes to propagate across the plugin due to this instance being singleton
                // The volume still needs to be manually updated for the change to be immediate
                _settings.Copy(value);

                // The Serialized edit clone does not define properties with the 'DontSerialize' flag
                _settings.ActiveModeSettings = _settings.DesktopSettings;
                _settings.CurrentUIStateSettings = _settings.DesktopSettings.UIStatesToSettings[UIState.MainMenu];
                OnPropertyChanged();

                _musicPlayer.SetVolume(_settings.CurrentUIStateSettings.MusicVolume);
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
            IModelFactory modelFactory,
            IMusicPlayer musicPlayer,
            PlayniteSounds plugin, 
            PlayniteSoundsSettings settings,
            Action<object> containerReleaseMethod)
        {
            DesktopSettingsModel = modelFactory.CreateModeSettingsModel(settings.DesktopSettings);
            FullscreenSettingsModel = modelFactory.CreateModeSettingsModel(settings.FullscreenSettings);
            _musicPlayer = musicPlayer;
            _plugin = plugin;
            _settings = settings;
            _containerReleaseMethod = containerReleaseMethod;
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
        private ISoundManager SoundManager { get; set; }
        public RelayCommand<object> ButOpenSoundsFolder_Click
            => new RelayCommand<object> (_ => SoundManager.OpenSoundsFolder());

        public RelayCommand<object> ButOpenMusicFolder_Click
            => new RelayCommand<object>(_ => SoundManager.OpenMusicFolder());

        public RelayCommand<object> ButOpenInfo_Click
            => new RelayCommand<object>(_ => SoundManager.HelpMenu());

        public RelayCommand<object> ButSaveSounds_Click
            => new RelayCommand<object>(_ => SoundManager.SaveSounds());

        public RelayCommand<object> ButLoadSounds_Click
            => new RelayCommand<object>(_ => SoundManager.LoadSounds());

        public RelayCommand<object> ButImportSounds_Click
            => new RelayCommand<object>(_ => SoundManager.ImportSounds());

        public RelayCommand<object> ButRemoveSounds_Click
            => new RelayCommand<object>(_ => SoundManager.RemoveSounds());

        public RelayCommand<object> ButOpenSoundManagerFolder_Click
            => new RelayCommand<object>(_ => SoundManager.OpenSoundManagerFolder());
        public void Dispose()
        {
            if (_disposed) return;
            _containerReleaseMethod(this);
            _disposed = true;
        }
    }
}