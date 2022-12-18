using Playnite.SDK.Plugins;
using System.Collections.Generic;

namespace PlayniteSounds.Services.UI
{
    internal interface IMainMenuFactory
    {
        IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs __);
    }
}
