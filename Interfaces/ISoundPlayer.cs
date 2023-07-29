using PlayniteSounds.Models;
using PlayniteSounds.Models.Audio.Sound;
using System;

namespace PlayniteSounds.Services.Audio
{
    public interface ISoundPlayer
    {
        void Play(SoundTypeSettings settings, Action callBack = null);
        void Preview(SoundTypeSettings settings, bool isDesktop);
        void Tick();
        void Trigger(SoundType soundType);
    }
}
