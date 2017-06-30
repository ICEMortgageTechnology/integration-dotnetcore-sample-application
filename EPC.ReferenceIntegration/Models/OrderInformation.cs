using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPC.ReferenceIntegration.Models
{
    public class OrderInformation
    {
        public string TransactionId { get; set; }
        public string OrderId { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public object LoanInformation { get; set; }
    }
}
