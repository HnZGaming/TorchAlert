using System;
using Utils.General;

namespace TorchAlert.Damage
{
    public sealed class DamageInfo : IDisposable
    {
        public sealed class Pool : ObjectPool<DamageInfo>
        {
            public static readonly Pool Instance = new Pool();

            protected override DamageInfo CreateNew()
            {
                return new DamageInfo();
            }

            protected override void Reset(DamageInfo obj)
            {
                obj.Initialize();
            }
        }

        DamageInfo()
        {
        }

        public long DefenderId { get; private set; }
        public long DefenderGridId { get; private set; }
        public string DefenderGridName { get; private set; }
        public long OffenderId { get; private set; }

        void Initialize()
        {
            DefenderId = 0;
            DefenderGridId = 0;
            DefenderGridName = null;
            OffenderId = 0;
        }

        public void Initialize(long defenderId, long defenderGridId, string defenderGridName, long offenderId)
        {
            DefenderId = defenderId;
            DefenderGridId = defenderGridId;
            DefenderGridName = defenderGridName;
            OffenderId = offenderId;
        }

        public void Dispose()
        {
            Pool.Instance.Pool(this);
        }
    }
}