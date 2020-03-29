using CitizenFX.Core;

namespace PocceMod.Shared
{
    public interface IRope
    {
        // Player and ID form a unique key together
        string Player { get; }
        int ID { get; }

        int Entity1 { get; }
        int Entity2 { get; }
        Vector3 Offset1 { get; }
        Vector3 Offset2 { get; }
        float Length { get; set; }

        void Update();
        void Clear();
    }
}
