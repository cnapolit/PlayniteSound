using Playnite.SDK;

namespace PlayniteSounds.Models
{
    public class GenericObjectOption : GenericItemOption
    {
        public Source Source { get; set; }
        public object Object { get; set; }

        public GenericObjectOption(string name, string description, object obj) : base(name, description) => Object = obj;
    }
}
