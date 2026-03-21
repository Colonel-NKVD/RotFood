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

            // Уникальный ключ по координатам
            Vector3 pos = __instance.transform.position;
            string storageKey = $"str_{pos.x:F1}_{pos.y:F1}_{pos.z:F1}";
            
            float multiplier = 1.0f;

            // Современный способ получения данных баррикады
            BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(__instance.transform);
            if (drop != null)
            {
                ushort storageId = drop.asset.id;
                if (RotFood.Instance.Configuration.Instance.FridgeIds.Contains(storageId))
                {
                    multiplier = RotFood.Instance.Configuration.Instance.FridgeDecayMultiplier;
                }
            }

            RotFood.Instance.ProcessDecay(__instance.items, storageKey, multiplier);
        }
    }
}
