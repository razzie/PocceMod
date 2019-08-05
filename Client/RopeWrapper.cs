using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;

namespace PocceMod.Client
{
    internal class RopeWrapper : Shared.Rope
    {
        private int _handle;
        private float _length;
        private readonly Scenario _scenario;

        private enum Scenario
        {
            EntityToEntity,
            EntityToGround,
            GroundToEntity,
            GroundToGround
        }

        public RopeWrapper(Player player, int entity1, int entity2, Vector3 offset1, Vector3 offset2, ModeFlag mode) : base(player, entity1, entity2, offset1, offset2, mode)
        {
            _scenario = GetScenario(entity1, entity2);

            GetWorldCoords(out Vector3 pos1, out Vector3 pos2);
            _length = (pos1 - pos2).Length();

            int unkPtr = 0;
            var ropePos = (_scenario == Scenario.EntityToGround) ? pos2 : pos1;
            _handle = API.AddRope(ropePos.X, ropePos.Y, ropePos.Z, 0f, 0f, 0f, _length, 1, _length, 1f, 0f, false, false, false, 5f, true, ref unkPtr);
            Attach(pos1, pos2);

            if ((Mode & ModeFlag.Grapple) == ModeFlag.Grapple)
                API.StartRopeWinding(_handle);

            Created = DateTime.Now;
        }

        public bool Exists
        {
            get
            {
                int rope = _handle;
                return API.DoesRopeExist(ref rope);
            }
        }

        public DateTime Created { get; }

        private static Scenario GetScenario(int entity1, int entity2)
        {
            if (entity1 > 0 && entity2 > 0)
                return Scenario.EntityToEntity;
            else if (entity1 > 0)
                return Scenario.EntityToGround;
            else if (entity2 > 0)
                return Scenario.GroundToEntity;
            else
                return Scenario.GroundToGround;
        }

        private void GetWorldCoords(out Vector3 pos1, out Vector3 pos2)
        {
            if (_scenario == Scenario.EntityToEntity || _scenario == Scenario.EntityToGround)
                pos1 = API.GetOffsetFromEntityInWorldCoords(Entity1, Offset1.X, Offset1.Y, Offset1.Z);
            else
                pos1 = Offset1;

            if (_scenario == Scenario.EntityToEntity || _scenario == Scenario.GroundToEntity)
                pos2 = API.GetOffsetFromEntityInWorldCoords(Entity2, Offset2.X, Offset2.Y, Offset2.Z);
            else
                pos2 = Offset2;
        }

        private void Attach(Vector3 pos1, Vector3 pos2)
        {
            switch (_scenario)
            {
                case Scenario.EntityToEntity:
                    string bone1 = null;
                    string bone2 = null;
                    if (API.IsEntityAPed(Entity1))
                    {
                        pos1 = Vector3.Zero;
                        bone1 = "SKEL_ROOT"; // "SKEL_R_Hand";
                    }
                    if (API.IsEntityAPed(Entity2))
                    {
                        pos2 = Vector3.Zero;
                        bone2 = "SKEL_ROOT"; // "SKEL_R_Hand";
                    }
                    API.AttachEntitiesToRope(_handle, Entity1, Entity2, pos1.X, pos1.Y, pos1.Z, pos2.X, pos2.Y, pos2.Z, _length, false, false, bone1, bone2);
                    break;

                case Scenario.EntityToGround:
                    API.AttachRopeToEntity(_handle, Entity1, pos1.X, pos1.Y, pos1.Z, true);
                    API.PinRopeVertex(_handle, API.GetRopeVertexCount(_handle) - 1, pos2.X, pos2.Y, pos2.Z);
                    break;

                case Scenario.GroundToEntity:
                    API.AttachRopeToEntity(_handle, Entity2, pos2.X, pos2.Y, pos2.Z, true);
                    API.PinRopeVertex(_handle, API.GetRopeVertexCount(_handle) - 1, pos1.X, pos1.Y, pos1.Z);
                    break;

                case Scenario.GroundToGround:
                    API.PinRopeVertex(_handle, 0, pos1.X, pos1.Y, pos1.Z);
                    API.PinRopeVertex(_handle, API.GetRopeVertexCount(_handle) - 1, pos2.X, pos2.Y, pos2.Z);
                    break;
            }
        }

        public override void Update()
        {
            if (_handle == -1)
                return;

            // if a rope is shot, it ceases to exist
            if (!Exists)
            {
                Clear();
                return;
            }

            if ((Entity1 != 0 && !API.DoesEntityExist(Entity1)) ||
                (Entity2 != 0 && !API.DoesEntityExist(Entity2)))
            {
                Clear();
                return;
            }

            if (_scenario == Scenario.GroundToGround)
            {
                if (Created + TimeSpan.FromMinutes(1) < DateTime.Now)
                {
                    Clear();
                    return;
                }
            }
            else
            {
                // if length is negative, rope is detached
                if (API.GetRopeLength(_handle) < 0f)
                {
                    GetWorldCoords(out Vector3 pos1, out Vector3 pos2);
                    Attach(pos1, pos2);
                }

                if ((Mode & ModeFlag.Grapple) == ModeFlag.Grapple && _length > 1f)
                {
                    _length -= 0.2f;
                    API.RopeForceLength(_handle, _length);
                }
            }
        }

        public override void Clear()
        {
            API.DeleteRope(ref _handle);
            _handle = -1;
        }
    }
}
