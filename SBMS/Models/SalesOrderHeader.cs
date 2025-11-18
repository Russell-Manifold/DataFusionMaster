using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SBMS.Models
{
    public class SalesOrderHeader
    {
		public DateTime DeliveryDate { get; set; }
        public long CustomerId { get; set; }
        public long ID { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }
        public string SalesRepName { get; set; }
        public long SalesRepresentativeId { get; set; }

        public string Reference { get; set; }
    }
}