using CitizenFX.Core;
using System;

namespace PocceMod.Client
{
    internal class RopegunState
    {
        private bool _firstUse;
        private int _lastEntity1;
        private int _lastEntity2;
        private Vector3 _lastOffset1;
        private Vector3 _lastOffset2;
        private DateTime _lastFire;

        public RopegunState()
        {
            _firstUse = true;
            Clear();
        }

        public void Update(ref int entity1, ref int entity2, ref Vector3 offset1, ref Vector3 offset2, out bool clearLast)
        {
            clearLast = false;

            var player = Common.GetPlayerPedOrVehicle();
            if (entity1 != player && entity2 != player)
                return;

            if (_firstUse)
            {
                _firstUse = false;
                Common.Notification("First time using ropegun, yay! You can connect 2 entities in 2 seconds");

                if (Ropes.RopegunWindKey.Exists)
                    Common.Notification(string.Format("Use {0} key for grappling", Ropes.RopegunWindKey.Label));
            }

            var timestamp = DateTime.Now;
            if (_lastEntity1 != -1 && _lastEntity2 != -1 && (timestamp - _lastFire) < TimeSpan.FromSeconds(2f))
            {
                clearLast = (_lastEntity1 == player || _lastEntity2 == player);

                if (entity1 == player)
                {
                    entity1 = _lastEntity2;
                    offset1 = _lastOffset2;
                }
                else
                {
                    entity2 = _lastEntity2;
                    offset2 = _lastOffset2;
                }
            }

            _lastEntity1 = entity1;
            _lastEntity2 = entity2;
            _lastOffset1 = offset1;
            _lastOffset2 = offset2;
            _lastFire = timestamp;
        }

        public void Undo(out bool clearLast)
        {
            var player = Common.GetPlayerPedOrVehicle();
            clearLast = (_lastEntity1 == player || _lastEntity2 == player);
            Clear();
        }

        public void Clear()
        {
            _lastEntity1 = -1;
            _lastEntity2 = -1;
            _lastOffset1 = Vector3.Zero;
            _lastOffset2 = Vector3.Zero;
            _lastFire = DateTime.MinValue;
        }
    }
}
