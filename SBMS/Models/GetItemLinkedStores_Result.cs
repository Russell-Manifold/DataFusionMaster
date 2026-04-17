using System;
namespace SBMS.Models
{
    [Serializable]
    public partial class GetItemLinkedStores_Result
    {
        public string StoreCode { get; set; }
        public int StoreID { get; set; }
        public decimal QOH { get; set; }
    }
}
