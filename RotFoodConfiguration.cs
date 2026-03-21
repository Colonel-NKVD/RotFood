using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace RotFood
{
    public class RotFoodConfiguration : IRocketPluginConfiguration
    {
        public ushort MoldItemId { get; set; }
        public float DefaultDecayRatePerMinute { get; set; }
        
        // Коэффициент гниения в холодильнике (0.1 = в 10 раз медленнее, 0.0 = не гниет)
        public float FridgeDecayMultiplier { get; set; }
        
        [XmlArrayItem("FridgeID")]
        public List<ushort> FridgeIds { get; set; }

        [XmlArrayItem("FoodDecay")]
        public List<FoodOverride> FoodOverrides { get; set; }

        public void Defaults()
        {
            MoldItemId = 70;
            DefaultDecayRatePerMinute = 0.5f;
            FridgeDecayMultiplier = 0.1f; 
            
            // Стандартные ID холодильников в Unturned (могут меняться от карт/модов)
            FridgeIds = new List<ushort> { 1230, 1235 }; 

            FoodOverrides = new List<FoodOverride>()
            {
                new FoodOverride(13, 0.1f),
                new FoodOverride(81, 2.0f)
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
