using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SBMS.Models
{
    public class SupplierInvoiceHeader
    {
        public long ID { get; set; }
        public DateTime DueDate { get; set; }
		public long SupplierId { get; set; }
		public string SupplierName { get; set; }
		public int StatusId { get; set; }		
		public DateTime Date { get; set; }
		public bool Inclusive { get; set; }
		public decimal DiscountPercentage { get; set; }
		public string TaxReference { get; set; }
		public string Reference { get; set; }
		public string Message { get; set; }
		public string PostalAddress01 { get; set; }
		public string PostalAddress02 { get; set; }
		public string PostalAddress03 { get; set; }
		public string PostalAddress04 { get; set; }
		public string PostalAddress05 { get; set; }
		public string DeliveryAddress01 { get; set; }
		public string DeliveryAddress02 { get; set; }
		public string DeliveryAddress03 { get; set; }
		public string DeliveryAddress04 { get; set; }
		public string DeliveryAddress05 { get; set; }
		public string FromDocument { get; set; }
		public string DocumentNumber { get; set; }
        public decimal Supplier_ExchangeRate { get; set; }
        public long Supplier_CurrencyId { get; set; }
    }
}