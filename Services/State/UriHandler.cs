using Playnite.SDK;
using Playnite.SDK.Events;
using PlayniteSounds.Common.Constants;
using PlayniteSounds.Interfaces;
using PlayniteSounds.Services.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayniteSounds.Services.State
{
    public class UriHandler : IUriHandler
    {
        private readonly IMusicPlayer _musicPlayer;

        public UriHandler(IUriHandlerAPI uriHandlerAPI, IMusicPlayer musicPlayer)
        {
            uriHandlerAPI.RegisterSource(App.SourceName, HandleUriEvent);
            _musicPlayer = musicPlayer;
        }

        // ex: playnite://Sounds/Play/someId
        // Sounds maintains a list of plugins who want the music paused and will only allow play when
        // no other plugins have paused.
        private void HandleUriEvent(PlayniteUriEventArgs args)
        {
            var action = args.Arguments[0];
            var senderId = args.Arguments[1];

            switch (action.ToLower())
            {
                case "play": _musicPlayer.Resume(senderId); break;
                case "pause": _musicPlayer.Pause(senderId); break;
            }
        }
    }
}
