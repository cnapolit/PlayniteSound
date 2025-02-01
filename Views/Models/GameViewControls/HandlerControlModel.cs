using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.State;

namespace PlayniteSounds.Views.Models.GameViewControls;

public class HandlerControlModel(IPlayniteEventHandler playniteEventHandler, IMusicPlayer musicPlayer)
{
    public readonly IPlayniteEventHandler PlayniteEventHandler = playniteEventHandler;

    public void SetVolume(float volume) => musicPlayer.SetVolume(volume);

    public void Pause() => musicPlayer.Pause("Theme");

    public void Play() => musicPlayer.Resume("Theme");
}