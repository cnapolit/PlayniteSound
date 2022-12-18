using Playnite.SDK.Plugins;
using System.Collections.Generic;

namespace PlayniteSounds.Services.UI
{
    internal interface IGameMenuFactory
    {
        IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs __);
    }
}