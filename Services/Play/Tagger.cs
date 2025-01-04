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

        public bool AddTag(Game game, string tagName) => AddTag(game, _databaseApi.Tags.Add(tagName));

        private bool AddTag(Game game, Tag tag)
        {
            if (game.Tags is null)
            {
                game.TagIds = new List<Guid> { tag.Id };
                LogAdd(game, tag.Name);
                return true;
            }

            if (!game.TagIds.Contains(tag.Id))
            {
                game.TagIds.Add(tag.Id);
                LogAdd(game, tag.Name);
                return true;
            }

            return false;
        }

        private void LogAdd(Game game, string tagName) 
            => _logger.Info($"Added tag '{tagName}' to game '{game.Name}'");

        public bool RemoveTag(Game game, string tagName) => RemoveTag(game, _databaseApi.Tags.Add(tagName));

        private bool RemoveTag(Game game, Tag tag)
        {
            if (game.Tags != null && game.TagIds.Remove(tag.Id))
            {
                _logger.Info($"Removed tag '{tag.Name}' from game '{game.Name}'");
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

        public void AddTag(IEnumerable<Game> games, string tagName) => UpdateTag(games, tagName, AddTag);

        public void RemoveTag(IEnumerable<Game> games, string tagName) => UpdateTag(games, tagName, RemoveTag);

        private void UpdateTag(IEnumerable<Game> games, string tagName, Func<Game, Tag, bool> tagFunc)
            => _databaseApi.Games.Update(games.Where(g => tagFunc(g, _databaseApi.Tags.Add(tagName))));

        #endregion
    }
}
