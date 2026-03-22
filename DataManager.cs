using Newtonsoft.Json;
using System.IO;
using Rocket.Core.Logging;
using System;

namespace RotFood
{
    public class DataManager
    {
        private readonly string _filePath;

        public DataManager(string pluginDirectory)
        {
            // Создаем папку плагина, если её нет, чтобы не было ошибки доступа
            if (!Directory.Exists(pluginDirectory))
                Directory.CreateDirectory(pluginDirectory);

            _filePath = Path.Combine(pluginDirectory, "DecayData.json");
        }

        public DecayData Load()
        {
            if (!File.Exists(_filePath))
            {
                Logger.Log("Файл DecayData.json не найден. Создаю новый...");
                return new DecayData();
            }

            try
            {
                string json = File.ReadAllText(_filePath);
                var data = JsonConvert.DeserializeObject<DecayData>(json);
                
                // Если файл пустой или поврежден, возвращаем чистый объект
                return data ?? new DecayData();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Ошибка при загрузке DecayData.json. Данные будут сброшены.");
                return new DecayData();
            }
        }

        public void Save(DecayData data)
        {
            try
            {
                // Сохраняем с отступами (Indented), чтобы ты мог вручную править аптайм в файле, если нужно
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Ошибка при сохранении DecayData.json");
            }
        }
    }
}
