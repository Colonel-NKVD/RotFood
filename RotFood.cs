using System;
using System.Collections.Generic;
using System.Linq;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using HarmonyLib;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace RotFood
{
    public class RotFood : RocketPlugin<RotFoodConfiguration>
    {
        public static RotFood Instance;
        private Harmony _harmony;
        public Dictionary<string, DateTime> LastUpdateMap = new Dictionary<string, DateTime>();

        protected override void Load()
        {
            Instance = this;
            _harmony = new Harmony("com.rotfood.patch");
            _harmony.PatchAll();

            U.Events.OnPlayerConnected += OnPlayerConnected;
            Logger.Log("RotFood с поддержкой холодильников загружен!");
        }

        protected override void Unload()
        {
            _harmony.UnpatchAll();
            U.Events.OnPlayerConnected -= OnPlayerConnected;
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            // У игрока обычная скорость гниения (множитель 1.0)
            ProcessDecay(player.Inventory, $"player_{player.CSteamID}", 1.0f);
        }

        public void ProcessDecay(Items inventory, string uniqueKey, float multiplier)
        {
            DateTime now = DateTime.Now;

            if (!LastUpdateMap.TryGetValue(uniqueKey, out DateTime lastUpdate))
            {
                LastUpdateMap[uniqueKey] = now;
                return;
            }

            double minutesPassed = (now - lastUpdate).TotalMinutes;
            // Обновляем метку времени сразу, чтобы избежать абуза с частым открытием
            LastUpdateMap[uniqueKey] = now;

            if (minutesPassed < 0.1) return; 

            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
            {
                if (inventory.items[page] == null) continue;

                for (int i = inventory.items[page].Count - 1; i >= 0; i--)
                {
                    ItemJar jar = inventory.items[page][i];
                    ItemAsset asset = (ItemAsset)Assets.find(EAssetType.ITEM, jar.item.id);

                    if (asset != null && (asset.type == EItemType.FOOD || asset.type == EItemType.WATER))
                    {
                        float baseRate = Configuration.Instance.FoodOverrides
                            .FirstOrDefault(x => x.ItemId == jar.item.id)?.DecayRate 
                            ?? Configuration.Instance.DefaultDecayRatePerMinute;

                        // Применяем множитель (для холодильника он будет низким)
                        float finalRate = baseRate * multiplier;
                        int damage = Mathf.FloorToInt((float)(minutesPassed * finalRate));
                        
                        if (damage > 0)
                        {
                            if (jar.item.quality <= damage)
                            {
                                inventory.removeItem(page, (byte)i);
                                inventory.addItem(Configuration.Instance.MoldItemId, true);
                            }
                            else
                            {
                                jar.item.quality -= (byte)damage;
                                inventory.sendUpdateQuality(page, (byte)i, jar.item.quality);
                            }
                        }
                    }
                }
            }
        }
    }
}
