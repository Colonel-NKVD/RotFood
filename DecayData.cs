using System;
using System.Collections.Generic;

namespace RotFood
{
    public class DecayData
    {
        // Новое поле: Общий аптайм сервера в минутах
        public long TotalServerUptime { get; set; } = 0;
        
        // Новые метки: Хранят значение TotalServerUptime на момент последней проверки
        public Dictionary<string, long> LastUptimeCheck { get; set; } = new Dictionary<string, long>();

        // Оставляем старые метки для обратной совместимости (не убираем функционал)
        public Dictionary<string, DateTime> LastUpdates { get; set; } = new Dictionary<string, DateTime>();
    }
}
