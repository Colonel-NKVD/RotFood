using System;
using System.Linq;
using System.Reflection;
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
            string[] possibleMethods = { "ReceiveOpenStorageRequest", "askOpen", "ReceiveOpen" };
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
            }

            _dataManager = new DataManager(Directory);
            Data = _dataManager.Load();

            U.Events.OnPlayerConnected += OnPlayerConnected;
            
            // НОВОЕ: Счетчик аптайма сервера (тикает раз в минуту)
            InvokeRepeating(nameof(IncrementUptime), 60f, 60f);

            // Каждую минуту проверяем онлайн-игроков[cite: 2]
            InvokeRepeating(nameof(CheckActivePlayers), 60f, 60f);
            InvokeRepeating(nameof(SaveData), 300f, 300f);

            Logger.Log("RotFood v1.6 (Uptime Mode) загружен. Гниение при выключенном сервере отключено.");
        }

        protected override void Unload()
        {
            SaveData();
            _harmony?.UnpatchAll("com.rotfood.patch");
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            CancelInvoke(nameof(IncrementUptime)); //[cite: 2]
            CancelInvoke(nameof(CheckActivePlayers));
            CancelInvoke(nameof(SaveData));
        }

        private void IncrementUptime()
        {
            // Увеличиваем общий счетчик работы сервера (только пока плагин загружен)[cite: 2]
            Data.TotalServerUptime++;
        }

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

        // --- ЛОГИКА ДЛЯ ИГРОКА (С СИНХРОНИЗАЦИЕЙ UI) ---
        private void CheckPlayerInventory(UnturnedPlayer player)
        {
            string key = $"p_{player.CSteamID}";
            long currentUptime = Data.TotalServerUptime;

            // Используем новую систему UptimeCheck вместо DateTime[cite: 2]
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

        // --- ЛОГИКА ДЛЯ СУНДУКОВ ---
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
                            inventory.tryAddItem(new Item(Configuration.Instance.MoldItemId, true), true);
                        }
                        else
                        {
                            jar.item.quality -= (byte)damage;
                            inventory.updateQuality((byte)i, jar.item.quality);
                        }
                    }
                }
            }
        }
    }
}
