using Playnite.SDK.Models;
using Playnite.SDK;
using System.Collections.Generic;
using System.Linq;

namespace PlayniteSounds.Common.Extensions
{
    internal static class Playnite
    {
        public static IEnumerable<Game> SelectedGames(this IPlayniteAPI api) => api.MainView.SelectedGames;

        public static bool SingleGame(this IPlayniteAPI api) => api.SelectedGames().Count() == 1;
    }
}
