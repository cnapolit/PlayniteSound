using System.Collections.Generic;
using Playnite.SDK.Models;
using PlayniteSounds.Models;

namespace PlayniteSounds.Services.State
{
    public interface IItemCollectionChangedHandler
    {
        bool ExtraMetaDataPluginIsLoaded { get; set; }
        void AddGames(IList<Game> games, Source? source);
    }
}
