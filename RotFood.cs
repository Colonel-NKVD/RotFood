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

            _dataManager = new DataManager(Directory);
            Data = _dataManager.Load();

            U.Events.OnPlayerConnected += OnPlayerConnected;
            InvokeRepeating(nameof(SaveData), 300f, 300f);

            Logger.Log("RotFood загружен.");
        }

        protected override void Unload()
        {
            SaveData();
            _harmony.UnpatchAll();
            U.Events.OnPlayerConnected -= OnPlayerConnected;
        }

        private void SaveData() => _dataManager?.Save(Data);

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            for (byte page = 0; page < PlayerInventory.PAGES; page++)
            {
                if (player.Inventory.items[page] != null)
                {
                    ProcessDecay(player.Inventory.items[page], $"p_{player.CSteamID}_{page}", 1.0f);
                }
            }
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
            Data.LastUpdates[key] = now;

            if (minutesPassed < 1.0) return;

            for (byte i = (byte)(inventory.getItemCount() - 1); i >= 0; i--)
            {
                ItemJar jar = inventory.getItem(i);
                if (jar == null || jar.item == null) continue;

                if (Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset && (asset.type == EItemType.FOOD || asset.type == EItemType.WATER))
                {
                    float baseRate = Configuration.Instance.FoodOverrides
                        .FirstOrDefault(x => x.ItemId == jar.item.id)?.DecayRate 
                        ?? Configuration.Instance.DefaultDecayRatePerMinute;

                    int damage = Mathf.FloorToInt((float)(minutesPassed * baseRate * multiplier));

                    if (damage > 0)
                    {
                        if (jar.item.quality <= damage)
                        {
                            byte x = jar.x;
                            byte y = jar.y;
                            byte rot = jar.rot;
                            inventory.removeItem(i);
                            // Самая базовая сигнатура: (Item item, byte x, byte y, byte rot)
                            inventory.tryAddItem(new Item(Configuration.Instance.MoldItemId, true), x, y, rot);
                        }
                        else
                        {
                            jar.item.quality -= (byte)damage;
                            inventory.updateQuality(i, jar.item.quality);
                        }
                    }
                }
                if (i == 0) break;
            }
        }
    }
}
