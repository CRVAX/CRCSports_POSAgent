using System;
using System.Diagnostics;
using System.Configuration;
using System.Data.SqlClient;
using System.Collections.Generic;
using Utility;
using System.IO;
using MonitoringDailySalesAgent.Logic;
using MonitoringDailySalesAgent.Enum;

namespace MonitoringDailySalesAgent
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            MonitoringUtility.WriteLog(DateTime.Now.ToString(ConstMessage.MonthDateSecondFormat) + ConstMessage.ColonSpace + ConstMessage.ServiceOnTimer);
            var storeNumber = string.Empty;
            var terminal = string.Empty;
            //var storeConfig = ConfigurationManager.AppSettings["StoreConfig"].ToString();
            //var dayPeriod = int.Parse(ConfigurationManager.AppSettings["DayPeriod"].ToString());
            //var listStore = MonitoringUtility.ReadCsvFile(storeConfig);

            ConfigurationStore configurationStore = StoreConfigLogic.GetByType(args, EnumTypeStoreConfig.StoreConfig);

            var listStore = configurationStore.storeConfigs;
            var dayPeriod = configurationStore.DayPeriod;
            var currentBusinessDate = DateTime.Today.AddDays(dayPeriod);

            foreach (var store in listStore)
            {
                var lstSummaryPOSOrder = new List<SummaryPOSOrders>();
                var summaryOrder = new SummaryPOSOrders();
                try
                {
                    var connectionString = string.Format(ConfigurationManager.ConnectionStrings["POSAXR3"].ConnectionString, store.POSName, store.DBName);
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();

                        var cmdStoreNumber = "select top 1 store, terminal " +
                                            "from ax.RETAILTRANSACTIONTABLE where transDate = @transDate";
                        using (SqlCommand sqlCmd = new SqlCommand(cmdStoreNumber, conn))
                        {
                            sqlCmd.Parameters.AddWithValue("@transDate", currentBusinessDate);
                            using (SqlDataReader reader = sqlCmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    storeNumber = reader["store"].ToString();
                                    terminal = reader["terminal"].ToString();
                                }
                            }
                        }
                        //Get amount from history pay and history orders
                        if (storeNumber != "")
                        {
                            var retailTableName = "RETAILTRANSACTIONTABLE";
                            var result = GetTotalOrderAndTotalPayment(conn, currentBusinessDate, retailTableName);
                            if (result.Count > 0)
                            {
                                summaryOrder.StoreNumber = storeNumber;
                                summaryOrder.TerminalId = terminal;
                                summaryOrder.TotalGrossAmount = decimal.Parse(result[1]);
                                summaryOrder.TotalReceipt = int.Parse(result[0]);
                                summaryOrder.TransDate = currentBusinessDate.ToString("dd/MM/yyyy");
                            }
                            else
                            {
                                summaryOrder.StoreNumber = storeNumber;
                                summaryOrder.TerminalId = terminal;
                                summaryOrder.TotalGrossAmount = 0;
                                summaryOrder.TotalReceipt = 0;
                                summaryOrder.TransDate = currentBusinessDate.ToString("dd/MM/yyyy");
                            }
                        }
                        lstSummaryPOSOrder.Add(summaryOrder);
                        uploadCSVToSFTP(store.POSName, currentBusinessDate, lstSummaryPOSOrder);
                        MonitoringUtility.WriteLog(store.POSName + " - " + DateTime.Now.ToString(ConstMessage.MonthDateSecondFormat) + ConstMessage.ColonSpace + ConstMessage.ExportCSVSuccess);
                    }
                }
                catch (Exception ex)
                {
                    storeNumber = store.POSName.Substring(5, 6);
                    terminal = store.POSName.Substring(5, 9);
                    summaryOrder.StoreNumber = storeNumber;
                    summaryOrder.TerminalId = terminal;
                    summaryOrder.TotalGrossAmount = 0;
                    summaryOrder.TotalReceipt = -9999;
                    summaryOrder.TransDate = currentBusinessDate.ToString("dd/MM/yyyy");
                    lstSummaryPOSOrder.Add(summaryOrder);
                    uploadCSVToSFTP(store.POSName, currentBusinessDate, lstSummaryPOSOrder, false);
                    var exceptionMessage = (ex.InnerException != null)
                          ? ex.InnerException.Message
                          : ex.Message;
                    MonitoringUtility.WriteLog(store.POSName + " - " + DateTime.Now.ToString(ConstMessage.MonthDateSecondFormat) + ConstMessage.ColonSpace + string.Format(ConstMessage.ServiceError, exceptionMessage));
                }
            }

            Console.ReadLine();
        }

        private static void uploadCSVToSFTP(string storeNumber, DateTime currentBusinessDate, List<SummaryPOSOrders> lstSummaryPOSOrder, bool replace = true)
        {
            //Export file to backup server
            var fileName = string.Concat(@"DailySales", ConstMessage.UnderScore, storeNumber, ConstMessage.UnderScore, currentBusinessDate.ToString("ddMMyyyy"), ".csv");
            var posLocalPath = ConfigurationManager.AppSettings["posLocalPath"].ToString();
            DirectoryInfo logDirInfo = null;
            logDirInfo = new DirectoryInfo(posLocalPath);
            if (!logDirInfo.Exists)
                logDirInfo.Create();
            var filePathLocal = Path.Combine(posLocalPath, fileName);
            if (!replace && File.Exists(filePathLocal))
            {
                return;
            }
            var exportSuccess = MonitoringUtility.WriteCsvWithHeaderToFile(lstSummaryPOSOrder, filePathLocal);
            //Upload csv file to SFTP server
            if (exportSuccess)
            {
                var host = ConfigurationManager.AppSettings["Host"].ToString();
                var port = int.Parse(ConfigurationManager.AppSettings["Port"].ToString());
                var userName = ConfigurationManager.AppSettings["UserName"].ToString();
                var password = ConfigurationManager.AppSettings["PassWord"].ToString();
                var uploadDirectory = ConfigurationManager.AppSettings["uploadFolder"].ToString();
                var filePathSFTP = string.Concat(uploadDirectory, fileName);

                var uploadStatus = MonitoringUtility.UploadFileFTP(host, port, userName, password, filePathLocal, filePathSFTP);
                if (!uploadStatus)
                    MonitoringUtility.WriteLog(DateTime.Now.ToString(ConstMessage.MonthDateSecondFormat) + ConstMessage.ColonSpace + ConstMessage.UploadError);
                else
                    MonitoringUtility.WriteLog(DateTime.Now.ToString(ConstMessage.MonthDateSecondFormat) + ConstMessage.ColonSpace + ConstMessage.UploadSuccess);
            }
            else
            {
                MonitoringUtility.WriteLog(DateTime.Now.ToString(ConstMessage.MonthDateSecondFormat) + ConstMessage.ColonSpace + ConstMessage.ExportCSVError);
            }

        }

        private static List<string> GetTotalOrderAndTotalPayment(SqlConnection conn, DateTime transDate, string tableName)
        {
            var result = new List<string>();
            var cmdTotalOrderPay = "SELECT count(receiptId) as TotalReceipt, sum(grossamount) as TotalGrossAmount" +
                                                             " from " + tableName +
                                                             " where transDate = @transDate" +
                                                             " and type = 2" +
                                                             " group by store, terminal, transDate";

            using (SqlCommand sqlCmdTotalOrder = new SqlCommand(cmdTotalOrderPay, conn))
            {
                sqlCmdTotalOrder.Parameters.AddWithValue("@transDate", transDate);
                using (SqlDataReader reader = sqlCmdTotalOrder.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(reader["TotalReceipt"].ToString());
                        result.Add(reader["TotalGrossAmount"].ToString());
                    }
                }
            }
            return result;
        }

    }
}
