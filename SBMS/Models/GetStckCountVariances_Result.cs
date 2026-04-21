using System;
namespace SBMS.Models
{
    [Serializable]
    public partial class GetStckCountVariances_Result
    {
        public string ItemCode { get; set; }
        public string ItemDescription { get; set; }
        public string CategoryDescript { get; set; }
        public decimal? SystemQOH { get; set; }
        public decimal? TotalCountedQty { get; set; }
        public decimal? Variance { get; set; }
        public string CountStatus { get; set; }
    }
}
