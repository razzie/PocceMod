using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;
using System;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    internal class RopeWrapper : IRope
    {
        public delegate void OnDespawnDelegate(IRope rope);
        public event OnDespawnDelegate OnDespawn;

        private static int _rootObject;
        private readonly int _netEntity1;
        private readonly int _netEntity2;
        private bool _tryResolve1;
        private bool _tryResolve2;
        private DateTime _retry;
        private Rope _rope;
        private float _initialLength;
        private float _length;
        private bool _showPoolErrMsg;

        private RopeWrapper(string player, int id, int netEntity1, int netEntity2, Vector3 offset1, Vector3 offset2, float length)
        {
            Player = player;
            ID = id;
            Offset1 = offset1;
            Offset2 = offset2;
            _initialLength = length;
            _length = length;

            _netEntity1 = netEntity1;
            _netEntity2 = netEntity2;

            if (_netEntity1 == 0)
                Entity1 = _rootObject;
            else
                _tryResolve1 = true;

            if (_netEntity2 == 0)
                Entity2 = _rootObject;
            else
                _tryResolve2 = true;

            _showPoolErrMsg = true;
        }

        public static async Task<RopeWrapper> Create(string player, int id, int netEntity1, int netEntity2, Vector3 offset1, Vector3 offset2, float length)
        {
            if (_rootObject == 0)
            {
                var model = (uint)API.GetHashKey("prop_devin_rope_01");
                await Common.RequestModel(model);
                _rootObject = API.CreateObject((int)model, 0f, 0f, 0f, false, false, false);
                API.SetModelAsNoLongerNeeded(model);
                API.FreezeEntityPosition(_rootObject, true);
            }

            return new RopeWrapper(player, id, netEntity1, netEntity2, offset1, offset2, length);
        }

        public string Player { get; }
        public int ID { get; }
        public int Entity1 { get; private set; }
        public int Entity2 { get; private set; }
        public Vector3 Offset1 { get; }
        public Vector3 Offset2 { get; }
        public float Length
        {
            get
            {
                return _length;
            }

            set
            {
                _length = value;

                if (_rope != null)
                    _rope.Length = value;
            }
        }

        private void Retry()
        {
            _retry = DateTime.Now + TimeSpan.FromSeconds(2);
        }

        public void Update()
        {
            if (_rope != null)
            {
                _rope.Update();

                if (!_rope.Exists)
                {
                    var tmpRope = _rope;
                    _rope = null;
                    Retry();
                    OnDespawn?.Invoke(tmpRope);
                }

                return;
            }

            if (DateTime.Now > _retry)
            {
                if (_tryResolve1)
                {
                    Entity1 = API.NetToEnt(_netEntity1);

                    if (!API.DoesEntityExist(Entity1))
                    {
                        Retry();
                        return;
                    }

                    _tryResolve1 = false;
                }

                if (_tryResolve2)
                {
                    Entity2 = API.NetToEnt(_netEntity2);

                    if (!API.DoesEntityExist(Entity2))
                    {
                        Retry();
                        return;
                    }

                    _tryResolve2 = false;
                }

                if (_netEntity1 == 0 && _netEntity2 == 0)
                {
                    var playerCoords = API.GetEntityCoords(API.GetPlayerPed(-1), true);
                    const float minDistSquared = 10000f;

                    if (Offset1.DistanceToSquared(playerCoords) > minDistSquared ||
                        Offset2.DistanceToSquared(playerCoords) > minDistSquared)
                    {
                        Retry();
                        return;
                    }
                }

                try
                {
                    _rope = new Rope(Player, ID, Entity1, Entity2, Offset1, Offset2, _initialLength);
                    _rope.Length = _length;
                    _showPoolErrMsg = true;
                }
                catch (Exception)
                {
                    if (_showPoolErrMsg)
                    {
                        Debug.WriteLine("[PocceMod] no rope available in pool.. waiting");
                        _showPoolErrMsg = false;
                    }

                    Retry();
                }
            }
        }

        public void Clear()
        {
            _rope?.Clear();
        }
    }
}
