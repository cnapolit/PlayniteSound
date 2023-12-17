using System;
using Playnite.SDK.Models;

namespace PlayniteSounds.Models.State
{
    public class PlayniteState
    {
        public UIState CurrentUIState { get; set; }
        public UIState PreviousUIState { get; set; }
        public int GamesPlaying { get; set; }
        public Game CurrentGame { get; set; }
        public Guid CurrentFilterGuid { get; set; }
    }
}
