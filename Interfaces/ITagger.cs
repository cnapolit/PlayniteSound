using Playnite.SDK.Models;

namespace PlayniteSounds.Services.Play
{
    public interface ITagger
    {
        void AddMissingTag(Game game);
        void UpdateMissingTag(Game game, bool fileCreated, string gameDirectory);
        void AddNormalizedTag(Game game);
    }
}
