using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringDailySalesAgent
{
    public class SummaryPOSOrders
    {
        public string StoreNumber { get; set; }
        public string TerminalId { get; set; }
        public decimal TotalGrossAmount { get; set; }
        public int TotalReceipt { get; set; }
        public string TransDate { get; set; }

    }
}
