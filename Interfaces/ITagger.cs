using Playnite.SDK.Models;

namespace PlayniteSounds.Services.Play
{
    internal interface ITagger
    {
        void AddMissingTag(Game game);
        void UpdateMissingTag(Game game, bool fileCreated, string gameDirectory);
        void AddNormalizedTag(Game game);
    }
}
