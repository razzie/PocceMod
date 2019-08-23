using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    internal class RopeWrapper : Shared.Rope
    {
        internal static int RootObject { get; private set; }
        private int _handle;
        private float _length;

        public static async Task<Shared.Rope> Create(int player, int netEntity1, int netEntity2, Vector3 offset1, Vector3 offset2, ModeFlag mode)
        {
            if (RootObject == 0)
            {
                var model = (uint)API.GetHashKey("prop_devin_rope_01");
                await Common.RequestModel(model);
                RootObject = API.CreateObject((int)model, 0f, 0f, 0f, false, false, false);
                API.SetModelAsNoLongerNeeded(model);
                API.FreezeEntityPosition(RootObject, true);
            }

            var entity1 = (netEntity1 == 0) ? RootObject : API.NetToEnt(netEntity1);
            var entity2 = (netEntity2 == 0) ? RootObject : API.NetToEnt(netEntity2);
            DateTime? timeout = null;

            if (entity1 == RootObject && entity2 == RootObject)
                timeout = DateTime.Now + TimeSpan.FromMinutes(1);

            if (!API.DoesEntityExist(entity1) || !API.DoesEntityExist(entity2))
                return new RopeWrapperDelayed(player, netEntity1, netEntity2, entity1, entity2, offset1, offset2, mode) { Timeout = timeout };
            else
                return new RopeWrapper(player, entity1, entity2, offset1, offset2, mode) { Timeout = timeout };
        }

        internal RopeWrapper(int player, int entity1, int entity2, Vector3 offset1, Vector3 offset2, ModeFlag mode) : base(new Player(player), entity1, entity2, offset1, offset2, mode)
        {
            GetWorldCoords(out Vector3 pos1, out Vector3 pos2);
            _length = (pos1 - pos2).Length();

            int unkPtr = 0;
            var ropePos = pos1;
            _handle = API.AddRope(ropePos.X, ropePos.Y, ropePos.Z, 0f, 0f, 0f, _length, 1, _length, 1f, 0f, false, false, false, 5f, true, ref unkPtr);
            Attach(pos1, pos2);

            if ((Mode & ModeFlag.Grapple) == ModeFlag.Grapple)
                API.StartRopeWinding(_handle);
        }

        public bool Exists
        {
            get
            {
                int rope = _handle;
                return API.DoesRopeExist(ref rope);
            }
        }

        public DateTime? Timeout { get; set; } = null;

        private void GetWorldCoords(out Vector3 pos1, out Vector3 pos2)
        {
            pos1 = API.GetOffsetFromEntityInWorldCoords(Entity1, Offset1.X, Offset1.Y, Offset1.Z);
            pos2 = API.GetOffsetFromEntityInWorldCoords(Entity2, Offset2.X, Offset2.Y, Offset2.Z);
        }

        private void Attach(Vector3 pos1, Vector3 pos2)
        {
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

            if (Timeout != null && Timeout < DateTime.Now)
            {
                Clear();
                return;
            }

            var length = API.GetRopeLength(_handle);

            // if length is negative, rope is detached
            if (length < 0f)
            {
                GetWorldCoords(out Vector3 pos1, out Vector3 pos2);
                Attach(pos1, pos2);
            }
            else if (length > _length * 4)
            {
                GetWorldCoords(out Vector3 pos1, out Vector3 pos2);

                if (Entity1 != 0)
                {
                    var dir = pos2 - pos1;
                    API.DetachEntity(Entity1, false, false);
                    API.ApplyForceToEntityCenterOfMass(Entity1, 1, dir.X, dir.Y, dir.Z, false, false, true, false);
                }

                if (Entity2 != 0)
                {
                    var dir = pos1 - pos2;
                    API.DetachEntity(Entity2, false, false);
                    API.ApplyForceToEntityCenterOfMass(Entity2, 1, dir.X, dir.Y, dir.Z, false, false, true, false);
                }
            }

            if ((Mode & ModeFlag.Grapple) == ModeFlag.Grapple && _length > 1f)
            {
                _length -= 0.2f;
                API.RopeForceLength(_handle, _length);
            }
        }

        public override void Clear()
        {
            API.DeleteRope(ref _handle);
            _handle = -1;
        }
    }
}
