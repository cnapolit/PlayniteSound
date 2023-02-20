using Playnite.SDK.Models;
using Playnite.SDK;
using PlayniteSounds.Common.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlayniteSounds.Services.Play
{
    public class Tagger : ITagger
    {
        #region Infrastructure

        private static readonly ILogger      logger = LogManager.GetLogger();
        private        readonly IPlayniteAPI _api;

        public Tagger(IPlayniteAPI api) => _api = api;

        #endregion

        #region Implementation

        #region UpdateMissingTag

        public void UpdateMissingTag(Game game, bool fileCreated, string gameDirectory)
        {
            var missingTag = _api.Database.Tags.Add(Resource.MissingTag);

            if (fileCreated)
            {
                RemoveTagFromGame(game, missingTag);
            }
            else if (!Directory.Exists(gameDirectory) || !Directory.GetFiles(gameDirectory).Any())
            {
                AddTagToGame(game, missingTag);
            }
        }

        #endregion

        #region AddMissingTag

        public void AddMissingTag(Game game)
            => AddTagToGame(game, _api.Database.Tags.Add(Resource.MissingTag));

        #endregion

        #region AddNormalizedTag

        public void AddNormalizedTag(Game game)
        {
            var normalizedTag = _api.Database.Tags.Add(Resource.NormTag);
            AddTagToGame(game, normalizedTag);
        }

        #endregion

        #region Helpers

        private void AddTagToGame(Game game, Tag tag)
        {
            var tagAdded = false;
            if (game.Tags is null)
            {
                game.TagIds = new List<Guid> { tag.Id };
                _api.Database.Games.Update(game);
                tagAdded = true;
            }
            else if (!game.TagIds.Contains(tag.Id))
            {
                game.TagIds.Add(tag.Id);
                _api.Database.Games.Update(game);
                tagAdded = true;
            }

            if (tagAdded)
            {
                logger.Info($"Added tag '{tag.Name}' to '{game.Name}'");
            }
        }

        private void RemoveTagFromGame(Game game, Tag tag)
        {
            if (game.Tags != null && game.TagIds.Remove(tag.Id))
            {
                _api.Database.Games.Update(game);
                logger.Info($"Removed tag '{tag.Name}' from '{game.Name}'");
            }
        }

        #endregion

        #endregion
    }
}
