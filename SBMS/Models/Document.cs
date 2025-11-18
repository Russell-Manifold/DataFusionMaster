using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SBMS.Models
{
    public class Document
    {
			public SupplierInvoiceHeader Header { get; set; }
			public List<DocumentLine> Lines { get; set; }
			//public string Document_Type { get; set; }
			//public string User_ID { get; set; }
		}
}