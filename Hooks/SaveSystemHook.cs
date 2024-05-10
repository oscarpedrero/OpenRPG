using HarmonyLib;
using ProjectM;
using OpenRPG.Utils;

namespace OpenRPG.Hooks
{
    [HarmonyPatch(typeof(TriggerPersistenceSaveSystem), nameof(TriggerPersistenceSaveSystem.TriggerSave))]
    public class TriggerPersistenceSaveSystem_Patch
    {
        public static void Postfix()
        {
            AutoSaveSystem.SaveDatabase(false, false);
        }
    }
}
