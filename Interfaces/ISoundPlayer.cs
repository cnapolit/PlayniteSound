using System;
using Playnite.SDK.Models;
using PlayniteSounds.Models;

namespace PlayniteSounds.Services.Audio
{
    public interface ISoundPlayer
    {
        Game StartingGame { get; set; }

        void PlaySound(SoundType soundType, Action onSoundEndCallback = null);
        void Preview(SoundType soundType, bool playDesktop);
    }
}
