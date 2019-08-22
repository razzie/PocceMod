using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;

namespace PocceMod.Client
{
    public class RopeWrapperDelayed : Shared.Rope
    {
        private readonly int _netEntity1;
        private readonly int _netEntity2;
        private bool _tryResolve1;
        private bool _tryResolve2;
        private DateTime _retry;
        private RopeWrapper _rope;

        public RopeWrapperDelayed(int player, int netEntity1, int netEntity2, int entity1, int entity2, Vector3 offset1, Vector3 offset2, ModeFlag mode) : base(new Player(player), entity1, entity2, offset1, offset2, mode)
        {
            _netEntity1 = netEntity1;
            _netEntity2 = netEntity2;

            _tryResolve1 = _netEntity1 != 0;
            _tryResolve2 = _netEntity2 != 0;

            Retry();
        }

        private void Retry()
        {
            _retry = DateTime.Now + TimeSpan.FromSeconds(10);
        }

        public override void Update()
        {
            if (_rope != null)
            {
                _rope.Update();
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

                _rope = new RopeWrapper(Player.Handle, Entity1, Entity2, Offset1, Offset2, Mode);
            }
        }

        public override void Clear()
        {
            _rope?.Clear();
        }
    }
}
