using Playnite.SDK.Data;
using System.Collections.Generic;

namespace PlayniteSounds.Models
{
    public class ModeSettings
    {
        public bool MusicEnabled { get; set; }
        public IDictionary<UIState, UIStateSettings> UIStatesToSettings { get; set; }

        [DontSerialize]
        public bool IsDesktop { get; set; }
    }
}
