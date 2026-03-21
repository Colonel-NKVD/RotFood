using HarmonyLib;
using SDG.Unturned;

namespace RotFood
{
    [HarmonyPatch(typeof(InteractableStorage), "askOpen")]
    public static class StoragePatch
    {
        [HarmonyPrefix]
        public static void Prefix(InteractableStorage __instance)
        {
            if (__instance == null || __instance.items == null) return;

            string storageKey = $"str_{__instance.transform.position.x}_{__instance.transform.position.y}_{__instance.transform.position.z}";
            float multiplier = 1.0f;

            // Правильный способ получить ID для InteractableStorage
            ushort storageId = __instance.id;
            
            if (RotFood.Instance.Configuration.Instance.FridgeIds.Contains(storageId))
            {
                multiplier = RotFood.Instance.Configuration.Instance.FridgeDecayMultiplier;
            }

            RotFood.Instance.ProcessDecay(__instance.items, storageKey, multiplier);
        }
    }
}
