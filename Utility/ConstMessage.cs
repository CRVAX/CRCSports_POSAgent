using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public class ConstMessage
    {
        public static string MonthDateSecondFormat = "MM/dd/yyyy HH:mm:ss";
        public const string ColonSpace = ": ";
        public const string UnderScore = "_";
        public const string ServiceStart = "Service has started.";
        public const string ServiceRunning = "Service is running.";
        public const string ServiceError = "Service has encountered an the following error: {0}";
        public const string ServiceStopPending= "Service is stop pending.";
        public const string ServiceStartPending = "Service is start pending.";
        public const string ServiceOnTimer = "Monitoring daily retail sales service starting to export data from MITPOS";
        public const string MonitorMITPOSServiceStatus = "Checking running status of MITPOS services";
        public const string ExportCSVSuccess = "Export sales data from MITPOS system into CSV file succesfully.";
        public const string ExportCSVError = "There is an error when exporting sales data from MITPOS system into CSV file.";
        public const string UploadSuccess = "Upload daily-sales csv file to SFTP server succesfully.";
        public const string UploadError = "There is an error when uploading daily-sales csv file to SFTP server.";
        public const string SFTPUploadError = "Cannot upload file {0} to SFTP sever.The exception is {1}";
        public const string ConnectSFTPError = "Cannot connect to SFTP sever.The exception is {0}";
        public const string WriteCSVError = "Cannot write data into csv file. The exception is {0}";
        public const string WriteLogFileError = "Cannot write information in log file. The exception is {0}";
        public const string ServiceStop = "Service has stopped.";
        public const string ServiceScheduleTime = "Service have been scheduled to export data csv to SFTP server at {0}";
        public const string ServiceReScheduleTime = "Service have been rescheduled to export data csv to SFTP server at {0}";
        public const string EvtSrcNameMonitorSales = "SB Monitoring Daily Sales Service";
        public const string EvtSrcNameMonitorMITPOSService = "SB Monitoring MITPOS Services";
        public const string EvtSrcNameMonitoringMITPOSAgent = "SB Monitoring MITPOS Agent";
        public const string MITPOSClientName = "MITPOS Client Service";
        public const string SendEmailError = "Cannot send email because of the following error: {0}";
        public const string StartingService = "Starting to restart {0} service.";
        public const string RestartServiceSuccess = "Restart {0} service successfully.";
        public const string ForceToStopService = "Force to stop {0} service.";
    }
}
