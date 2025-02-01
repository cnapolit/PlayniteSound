using Playnite.SDK.Models;
using PlayniteSounds.Models.UI;
using System.Collections.Generic;

namespace PlayniteSounds.Models.State;

public class PlayniteEventOccurredArgs
{
    public SoundTypeSettings SoundTypeSettings { get; set; }
    public PlayniteEvent Event { get; set; }
    public IList<Game> Games { get; set; }
}