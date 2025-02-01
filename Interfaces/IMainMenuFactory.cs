using Playnite.SDK.Plugins;
using System.Collections.Generic;

namespace PlayniteSounds.Services.UI;

public interface IMainMenuFactory
{
    IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs __);
}