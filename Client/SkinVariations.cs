using System.Collections.Generic;

namespace PocceMod.Client
{
    public class SkinVariations
    {
        public SkinVariations(KeyValuePair<string, List<Skin>> pair)
        {
            Model = pair.Key;
            Skins = pair.Value;
        }

        public string Model { get; }
        public List<Skin> Skins { get; }
    }
}
