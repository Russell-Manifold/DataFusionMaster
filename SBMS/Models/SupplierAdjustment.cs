using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SBMS.Models
{
    public class SupplierAdjustment
    {
		public DateTime Date { get; set; }
		public long SupplierId { get; set; }
		public string DocumentNumber { get; set; }
		public string Reference { get; set; }
		public string Description { get; set; }
		public int TaxTypeId { get; set; }
		public decimal Exclusive { get; set; }
	}
}