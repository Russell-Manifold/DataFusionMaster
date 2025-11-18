using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SBMS.Models
{
    public class ItemAdjustment
    {
        public long ID { get; set; }
        public DateTime Date { get; set; }
        public long ItemID { get; set; }
        public decimal AverageCost { get; set; }
        public decimal Quantity { get; set; }
        public string Reason { get; set; }
        public DateTime Created { get; set; }
    }
}