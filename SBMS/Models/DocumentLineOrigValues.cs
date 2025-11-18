using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SBMS.Models
{
    public class DocumentLineOrigValues
    {
		public long ID { get; set; }
        public string Code { get; set; }
        public decimal QuantityOnHand { get; set; }
		public decimal AverageCost { get; set; }
	}
}