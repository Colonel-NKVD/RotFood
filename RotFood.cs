using System;
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
        private DataManager _dataManager;
        public DecayData Data;

        protected override void Load()
        {
            Instance = this;
            _harmony = new Harmony("com.rotfood.patch");
            _harmony.PatchAll();

            // Инициализация данных
            _dataManager = new DataManager(Directory);
            Data = _dataManager.Load();

            U.Events.OnPlayerConnected += OnPlayerConnected;
            
            // Авто-сохранение каждые 5 минут (на всякий случай)
            InvokeRepeating(nameof(SaveData), 300f, 300f);

            Logger.Log("RotFood загружен. Данные JSON синхронизированы.");
        }

        protected override void Unload()
        {
            SaveData();
            _harmony.UnpatchAll();
            U.Events.OnPlayerConnected -= OnPlayerConnected;
        }

        private void SaveData()
        {
            _dataManager?.Save(Data);
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            ProcessDecay(player.Inventory, $"p_{player.CSteamID}", 1.0f);
        }

        public void ProcessDecay(Items inventory, string key, float multiplier)
        {
            DateTime now = DateTime.Now;

            if (!Data.LastUpdates.TryGetValue(key, out DateTime lastUpdate))
            {
                Data.LastUpdates[key] = now;
                return;
            }

            double minutesPassed = (now - lastUpdate).TotalMinutes;
            
            // Обновляем метку времени в памяти
            Data.LastUpdates[key] = now;

            if (minutesPassed < 1.0) return; 

            for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
            {
                if (inventory?.items[page] == null) continue;

                for (int i = inventory.items[page].Count - 1; i >= 0; i--)
                {
                    ItemJar jar = inventory.items[page][i];
                    ItemAsset asset = Assets.find(EAssetType.ITEM, jar.item.id) as ItemAsset;

                    if (asset != null && (asset.type == EItemType.FOOD || asset.type == EItemType.WATER))
                    {
                        float baseRate = Configuration.Instance.FoodOverrides
                            .FirstOrDefault(x => x.ItemId == jar.item.id)?.DecayRate 
                            ?? Configuration.Instance.DefaultDecayRatePerMinute;

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
