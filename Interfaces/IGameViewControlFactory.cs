using Playnite.SDK.Plugins;
using System.Windows.Controls;

namespace PlayniteSounds.Services.UI;

public interface IGameViewControlFactory
{
    Control GetGameViewControl(GetGameViewControlArgs args);
}