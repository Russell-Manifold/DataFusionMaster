using System;

namespace SBMS.Classes
{
    public class CompanyDetails
    {
        public string CompanyName { get; set; }
        public bool UseGenericLogin { get; set; }
        public string GenLoginName { get; set; }
        public string GenLoginPwd { get; set; }
        public string GenLoginEncrypted { get; set; }
        public long CoID { get; set; }
        public bool UseJobCards { get; set; }

        public bool UseProduction { get; set; }

    }
} 
