using PlayniteSounds.Common.Constants;
using PlayniteSounds.Models;
using PlayniteSounds.Services.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Playnite.SDK.Models;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PlayniteSounds.Models.Audio.SampleProviders;
using PlayniteSounds.Models.Audio;

namespace PlayniteSounds.Services.Audio
{
    public class SoundPlayer : BasePlayer, ISoundPlayer, IDisposable
    {
        #region Infrastructure

        private readonly IErrorHandler   _errorHandler;
        private readonly IPathingService _pathingService;
        private readonly IList<bool>     _activePlayers = new List<bool>(new bool[9]);
        private          CachedSound     _cachedSelectedGameSound;
        private          string          _selectedSoundFilePath;
        private          bool            _firstSelectSound = true;
        private          bool            _disposed;
        private ControllableSampleProvider _gameStartingProvider;

        public Game StartingGame { get; set; }


        public SoundPlayer(
            IErrorHandler errorHandler,
            IPathingService pathingService,
            MixingSampleProvider mixer,
            PlayniteSoundsSettings settings) : base(mixer, settings)
        {
            _errorHandler = errorHandler;
            _pathingService = pathingService;
            _mixer.MixerInputEnded += SoundEnded;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            // Give AppStopped and any other sounds 5 seconds to finish
            for (var i = 0; _activePlayers.Any(p => p) && i < 50; i++)
            {
                Thread.Sleep(100);
            }
        }

        private static void SoundEnded(object mixer, SampleProviderEventArgs args)
        {
            if (args.SampleProvider is CallBackSampleProvider callBackSampleProvider)
            {
                callBackSampleProvider.Callback();
            }
        }

        #endregion

        #region Implementation

        #region Preview

        public void Preview(SoundType soundType, bool playDesktop)
        {
            if (_settings.ActiveModeSettings.IsDesktop == playDesktop)
            {
                PlaySound(soundType);
                return;
            }

            var soundSettings = _settings.CurrentUIStateSettings.SoundTypesToSettings[soundType];

            var prefix = playDesktop ? SoundFile.DesktopPrefix : SoundFile.FullScreenPrefix;
            var baseFileName = SoundTypeToBaseFileName(soundType);
            var filePath = Path.Combine(
                _pathingService.ExtraMetaDataFolder, SoundDirectory.Sound, prefix + baseFileName);

            _errorHandler.Try(() => AttemptPlaySound(new AutoDisposeFileReader(filePath, soundSettings.Volume), null));
        }
        
        private static string SoundTypeToBaseFileName(SoundType soundType)
        {
            switch (soundType)
            {
                case SoundType.AppStarted:      return SoundFile. BaseApplicationStartedSound;
                case SoundType.AppStopped:      return SoundFile. BaseApplicationStoppedSound;
                case SoundType.GameStarting:    return SoundFile.       BaseGameStartingSound;
                case SoundType.GameStarted:     return SoundFile.        BaseGameStartedSound;
                case SoundType.GameStopped:     return SoundFile.        BaseGameStoppedSound;
                case SoundType.GameSelected:    return SoundFile.       BaseGameSelectedSound;
                case SoundType.GameInstalled:   return SoundFile.      BaseGameInstalledSound;
                case SoundType.GameUninstalled: return SoundFile.    BaseGameUninstalledSound;
                default:                        return SoundFile.     BaseLibraryUpdatedSound;
            }
        }

        #region PlaySound

        public void PlaySound(SoundType soundType, Action onSoundEndCallback = null)
        {
            ISampleProvider sampleProvider = null;

            var soundSettings = _settings.CurrentUIStateSettings.SoundTypesToSettings[soundType];
            if (soundSettings.Enabled) switch (soundType)
            {
                case SoundType.GameStarting:
                    _activePlayers[(int)SoundType.GameStarting] = true;
                    onSoundEndCallback = () => _activePlayers[(int)SoundType.GameStarting] = false;
                    sampleProvider = GetStartingSoundSampleProvider(soundSettings);
                    break;
                case SoundType.GameSelected:
                    sampleProvider = GetSelectSoundSampleProvider(soundSettings);
                    break;
                case SoundType.GameCancelled:
                    _gameStartingProvider.Stop();
                    _gameStartingProvider = null;
                    goto default;
                default:
                    sampleProvider = GetSoundSampleProvider(soundType, soundSettings);
                    break;
            }

            if (sampleProvider is null)
            {
                onSoundEndCallback?.Invoke();
                return;
            }

            _errorHandler.Try(() => AttemptPlaySound(sampleProvider, onSoundEndCallback));
        }

        #endregion

        #endregion

        #region Helpers

        private ISampleProvider GetSelectSoundSampleProvider(SoundTypeSettings soundSettings)
        {
            if (_firstSelectSound && _settings.SkipFirstSelectSound)
            {
                _firstSelectSound = false;
                return null;
            }

            var filePath = GetFilePath(SoundType.GameSelected);
            if (filePath is null)
            {
                return null;
            }

            if (_selectedSoundFilePath != filePath || _cachedSelectedGameSound?.Volume != soundSettings.Volume)
            {
                // We must reload the cached sound to inherit the new audio file or update the volume
                _cachedSelectedGameSound = null;
                _selectedSoundFilePath = filePath;
            }

            if (_cachedSelectedGameSound is null)
            {
                _cachedSelectedGameSound = new CachedSound(filePath, soundSettings.Volume);
            }

            return new CachedSoundSampleProvider(_cachedSelectedGameSound);
        }

        private ISampleProvider GetStartingSoundSampleProvider(SoundTypeSettings soundSettings)
        {
            var gameStartingSoundFile = _pathingService.GetGameStartSoundFile(StartingGame) 
                ?? Path.Combine(
                    _pathingService.ExtraMetaDataFolder,
                    SoundDirectory.Sound,
                    SoundTypeToFileName(SoundType.GameStarting));

            return File.Exists(gameStartingSoundFile) 
                ? new AutoDisposeFileReader(gameStartingSoundFile, soundSettings.Volume) 
                : null;
        }

        private ISampleProvider GetSoundSampleProvider(SoundType soundType, SoundTypeSettings soundSettings)
        {
            string filePath = GetFilePath(soundType);
            return filePath is null ? null : new AutoDisposeFileReader(filePath, soundSettings.Volume);
        }

        private void AttemptPlaySound(ISampleProvider reader, Action callBack)
        {
            reader = ConvertProvider(reader);

            if (callBack != null)
            {
                reader = new CallBackSampleProvider(reader, callBack);
            }

            _mixer.AddMixerInput(reader);
        }

        private string GetFilePath(SoundType soundType)
        {
            var fileName = SoundTypeToFileName(soundType);
            var filePath = Path.Combine(_pathingService.ExtraMetaDataFolder, SoundDirectory.Sound, fileName);
            return File.Exists(filePath) ? filePath : null;
        }
        
        private string SoundTypeToFileName(SoundType soundType)
        {
            switch (soundType)
            {
                case SoundType.AppStarted:      return SoundFile. ApplicationStartedSound;
                case SoundType.AppStopped:      return SoundFile. ApplicationStoppedSound;
                case SoundType.GameStarting:    return SoundFile.       GameStartingSound;
                case SoundType.GameStarted:     return SoundFile.        GameStartedSound;
                case SoundType.GameCancelled:
                case SoundType.GameStopped:     return SoundFile.        GameStoppedSound;
                case SoundType.GameSelected:    return SoundFile.       GameSelectedSound;
                case SoundType.GameInstalled:   return SoundFile.      GameInstalledSound;
                case SoundType.GameUninstalled: return SoundFile.    GameUninstalledSound;
                default:                        return SoundFile.     LibraryUpdatedSound;
            }
        }

        #endregion

        #endregion
    }
}
