using System.Collections.Generic;

namespace PocceMod.Client
{
    public class SkinSet
    {
        private readonly Dictionary<string, List<Skin>> _skins = new Dictionary<string, List<Skin>>();

        public IEnumerable<KeyValuePair<string, List<Skin>>> Skins
        {
            get { return _skins; }
        }

        public int Count
        {
            get { return _skins.Count; }
        }

        public void Add(Skin skin)
        {
            if (_skins.TryGetValue(skin.Name, out List<Skin> list))
            {
                foreach (var item in list)
                {
                    if (item.Equals(skin))
                        return;
                }

                list.Add(skin);
            }
            else
            {
                _skins.Add(skin.Name, new List<Skin> { skin });
            }
        }

        public void Add(IEnumerable<Skin> skins)
        {
            foreach (var skin in skins)
            {
                Add(skin);
            }
        }

        public void Add(string model)
        {
            if (!_skins.ContainsKey(model))
                _skins.Add(model, new List<Skin>());
        }

        public void Clear()
        {
            _skins.Clear();
        }
    }
}
