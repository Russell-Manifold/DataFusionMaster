using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SBMS.Classes
{
    /// <summary>One Sage customer, as needed to set up a consignment store.</summary>
    [Serializable]
    public class SageCustomerDto
    {
        public long ID { get; set; }
        public string Name { get; set; }
        /// <summary>Sage delivery address lines joined with newlines (empty lines dropped).</summary>
        public string DeliveryAddress { get; set; }
    }

    /// <summary>Result of a customer read: the list, whether Sage answered every page, and when it was read.</summary>
    [Serializable]
    public class SageCustomerList
    {
        public List<SageCustomerDto> Customers = new List<SageCustomerDto>();
        public bool Complete;            // false = a page failed; the list may be short
        public DateTime ReadAt;
    }

    /// <summary>Reads the company's active customers from Sage (Customer/Get, paged). Read-only.
    /// Pages until Sage's TotalResults is reached (hard stop at 20,000). A page error stops the read and
    /// marks the list incomplete so the caller can say so instead of treating it as "no customers".</summary>
    public static class SageCustomers
    {
        public static async Task<SageCustomerList> GetAllAsync(UserDetails user)
        {
            var result = new SageCustomerList { Complete = true, ReadAt = DateTime.Now };
            var api = new ApiUrlCall();
            int skip = 0;
            for (int page = 0; page < 200; page++)
            {
                string url = ApiUrlCall.sageurl + "Customer/Get?apikey={" + ApiUrlCall.APIKey + "}&CompanyID=" + user.CoID
                           + "&$filter=Active%20eq%20true&$orderby=Name&$skip=" + skip;
                JObject r = await api.ApiCallAsync(url, user);
                if (r["error"] != null)
                {
                    api.LogErrorToFile($"CoID:{user.CoID} SageCustomers.GetAllAsync page {page} failed - {r["error"]}");
                    result.Complete = false;
                    break;
                }
                var results = r["Results"] as JArray;
                if (results == null || results.Count == 0) break;
                foreach (JObject c in results.OfType<JObject>())
                {
                    long id;
                    if (!long.TryParse(c["ID"]?.ToString(), out id)) continue;
                    var lines = new List<string>();
                    foreach (string k in new[] { "DeliveryAddress01", "DeliveryAddress02", "DeliveryAddress03", "DeliveryAddress04", "DeliveryAddress05" })
                    {
                        string v = c[k]?.ToString();
                        if (!string.IsNullOrWhiteSpace(v)) lines.Add(v.Trim());
                    }
                    result.Customers.Add(new SageCustomerDto { ID = id, Name = (c["Name"]?.ToString() ?? "").Trim(), DeliveryAddress = string.Join("\n", lines) });
                }
                int total = (int?)r["TotalResults"] ?? 0;
                skip += results.Count;
                if (skip >= total) break;
            }
            result.Customers = result.Customers.OrderBy(x => x.Name).ToList();
            return result;
        }
    }
}
