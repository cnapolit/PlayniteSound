using Playnite.SDK;
using System.Linq;

namespace PlayniteSounds.Common.Extensions;

public static class Playnite
{
    public static bool SingleGame(this IMainViewAPI api) => api.SelectedGames.Count() == 1;
}