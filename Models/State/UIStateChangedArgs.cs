using Playnite.SDK.Models;

namespace PlayniteSounds.Models.State
{
    public class UIStateChangedArgs
    {
        public UIStateSettings OldSettings { get; set; }
        public UIStateSettings NewSettings { get; set; }
        public UIState OldState { get; set; }
        public UIState NewState { get; set; }
        public Game Game { get; set; }
    }
}
