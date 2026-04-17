using System;
namespace SBMS.Models
{
    [Serializable]
    public partial class GetOpeningBalancesByStore_Result
    {
        public long? ItemID { get; set; }
        public string Code { get; set; }
        public string Unit { get; set; }
        public string ItemDescription { get; set; }
        public decimal? QOH { get; set; }
        public decimal? AverageCost { get; set; } 
    }
}
