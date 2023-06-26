using Playnite.SDK.Models;
using Playnite.SDK;

namespace PlayniteSounds.Common.Utilities
{
    public static class UIUtilities
    {
        public static string GenerateTitle(GlobalProgressActionArgs args, Game game, string progressTitle)
            => $"{progressTitle}\n\n{++args.CurrentProgressValue}/{args.ProgressMaxValue}\n{game.Name}";
    }
}
