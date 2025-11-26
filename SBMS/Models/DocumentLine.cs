using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SBMS.Models
{
    public class DocumentLine
    {
		public long SelectionId { get; set; }
		public int TaxTypeId { get; set; }
		public string Description { get; set; }

		// LIne Type
		// 0 = Item
		// 1 = Account (GL Transaction)
		public int LineType { get; set; }
		public decimal Quantity { get; set; }
		public decimal UnitPriceExclusive { get; set; }
		public decimal UnitPriceInclusive { get; set; }
		public decimal TaxPercentage { get; set; }
		public string Unit { get; set; }
		public decimal DiscountPercentage { get; set; }
		public decimal Exclusive { get; set; }
		public decimal Discount { get; set; }
		public decimal Tax { get; set; }
		public decimal Total { get; set; }
		public string Comments { get; set; }
		public long AnalysisCategoryId1 { get; set; }
		public long AnalysisCategoryId2 { get; set; }
		public long AnalysisCategoryId3 { get; set; }
		public int ItemType { get; set; }
        public long CurrencyId { get; set; }
        public decimal ExchRate { get; set; }
        public decimal localCurrLineVal { get; set; }

    }
}