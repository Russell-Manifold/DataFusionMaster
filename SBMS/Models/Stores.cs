using System;

namespace SBMS.Models
{
    [Serializable]
    public class Stores
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; }
        public string Location { get; set; }
    }
}