using MonitoringDailySalesAgent.Enum;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Dynamics.BusinessConnectorNet;
using Utility;

namespace MonitoringDailySalesAgent.CallAX
{
    public class CallStoreConfig
    {

        [STAThread]
        public static string CallAX_GetListStoreConfig(EnumTypeStoreConfig _typeStoreConfig)
        {
            string result = "";
            Axapta ax = new Axapta();
            try
            {
                string UserAX = ConfigurationManager.AppSettings["UserAX"], PasswordAX = ConfigurationManager.AppSettings["PasswordAX"], 
                    ClassName = ConfigurationManager.AppSettings["ClassName"], MethodName = ConfigurationManager.AppSettings["MethodName"];

                System.Net.NetworkCredential nc = new System.Net.NetworkCredential(UserAX, PasswordAX);
                ax.LogonAs(UserAX.Trim(), "", nc, "", "", "", "");;
                object reponse = ax.CallStaticClassMethod(ClassName, MethodName, _typeStoreConfig);
                result = reponse.ToString();
            }
            catch (Exception ex)
            {
                MonitoringUtility.WriteLog(string.Format("Error CallAX: {0}", ex.Message));
                ax.Logoff();
            }
            ax.Logoff();
            return result;
        }
    }
}
