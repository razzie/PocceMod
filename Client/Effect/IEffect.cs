using System.Threading.Tasks;

namespace PocceMod.Client.Effect
{
    public interface IEffect
    {
        string Key { get; }
        bool Expired { get; }
        Task Init();
        void Update();
        void Clear();
    }
}
