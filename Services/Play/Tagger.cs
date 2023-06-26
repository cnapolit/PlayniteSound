using Playnite.SDK.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayniteSounds.Services.Play
{
    public class Tagger : ITagger
    {
        #region Infrastructure

        private readonly ILogger          _logger;
        private readonly IGameDatabaseAPI _databaseApi;

        public Tagger(IGameDatabaseAPI databaseApi, ILogger logger)
        {
            _logger = logger;
            _databaseApi = databaseApi;
        }

        #endregion

        #region Implementation

        public bool AddTag(Game game, string tagName)
        {
            var tag = _databaseApi.Tags.Add(tagName);
            if (game.Tags is null)
            {
                game.TagIds = new List<Guid> { tag.Id };
                LogAdd(game, tagName);
                return true;
            }
            else if (!game.TagIds.Contains(tag.Id))
            {
                game.TagIds.Add(tag.Id);
                LogAdd(game, tagName);
                return true;
            }

            return false;
        }

        private void LogAdd(Game game, string tagName) 
            => _logger.Info($"Added tag '{tagName}' to game '{game.Name}'");

        public bool RemoveTag(Game game, string tagName)
        {
            var tag = _databaseApi.Tags.Add(tagName);
            if (game.Tags != null && game.TagIds.Remove(tag.Id))
            {
                _logger.Info($"Removed tag '{tagName}' from game '{game.Name}'");
                return true;
            }

            return false;
        }

        public void UpdateGames(IEnumerable<Game> games) => UpdateGames(games.ToList());
        public void UpdateGames(IList<Game> games)
        {
            _databaseApi.Games.Update(games);
            _logger.Info($"Updated the tags of {games.Count} games: {games.Select(g => g.Name)}");
        }

        #endregion
    }
}
