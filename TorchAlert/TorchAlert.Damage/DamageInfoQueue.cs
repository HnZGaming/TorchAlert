using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Sandbox.Game.GameSystems;
using Utils.General;
using VRage.Game.ModAPI;

namespace TorchAlert.Damage
{
    public sealed class DamageInfoQueue
    {
        readonly ConcurrentQueue<DamageInfo> _damageInfoQueue;

        public DamageInfoQueue()
        {
            _damageInfoQueue = new ConcurrentQueue<DamageInfo>();
        }

        public void Initialize()
        {
            MyDamageSystem.Static.RegisterAfterDamageHandler(0, (o, i) => OnDamage(o, i));
        }

        // must be super light
        void OnDamage(object o, MyDamageInformation i)
        {
            if (o is IMySlimBlock block)
            {
                var grid = block.CubeGrid;
                if (grid.BigOwners.Count == 0) return; // exceptional filter

                var info = DamageInfo.Pool.Instance.UnpoolOrCreate();
                info.Initialize(grid.BigOwners[0], grid.EntityId, grid.DisplayName, i.AttackerId);
                _damageInfoQueue.Enqueue(info);
            }
        }

        public IDisposable DequeueDamageInfos(out IEnumerable<DamageInfo> damageInfos)
        {
            var disposable = new DisposableCollection();
            damageInfos = new List<DamageInfo>();
            while (_damageInfoQueue.TryDequeue(out var damageInfo))
            {
                damageInfo.AddedTo(disposable);
                ((List<DamageInfo>) damageInfos).Add(damageInfo);
            }

            return disposable;
        }

        public void Clear()
        {
            while (_damageInfoQueue.TryDequeue(out var damageInfo))
            {
                damageInfo.Dispose();
            }
        }
    }
}