using Playnite.SDK.Models;
using System.Collections.Generic;

namespace PlayniteSounds.Services.Play;

public interface ITagger
{
    bool AddTag(Game game, string tagName);
    bool RemoveTag(Game game, string tagName);
    void UpdateGames(IEnumerable<Game> games);
    void UpdateGames(IList<Game> games);
    void AddTag(IEnumerable<Game> games, string tagName);
    void RemoveTag(IEnumerable<Game> games, string tagName);
}