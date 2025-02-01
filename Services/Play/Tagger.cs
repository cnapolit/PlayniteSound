using Playnite.SDK.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayniteSounds.Services.Play;

public class Tagger(IGameDatabaseAPI databaseApi, ILogger logger) : ITagger
{
    #region Infrastructure

    #endregion

    #region Implementation

    public bool AddTag(Game game, string tagName) => AddTag(game, databaseApi.Tags.Add(tagName));

    private bool AddTag(Game game, Tag tag)
    {
        if (game.Tags is null)
        {
            game.TagIds = [tag.Id];
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
        => logger.Info($"Added tag '{tagName}' to game '{game.Name}'");

    public bool RemoveTag(Game game, string tagName) => RemoveTag(game, databaseApi.Tags.Add(tagName));

    private bool RemoveTag(Game game, Tag tag)
    {
        if (game.Tags != null && game.TagIds.Remove(tag.Id))
        {
            logger.Info($"Removed tag '{tag.Name}' from game '{game.Name}'");
            return true;
        }

        return false;
    }

    public void UpdateGames(IEnumerable<Game> games) => UpdateGames(games.ToList());
    public void UpdateGames(IList<Game> games)
    {
        databaseApi.Games.Update(games);
        logger.Info($"Updated the tags of {games.Count} games: {games.Select(g => g.Name)}");
    }

    public void AddTag(IEnumerable<Game> games, string tagName) => UpdateTag(games, tagName, AddTag);

    public void RemoveTag(IEnumerable<Game> games, string tagName) => UpdateTag(games, tagName, RemoveTag);

    private void UpdateTag(IEnumerable<Game> games, string tagName, Func<Game, Tag, bool> tagFunc)
        => databaseApi.Games.Update(games.Where(g => tagFunc(g, databaseApi.Tags.Add(tagName))));

    #endregion
}