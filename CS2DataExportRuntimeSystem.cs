using System;
using Game;

namespace CS2DataExport
{
    public sealed partial class CS2DataExportRuntimeSystem : GameSystemBase
    {
        protected override void OnUpdate()
        {
            Mod? mod = Mod.TryGetInstance();
            if (mod == null)
            {
                return;
            }

            mod.SetRuntimeContext(EntityManager, World);
            mod.OnUpdate(DateTimeOffset.UtcNow);
        }
    }
}

