namespace PlayniteSounds.Models
{
    public class SoundTypeSettings
    {
        public bool Enabled { get; set; }
        public float Volume { get; set; } = .5f;
        public AudioSource Source { get; set; }
    }
}
