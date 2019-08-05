using CitizenFX.Core;
using System;

namespace PocceMod.Shared
{
    public class Rope
    {
        [Flags]
        public enum ModeFlag
        {
            Normal = 0,
            Tow = 1,
            Ropegun = 2,
            Grapple = 4
        }

        public Rope(Player player, int entity1, int entity2, Vector3 offset1, Vector3 offset2, ModeFlag mode)
        {
            Player = player;
            Entity1 = entity1;
            Entity2 = entity2;
            Offset1 = offset1;
            Offset2 = offset2;
            Mode = mode;
        }

        public Player Player { get; private set; }
        public int Entity1 { get; private set; }
        public int Entity2 { get; private set; }
        public Vector3 Offset1 { get; private set; }
        public Vector3 Offset2 { get; private set; }
        public ModeFlag Mode { get; private set; }
        
        public virtual void Update()
        {
        }

        public virtual void Clear()
        {
        }
    }
}
