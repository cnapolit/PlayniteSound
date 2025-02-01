using Playnite.SDK;

namespace PlayniteSounds.Models;

public class GenericObjectOption(string name, string description, object obj) : GenericItemOption(name, description)
{
    public Source Source { get; set; }
    public object Object { get; set; } = obj;
}