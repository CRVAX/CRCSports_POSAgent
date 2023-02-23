using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public class StoreConfig
    {
        public string POSName { get; set; }
        public string DBName { get; set; }

    }

    public class ConfigurationStore
    {
        public List<StoreConfig> storeConfigs { get; set; } = new List<StoreConfig>();
        public int DayPeriod { get; set; } = 0;
    }

    public class ArgModel
    {
        public string Environment { get; set; } = "";
        public int DayPeriod { get; set; }
    }
}
