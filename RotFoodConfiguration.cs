using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace RotFood
{
    public class RotFoodConfiguration : IRocketPluginConfiguration
    {
        public ushort MoldItemId { get; set; }
        public float DefaultDecayRatePerMinute { get; set; }
        public float FridgeDecayMultiplier { get; set; }
        
        [XmlArrayItem("FridgeID")]
        public List<ushort> FridgeIds { get; set; }

        [XmlArrayItem("FoodDecay")]
        public List<FoodOverride> FoodOverrides { get; set; }

        // Исправленное имя метода для соответствия интерфейсу RocketMod
        public void LoadDefaults()
        {
            MoldItemId = 70;
            DefaultDecayRatePerMinute = 0.1f;
            FridgeDecayMultiplier = 0.1f;
            
            FridgeIds = new List<ushort> { 1230, 1235 };

            FoodOverrides = new List<FoodOverride>
            {
                new FoodOverride(13, 0.01f),
                new FoodOverride(81, 0.5f)
            };
        }
    }

    public class FoodOverride
    {
        [XmlAttribute("ID")]
        public ushort ItemId;
        [XmlAttribute("Rate")]
        public float DecayRate;

        public FoodOverride() { }
        public FoodOverride(ushort id, float rate)
        {
            ItemId = id;
            DecayRate = rate;
        }
    }
}
