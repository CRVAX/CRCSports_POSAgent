using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringDailySalesAgent.Enum
{
    public class EnumLogic
    {
        public static string GetEnumArgsKey(EnumArgs param)
        {
            return $"--{param}";
        }
    }

    public enum EnumTypeStoreConfig
    {
        StoreConfig,
        StoreConfigBackDate
    }

    public enum EnumEnvironment
    {
        UAT,
        PROD
    }

    public enum EnumArgs
    {
        Environment,
        DayPeriod
    }
}
