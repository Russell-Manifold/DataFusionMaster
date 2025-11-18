using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SBMS.Models
{
    public class SageSalesOrder
    {
			public SalesOrderHeader Header { get; set; }
			public List<DocumentLine> Lines { get; set; }
		}
}