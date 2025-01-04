using System;
using System.Collections.Generic;
using Playnite.SDK.Models;

namespace PlayniteSounds.Models.State
{
    public class PlayniteState
    {
        public UIState CurrentUIState { get; set; }
        public UIState PreviousUIState { get; set; }
        public Game CurrentGame { get; set; }
        public Game PreviousGame { get; set; }
        public Guid CurrentFilterGuid { get; set; }
        public bool HasFocus { get; set; } = true;

        public int GamesPlaying => _gamesPlaying.Count;

        private readonly HashSet<Guid> _gamesPlaying = new HashSet<Guid>();
        private readonly object _lock = new object();

        public bool GameIsStarting(Guid gameId)
        {
            lock (_lock) /* Then */ return _gamesPlaying.Add(gameId);
        }

        public bool GameHasEnded(Guid gameId)
        {
            lock (_lock) /* Then */ return _gamesPlaying.Remove(gameId);
        }

    }
}
