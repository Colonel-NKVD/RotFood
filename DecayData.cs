using System;
using System.Collections.Generic;

namespace RotFood
{
    public class DecayData
    {
        // Ключ: уникальный ID (игрока или сундука), Значение: время последнего обновления
        public Dictionary<string, DateTime> LastUpdates { get; set; }

        public DecayData()
        {
            LastUpdates = new Dictionary<string, DateTime>();
        }
    }
}
