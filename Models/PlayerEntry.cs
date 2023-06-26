using NAudio.Wave;

namespace PlayniteSounds
{
    class PlayerEntry
    {
        //public MediaPlayer MediaPlayer { get; set; }
        public IWavePlayer WavePlayer { get; set; }
        public bool IsPlaying { get; set; }
        public string FilePath { get; set; }
    }
}
