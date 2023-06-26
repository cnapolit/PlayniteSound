using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace PlayniteSounds.Models
{
    public class UIStateSettings
    {
        public UIStateSettings() 
            => _lazySelectedSoundType = new Lazy<SoundTypeSettings>(() => SoundTypesToSettings[SoundType.GameSelected]);

        public bool MusicMuffled { get; set; }
        public float MusicVolume { get; set; } = .25f;
        public AudioSource MusicSource { get; set; }
        public IDictionary<SoundType, SoundTypeSettings> SoundTypesToSettings { get; set; } = new Dictionary<SoundType, SoundTypeSettings>
        {
            { SoundType.AppStarted,      new SoundTypeSettings() },
            { SoundType.AppStopped,      new SoundTypeSettings() },
            { SoundType.GameStarting,    new SoundTypeSettings() },
            { SoundType.GameStarted,     new SoundTypeSettings() },
            { SoundType.GameStopped,     new SoundTypeSettings() },
            { SoundType.GameSelected,    new SoundTypeSettings() },
            { SoundType.GameInstalled,   new SoundTypeSettings() },
            { SoundType.GameUninstalled, new SoundTypeSettings() },
            { SoundType.LibraryUpdated,  new SoundTypeSettings() }
        };

        public static IDictionary<SoundType, SoundTypeSettings> DefaultFullscreenSoundTypeSettings() => new Dictionary<SoundType, SoundTypeSettings>
        {
            { SoundType.AppStarted,      new SoundTypeSettings { Enabled = true } },
            { SoundType.AppStopped,      new SoundTypeSettings { Enabled = true } },
            { SoundType.GameStarting,    new SoundTypeSettings { Enabled = true, Source = AudioSource.Game } },
            { SoundType.GameInstalled,   new SoundTypeSettings { Enabled = true } },
            { SoundType.GameUninstalled, new SoundTypeSettings { Enabled = true } },
            { SoundType.LibraryUpdated,  new SoundTypeSettings { Enabled = true } },
            { SoundType.GameStarted,     new SoundTypeSettings() },
            { SoundType.GameStopped,     new SoundTypeSettings() },
            { SoundType.GameSelected,    new SoundTypeSettings() }
        };

        [DontSerialize] private readonly Lazy<SoundTypeSettings> _lazySelectedSoundType;
        [DontSerialize] public SoundTypeSettings SelectedSoundType => _lazySelectedSoundType.Value;
    }
}
