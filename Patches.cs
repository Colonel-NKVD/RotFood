using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace RotFood
{
    [HarmonyPatch(typeof(InteractableStorage), "askOpen")]
    public static class StoragePatch
    {
        [HarmonyPrefix]
        public static void Prefix(InteractableStorage __instance)
        {
            if (__instance == null || __instance.items == null) return;

            string storageKey = $"str_{__instance.transform.position.ToString()}";
            float multiplier = 1.0f;

            // У InteractableStorage нет .id, но есть .asset.id
            if (__instance.asset != null && RotFood.Instance.Configuration.Instance.FridgeIds.Contains(__instance.asset.id))
            {
                multiplier = RotFood.Instance.Configuration.Instance.FridgeDecayMultiplier;
            }

            RotFood.Instance.ProcessDecay(__instance.items, storageKey, multiplier);
        }
    }
}
