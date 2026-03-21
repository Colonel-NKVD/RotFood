using HarmonyLib;
using SDG.Unturned;
using System;

namespace RotFood
{
    [HarmonyPatch(typeof(InteractableStorage), "askOpen")]
    public static class StoragePatch
    {
        [HarmonyPrefix]
        public static void Prefix(InteractableStorage __instance)
        {
            if (__instance == null || __instance.items == null) return;

            // Определяем уникальный ключ сундука по его позиции
            string storageKey = $"storage_{__instance.transform.position.ToString()}";
            
            // Проверяем, является ли этот предмет холодильником
            float multiplier = 1.0f; // По умолчанию обычная скорость
            
            // Получаем ID предмета (барикады)
            if (RotFood.Instance.Configuration.Instance.FridgeIds.Contains(__instance.id))
            {
                multiplier = RotFood.Instance.Configuration.Instance.FridgeDecayMultiplier;
            }

            RotFood.Instance.ProcessDecay(__instance.items, storageKey, multiplier);
        }
    }
}
