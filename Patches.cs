using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace RotFood
{
    // Используем ручной метод патчинга или проверяем наличие метода.
    // В новых версиях SDG запрос на открытие идет через ReceiveOpenStorageRequest
    [HarmonyPatch(typeof(InteractableStorage))]
    [HarmonyPatch("ReceiveOpenStorageRequest")] 
    public static class StoragePatch
    {
        [HarmonyPrefix]
        public static void Prefix(InteractableStorage __instance)
        {
            // Если этот метод не найден в твоей версии, Harmony выдаст ту же ошибку.
            // Проверяем объект и наличие предметов
            if (__instance == null || __instance.items == null) return;

            Vector3 pos = __instance.transform.position;
            // Используем координаты как ключ (округляем для стабильности)
            string storageKey = $"str_{Mathf.RoundToInt(pos.x)}_{Mathf.RoundToInt(pos.y)}_{Mathf.RoundToInt(pos.z)}";
            
            float multiplier = 1.0f;

            // Получаем данные баррикады
            var drop = BarricadeManager.FindBarricadeByRootTransform(__instance.transform);
            if (drop != null && drop.asset != null)
            {
                if (RotFood.Instance.Configuration.Instance.FridgeIds.Contains(drop.asset.id))
                {
                    multiplier = RotFood.Instance.Configuration.Instance.FridgeDecayMultiplier;
                }
            }

            RotFood.Instance.ProcessDecay(__instance.items, storageKey, multiplier);
        }
    }
}
