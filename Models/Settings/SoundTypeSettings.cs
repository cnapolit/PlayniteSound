using PlayniteSounds.Models.Audio.Sound;

namespace PlayniteSounds.Models;

public class SoundTypeSettings
{
    public bool Enabled { get; set; } = true;
    public float Volume { get; set; } = 1;
    public AudioSource Source { get; set; } = AudioSource.Filter;
    public SoundType SoundType { get; set; }
}