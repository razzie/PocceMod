using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;

namespace PocceMod.Client
{
    internal class Rope : IRope
    {
        private int _handle;

        public Rope(string player, int id, int entity1, int entity2, Vector3 offset1, Vector3 offset2, float length)
        {
            Player = player;
            ID = id;
            Entity1 = entity1;
            Entity2 = entity2;
            Offset1 = offset1;
            Offset2 = offset2;
            Length = length;

            _handle = RopePool.AddRope();

            GetWorldCoords(out Vector3 pos1, out Vector3 pos2);
            Attach(pos1, pos2);

            API.StartRopeWinding(_handle);
            API.RopeForceLength(_handle, length);
        }

        public string Player { get; }
        public int ID { get; }
        public int Entity1 { get; }
        public int Entity2 { get; }
        public Vector3 Offset1 { get; }
        public Vector3 Offset2 { get; }
        public float Length { get; set; }

        public bool Exists
        {
            get { return _handle != -1; }
        }

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

            API.AttachEntitiesToRope(_handle, Entity1, Entity2, pos1.X, pos1.Y, pos1.Z, pos2.X, pos2.Y, pos2.Z, Length, false, false, bone1, bone2);
        }

        private void FixLongRopeBug()
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

        public void Update()
        {
            if (!Exists)
                return;

            if (!API.DoesEntityExist(Entity1) || !API.DoesEntityExist(Entity2))
            {
                Clear();
                return;
            }

            if (Length > Ropes.MaxLength)
                Length = Ropes.MaxLength;

            var length = API.GetRopeLength(_handle);
            if (length < 0f) // if length is negative, rope is detached
            {
                GetWorldCoords(out Vector3 pos1, out Vector3 pos2);
                Attach(pos1, pos2);
                return;
            }
            else if (length > Length + 0.2f) // current length > desired length : winding case
            {
                API.StopRopeUnwindingFront(_handle);
                API.StartRopeWinding(_handle);
                API.RopeForceLength(_handle, length - 0.2f);

                if (length > Length + 10f)
                    FixLongRopeBug();
            }
            else if (length < Length - 0.2f) // current length < desired length : unwinding case
            {
                API.StopRopeWinding(_handle);
                API.StartRopeUnwindingFront(_handle);
                API.RopeForceLength(_handle, length + 0.2f);
            }
            else
            {
                API.StartRopeWinding(_handle);
                API.RopeForceLength(_handle, Length);
                API.RopeConvertToSimple(_handle);
            }
        }

        public void Clear()
        {
            if (_handle == -1)
                return;

            API.DetachRopeFromEntity(_handle, Entity1);
            API.DetachRopeFromEntity(_handle, Entity2);
            RopePool.DeleteRope(ref _handle);
            _handle = -1;
        }
    }
}
