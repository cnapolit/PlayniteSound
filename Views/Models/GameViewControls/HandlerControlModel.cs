using PlayniteSounds.Services.Audio;
using PlayniteSounds.Services.State;

namespace PlayniteSounds.Views.Models.GameViewControls
{
    public class HandlerControlModel
    {
        private readonly IPlayniteEventHandler _playniteEventHandler;
        private readonly IMusicPlayer _musicPlayer;

        public HandlerControlModel(IPlayniteEventHandler playniteEventHandler, IMusicPlayer musicPlayer)
        {
            _playniteEventHandler = playniteEventHandler;
            _musicPlayer = musicPlayer;
        }

        public void SetVolume(float volume) => _musicPlayer.SetVolume(volume);

        public void Pause() => _musicPlayer.Pause("Theme");

        public void Play() => _musicPlayer.Resume("Theme");
    }
}
