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

            // План Б: Динамический патчинг сундуков
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

            // НОВОЕ: Запускаем проверку активных игроков каждую минуту (60 секунд)
            InvokeRepeating(nameof(CheckActivePlayers), 60f, 60f);
            
            InvokeRepeating(nameof(SaveData), 300f, 300f);

            Logger.Log("RotFood v1.4 (Real-Time Fix) загружен.");
        }

        protected override void Unload()
        {
            SaveData();
            _harmony?.UnpatchAll("com.rotfood.patch");
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            
            // Обязательно отменяем таймеры при выгрузке
            CancelInvoke(nameof(CheckActivePlayers));
            CancelInvoke(nameof(SaveData));
        }

        private void SaveData() => _dataManager?.Save(Data);

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            CheckPlayerInventory(player);
        }

        // Вынес логику проверки игрока в отдельный метод
        private void CheckPlayerInventory(UnturnedPlayer player)
        {
            for (byte page = 0; page < PlayerInventory.PAGES; page++)
            {
                var items = player.Inventory.items[page];
                if (items != null)
                {
                    ProcessDecay(items, $"p_{player.CSteamID}_{page}", 1.0f);
                }
            }
        }

        // НОВОЕ: Процесс гниения еды прямо в руках у играющих людей
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

        public void ProcessDecay(Items inventory, string key, float multiplier)
        {
            DateTime now = DateTime.Now;
            if (!Data.LastUpdates.TryGetValue(key, out DateTime lastUpdate))
            {
                Data.LastUpdates[key] = now;
                return;
            }

            double minutesPassed = (now - lastUpdate).TotalMinutes;

            // ИСПРАВЛЕНИЕ ОШИБКИ: Не сбрасываем таймер, если прошло меньше минуты!
            if (minutesPassed < 1.0) return;

            // Теперь таймер обновится только если время реально зачтено
            Data.LastUpdates[key] = now;

            for (int i = inventory.getItemCount() - 1; i >= 0; i--)
            {
                ItemJar jar = inventory.getItem((byte)i);
                if (jar == null || jar.item == null) continue;

                if (Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset && (asset.type == EItemType.FOOD || asset.type == EItemType.WATER))
                {
                    float baseRate = Configuration.Instance.FoodOverrides
                        .FirstOrDefault(x => x.ItemId == jar.item.id)?.DecayRate 
                        ?? Configuration.Instance.DefaultDecayRatePerMinute;

                    // Считаем урон с учетом множителя холодильника
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
