using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;
using System;

namespace PocceMod.Client
{
    internal class Rope : IRope
    {
        private int _handle;
        private readonly string _bone1;
        private readonly string _bone2;
        private readonly int _boneIndex1;
        private readonly int _boneIndex2;

        public Rope(string player, int id, int entity1, int entity2, Vector3 offset1, Vector3 offset2, float length)
        {
            Player = player;
            ID = id;
            Entity1 = entity1;
            Entity2 = entity2;
            Offset1 = offset1;
            Offset2 = offset2;
            Length = length;

            if (API.IsEntityAPed(Entity1))
            {
                _bone1 = Peds.GetClosestPedBoneToOffset(Entity1, Offset1);
                _boneIndex1 = API.GetEntityBoneIndexByName(Entity1, _bone1);
                Offset1 = Vector3.Zero;
            }

            if (API.IsEntityAPed(Entity2))
            {
                _bone2 = Peds.GetClosestPedBoneToOffset(Entity2, Offset2);
                _boneIndex2 = API.GetEntityBoneIndexByName(Entity2, _bone2);
                Offset2 = Vector3.Zero;
            }

            _handle = RopePool.AddRope();
            Attach();
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
            if (_bone1 == null)
                pos1 = API.GetOffsetFromEntityInWorldCoords(Entity1, Offset1.X, Offset1.Y, Offset1.Z);
            else
                pos1 = API.GetPedBoneCoords(Entity1, _boneIndex1, 0f, 0f, 0f);

            if (_bone2 == null)
                pos2 = API.GetOffsetFromEntityInWorldCoords(Entity2, Offset2.X, Offset2.Y, Offset2.Z);
            else
                pos2 = API.GetPedBoneCoords(Entity2, _boneIndex2, 0f, 0f, 0f);
        }

        private void Attach()
        {
            GetWorldCoords(out Vector3 pos1, out Vector3 pos2);

            if (_bone1 != null)
                pos1 = Vector3.Zero;

            if (_bone2 != null)
                pos2 = Vector3.Zero;

            API.AttachEntitiesToRope(_handle, Entity1, Entity2, pos1.X, pos1.Y, pos1.Z, pos2.X, pos2.Y, pos2.Z, Length, false, false, _bone1, _bone2);
        }

        private void FixLongRopeBug()
        {
            GetWorldCoords(out Vector3 pos1, out Vector3 pos2);
            var dir = pos2 - pos1;

            API.DetachEntity(Entity1, false, false);
            //API.ApplyForceToEntityCenterOfMass(Entity1, 1, dir.X, dir.Y, dir.Z, false, false, true, false);
            API.ApplyForceToEntity(Entity1, 1, dir.X, dir.Y, dir.Z, Offset1.X, Offset1.Y, Offset1.Z, _boneIndex1, false, true, true, false, false);

            API.DetachEntity(Entity2, false, false);
            //API.ApplyForceToEntityCenterOfMass(Entity2, 1, dir.X, dir.Y, dir.Z, false, false, true, false);
            API.ApplyForceToEntity(Entity2, 1, -dir.X, -dir.Y, -dir.Z, Offset2.X, Offset2.Y, Offset2.Z, _boneIndex2, false, true, true, false, false);
        }

        private float GetDesiredLength()
        {
            GetWorldCoords(out Vector3 pos1, out Vector3 pos2);
            return Math.Min(API.GetRopeLength(_handle), Vector3.Distance(pos1, pos2));
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

            var length = GetDesiredLength();
            if (length < 0f) // if length is negative, rope is detached
            {
                Attach();
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
            /*else
            {
                API.StartRopeWinding(_handle);
                API.RopeForceLength(_handle, Length);
                API.RopeConvertToSimple(_handle);
            }*/
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
