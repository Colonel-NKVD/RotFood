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

            // ИСПРАВЛЕНИЕ: Правильный способ получения информации о баррикаде в Unturned API
            if (BarricadeManager.tryGetInfo(__instance.transform, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion region))
            {
                if (region != null && index < region.barricades.Count)
                {
                    ushort id = region.barricades[index].barricade.asset.id;
                    if (RotFood.Instance.Configuration.Instance.FridgeIds.Contains(id))
                    {
                        multiplier = RotFood.Instance.Configuration.Instance.FridgeDecayMultiplier;
                    }
                }
            }

            RotFood.Instance.ProcessStorageDecay(__instance.items, storageKey, multiplier);
        }
    }
}
