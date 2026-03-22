using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
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

            var storageType = typeof(InteractableStorage);
            string[] possibleMethods = { "ReceiveInteractRequest", "ReceiveOpenRequest", "ReceiveOpenStorageRequest", "askOpen", "ReceiveOpen" };
            MethodInfo targetMethod = null;

            foreach (var methodName in possibleMethods)
            {
                targetMethod = storageType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (targetMethod != null) break;
            }

            if (targetMethod != null)
            {
                var prefix = typeof(StoragePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                _harmony.Patch(targetMethod, new HarmonyMethod(prefix));
                Logger.Log($"[RotFood] Harmony успешно привязан к: {targetMethod.Name}");
            }
            else
            {
                Logger.LogError("[RotFood] ОШИБКА: Не удалось найти метод для патча сундуков!");
            }

            _dataManager = new DataManager(Directory);
            Data = _dataManager.Load();

            U.Events.OnPlayerConnected += OnPlayerConnected;
            
            InvokeRepeating(nameof(IncrementUptime), 60f, 60f); 
            InvokeRepeating(nameof(CheckActivePlayers), 60f, 60f); 
            InvokeRepeating(nameof(CheckGroundItems), 60f, 60f);
            InvokeRepeating(nameof(SaveData), 300f, 300f);

            Logger.Log("RotFood v1.7.1 загружен. Все системы гниения активны.");
        }

        protected override void Unload()
        {
            SaveData();
            _harmony?.UnpatchAll("com.rotfood.patch");
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            CancelInvoke(nameof(IncrementUptime)); 
            CancelInvoke(nameof(CheckActivePlayers)); 
            CancelInvoke(nameof(CheckGroundItems));
            CancelInvoke(nameof(SaveData));
        }

        private void IncrementUptime() => Data.TotalServerUptime++;

        private void SaveData() => _dataManager?.Save(Data);

        private void OnPlayerConnected(UnturnedPlayer player) => CheckPlayerInventory(player);

        private void CheckActivePlayers()
        {
            foreach (var steamPlayer in Provider.clients)
            {
                UnturnedPlayer player = UnturnedPlayer.FromSteamPlayer(steamPlayer);
                if (player != null && player.Inventory != null)
                {
                    CheckPlayerInventory(player);
                }
            }
        }

        private void CheckPlayerInventory(UnturnedPlayer player)
        {
            string key = $"p_{player.CSteamID}";
            long currentUptime = Data.TotalServerUptime;

            if (!Data.LastUptimeCheck.TryGetValue(key, out long lastCheck))
            {
                Data.LastUptimeCheck[key] = currentUptime;
                return;
            }

            long minutesPassed = currentUptime - lastCheck;
            if (minutesPassed < 1) return;

            Data.LastUptimeCheck[key] = currentUptime;

            for (byte page = 0; page < PlayerInventory.PAGES; page++)
            {
                var items = player.Player.inventory.items[page];
                if (items == null) continue;

                for (int i = items.getItemCount() - 1; i >= 0; i--)
                {
                    ItemJar jar = items.getItem((byte)i);
                    if (jar == null || jar.item == null) continue;

                    if (Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset && (asset.type == EItemType.FOOD || asset.type == EItemType.WATER))
                    {
                        float baseRate = Configuration.Instance.FoodOverrides
                            .FirstOrDefault(x => x.ItemId == jar.item.id)?.DecayRate 
                            ?? Configuration.Instance.DefaultDecayRatePerMinute;

                        int damage = Mathf.FloorToInt((float)(minutesPassed * baseRate));

                        if (damage > 0)
                        {
                            if (jar.item.quality <= damage)
                            {
                                byte x = jar.x;
                                byte y = jar.y;
                                items.removeItem((byte)i);
                                player.Player.inventory.sendUpdateQuality(page, x, y, 0); 
                                player.Player.inventory.tryAddItem(new Item(Configuration.Instance.MoldItemId, true), true);
                            }
                            else
                            {
                                jar.item.quality -= (byte)damage;
                                player.Player.inventory.sendUpdateQuality(page, jar.x, jar.y, jar.item.quality);
                            }
                        }
                    }
                }
            }
        }

        public void ProcessStorageDecay(Items inventory, string key, float multiplier)
        {
            long currentUptime = Data.TotalServerUptime;

            if (!Data.LastUptimeCheck.TryGetValue(key, out long lastCheck))
            {
                Data.LastUptimeCheck[key] = currentUptime;
                return;
            }

            long minutesPassed = currentUptime - lastCheck;
            if (minutesPassed < 1) return;

            Data.LastUptimeCheck[key] = currentUptime;

            for (int i = inventory.getItemCount() - 1; i >= 0; i--)
            {
                ItemJar jar = inventory.getItem((byte)i);
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
                            inventory.removeItem((byte)i);
                            inventory.tryAddItem(new Item(Configuration.Instance.MoldItemId, true));
                        }
                        else
                        {
                            jar.item.quality -= (byte)damage;
                        }
                    }
                }
            }
        }

        private void CheckGroundItems()
        {
            float defaultRate = Configuration.Instance.DefaultDecayRatePerMinute;
            ushort moldId = Configuration.Instance.MoldItemId;

            for (byte x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    ItemRegion region = ItemManager.regions[x, y];
                    
                    for (int i = region.drops.Count - 1; i >= 0; i--)
                    {
                        ItemDrop drop = region.drops[i];
                        if (drop == null || drop.item == null) continue;

                        Item groundItem = drop.item;

                        if (Assets.find(EAssetType.ITEM, groundItem.id) is ItemAsset asset && (asset.type == EItemType.FOOD || asset.type == EItemType.WATER))
                        {
                            float rate = Configuration.Instance.FoodOverrides
                                .FirstOrDefault(o => o.ItemId == groundItem.id)?.DecayRate ?? defaultRate;

                            int damage = Mathf.Max(1, Mathf.FloorToInt(rate));

                            if (groundItem.quality <= damage)
                            {
                                Vector3 lastPos = drop.model.position;
                                ItemManager.removeItem(x, y, (uint)i);
                                ItemManager.dropItem(new Item(moldId, true), lastPos, false, false, false);
                            }
                            else
                            {
                                groundItem.quality -= (byte)damage;
                                ItemManager.parenthesizeQuality(drop.model, groundItem.quality);
                            }
                        }
                    }
                }
            }
        }
    }
}
