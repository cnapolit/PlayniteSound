using Playnite.SDK.Plugins;
using System.Collections.Generic;

namespace PlayniteSounds.Services.UI
{
    public interface IGameMenuFactory
    {
        IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs __);
    }
}