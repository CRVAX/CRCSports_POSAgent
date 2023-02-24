using MonitoringDailySalesAgent.Enum;
using MonitoringDailySalesAgent.CallAX;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace MonitoringDailySalesAgent.Logic
{
    public class StoreConfigLogic
    {
        public static ConfigurationStore GetByType(string[] args, EnumTypeStoreConfig _typeStoreConfig)
        {
            ConfigurationStore resultConfig = new ConfigurationStore();
            try
            {
                ArgModel _arg = GetValueArgs(args);
                resultConfig.DayPeriod = _arg.DayPeriod;
                string reponse = CallStoreConfig.CallAX_GetListStoreConfig(_typeStoreConfig);

                if (string.IsNullOrEmpty(reponse))
                {
                    throw new Exception("Error GetByType: response null");
                }

                string[] arrStore = reponse.Split('|');

                if (arrStore == null || arrStore.Length <= 0)
                {
                    throw new Exception("Error GetByType: Array Store Config null");
                }

                resultConfig.storeConfigs = new List<StoreConfig>();
                foreach (string obj in arrStore)
                {
                    if (string.IsNullOrEmpty(obj))
                    {
                        continue;
                    }

                    string[] arrConfig = obj.Split('-');

                    if (arrConfig == null && arrConfig.Length <= 0)
                    {
                        continue;
                    }

                    resultConfig.storeConfigs.Add(new StoreConfig()
                    {
                        POSName = arrConfig[0],
                        DBName = arrConfig[1]
                    });
                }
            }
            catch (Exception ex)
            {
                MonitoringUtility.WriteLog(string.Format("Error GetByType: {0}", ex.Message));
            }
            return resultConfig;
        }

        public static ArgModel GetValueArgs(string[] args)
        {
            ArgModel objArg = new ArgModel();
            int dayPeriod;

            foreach (string s in args)
            {
                string[] parts = s.Split('=');

                if(parts[0].ToUpper().ToString() == EnumLogic.GetEnumArgsKey(EnumArgs.Environment))
                {
                    objArg.Environment = parts[1].Trim().ToUpper().ToString();
                    continue;
                }

                if (parts[0].ToUpper().ToString() == EnumLogic.GetEnumArgsKey(EnumArgs.DayPeriod))
                {
                    int.TryParse(parts[1].Trim(), out dayPeriod);
                    objArg.DayPeriod = dayPeriod;
                    continue;
                }
            }
            return objArg;
        }
    }
}
