using Game;
using Game.Modding;

namespace CS2DataExport
{
    public sealed partial class Mod : IMod
    {
        private static Mod? s_instance;

        internal static Mod? TryGetInstance()
        {
            return s_instance;
        }

        void IMod.OnLoad(UpdateSystem updateSystem)
        {
            s_instance = this;
            updateSystem.UpdateAt<CS2DataExportRuntimeSystem>(SystemUpdatePhase.GameSimulation);
            OnLoad();
        }

        void IMod.OnDispose()
        {
            OnDispose();
            s_instance = null;
        }
    }
}
