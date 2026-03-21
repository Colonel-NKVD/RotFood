using SDG.Unturned;
using UnityEngine;
using HarmonyLib;

namespace RotFood
{
    public static class StoragePatch
    {
        public static void Prefix(InteractableStorage __instance)
        {
            if (__instance == null || __instance.items == null) return;

            Vector3 pos = __instance.transform.position;
            string storageKey = $"str_{Mathf.RoundToInt(pos.x)}_{Mathf.RoundToInt(pos.y)}_{Mathf.RoundToInt(pos.z)}";
            
            float multiplier = 1.0f;

            var drop = BarricadeManager.FindBarricadeByRootTransform(__instance.transform);
            if (drop != null && drop.asset != null)
            {
                if (RotFood.Instance.Configuration.Instance.FridgeIds.Contains(drop.asset.id))
                {
                    multiplier = RotFood.Instance.Configuration.Instance.FridgeDecayMultiplier;
                }
            }

            // Изменено название метода
            RotFood.Instance.ProcessStorageDecay(__instance.items, storageKey, multiplier);
        }
    }
}
