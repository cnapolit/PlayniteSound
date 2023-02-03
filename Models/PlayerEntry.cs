using System.Windows.Media;

namespace PlayniteSounds
{
    class PlayerEntry
    {
        public MediaPlayer MediaPlayer { get; set; }
        public bool IsPlaying { get; set; }
        public string FilePath { get; set; }
    }
}
