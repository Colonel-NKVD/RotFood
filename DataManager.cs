using Newtonsoft.Json;
using System.IO;
using Rocket.Core.Logging;

namespace RotFood
{
    public class DataManager
    {
        private readonly string _filePath;

        public DataManager(string pluginDirectory)
        {
            _filePath = Path.Combine(pluginDirectory, "DecayData.json");
        }

        public DecayData Load()
        {
            if (!File.Exists(_filePath))
                return new DecayData();

            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonConvert.DeserializeObject<DecayData>(json) ?? new DecayData();
            }
            catch (System.Exception ex)
            {
                Logger.LogException(ex, "Ошибка при загрузке DecayData.json");
                return new DecayData();
            }
        }

        public void Save(DecayData data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
            catch (System.Exception ex)
            {
                Logger.LogException(ex, "Ошибка при сохранении DecayData.json");
            }
        }
    }
}
