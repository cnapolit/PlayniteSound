using System;
using System.Windows.Controls;
using PlayniteSounds.Models;
using Playnite.SDK.Controls;
using Playnite.SDK.Plugins;
using PlayniteSounds.Services.State;
using PlayniteSounds.Services.Audio;
using PlayniteSounds.Views.Layouts.GameViewControls;
using PlayniteSounds.Views.Models.GameViewControls;

namespace PlayniteSounds.Services.UI;

public class GameViewControlFactory(IPlayniteEventHandler playniteEventHandler, IMusicPlayer musicPlayer)
   : IGameViewControlFactory
{
    // args: FilePath_{Default,Filter,Platform,Game},Handler
    public Control GetGameViewControl(GetGameViewControlArgs args)
    {
        var strArgs = args.Name.Split('_');

        var controlType = strArgs[0];

        switch (controlType)
        {
            case "FilePath": return ConstructFilePathView(RetrieveMusicType(strArgs));
            case "Handler":  return new HandlerControl()
            {
                DataContext = new HandlerControlModel(playniteEventHandler, musicPlayer)
            };
            default: throw new ArgumentException($"Unrecognized controlType '{controlType}' for request '{args.Name}'");
        }
    }

    private PluginUserControl ConstructFilePathView(AudioSource musicSource)
    {
        return null;
    }

    private static AudioSource RetrieveMusicType(string[] strArgs)
    {
        var musicTypeStr = strArgs[1];
        if (Enum.TryParse<AudioSource>(musicTypeStr, true, out var musicType))
        {
            return musicType;
        }

        throw new ArgumentException($"Unrecognized musicType '{musicTypeStr}'");
    }
}