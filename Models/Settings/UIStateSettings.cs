using PlayniteSounds.Models.Audio.Sound;

namespace PlayniteSounds.Models
{
    public class UIStateSettings
    {
        public bool MusicMuffled { get; set; } = true;
        public float MusicVolume { get; set; } = 2;
        public AudioSource MusicSource { get; set; }
        public SoundTypeSettings EnterSettings { get; set; } = new SoundTypeSettings { SoundType = SoundType.Enter };
        public SoundTypeSettings ExitSettings  { get; set; } = new SoundTypeSettings { SoundType = SoundType.Exit };
        public SoundTypeSettings TickSettings  { get; set; } = new SoundTypeSettings { SoundType = SoundType.Tick };
    }
}
