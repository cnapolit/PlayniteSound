using Playnite.SDK.Data;
using PlayniteSounds.Models.UI;
using System.Collections.Generic;

namespace PlayniteSounds.Models
{
    public class ModeSettings
    {
        public bool MusicEnabled { get; set; }
        public float SoundMasterVolume { get; set; } = 1;
        public float MusicMasterVolume { get; set; } = 1;
        public IDictionary<UIState, UIStateSettings> UIStatesToSettings { get; set; }
        public IDictionary<PlayniteEvent, SoundTypeSettings> PlayniteEventToSoundTypesSettings { get; set; }

        [DontSerialize]
        public bool IsDesktop { get; set; }
    }
}
