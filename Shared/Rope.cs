using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;

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

#if CLIENT
            GetWorldCoords(out Vector3 pos1, out Vector3 pos2);
            _length = (pos1 - pos2).Length();

            int unkPtr = 0;
            _handle = API.AddRope(pos1.X, pos1.Y, pos1.Z, 0f, 0f, 0f, _length, 1, _length, 1f, 0f, false, false, false, 5f, true, ref unkPtr);
            API.AttachEntitiesToRope(_handle, Entity1, Entity2, pos1.X, pos1.Y, pos1.Z, pos2.X, pos2.Y, pos2.Z, _length, false, false, null, null);
#endif
        }

        public Player Player { get; private set; }
        public int Entity1 { get; private set; }
        public int Entity2 { get; private set; }
        public Vector3 Offset1 { get; private set; }
        public Vector3 Offset2 { get; private set; }
        public ModeFlag Mode { get; private set; }

#if CLIENT
        private int _handle;
        private float _length;

        private void GetWorldCoords(out Vector3 pos1, out Vector3 pos2)
        {
            pos1 = API.GetOffsetFromEntityInWorldCoords(Entity1, Offset1.X, Offset1.Y, Offset1.Z);
            pos2 = API.GetOffsetFromEntityInWorldCoords(Entity2, Offset2.X, Offset2.Y, Offset2.Z);
        }

        public void Update()
        {
            // if a rope is shot, it ceases to exist
            var rope = _handle;
            if (!API.DoesRopeExist(ref rope))
                return;

            // if length is negative, rope is detached
            if (API.GetRopeLength(_handle) < 0f)
            {
                GetWorldCoords(out Vector3 pos1, out Vector3 pos2);
                API.AttachEntitiesToRope(_handle, Entity1, Entity2, pos1.X, pos1.Y, pos1.Z, pos2.X, pos2.Y, pos2.Z, _length, false, false, null, null);
            }

            if ((Mode & ModeFlag.Grapple) == ModeFlag.Grapple && _length > 1f)
            {
                _length -= 0.2f;
                API.RopeForceLength(_handle, _length);
            }
        }

        public void Clear()
        {
            API.DeleteRope(ref _handle);
        }
#else
        public void Clear()
        {
        }
#endif
    }

    public class RopeSet
    {
        private readonly Dictionary<Player, List<Rope>> _ropes = new Dictionary<Player, List<Rope>>();

        public Rope[] GetRopes() // too bad iterator blocks cannot be used in async methods
        {
            return _ropes.Values.SelectMany(ropes => ropes).ToArray();
        }

        public void AddRope(Rope rope)
        {
            if (_ropes.TryGetValue(rope.Player, out List<Rope> playerRopes))
                playerRopes.Add(rope);
            else
                _ropes.Add(rope.Player, new List<Rope> { rope });
        }

        public void ClearRopes(Player player)
        {
            if (_ropes.TryGetValue(player, out List<Rope> playerRopes))
            {
                foreach (var rope in playerRopes)
                {
                    rope.Clear();
                }

                playerRopes.Clear();
            }
        }

        public void ClearLastRope(Player player)
        {
            if (_ropes.TryGetValue(player, out List<Rope> playerRopes))
            {
                if (playerRopes.Count == 0)
                    return;
                
                var rope = playerRopes[playerRopes.Count - 1];
                rope.Clear();

                playerRopes.RemoveAt(playerRopes.Count - 1);
            }
        }
    }
}
