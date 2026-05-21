using System;

namespace SBMS.Models
{
    public class HelpArticle
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public string Keywords { get; set; }
        public string PageUrl { get; set; }
        public string Module { get; set; }
        public int UsageCount { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool Published { get; set; }
        public long? CompanyID { get; set; }
    }
}
