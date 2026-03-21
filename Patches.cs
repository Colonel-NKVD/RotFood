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

            // Ищем данные о баррикаде через регион, в котором она находится
            if (BarricadeManager.tryGetRegion(__instance.transform, out byte x, out byte y, out ushort plant, out BarricadeRegion region))
            {
                // Ищем конкретную баррикаду по ее трансформу в этом регионе
                var data = region.barricades.Find(b => b.model == __instance.transform);
                if (data != null)
                {
                    ushort storageId = data.barricade.id;
                    if (RotFood.Instance.Configuration.Instance.FridgeIds.Contains(storageId))
                    {
                        multiplier = RotFood.Instance.Configuration.Instance.FridgeDecayMultiplier;
                    }
                }
            }

            RotFood.Instance.ProcessDecay(__instance.items, storageKey, multiplier);
        }
    }
}
