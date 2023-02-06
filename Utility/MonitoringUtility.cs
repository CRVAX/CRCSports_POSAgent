using CsvHelper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Windows.Forms;
using WinSCP;

namespace Utility
{
    public class MonitoringUtility
    {
        //EventLog MonitoringDailySalesEventLog;

        public static EventLog initEventLog(string eventSourceName)
        {
            var MonitoringDailySalesEventLog = new EventLog();
            if (!EventLog.SourceExists(eventSourceName))
            {
                EventLog.CreateEventSource(eventSourceName,"");
            }
            MonitoringDailySalesEventLog.Source = eventSourceName;
            return MonitoringDailySalesEventLog;
        }
        public static void WriteLog(string strLog)
        {
            try
            {
                StreamWriter log;
                FileStream fileStream = null;
                DirectoryInfo logDirInfo = null;
                FileInfo logFileInfo;

                string logFilePath = Application.StartupPath;
                logFilePath = string.Concat(logFilePath, @"\Log\","Log", ConstMessage.UnderScore, DateTime.Now.ToString("dd-MM-yyyy"), ".txt");
                logFileInfo = new FileInfo(logFilePath);
                logDirInfo = new DirectoryInfo(logFileInfo.DirectoryName);
                if (!logDirInfo.Exists)
                    logDirInfo.Create();
                if (!logFileInfo.Exists)
                {
                    fileStream = logFileInfo.Create();
                }
                else
                {
                    fileStream = new FileStream(logFilePath, FileMode.Append);
                }
                log = new StreamWriter(fileStream);
                log.WriteLine(strLog);
                log.Close();
            }
            catch (IOException ioEx)
            {
                var exceptionMessage = (ioEx.InnerException != null)
                ? ioEx.InnerException.Message
                : ioEx.Message;
                var eventLog = initEventLog(ConstMessage.EvtSrcNameMonitoringMITPOSAgent);
                eventLog.WriteEntry(string.Format(ConstMessage.WriteLogFileError, exceptionMessage), EventLogEntryType.Error);
            }
        }

        public static bool WriteCsvWithHeaderToFile<T>(IEnumerable<T> _lstSummaryMITPOSOrder, string filePath) where T : class
        {
            try
            {
                using (var streamWriter = new StreamWriter(filePath))
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.CurrentCulture))
                {
                    csvWriter.WriteRecords(_lstSummaryMITPOSOrder);
                }
                return File.Exists(filePath);
            }
            catch (CsvHelperException csvEx)
            {
                var exceptionMessage = (csvEx.InnerException != null)
                ? csvEx.InnerException.Message
                : csvEx.Message;
                WriteLog(DateTime.Now.ToString(ConstMessage.MonthDateSecondFormat) + ConstMessage.ColonSpace + string.Format(ConstMessage.WriteCSVError, exceptionMessage.ToString()));
                return false;
            }
        }

        public static IEnumerable<StoreConfig> ReadCsvFile(string filePath)
        {
            var listStore = new List<StoreConfig>();
            var value = string.Empty;
            var csvConfiguration = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.CurrentCulture);
            csvConfiguration.HasHeaderRecord = true;
            try
            {
                using (var textReader = File.OpenText(filePath))
                {
                    using (var csvReader = new CsvReader(textReader, csvConfiguration))
                    {
                        listStore.AddRange(csvReader.GetRecords<StoreConfig>());
                    }
                }
                return listStore;
            }
            catch (CsvHelperException csvEx)
            {
                var exceptionMessage = (csvEx.InnerException != null)
                ? csvEx.InnerException.Message
                : csvEx.Message;
                WriteLog(DateTime.Now.ToString(ConstMessage.MonthDateSecondFormat) + ConstMessage.ColonSpace + string.Format(ConstMessage.WriteCSVError, exceptionMessage.ToString()));
                return null;
            }
        }

        public static bool UploadFileSFTP(string host, int port, string userName, string password, string sshHostKey, string fileLocalName, string fileUploadName, int limitedUploadTry)
        {
            try
            {
                // Setup session options
                SessionOptions sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Sftp,
                    HostName = host,
                    UserName = userName,
                    Password = password,
                    SshHostKeyFingerprint = sshHostKey
                };
                using (Session session = new Session())
                {
                    var waitingHours = int.Parse(ConfigurationManager.AppSettings["RetryUploadHours"].ToString()) * 60 * 60 * 1000;
                    var result = TryReConnect(() =>
                    {
                        // Connect
                        session.Open(sessionOptions);
                        // Upload files
                        TransferOptions transferOptions = new TransferOptions();
                        transferOptions.ResumeSupport.State = TransferResumeSupportState.On;
                        transferOptions.TransferMode = TransferMode.Binary;
                        var transferResult = session.PutFiles(fileLocalName, fileUploadName, false, transferOptions);
                        transferResult.Check();
                        return transferResult;
                    }, limitedUploadTry, waitingHours);
                    return result.IsSuccess;
                }
            }
            catch (Exception ex)
            {
                var exceptionMessage = (ex.InnerException != null)
                ? ex.InnerException.Message
                : ex.Message;
                WriteLog(DateTime.Now.ToString(ConstMessage.MonthDateSecondFormat) + ConstMessage.ColonSpace + string.Format(ConstMessage.SFTPUploadError, fileUploadName, exceptionMessage));
                return false;
            }
        }

        public static bool UploadFileFTP(string host, int port, string userName, string password, string fileLocalName, string fileUploadName)
        {
            try
            {
                // Setup session options
                SessionOptions sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Ftp,
                    HostName = host,
                    UserName = userName,
                    Password = password
                };
                using (Session session = new Session())
                {
                    // Connect
                    session.Open(sessionOptions);
                    // Upload files
                    TransferOptions transferOptions = new TransferOptions();
                    transferOptions.ResumeSupport.State = TransferResumeSupportState.On;
                    transferOptions.TransferMode = TransferMode.Binary;
                    var transferResult = session.PutFiles(fileLocalName, fileUploadName, false, transferOptions);
                    transferResult.Check();
                    return transferResult.IsSuccess;
                }
            }
            catch (Exception ex)
            {
                var exceptionMessage = (ex.InnerException != null)
                ? ex.InnerException.Message
                : ex.Message;
                WriteLog(DateTime.Now.ToString(ConstMessage.MonthDateSecondFormat) + ConstMessage.ColonSpace + string.Format(ConstMessage.SFTPUploadError, fileUploadName, exceptionMessage));
                return false;
            }
        }

        private static T TryReConnect<T>(Func<T> ConnectToSFTP, int limitedTimes, int waitingHours)
        {
            var tryCount = 0;
            while (tryCount <= limitedTimes)
            {
                try
                {
                    return ConnectToSFTP();
                }
                catch (Exception ex)
                {
                    tryCount++;
                    var exceptionMessage = (ex.InnerException != null)
                    ? ex.InnerException.Message
                    : ex.Message;
                    WriteLog(DateTime.Now.ToString(ConstMessage.MonthDateSecondFormat) + ConstMessage.ColonSpace + string.Format(ConstMessage.ConnectSFTPError, exceptionMessage));
                    Thread.Sleep(waitingHours);
                }
            }
            return default (T);
        }

        public static SmtpClient InitSmtpClient(string userName, string passWord, string host, int port, bool useDefaultCredentials, bool enableSsl,  string targetName)
        {
            var basicCredential = new NetworkCredential(userName, passWord);
            return new SmtpClient()
            {
                Host = host,
                Port = port,
                UseDefaultCredentials = useDefaultCredentials,
                EnableSsl = enableSsl,
                Credentials = basicCredential,
                TargetName = targetName
            };
        }

        public static bool SendEmail(SmtpClient smtp, string sender, string receiver, string listCC, string subject, string message)
        {
            try
            {
                MailMessage mail = new MailMessage()
                {
                    To = { new MailAddress(receiver) },                   
                    From = new MailAddress(sender),
                    Subject = subject,
                    IsBodyHtml = true,
                    Body = message,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    SubjectEncoding = System.Text.Encoding.UTF8
                };
                foreach (var toAddress in listCC.Split(';'))
                {
                    mail.CC.Add(toAddress);
                }
                smtp.Send(mail);
                return true;
            }
            catch (SmtpException Smtpex)
            {
                var exceptionMessage = (Smtpex.InnerException != null)
                ? Smtpex.InnerException.Message
                : Smtpex.Message;
                WriteLog(DateTime.Now.ToString(ConstMessage.MonthDateSecondFormat) + ConstMessage.ColonSpace + string.Format(ConstMessage.SendEmailError, exceptionMessage));
                return false;
            }
        }

        public static dynamic GetSQLReaderValue(SqlDataReader reader, string colName)
        {
            var index = reader.GetOrdinal(colName);
            var value = reader.GetValue(index);
            var type = value.GetType();

            if (type.Name == "DBNull")
            {
                return null;
            }

            if (value != null)
            {
                return value;
            }
            else
            {
                return GetDefault(type);
            }
        }

        public static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
    }
}
