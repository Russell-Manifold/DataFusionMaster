using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace SBMS.Classes
{
    public class ApiUrlCall
    {///
        // In SBMSEntities.Context.cs
        // Replace this:
        // public SBMSEntities()
        //    : base("name=SBMSEntities") 

        //{
        //}
        // WITH THIS
        //public SBMSEntities(string connectionString) : base(connectionString) { }

        //  for demo version data
      public static string dbName = $"MyDataFusionDemo2";
       // LIVE data
      /// <summary>
      //public static string dbName = $"MyDataFusion";
      /// </summary>

        public static string constr = $"Data Source=SYNCFLO-DESKTOP\\SYNCFLOSQL;Initial Catalog={dbName};Persist Security Info=True;User ID=sa;Password=M@nif0LD";
        //public static string constr = $"Data Source=RUSSELL-DELL\\DELLSQL;Initial Catalog={dbName};Persist Security Info=True;User ID=sa;Password=M@nif0LD";
        public static string constrP = $"Data Source=RHYOLITEHEXAGON\\MANIFOLDSQL;Initial Catalog={dbName};Persist Security Info=True;User ID=sa;Password=M@nif0LD";
              
        static DateTime CustDT = Convert.ToDateTime("01 Jan 2015"), SuppDT = Convert.ToDateTime("01 Jan 2015"), ItemDT = Convert.ToDateTime("01 Jan 2015"), PODT = Convert.ToDateTime("01 Jan 2015"), InvoiceDT = Convert.ToDateTime("01 Jan 2015"), CNoteDT = Convert.ToDateTime("01 Jan 2015");
        static DateTime SuppInvDT = Convert.ToDateTime("01 Jan 2015"), SuppRetDT = Convert.ToDateTime("01 Jan 2015"), JrnlDT = Convert.ToDateTime("01 Jan 2015"), QuoteDT = Convert.ToDateTime("01 Jan 2015"), SOrdDT = Convert.ToDateTime("01 Jan 2015"), GLegDT = Convert.ToDateTime("01 Jan 2015");

       //public static string sageurl = "https://accounting.sageone.co.za/api/2.0.0/";
      // public static string APIKey = "5850E392-0FE8-43B4-9EEB-18D2B28B115C";
        
      public static string sageurl = "https://resellers.accounting.sageone.co.za/api/2.0.0/";
     public static string APIKey = "2B7B61BA-41B8-4212-B2A2-77B8734BA688";

        // Syncflo SBCA profile - SANDBOX KEY
        //public static string APIKey = "934D4C3F-FF4D-4311-9380-F21ACB54DCBB";
        // CompanyID = 15240


        public static byte[] key = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
        public static byte[] iv = { 8, 7, 6, 5, 4, 3, 2, 1 };

        private static volatile bool _SOisSyncRunning = false;
        private static readonly object _SOsyncLock = new object();

        private static volatile bool _POisSyncRunning = false;
        private static readonly object _POsyncLock = new object();

        public async Task<JObject> ApiCallAsync(string requestUrl, UserDetails userDetails)
        {
            using (HttpClient client = new HttpClient())
            {
                // Set Basic Authentication Header
                client.Timeout = TimeSpan.FromSeconds(30); // Set the timeout as per your need
                string combined = $"{userDetails.LoginName}:{userDetails.LoginPwd}";
                string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Encoded);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                JObject parsedJSON = new JObject();
                try
                {
                    // Send GET request
                    Console.WriteLine($"Making request to: {requestUrl}");
                    HttpResponseMessage response = await client.GetAsync(requestUrl).ConfigureAwait(false);
                    Console.WriteLine($"Response Status: {response.StatusCode}");
                    // Check if response was successful
                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(content) && content.Trim() != "null")
                        {
                            parsedJSON = JObject.Parse(content);

                            // Trap empty result set: {"TotalResults":0,"ReturnedResults":0,"Results":[]}
                            if (parsedJSON["TotalResults"] != null &&
                                parsedJSON["TotalResults"].Value<int>() == 0 &&
                                parsedJSON["ReturnedResults"] != null &&
                                parsedJSON["ReturnedResults"].Value<int>() == 0)
                            {
                                parsedJSON["isEmpty"] = true;
                            }
                        }
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        parsedJSON["error"] = new JObject
                        {
                            ["statusCode"] = (int)response.StatusCode,
                            ["reason"] = response.ReasonPhrase,
                            ["message"] = errorContent
                        };
                        //Debug.WriteLine($"Request failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    }
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine($"Timeout error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                return parsedJSON;
            }
        }

        //public async Task<JObject> ApiCallAsync(string requestUrl, UserDetails userDetails)
        //{
        //    var options = new RestClientOptions(requestUrl)
        //    {
        //        ThrowOnAnyError = true,  // Ensures exceptions are thrown on errors
        //        ThrowOnDeserializationError = true
        //    };

        //    var client = new RestClient(options);
        //    var requ = new RestRequest(); 
        //    requ.Method = Method.Get; 

        //    // Set Basic Authentication
        //    string combined = $"{userDetails.LoginName}:{userDetails.LoginPwd}";
        //    string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
        //    requ.AddHeader("Authorization", "Basic " + base64Encoded);

        //    // Ensure TLS 1.2 security
        //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        //    JObject parsedJSON = new JObject();

        //    try
        //    {
        //        var response = await client.ExecuteAsync(requ);

        //        if (response.IsSuccessful && !string.IsNullOrWhiteSpace(response.Content))
        //        {
        //            parsedJSON = JObject.Parse(response.Content);
        //        }
        //        else
        //        {
        //            Console.WriteLine($"Request failed: {response.StatusCode} - {response.Content}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: {ex.Message}");
        //    }

        //    return parsedJSON;
        //}

        public async Task<byte[]> DownloadPdfAsync(string requestUrl, UserDetails Userdetails)
        {
            var options = new RestClientOptions(requestUrl)
            {
                ThrowOnAnyError = false, // Prevents exceptions on failed requests
                ThrowOnDeserializationError = false
            };

            var client = new RestClient(options);
            var requ = new RestRequest(); 
            requ.Method = Method.Get; 

                    // ✅ Correct Basic Authentication
                    string combined = $"{Userdetails.LoginName}:{Userdetails.LoginPwd}";
            string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
            requ.AddHeader("Authorization", "Basic " + base64Encoded);

            // ✅ Ensure TLS 1.2 security
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                var response = await client.ExecuteAsync(requ);

                if (response.IsSuccessful && response.RawBytes != null)
                {
                    return response.RawBytes; // ✅ Return the PDF content as a byte array
                }
                else
                {
                    Console.WriteLine($"Request failed: {response.StatusCode} - {response.Content}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return null; // ✅ Return null on failure
        }

        //public async Task<JObject> APIPostDocumentAsync(string DocType, string JsonStr, UserDetails Userdetails)
        //{
        //    JObject parsedJSON = new JObject();
        //    //string requestUrl = $"{sageurl}{DocType}/Save?apikey={{{APIKey}}}&CompanyID={Userdetails.CoID}";
        //    string requestUrl = sageurl + DocType+ "/Save?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID;
        //    using (HttpClient client = new HttpClient())
        //    {
        //        string combined = $"{Userdetails.LoginName}:{Userdetails.LoginPwd}";
        //        string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
        //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Encoded);
        //       ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        //        try
        //        {
        //            StringContent content = new StringContent(JsonStr, Encoding.UTF8, "application/json");
        //            HttpResponseMessage response = await client.PostAsync(requestUrl, content);
        //            // Check if response was successful
        //            if (response.IsSuccessStatusCode)
        //            {
        //                string responseContent = await response.Content.ReadAsStringAsync();
        //                if (!string.IsNullOrWhiteSpace(responseContent))
        //                {
        //                    parsedJSON = JObject.Parse(responseContent);
        //                }
        //            }
        //            else
        //            {
        //                Console.WriteLine($"Request failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Error: {ex.Message}");
        //        }
        //    }

        //     return parsedJSON;
        //  }

        public async Task<JObject> APIPostDocumentAsync(string DocType, string JsonStr, UserDetails Userdetails)
        {
            JObject parsedJSON = new JObject();

            string requestUrl = $"{sageurl}{DocType}/Save?apikey={{{APIKey}}}&CompanyID={Userdetails.CoID}";

            var options = new RestClientOptions(requestUrl)
            {
                ThrowOnAnyError = false, // Allows handling failed requests properly
                ThrowOnDeserializationError = false
            };

            var client = new RestClient(options);
            var requ = new RestRequest();

           string combined = $"{Userdetails.LoginName}:{Userdetails.LoginPwd}";
            string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
            requ.AddHeader("Authorization", "Basic " + base64Encoded);
            requ.Method = Method.Post;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
           requ.AddJsonBody(JsonStr);
            try
            {
                var response = await client.ExecuteAsync(requ);

                // Successful + valid JSON
                if (response.IsSuccessful && !string.IsNullOrWhiteSpace(response.Content))
                {
                    return JObject.Parse(response.Content);
                }

                // Failed request, return error JSON
                return new JObject
                {
                    ["Success"] = false,
                    ["StatusCode"] = response.StatusCode.ToString(),
                    ["Message"] = response.Content ?? "Empty response"
                };
            }
            catch (Exception ex)
            {
                // Exception, return error JSON
                return new JObject
                {
                    ["Success"] = false,
                    ["StatusCode"] = "Exception",
                    ["Message"] = ex.Message
                };
            }
            return parsedJSON;
        }

        public async Task<JObject> APIUpdateSalesOrderAsync(string DocType, string JsonStr, UserDetails Userdetails)
        {
            JObject parsedJSON = new JObject();
            string requestUrl = sageurl+DocType+"/Save?useSystemDocumentNumber=true&apikey={"+APIKey+"}&CompanyID="+Userdetails.CoID;
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                string combined = $"{Userdetails.LoginName}:{Userdetails.LoginPwd}";
                string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Encoded);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                try
                {
                    StringContent content = new StringContent(JsonStr, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(requestUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(responseContent))
                        {
                            parsedJSON = JObject.Parse(responseContent);
                        }
                    }
                    else
                    {
                        LogErrorToFile($"APIUpdateSalesOrderAsync {DocType}/Save failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    }
                }
                catch (Exception ex)
                {
                    LogErrorToFile($"APIUpdateSalesOrderAsync {DocType}/Save error: {ex.Message}");
                }
            }

            return parsedJSON;
        }

        public async Task<JObject> APIUpdatePurchaseOrderAsync(string DocType, string JsonStr, UserDetails Userdetails)
        {
            JObject parsedJSON = new JObject();
            string requestUrl = sageurl + DocType + "/Save?useSystemDocumentNumber=true&apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID;
            using (HttpClient client = new HttpClient())
            {
                string combined = $"{Userdetails.LoginName}:{Userdetails.LoginPwd}";
                string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Encoded);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                try
                {
                    StringContent content = new StringContent(JsonStr, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(requestUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(responseContent))
                        {
                            parsedJSON = JObject.Parse(responseContent);
                        }
                    }
                    else
                    {
                        LogErrorToFile($"APIUpdatePurchaseOrderAsync {DocType}/Save failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    }
                }
                catch (Exception ex)
                {
                    LogErrorToFile($"APIUpdatePurchaseOrderAsync {DocType}/Save error: {ex.Message}");
                }
            }

            return parsedJSON;
        }

        //public async Task<JObject> APIUpdateSalesOrderAsync(string DocType, string JsonStr, UserDetails Userdetails)
        //{
        //    JObject parsedJSON = new JObject();

        //    string requestUrl = $"{sageurl}{DocType}/Save?useSystemDocumentNumber=true&apikey={{ {APIKey} }}&CompanyID={Userdetails.CoID}";

        //    var options = new RestClientOptions(requestUrl)
        //    {
        //        ThrowOnAnyError = false, // Allows proper error handling
        //        ThrowOnDeserializationError = false
        //    };

        //    var client = new RestClient(options);
        //    var requ = new RestRequest();

        //    // ✅ Correct authentication method
        //    string combined = $"{Userdetails.LoginName}:{Userdetails.LoginPwd}";
        //    string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
        //    requ.AddHeader("Authorization", "Basic " + base64Encoded);

        //    // ✅ Set HTTP method separately
        //    requ.Method = Method.Post;

        //    // ✅ Ensure TLS 1.2 security
        //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        //    // ✅ Send JSON body correctly
        //    requ.AddJsonBody(JsonStr);

        //    try
        //    {
        //        var response = await client.ExecuteAsync(requ);

        //        if (response.IsSuccessful && !string.IsNullOrWhiteSpace(response.Content))
        //        {
        //            parsedJSON = JObject.Parse(response.Content);
        //        }
        //        else
        //        {
        //            Console.WriteLine($"Request failed: {response.StatusCode} - {response.Content}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: {ex.Message}");
        //    }

        //    return parsedJSON;
        //}

        //public JObject APIPost(string Controllr, string JsonStr, UserDetails Userdetails)
        //{
        //    RestClient client = new RestSharp.RestClient();
        //    string requestUrl = sageurl + Controllr + "/Save?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID;
        //    var requ = new RestSharp.RestRequest();
        //    requ.Credentials = new NetworkCredential(Userdetails.LoginName, Userdetails.LoginPwd);
        //    string combined = $"{Userdetails.LoginName}:{Userdetails.LoginPwd}";
        //    byte[] byteArray = Encoding.UTF8.GetBytes(combined);
        //    string base64Encoded = Convert.ToBase64String(byteArray);
        //    requ.AddHeader("Authorization", "Basic " + base64Encoded);
        //    client.BaseUrl = new Uri(requestUrl);
        //    requ.Method = RestSharp.Method.POST;
        //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        //    requ.RequestFormat = DataFormat.Json;
        //    requ.AddJsonBody(JsonStr);
        //    var response = client.Execute<Document>(requ);
        //    JObject parsedJSON = new JObject();
        //    try
        //    {
        //        parsedJSON = JObject.Parse(response.Content);
        //    }
        //    catch { }
        //    return parsedJSON;
        //}

        public async Task<string> ValidateUserAsync(string controller, string jsonStr, UserDetails userDetails)
        {
            using (HttpClient client = new HttpClient())
            {
                string requestUrl = $"{sageurl}{controller}/Get/{userDetails.CoID}?apikey={APIKey}";

                //Set Basic Authentication Header
                string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userDetails.LoginName}:{userDetails.LoginPwd}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                //Set request content
               var content = new StringContent(jsonStr, Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.GetAsync(requestUrl).ConfigureAwait(false);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    if (responseContent != "null")
                    {
                        if (responseContent.ToString().ToLower().Contains("failed"))
                        {
                            return responseContent.ToString();
                        }
                        else { return "OK"; }
                    }
                    else
                    {
                        return $" Invalid Company ID";
                    }
                }
                catch (Exception ex)
                {
                    return $"Validate User Error: {ex.Message}";
                }
            }
        }

        public async Task<JObject> GetCompaniesEnrollAsync(string username, string userpwd)
        {
            JObject result = new JObject
            {
                ["success"] = false,  // default to false
                ["data"] = null,
                ["error"] = null
            };

            string requestUrl = sageurl + "Company/GET?apikey={" + APIKey + "}";
            using (HttpClient client = new HttpClient())
            {
                // Set Basic Authentication Header
                string combined = $"{username}:{userpwd}";
                string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Encoded);

                // Ensure TLS 1.2 security
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                try
                {
                    HttpResponseMessage response = await client.GetAsync(requestUrl);

                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        if (!string.IsNullOrWhiteSpace(responseContent))
                        {
                            result["success"] = true;
                            result["data"] = JObject.Parse(responseContent);
                        }
                    }
                    else
                    {
                        result["error"] = new JObject
                        {
                            ["statusCode"] = (int)response.StatusCode,
                            ["reason"] = response.ReasonPhrase,
                            ["message"] = responseContent
                        };
                    }
                }
                catch (Exception ex)
                {
                    result["error"] = new JObject
                    {
                        ["exception"] = ex.Message,
                        ["stackTrace"] = ex.StackTrace
                    };
                }
            }

            return result;
        }

        //public static async Task<JObject> GetCompaniesEnrollAsync(string username, string userpwd)
        // {
        //     JObject parsedJSON = new JObject();

        //     string requestUrl = $"{sageurl}Company/GET?apikey={{ {APIKey} }}";

        //     var options = new RestClientOptions(requestUrl)
        //     {
        //         ThrowOnAnyError = false, // Allows handling failed requests properly
        //         ThrowOnDeserializationError = false
        //     };

        //     var client = new RestClient(options);
        //     var requ = new RestRequest();

        //     // ✅ Correct authentication method
        //     string combined = $"{username}:{userpwd}";
        //     string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
        //     requ.AddHeader("Authorization", "Basic " + base64Encoded);

        //     // ✅ Set HTTP method separately
        //     requ.Method = Method.Get;

        //     // ✅ Ensure TLS 1.2 security
        //     ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        //     try
        //     {
        //         var response = await client.ExecuteAsync(requ);

        //         if (response.IsSuccessful && !string.IsNullOrWhiteSpace(response.Content))
        //         {
        //             parsedJSON = JObject.Parse(response.Content);
        //         }
        //         else
        //         {
        //             Console.WriteLine($"Request failed: {response.StatusCode} - {response.Content}");
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"Error: {ex.Message}");
        //     }

        //     return parsedJSON;
        // }

        public async Task<JObject> GetTaxType(UserDetails Userdetails)
        {
            string requestUrl = sageurl + "TaxType/GET?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID;
            JObject parsedJSON = null;
            try
            {
                parsedJSON = await ApiCallAsync(requestUrl, Userdetails);
            }
            catch (Exception ex)
            {
                parsedJSON = new JObject
                {
                    ["error"] = "Err 565 - " + ex.Message
                };
            }

            if (parsedJSON.Count > 0)
            {
                JArray items = (JArray)parsedJSON["Results"];
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    if (items != null)
                    {
                        var TaxList = _db.TaxTypesMasters.Where(x=>x.CompanyID == Userdetails.CoID).ToList();
                        _db.TaxTypesMasters.RemoveRange(TaxList);
                        _db.SaveChanges();
                        foreach (var item in items)
                        {
                            TaxTypesMaster TaxNew = new TaxTypesMaster
                            {
                                CompanyID = Userdetails.CoID,
                                TaxTypeID = Convert.ToInt64(item["ID"]),
                                TaxPerc  = Convert.ToDecimal(item["Percentage"]),
                                TaxTypeName = item["Name"].ToString(),
                            };
                            _db.TaxTypesMasters.Add(TaxNew);
                            try
                            {
                                _db.SaveChanges();
                            } catch (Exception ex)
                            {
                                string str = ex.Message;
                            }
                        }
                       // _db.SaveChanges();
                    }
                 }
            }
            return parsedJSON;
        }

        public DataSet validateUser(string userguid)
        {
            string conString = constrP;
#if DEBUG
            conString = constr;
#endif
            DataSet ds = new DataSet();
            using (SqlConnection con = new SqlConnection(conString))
            {
                SqlCommand cmd = new SqlCommand("Select * FROM dbo.Users WHERE userguid = '" + userguid + "'", con);
                using (var oda = new SqlDataAdapter())
                {
                    con.Open();
                    try
                    {
                        cmd.CommandTimeout = 0;
                        oda.SelectCommand = cmd;
                        oda.Fill(ds);
                    }
                    catch (Exception ex)
                    {
                        var srt = ex.Message;
                    }
                    con.Close();
                }
            }
            return ds;
        }

        //private static string FiltDate(string dtS)
        //{
        //    DateTime dt = Convert.ToDateTime(dtS);
        //    string DtStr = "&$filter=((Modified gt datetime'" + dt.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture) + "' or Created gt datetime'" + dt.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture) + "')";
        //    return (DtStr);
        //}

        private static string FiltDate(string dtS)
        {
            DateTime dt;

            // Try to parse the date using multiple common formats
            string[] formats = GetAllDateFormats();

            // Try parsing with explicit formats first
            if (!DateTime.TryParseExact(dtS, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                // Try parsing with current culture
                if (!DateTime.TryParse(dtS, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
                {
                    // Try parsing with invariant culture
                    if (!DateTime.TryParse(dtS, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    {
                        // If all parsing attempts fail, log and throw a meaningful exception
                        string error = $"Unable to parse date string: '{dtS}'. Supported formats include: " +
                                      "ISO 8601 (yyyy-MM-ddTHH:mm:ss), US (MM/dd/yyyy), EU (dd/MM/yyyy), " +
                                      "and various other common formats.";
                        throw new ArgumentException(error);
                    }
                }
            }

            // Ensure we have a consistent format for the OData filter
            string DtStr = "&$filter=((Modified gt datetime'" + dt.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture) + "' or Created gt datetime'" + dt.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture) +"')";
            return DtStr;
        }

        private static string[] GetAllDateFormats()
        {
            return new[] {
        // ISO 8601 formats
        "yyyy-MM-ddTHH:mm:ss.fffffff",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm",
        "yyyy-MM-dd",
        
        // Common separators
        "yyyy-MM-dd HH:mm:ss.fff",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm",
        
        // US formats
        "MM/dd/yyyy HH:mm:ss.fff",
        "MM/dd/yyyy HH:mm:ss",
        "MM/dd/yyyy HH:mm",
        "MM/dd/yyyy",
        "M/d/yyyy H:mm:ss",
        "M/d/yyyy H:mm",
        "M/d/yyyy",
        
        // European formats
        "dd/MM/yyyy HH:mm:ss.fff",
        "dd/MM/yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm",
        "dd/MM/yyyy",
        "d/M/yyyy H:mm:ss",
        "d/M/yyyy H:mm",
        "d/M/yyyy",
        
        // Month name formats
        "dd-MMM-yyyy HH:mm:ss.fff",
        "dd-MMM-yyyy HH:mm:ss",
        "dd-MMM-yyyy HH:mm",
        "dd-MMM-yyyy",
        "dd MMM yyyy HH:mm:ss.fff",
        "dd MMM yyyy HH:mm:ss",
        "dd MMM yyyy HH:mm",
        "dd MMM yyyy",
        "MMM dd, yyyy HH:mm:ss",
        "MMM dd, yyyy",
        
        // Compact formats
        "yyyyMMddTHHmmssfff",
        "yyyyMMddTHHmmss",
        "yyyyMMddTHHmm",
        "yyyyMMdd",
        
        // Additional common formats
        "dd.MM.yyyy HH:mm:ss",
        "dd.MM.yyyy",
        "MM.dd.yyyy HH:mm:ss",
        "MM.dd.yyyy",
        "yyyy.MM.dd HH:mm:ss",
        "yyyy.MM.dd",
        
        // RFC 1123/2822
        "ddd, dd MMM yyyy HH:mm:ss GMT",
        "ddd, dd MMM yyyy HH:mm:ss UTC",
        
        // Sortable format
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss",
        
        // Current culture patterns
        CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " +
            CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern,
        CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " +
            CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern,
        CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern + " " +
            CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern,
        CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern + " " +
            CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern,
        CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern,
        CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern
    };
        }
        public class DocumentLine
        {
            public long SelectionId { get; set; }
            public int TaxTypeId { get; set; }
            public long ID { get; set; }
            public long SBCALineID { get; set; }
            public string Description { get; set; }
            public int LineType { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPriceExclusive { get; set; }
            public decimal UnitPriceInclusive { get; set; }
            public decimal TaxPercentage { get; set; }
            public decimal DiscountPercentage { get; set; }
            public decimal Exclusive { get; set; }
            public decimal Discount { get; set; }
            public decimal Tax { get; set; }
            public decimal Total { get; set; }
            public string Comments { get; set; }
            public long AnalysisCategoryId1 { get; set; }
            public long AnalysisCategoryId2 { get; set; }
            public long AnalysisCategoryId3 { get; set; }
            public decimal UnitCost { get; set; }
            public decimal Line_GP { get; set; }
            public string DocumentMessage { get; set; }
            //public string LineMessage { get; set; }
            public long CurrencyId { get; set; }
            public decimal ExchRate { get; set; }
            public decimal localCurrLineVal { get; set; }
        }

        public static bool CheckForInternetConnection(int timeoutMs = 10000, string url = null)
        {
            try
            {
                Ping myPing = new Ping();
                String host = "google.com";
                byte[] buffer = new byte[32];
                int timeout = 1000;
                PingOptions pingOptions = new PingOptions();
                PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
                return (reply.Status == IPStatus.Success);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetLineType(int TypeID)
        {
            // set line type from ID
            string LnType = "";
            if (TypeID == 0)
            {
                LnType = "Item";
            }
            else
            if (TypeID == 1)
            {
                LnType = "Account";
            }
            else
            if (TypeID == 3)
            {
                LnType = "Time";
            }
            else
            if (TypeID == 4)
            {
                LnType = "TimeEntry";
            }
            else
            if (TypeID == 5)
            {
                LnType = "Recharge";
            }
            else if (TypeID == 6)
            {
                LnType = "Recharge";
            }
            return LnType;
        }

        public class InventoryDemandsLine
        {
            public string CoID { get; set; }
            public string code { get; set; }
            public string item { get; set; }
            public double QOH { get; set; }
            public double Qty_Due_On_SO { get; set; }
            public double Qty_Due_On_Quote { get; set; }
            public int WeekNumber { get; set; }
            public int Year { get; set; }
            public int Year_Month { get; set; }
         }

        public async Task <JObject> LoadPurchaseOrders(UserDetails Userdetails)
        {
            lock (_POsyncLock)
            {
                if (_POisSyncRunning)
                    return new JObject();
                _POisSyncRunning = true;
            } 
            
            bool UpdateDate = false;
            double skipQty = 0; float TotQty = 0; int RetQty = 0;
            int usedoc = 0;
            DataSet ds = new DataSet();
            DateTime LastCallDt = DateTime.Now;
            JObject parsedJSON = null;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var LastItmCall = _db.LastCallLogs.Where(x => x.CompanyID == Userdetails.CoID).FirstOrDefault();
                if (LastItmCall != null)
                {
                    if (LastItmCall.LastPODate != null)
                    {
                        PODT = (DateTime)LastItmCall.LastPODate;
                        PODT = PODT.AddMinutes(-10);
                    }
                    else
                    {
                        PODT = DateTime.Today.AddMonths(-12);
                    }
                    
                }
                else
                {
                    PODT = DateTime.Today.AddMonths(-12);
                }
                LastCallDt = DateTime.Now;

                // get all incomplete POs
                var CompList = _db.DocHeaders
                 .Where(x => x.CompanyID == Userdetails.CoID && x.DocType == 1 && x.Complete == true)
                 .Select(x => x.DocID)
                 .ToList();
                var compDocIds = new HashSet<long>(CompList);

                do
                    {
                      string requestUrl = sageurl + "PurchaseOrder/GET?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID + "&$skip=" + skipQty + FiltDate(PODT.ToString()) + " and Status ne 'Invoiced')&includeDetail=true&includeSupplierDetails=false";
                    try
                    {
                        parsedJSON = await ApiCallAsync(requestUrl, Userdetails);
                    }
                    catch (Exception ex)
                    {
                        parsedJSON = new JObject
                        {
                            ["error"] = "Err 776 - " + ex.Message
                        };
                    }

                    // Sage returned a non-success response (e.g. 500). ApiCallAsync surfaces it
                    // as parsedJSON["error"] rather than throwing, so stop here and hand the
                    // message back to the caller to display instead of falling through and
                    // crashing on the absent Results array.
                    if (parsedJSON != null && parsedJSON["error"] != null)
                    {
                        _POisSyncRunning = false;
                        return new JObject
                        {
                            ["error"] = "Sage Error: " + parsedJSON["error"].ToString()
                        };
                    }

                    if (parsedJSON.Count > 0)
                    {
                        JArray items = (JArray)parsedJSON["Results"];
                        TotQty = Convert.ToInt32(parsedJSON["TotalResults"]);
                        RetQty = Convert.ToInt32(parsedJSON["ReturnedResults"]);

                        string SalesRep = string.Empty; string Ref = string.Empty;
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                long thisdocid = Convert.ToInt64(item["ID"].ToString());
                                if (item["Lines"] != null)
                                {
                                    List<DocumentLine> myObjects = JsonConvert.DeserializeObject<List<DocumentLine>>(item["Lines"].ToString());
                                    foreach (DocumentLine obj in myObjects)
                                    {
                                        if (obj.LineType.ToString() == "0")
                                        {
                                            usedoc = 1;
                                            break;
                                        }
                                    }
                                }
                                if (usedoc == 1)
                                {
                                    var ChkDoc = _db.DocHeaders.Where(x => x.DocID == thisdocid && x.CompanyID == Userdetails.CoID).FirstOrDefault();
                                    if (ChkDoc != null)
                                    {
                                        ChkDoc.DocDate = Convert.ToDateTime(item["Date"].ToString());
                                        ChkDoc.DueDelDate = Convert.ToDateTime(item["DeliveryDate"] ?? "");
                                        ChkDoc.CustSuppID = Convert.ToInt64(item["SupplierId"].ToString());
                                        ChkDoc.CustSupName = item["SupplierName"].ToString() ?? "";
                                        ChkDoc.Status = item["Status"].ToString();
                                        ChkDoc.Discount = Convert.ToDecimal(item["Discount"].ToString());
                                        ChkDoc.Exclusive = Convert.ToDecimal(item["Exclusive"].ToString());
                                        ChkDoc.Tax = Convert.ToDecimal(item["Tax"].ToString());
                                        ChkDoc.Rounding = Convert.ToDecimal(item["Rounding"].ToString());
                                        ChkDoc.Total = Convert.ToDecimal(item["Total"].ToString());
                                        ChkDoc.Reference = item["Reference"].ToString();
                                        ChkDoc.Message = item["Message"].ToString();
                                        ChkDoc.Inclusive = Convert.ToBoolean(item["Inclusive"].ToString());
                                        ChkDoc.DiscountPercentage = Convert.ToDecimal(item["DiscountPercentage"].ToString());
                                        ChkDoc.TaxReference = string.Empty;
                                        ChkDoc.Supplier_ExchangeRate = 1;
                                        if (item["Supplier_ExchangeRate"] != null)
                                        {
                                            ChkDoc.Supplier_ExchangeRate = Convert.ToDecimal(item["Supplier_ExchangeRate"]);
                                            ChkDoc.Supplier_CurrencyId = Convert.ToInt64(item["Supplier_CurrencyId"]);
                                        }
                                            
                                    if (item["TaxReference"] != null) ChkDoc.TaxReference = item["TaxReference"].ToString();
                                        _db.Entry(ChkDoc).State = System.Data.Entity.EntityState.Modified;
                                        try
                                        {
                                            _db.SaveChanges();
                                        }
                                        catch (Exception ex) { string str = ex.Message; }
                                        long currentDocId = Convert.ToInt64(item["ID"].ToString());
                                        if (!compDocIds.Contains(currentDocId))
                                        {
                                            await LoadPOLines(currentDocId, Userdetails);
                                        }
                                    //await LoadPOLines(Convert.ToInt64(item["ID"].ToString()), Userdetails);
                                    }
                                    else
                                    {
                                        DocHeader DocH = new DocHeader();
                                        DocH.DocDate = Convert.ToDateTime(item["Date"].ToString());
                                        DocH.DocID = Convert.ToInt64(item["ID"].ToString());
                                        DocH.DueDelDate = Convert.ToDateTime(item["DeliveryDate"] ?? "");
                                        DocH.DocumentNumber = item["DocumentNumber"].ToString() ?? "";
                                        DocH.CustSuppID = Convert.ToInt64(item["SupplierId"].ToString());
                                        DocH.CustSupName = item["SupplierName"].ToString() ?? "";
                                        DocH.CompanyID = Convert.ToInt32(Userdetails.CoID);
                                        DocH.Started = false;
                                        DocH.Complete = false;
                                        DocH.Status = item["Status"].ToString();
                                        DocH.Reference = item["Reference"].ToString();
                                        DocH.Message = item["Message"].ToString();
                                        DocH.Discount = Convert.ToDecimal(item["Discount"].ToString());
                                        DocH.Exclusive = Convert.ToDecimal(item["Exclusive"].ToString());
                                        DocH.Tax = Convert.ToDecimal(item["Tax"].ToString());
                                        DocH.Rounding = Convert.ToDecimal(item["Rounding"].ToString());
                                        DocH.Total = Convert.ToDecimal(item["Total"].ToString());
                                        DocH.DocType = 1;
                                        DocH.Inclusive = Convert.ToBoolean(item["Inclusive"].ToString());
                                        DocH.DiscountPercentage = Convert.ToDecimal(item["DiscountPercentage"].ToString());
                                        DocH.TaxReference = string.Empty;
                                        DocH.DocGUID = Guid.NewGuid();
                                        DocH.Supplier_ExchangeRate = 1;
                                        if (item["Supplier_ExchangeRate"] != null)
                                        {
                                            DocH.Supplier_ExchangeRate = Convert.ToDecimal(item["Supplier_ExchangeRate"]);
                                            DocH.Supplier_CurrencyId = Convert.ToInt64(item["Supplier_CurrencyId"]);
                                        }

                                        if (item["TaxReference"] != null) DocH.TaxReference = item["TaxReference"].ToString();
                                        _db.DocHeaders.Add(DocH);
                                        try
                                        {
                                            _db.SaveChanges();
                                        }
                                    catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                                    {
                                        var errorMessages = ex.EntityValidationErrors
                                            .SelectMany(x => x.ValidationErrors)
                                            .Select(x => x.PropertyName + ": " + x.ErrorMessage);

                                        string fullErrorMessage = string.Join("; ", errorMessages);
                                        string code = item?["Code"]?.ToString() ?? "Unknown";
                                        string name = item?["Description"]?.ToString() ?? "Unknown";

                                        string errMsg = $"CoID: {Userdetails.CoID} + LoadPurchaseOrders Entity error: PO {item["DocumentNumber"].ToString() ?? ""}: {fullErrorMessage}";
                                        LogErrorToFile(errMsg);
                                    }
                                    catch (Exception ex)
                                    {
                                        string code = item?["Code"]?.ToString() ?? "Unknown";
                                        string name = item?["Description"]?.ToString() ?? "Unknown";
                                        string errMsg = $"CoID: {Userdetails.CoID} + LoadPurchaseOrders error: PO {item["DocumentNumber"].ToString() ?? ""}: {ex.Message}";
                                        LogErrorToFile(errMsg);
                                    }

                                        long currentDocId = Convert.ToInt64(item["ID"].ToString());
                                        if (!compDocIds.Contains(currentDocId))
                                        {
                                            await LoadPOLines(currentDocId, Userdetails);
                                        }

                                    //await LoadPOLines(Convert.ToInt64(item["ID"].ToString()), Userdetails);
                                    }
                                }
                                usedoc = 0;
                            }
                        }
                        UpdateDate = true;
                        }
                        // Guard against a stuck page: if Sage returns no rows while
                        // TotalResults still reports more, skipQty would never advance
                        // and the loop (and _POisSyncRunning flag) would hang forever.
                        if (RetQty <= 0) break;
                        skipQty = skipQty + RetQty;
                    } while (skipQty < TotQty);

                if (UpdateDate)
                {
                    if (LastItmCall != null)
                    {
                        LastItmCall.LastPODate = Convert.ToDateTime(LastCallDt.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        LastCallLog newlog = new LastCallLog();
                        newlog.CompanyID = Userdetails.CoID;
                        newlog.LastPODate = Convert.ToDateTime(LastCallDt.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture));
                        _db.LastCallLogs.Add(newlog);
                    }
                    try
                    {
                        _db.SaveChanges();
                    }
                    catch (Exception ex) 
                    {
                        string errMsg = $"CoID: {Userdetails.CoID} + Error saving LastCallDate for Purchase Orders: {ex.Message}";
                        LogErrorToFile(errMsg);
                    }
                }
            }
            _POisSyncRunning = false;
            return parsedJSON;
        }

        public async Task<POReconcileSummary> LoadPOLines(long poid, UserDetails Userdetails)
        {
            var summary = new POReconcileSummary();
            DataSet ds = new DataSet();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // check for document attachments first
                await LoadPOAttachments(poid, Userdetails);

                // get latest doclines
                string requestUrl = sageurl + "PurchaseOrder/GET/" + poid + "?includeDetail={True}&includeSupplierDetails={True}&apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID;
                JObject parsedJSON = await ApiCallAsync(requestUrl, Userdetails);

                // Safety guard: only reconcile if Sage returned a usable PO with a Lines array.
                // - parsedJSON.Count == 0      -> network/timeout/empty body, do nothing
                // - parsedJSON["error"] set    -> non-2xx response, do nothing
                // - parsedJSON["Lines"] null   -> shape we don't recognise, do nothing
                // A legitimately empty array (PO with zero lines) IS allowed - we will then
                // delete/hide every local line as appropriate.
                if (parsedJSON.Count == 0 || parsedJSON["error"] != null || parsedJSON["Lines"] == null)
                {
                    summary.Skipped = true;
                    return summary;
                }

                List<DocumentLine> myObjects = JsonConvert.DeserializeObject<List<DocumentLine>>(parsedJSON["Lines"].ToString());
                if (myObjects == null) myObjects = new List<DocumentLine>();

                // Collect the set of Sage line IDs returned so we can detect deletions afterwards.
                var sageLineIds = new HashSet<long>(myObjects.Select(o => o.ID));

                foreach (DocumentLine obj in myObjects)
                {
                    // check if line exists and update,
                    var Line = _db.DocLines.Where(x => x.SBCALineID == obj.ID).FirstOrDefault();
                    if (Line != null)
                    {
                        // Track quantity change for the refresh banner.
                        decimal prevQty = Line.Quantity ?? 0;
                        decimal newQty = Convert.ToDecimal(obj.Quantity);
                        if (prevQty != newQty) summary.QtyChanged++;

                            Line.Discount = obj.Discount;
                            Line.DiscountPercentage = obj.DiscountPercentage;
                            Line.Exclusive = obj.Exclusive;
                            var itm = _db.ItemsMasters.Where(x => x.ID == obj.SelectionId).FirstOrDefault();
                            if (itm != null)
                            {
                                Line.ItemCode = itm.Code;
                            }
                            if (itm != null) Line.Unit = itm.Unit ?? "".ToString();
                            Line.ItemType = 1;
                            if (itm != null)
                            {
                                if (itm.Physical == true) Line.ItemType = 0;
                            }
                            Line.ItemDescription = obj.Description ?? "".ToString();
                            Line.LineType = obj.LineType;
                            Line.Quantity = Convert.ToDecimal(obj.Quantity);
                            Line.SelectionId = obj.SelectionId;
                            Line.Tax = Convert.ToDecimal(obj.Tax);
                            Line.TaxPercentage = Convert.ToDecimal(obj.TaxPercentage);
                            Line.Total = Convert.ToDecimal(obj.Total);
                            Line.UnitCost = Convert.ToDecimal(obj.UnitCost, CultureInfo.InvariantCulture);
                            Line.UnitPriceExclusive = Convert.ToDecimal(obj.UnitPriceExclusive, CultureInfo.InvariantCulture);
                            Line.UnitPriceInclusive = Convert.ToDecimal(obj.UnitPriceInclusive, CultureInfo.InvariantCulture);
                            Line.AnalysisCategoryId1 = obj.AnalysisCategoryId1;
                            Line.AnalysisCategoryId2 = obj.AnalysisCategoryId2;
                            Line.AnalysisCategoryId3 = obj.AnalysisCategoryId3;
                            Line.LineTaxTypeID = obj.TaxTypeId;
                            Line.ExchRate = 1;
                            Line.localCurrLineVal = Line.Total;
                            if (obj.ExchRate != 0)
                            {
                                Line.ExchRate = obj.ExchRate;
                                Line.localCurrLineVal = obj.Total / obj.ExchRate;
                                if (obj.CurrencyId!= 0)
                                {
                                    Line.CurrencyID = obj.CurrencyId;
                                }
                            }
                            _db.Entry(Line).State = System.Data.Entity.EntityState.Modified;
                        }
                        else
                        {
                            summary.Added++;
                            DocLine dl = new DocLine();
                            dl.SBCALineID = obj.ID;
                            dl.Discount = obj.Discount;
                            dl.DiscountPercentage = obj.DiscountPercentage;
                            dl.Exclusive = obj.Exclusive;
                            var itm = _db.ItemsMasters.Where(x => x.ID == obj.SelectionId).FirstOrDefault();
                            if (itm != null)
                            {
                                dl.ItemCode = itm.Code;
                            }
                            if (itm != null) dl.Unit = itm.Unit ?? "".ToString();
                            dl.ItemType = 1;
                            if (itm != null)
                            {
                                if (itm.Physical == true) dl.ItemType = 0;
                            }
                            dl.ItemDescription = obj.Description ?? "".ToString();
                            dl.LineType = obj.LineType;
                            dl.Quantity = Convert.ToDecimal(obj.Quantity);
                            dl.SelectionId = obj.SelectionId;
                            dl.Tax = Convert.ToDecimal(obj.Tax);
                            dl.TaxPercentage = Convert.ToDecimal(obj.TaxPercentage);
                            dl.Total = Convert.ToDecimal(obj.Total);
                            dl.UnitCost = Convert.ToDecimal(obj.UnitCost, CultureInfo.InvariantCulture);
                            dl.UnitPriceExclusive = Convert.ToDecimal(obj.UnitPriceExclusive, CultureInfo.InvariantCulture);
                            dl.UnitPriceInclusive = Convert.ToDecimal(obj.UnitPriceInclusive, CultureInfo.InvariantCulture);
                            dl.DocID = poid;
                            dl.ToReceive = false;
                            dl.ReceiveQty = 0;
                            dl.ReceiveComplete = false;
                            if (dl.LineType == 1)
                            {
                                dl.ToReceive = true;
                                dl.ReceiveQty = 1;
                            }
                            dl.AnalysisCategoryId1 = obj.AnalysisCategoryId1;
                            dl.AnalysisCategoryId2 = obj.AnalysisCategoryId2;
                            dl.AnalysisCategoryId3 = obj.AnalysisCategoryId3;
                            dl.QtyLeft = Convert.ToDecimal(obj.Quantity);
                            dl.LineTaxTypeID = obj.TaxTypeId;
                            dl.CompanyID = Userdetails.CoID;
                            dl.ExchRate = 1;
                            dl.localCurrLineVal = dl.Total;
                            if (obj.ExchRate != 0)
                            {
                                Line.ExchRate = obj.ExchRate;
                                obj.localCurrLineVal = obj.Total / obj.ExchRate;
                                if (obj.CurrencyId != 0)
                                {
                                    Line.CurrencyID = obj.CurrencyId;
                                }
                            }
                            _db.DocLines.Add(dl);
                        }
                    }

                // -------------------------------------------------------------
                // Reconcile local DocLines against the Sage line set.
                //
                // Sage line still present  -> already updated in the loop above.
                // Sage line removed, NO receivings        -> delete locally (safe).
                // Sage line removed, HAS receivings       -> DO NOTHING locally.
                //   We deliberately keep the local DocLine active so the user
                //   can still process the receipt and the supplier invoice goes
                //   out with the full line set. The Sage-side discrepancy is
                //   surfaced via the refresh banner and reconciled manually
                //   in Sage afterwards.
                // -------------------------------------------------------------
                var localLines = _db.DocLines
                                    .Where(x => x.DocID == poid)
                                    .ToList();

                foreach (var local in localLines)
                {
                    if (sageLineIds.Contains(local.SBCALineID)) continue;

                    // Does this line have ANY receivings (open or archived)?
                    // Match by SBCALineID for modern rows, fall back to
                    // (PODocID + ItemCode) for legacy NULL-SBCALineID rows.
                    bool hasReceivings = _db.ReceivingOutstandings.Any(x =>
                            x.PODocID == poid
                         && (x.SBCALineID == local.SBCALineID
                             || (x.SBCALineID == null && x.ItemCode == local.ItemCode)));

                    if (hasReceivings)
                    {
                        // Keep the line untouched. Just flag for the banner so
                        // the user knows to reconcile Sage after processing.
                        summary.Hidden++;
                    }
                    else
                    {
                        _db.DocLines.Remove(local);
                        summary.Removed++;
                    }
                }

                _db.SaveChanges();
            }
            return summary;
        }

        public async Task<List<string>> LoadItems(UserDetails Userdetails)
        {
            bool UpdateDate = false;
            int TotQty = 0, RetQty = 0, skipQty = 0;
            DateTime LastCallDt = DateTime.Now;
            var errorList = new List<string>();
            
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var LastItmCall = _db.LastCallLogs.Where(x => x.CompanyID == Userdetails.CoID).FirstOrDefault();
                if (LastItmCall != null)
                {
                    if (LastItmCall.LastItemDate != null)
                    {
                        ItemDT = (DateTime)LastItmCall.LastItemDate;
                        ItemDT = ItemDT.AddMinutes(-10);
                    }
                    else
                    {
                        ItemDT = Convert.ToDateTime("01 Jan 2015");
                    }
                   
                } else
                {
                    ItemDT = Convert.ToDateTime("01 Jan 2015");
                }
                LastCallDt = DateTime.Now;
                do
                {
                    DateTime requestStart = DateTime.Now;
                    string requestUrl = sageurl + "Item/GET?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID + "&$skip=" + skipQty + FiltDate(ItemDT.ToString()) + " and Active eq true)&includeAdditionalItemPrices=true&includeAttachments=false";
                    JObject parsedJSON = await ApiCallAsync(requestUrl, Userdetails);
                    if (parsedJSON.Count > 0)
                    {
                        JArray items = (JArray)parsedJSON["Results"];
                        TotQty = Convert.ToInt32(parsedJSON["TotalResults"]);
                        RetQty = Convert.ToInt32(parsedJSON["ReturnedResults"]);
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                string Categ = string.Empty; int CategID = 0;
                                if (item.ToString().Contains("Category"))
                                {
                                    Categ = item?["Category"]?["Description"]?.ToString() ?? "";
                                    if (Categ.Length > 50)
                                    {
                                        Categ = Categ.Substring(0, 50);
                                    }
                                    CategID = item?["Category"]?["ID"] != null ? Convert.ToInt32(item["Category"]["ID"].ToString()) : 0;
                                }
                                string unt = "EACH";
                                if (item["Unit"] != null) unt = item?["Unit"]?.ToString();

                                long itemid = Convert.ToInt64(item["ID"] ?? 0);
                                var itm = _db.ItemsMasters.Where(x => x.ID == itemid).FirstOrDefault();
                                if (itm != null)
                                {
                                    if (item["Active"] != null) itm.Active = Convert.ToBoolean(item["Active"].ToString() ?? "");
                                    itm.AverageCost = Convert.ToDecimal(item["AverageCost"] ?? "", CultureInfo.InvariantCulture);
                                    itm.CategoryDescript = Categ;
                                    itm.CategoryID = CategID;
                                    itm.CompanyID = Userdetails.CoID;
                                    itm.Code = item?["Code"]?.ToString() ?? "";
                                    if (itm.Code.Length > 50) { itm.Code = itm.Code.Substring(0, 50); }
                                    itm.Description = item?["Description"]?.ToString() ?? ""; if (itm.Description.Length > 100) { itm.Description = itm.Description.Substring(0, 100); }
                                    itm.LastCost = Convert.ToDecimal(item?["LastCost"] ?? 0, CultureInfo.InvariantCulture);
                                    itm.NumericUserField1 = Convert.ToDecimal(item?["NumericUserField1"] ?? 0, CultureInfo.InvariantCulture);
                                    itm.NumericUserField2 = Convert.ToDecimal(item?["NumericUserField2"] ?? 0, CultureInfo.InvariantCulture);
                                    itm.NumericUserField3 = Convert.ToDecimal(item?["NumericUserField3"] ?? 0, CultureInfo.InvariantCulture);
                                    itm.Physical = Convert.ToBoolean(item?["Physical"]?.ToString() ?? "");
                                    itm.PriceExclusive = Convert.ToDecimal(item["PriceExclusive"] ?? 0, CultureInfo.InvariantCulture);
                                    itm.PriceInclusive = Convert.ToDecimal(item["PriceInclusive"] ?? 0, CultureInfo.InvariantCulture);
                                    itm.QuantityOnHand = Convert.ToDecimal(item["QuantityOnHand"] ?? 0, CultureInfo.InvariantCulture);
                                    itm.QuantityOnHand = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(itm.QuantityOnHand, CultureInfo.InvariantCulture), Userdetails.CompanyDecPlaces);
                                    itm.QuantityReserved = Convert.ToDecimal(item["QuantityReserved"] ?? 0, CultureInfo.InvariantCulture);
                                    itm.QuantityReserved = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(itm.QuantityReserved, CultureInfo.InvariantCulture), Userdetails.CompanyDecPlaces);
                                    itm.TextUserField1 = item?["TextUserField1"]?.ToString() ?? ""; if (itm.TextUserField1.Length > 100) { itm.TextUserField1 = itm.TextUserField1.Substring(0, 100); };
                                    itm.TextUserField2 = item?["TextUserField2"]?.ToString() ?? ""; if (itm.TextUserField1.Length > 100) { itm.TextUserField2 = itm.TextUserField2.Substring(0, 100); };
                                    itm.TextUserField3 = item?["TextUserField3"]?.ToString() ?? ""; if (itm.TextUserField3.Length > 100) { itm.TextUserField3 = itm.TextUserField3.Substring(0, 100); };
                                    string unit = item?["Unit"]?.ToString() ?? "";
                                    itm.Unit = unit.Length > 10 ? unit.Substring(0, 10) : unit;
                                    try
                                    {
                                        if (item["YesNoUserField1"] != null) itm.YesNoUserField1 = Convert.ToBoolean(item["YesNoUserField1"].ToString() ?? "");
                                        if (item["YesNoUserField2"] != null) itm.YesNoUserField2 = Convert.ToBoolean(item["YesNoUserField2"].ToString() ?? "");
                                        if (item["YesNoUserField3"] != null) itm.YesNoUserField3 = Convert.ToBoolean(item["YesNoUserField3"].ToString() ?? "");
                                    }
                                    catch { }
                                    try
                                    {
                                        if (Userdetails.SageWeightField.ToLower().Contains("userfield"))
                                        {
                                            decimal unitmass = Convert.ToDecimal(item[Userdetails.SageWeightField]);
                                            if (unitmass > 0) itm.NettMass = Convert.ToDecimal(item[Userdetails.SageWeightField] ?? 0, CultureInfo.InvariantCulture);
                                        }
                                    }
                                    catch { }
                                    
                                    try
                                    {
                                        itm.TotQOH_MDF = _db.ItemTransactions.Where(it => it.CompanyID == Userdetails.CoID && it.ItemID == itemid).Sum(it => it.Qty) ?? 0;
                                        itm.TotQOH_MDF = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(itm.TotQOH_MDF, CultureInfo.InvariantCulture), Userdetails.CompanyDecPlaces);
                                    }
                                    catch { itm.TotQOH_MDF = 0; }
                                try
                                {
                                    itm.TaxTypeIdSales = Convert.ToInt32(item["TaxTypeIdSales"] ?? 0);
                                }
                                catch { itm.TaxTypeIdSales = 0; }
                                    
                                    try
                                    {
                                        itm.TaxTypeSalesPerc = _db.TaxTypesMasters.Where(x => x.CompanyID == Userdetails.CoID && x.TaxTypeID == itm.TaxTypeIdSales).Select(x => x.TaxPerc).FirstOrDefault();
                                    }
                                    catch { };

                                    itm.TaxTypeIdPurchase = Convert.ToInt32(item["TaxTypeIdPurchases"] ?? 0);
                                    try
                                    {
                                        itm.TaxTypePurchPerc = _db.TaxTypesMasters.Where(x => x.CompanyID == Userdetails.CoID && x.TaxTypeID == itm.TaxTypeIdPurchase).Select(x => x.TaxPerc).FirstOrDefault();
                                    }
                                    catch { }

                                    try
                                    {
                                        itm.GPPercentage = Math.Round((decimal)(((itm.PriceExclusive - itm.AverageCost) / itm.PriceExclusive) * 100),2);
                                    }
                                    catch( Exception ex)
                                    { itm.GPPercentage = 0; }

                                    _db.Entry(itm).State = System.Data.Entity.EntityState.Modified;
                                    }
                                else
                                {
                                    ItemsMaster thisitm = new ItemsMaster();
                                    if (item["Active"] != null) thisitm.Active = Convert.ToBoolean(item["Active"].ToString() ?? "");
                                    thisitm.AverageCost = Convert.ToDecimal(item["AverageCost"] ?? "", CultureInfo.InvariantCulture);
                                    thisitm.CategoryDescript = Categ;
                                    thisitm.CategoryID = CategID;
                                    thisitm.CompanyID = Userdetails.CoID;
                                    thisitm.Code = item?["Code"]?.ToString() ?? "";
                                    if (thisitm.Code.Length > 50) { thisitm.Code = thisitm.Code.Substring(0, 50); }
                                    thisitm.Description = item?["Description"]?.ToString() ?? "";
                                    if (thisitm.Description.Length > 100) { thisitm.Description = thisitm.Description.Substring(0, 10); }
                                    thisitm.ID = Convert.ToInt32(item["ID"].ToString());
                                    thisitm.LastCost = Convert.ToDecimal(item["LastCost"] ?? 0, CultureInfo.InvariantCulture);
                                    thisitm.NumericUserField1 = Convert.ToDecimal(item["NumericUserField1"] ?? 0, CultureInfo.InvariantCulture);
                                    thisitm.NumericUserField2 = Convert.ToDecimal(item["NumericUserField2"] ?? 0, CultureInfo.InvariantCulture);
                                    thisitm.NumericUserField3 = Convert.ToDecimal(item["NumericUserField3"] ?? 0, CultureInfo.InvariantCulture);
                                    thisitm.Physical = Convert.ToBoolean(item["Physical"].ToString() ?? "");
                                    thisitm.IsLotTracked = false;
                                    if (thisitm.Physical == true) {thisitm.IsLotTracked = true;}
                                    thisitm.PriceExclusive = Convert.ToDecimal(item["PriceExclusive"] ?? 0, CultureInfo.InvariantCulture);
                                    thisitm.PriceInclusive = Convert.ToDecimal(item["PriceInclusive"] ?? 0, CultureInfo.InvariantCulture);
                                    thisitm.QuantityOnHand = Convert.ToDecimal(item["QuantityOnHand"] ?? 0, CultureInfo.InvariantCulture);
                                    thisitm.QuantityOnHand = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(thisitm.QuantityOnHand, CultureInfo.InvariantCulture), Userdetails.CompanyDecPlaces);
                                    thisitm.QuantityReserved = Convert.ToDecimal(item["QuantityReserved"] ?? 0, CultureInfo.InvariantCulture);
                                    thisitm.QuantityReserved = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(thisitm.QuantityReserved, CultureInfo.InvariantCulture), Userdetails.CompanyDecPlaces);
                                    thisitm.TextUserField1 = item?["TextUserField1"]?.ToString() ?? ""; if (thisitm.TextUserField1.Length > 100) { thisitm.TextUserField1 = thisitm.TextUserField1.Substring(0, 100); };
                                    thisitm.TextUserField2 = item?["TextUserField2"]?.ToString() ?? ""; if (thisitm.TextUserField1.Length > 100) { thisitm.TextUserField2 = thisitm.TextUserField2.Substring(0, 100); };
                                    thisitm.TextUserField3 = item?["TextUserField3"]?.ToString() ?? ""; if (thisitm.TextUserField3.Length > 100) { thisitm.TextUserField3 = thisitm.TextUserField3.Substring(0, 100); };
                                    thisitm.UOMConvert = 1;
                                    string unit = item?["Unit"]?.ToString() ?? "";
                                    thisitm.Unit = unit.Length > 10 ? unit.Substring(0, 10) : unit;
                                    if (item["YesNoUserField1"] != null) thisitm.YesNoUserField1 = Convert.ToBoolean(item["YesNoUserField1"].ToString() ?? "");
                                    if (item["YesNoUserField2"] != null) thisitm.YesNoUserField2 = Convert.ToBoolean(item["YesNoUserField2"].ToString() ?? "");
                                    if (item["YesNoUserField3"] != null) thisitm.YesNoUserField3 = Convert.ToBoolean(item["YesNoUserField3"].ToString() ?? "");

                                    if (Userdetails.SageWeightField.ToLower().Contains("userfield"))
                                    {
                                        decimal unitmass = Convert.ToDecimal(item[Userdetails.SageWeightField]);
                                        if (unitmass > 0) thisitm.NettMass = Convert.ToDecimal(item[Userdetails.SageWeightField] ?? 0, CultureInfo.InvariantCulture);
                                    }
                                    try
                                    {
                                        thisitm.TotQOH_MDF = _db.ItemTransactions.Where(it => it.CompanyID == Userdetails.CoID && it.ItemID == itemid).Sum(it => it.Qty) ?? 0;
                                        thisitm.TotQOH_MDF = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(thisitm.TotQOH_MDF, CultureInfo.InvariantCulture), Userdetails.CompanyDecPlaces);
                                    }
                                    catch { thisitm.TotQOH_MDF = 0; }
                                    thisitm.IsBOMComponent = false;
                                    thisitm.IsKitComponent = false;
                                    thisitm.IsFinishedGoods = true;
                                    thisitm.IsFromBOM = false;
                                    thisitm.IsFromKit = false;
                                    try
                                    {
                                        thisitm.TaxTypeIdSales = Convert.ToInt32(item["TaxTypeIdSales"] ?? 0);
                                    }   
                                    catch { thisitm.TaxTypeIdSales = 0; }

                                try
                                    {
                                    thisitm.TaxTypeSalesPerc = _db.TaxTypesMasters.Where(x => x.CompanyID == Userdetails.CoID && x.TaxTypeID == thisitm.TaxTypeIdSales).Select(x => x.TaxPerc).FirstOrDefault();
                                    }
                                    catch { }
                                    thisitm.TaxTypeIdPurchase = Convert.ToInt32(item["TaxTypeIdPurchases"] ?? 0);
                                    try
                                    {
                                    thisitm.TaxTypePurchPerc = _db.TaxTypesMasters.Where(x => x.CompanyID == Userdetails.CoID && x.TaxTypeID == thisitm.TaxTypeIdPurchase).Select(x => x.TaxPerc).FirstOrDefault();
                                    }
                                    catch { }
                                try
                                {
                                    thisitm.GPPercentage = Math.Round((decimal)(((thisitm.PriceExclusive - thisitm.AverageCost) / thisitm.PriceExclusive) * 100), 2);
                                }
                                catch { thisitm.GPPercentage = 0; }
                                _db.ItemsMasters.Add(thisitm);
                                }
                            }
                        }
                        UpdateDate = true;

                    }
                    parsedJSON.RemoveAll();
                    skipQty = skipQty + RetQty;

                    TimeSpan elapsed = DateTime.Now - requestStart;
                    if (elapsed.TotalMilliseconds < 1000)
                    {
                        await Task.Delay(1000 - (int)elapsed.TotalMilliseconds);
                    }

                    // moved save to here, so it saves ever 100 records. (from **Here** below
                    try
                    {
                        _db.SaveChanges();
                    }
                    catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                    {
                        foreach (var entityError in ex.EntityValidationErrors)
                        {
                            var errorMessages = entityError.ValidationErrors
                                .Select(x => x.PropertyName + ": " + x.ErrorMessage);

                            string fullErrorMessage = string.Join("; ", errorMessages);
                            var itemEntity = entityError.Entry.Entity as ItemsMaster;
                            string code = itemEntity?.Code ?? "Unknown";
                            string name = itemEntity?.Description ?? "Unknown";

                            string errMsg = $"CoID: {Userdetails.CoID} + LoadItems Entity error: Item {code} - {name}: {fullErrorMessage}";
                            LogErrorToFile(errMsg);
                        }
                    }
                    catch (Exception ex)
                    {
                        string errMsg = $"CoID: {Userdetails.CoID} + LoadItems error: {ex.Message}";
                        LogErrorToFile(errMsg);
                    }

                } while (skipQty < TotQty);

                //** Here **  See line 1301 above

                if (UpdateDate)
                {
                    if (LastItmCall != null)
                    {
                        LastItmCall.LastItemDate = Convert.ToDateTime(LastCallDt.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        LastCallLog newlog = new LastCallLog();
                        newlog.CompanyID = Userdetails.CoID;
                        newlog.LastItemDate = Convert.ToDateTime(LastCallDt.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture));
                        _db.LastCallLogs.Add(newlog);
                    }
                    try
                    {
                        _db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                       string errMsg = $"CoID: {Userdetails.CoID} + Error saving LastCallDate for Items: {ex.Message}";
                        LogErrorToFile(errMsg);
                    }
                }
                await LoadBundles(false, Userdetails);
                // update doclines, 
                string result = ApiUrlCall.SetSQLDataFromString($"EXEC Fix_DocLines_ItemCodes @CoID = {Userdetails.CoID}" );
                if (result != "OK")
                {
                    ApiUrlCall api = new ApiUrlCall();
                    api.LogErrorToFile("Ln1345 - Update item codes error:- " + result);
                }
                if (Userdetails.SageWeightField != null && Userdetails.SageWeightField != "")
                {
                    string result2 = ApiUrlCall.SetSQLDataFromString($"EXEC Updateweights @CoID = {Userdetails.CoID} , @massfield = '{Userdetails.SageWeightField}'");
                    if (result2 != "OK")
                    {
                        ApiUrlCall api = new ApiUrlCall();
                        api.LogErrorToFile("Ln1493 - Update Nett Mass error:- " + result2);
                    }
                }
            }
            return errorList;
        }

        public async Task LoadBundles(bool refresh, UserDetails Userdetails)
        {
            int TotQty = 0, RetQty = 0, skipQty = 0;
            DateTime LastCallDt = DateTime.Now;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
               do
                {
                    string requestUrl = sageurl + "ItemBundle/Get?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID + "&$skip=" + skipQty + "&includeDetail=true";
                    JObject parsedJSON = await ApiCallAsync(requestUrl, Userdetails);
                    if (parsedJSON.Count > 0)
                    {
                        JArray items = (JArray)parsedJSON["Results"];
                        TotQty = Convert.ToInt32(parsedJSON["TotalResults"]);
                        RetQty = Convert.ToInt32(parsedJSON["ReturnedResults"]);
                       
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                string Bundlecode = item["BundleCode"].ToString();
                                // add to BundleHeaders
                                var thisbund = _db.BundlesHeaders.Where(x => x.CompanyID == Userdetails.CoID && x.BundCode == Bundlecode).FirstOrDefault(); 
                                if (thisbund != null)
                                {
                                    // update
                                } else
                                {
                                    // insert
                                    BundlesHeader newbund = new BundlesHeader();
                                    newbund.Active = true;
                                    newbund.BundCode = item["BundleCode"].ToString();
                                    if (newbund.BundCode.Length > 50) { newbund.BundCode = newbund.BundCode.Substring(0, 50); }
                                    newbund.BundDescription = item["Description"].ToString();
                                    if (newbund.BundDescription.Length > 100) { newbund.BundDescription = newbund.BundDescription.Substring(0, 100); }
                                    newbund.BundCodeAndDescription = item["BundleCode"].ToString() + item["Description"].ToString();
                                    newbund.CompanyID = Userdetails.CoID;
                                    newbund.SBCAID = Convert.ToInt64(item["ID"].ToString());
                                    _db.BundlesHeaders.Add(newbund);  
                                }

                                // delete all current bundle lines
                                var DelBund = _db.BundlesLines.Where(x => x.CompanyID == Userdetails.CoID && x.BundCode == Bundlecode).ToList();
                                _db.BundlesLines.RemoveRange(DelBund);
                                _db.SaveChanges();
                                JArray BundItems = (JArray)item["ItemBundleItems"];
                                foreach (var BundItem in BundItems)
                                {    
                                    // Add lines to BundleLines
                                    BundlesLine BundL = new BundlesLine();
                                    BundL.SBCAID = Convert.ToInt64(BundItem["ItemId"].ToString());
                                    BundL.BLQuantity = Convert.ToDecimal(BundItem["Quantity"].ToString());
                                    BundL.BundCode = item["BundleCode"].ToString();
                                    if (BundL.BundCode.Length > 50) { BundL.BundCode = BundL.BundCode.Substring(0, 50); }
                                    BundL.BundDescription = item["Description"].ToString();
                                    if (BundL.BundDescription.Length > 100) { BundL.BundDescription = BundL.BundDescription.Substring(0, 100); }
                                    BundL.CompanyID = Userdetails.CoID;
                                    long ItmIDL = Convert.ToInt64(BundItem["ItemId"].ToString());
                                    var itm = _db.ItemsMasters.Where(x => x.ID == ItmIDL && x.CompanyID == Userdetails.CoID).FirstOrDefault();
                                   if (itm != null){
                                        BundL.BLCode = itm.Code;
                                        BundL.BLDescription = itm.Description;
                                        BundL.BLAverageCost = Convert.ToDecimal(itm.AverageCost);
                                        BundL.PriceListCostExclusive = Convert.ToDecimal(itm.PriceExclusive) * BundL.BLQuantity;
                                        BundL.PriceListCostInclusive = Convert.ToDecimal(itm.PriceInclusive) * BundL.BLQuantity;
                                    };    
                                    _db.BundlesLines.Add(BundL);
                                }
                                try
                                {
                                    _db.SaveChanges();
                                }
                                catch (Exception ex) { }
                            }
                        }
                    }
                    parsedJSON.RemoveAll();
                    skipQty = skipQty + RetQty;
                } while (skipQty < TotQty);
            }
         }

        public async Task LoadOneItem(long ItemID, UserDetails Userdetails)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string requestUrl = sageurl + "Item/GET/" + ItemID + "?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID;
                JObject parsedJSON = await ApiCallAsync(requestUrl, Userdetails);
                if (parsedJSON.Count > 0)
                    {
                            string Categ = string.Empty; int CategID = 0;
                            if (parsedJSON.ToString().Contains("Category"))
                            {
                                Categ = parsedJSON["Category"]["Description"].ToString();
                                CategID = Convert.ToInt32(parsedJSON["Category"]["ID"].ToString());
                            }
                            string unt = "EACH";
                            if (parsedJSON["Unit"] != null) unt = parsedJSON["Unit"].ToString();

                            var itm = _db.ItemsMasters.Where(x => x.ID == ItemID).FirstOrDefault();
                            if (itm != null)
                            {
                                if (parsedJSON["Active"] != null) itm.Active = Convert.ToBoolean(parsedJSON["Active"].ToString() ?? "");
                                itm.AverageCost = Convert.ToDecimal(parsedJSON["AverageCost"] ?? "", CultureInfo.InvariantCulture);
                                itm.CategoryDescript = Categ;
                                itm.CategoryID = CategID;
                                itm.CompanyID = Userdetails.CoID;
                                itm.Code = (parsedJSON["Code"] ?? "").ToString();
                                itm.Description = (parsedJSON["Description"] ?? "").ToString();
                                itm.LastCost = Convert.ToDecimal(parsedJSON["LastCost"] ?? 0, CultureInfo.InvariantCulture);
                                itm.NumericUserField1 = Convert.ToDecimal(parsedJSON["NumericUserField1"] ?? 0, CultureInfo.InvariantCulture);
                                itm.NumericUserField2 = Convert.ToDecimal(parsedJSON["NumericUserField2"] ?? 0, CultureInfo.InvariantCulture);
                                itm.NumericUserField3 = Convert.ToDecimal(parsedJSON["NumericUserField3"] ?? 0, CultureInfo.InvariantCulture);
                                itm.Physical = Convert.ToBoolean(parsedJSON["Physical"].ToString() ?? "");
                                itm.PriceExclusive = Convert.ToDecimal(parsedJSON["PriceExclusive"] ?? 0, CultureInfo.InvariantCulture);
                                itm.PriceInclusive = Convert.ToDecimal(parsedJSON["PriceInclusive"] ?? 0, CultureInfo.InvariantCulture);
                                itm.QuantityOnHand = Convert.ToDecimal(parsedJSON["QuantityOnHand"] ?? 0, CultureInfo.InvariantCulture);
                                itm.QuantityReserved = Convert.ToDecimal(parsedJSON["QuantityReserved"] ?? 0, CultureInfo.InvariantCulture);
                                itm.TextUserField1 = (parsedJSON["TextUserField1"] ?? "").ToString();
                                itm.TextUserField2 = (parsedJSON["TextUserField2"] ?? "").ToString();
                                itm.TextUserField3 = (parsedJSON["TextUserField3"] ?? "").ToString();
                            string unit = (parsedJSON["Unit"] ?? "").ToString() ?? "";
                            itm.Unit = unit.Length > 10 ? unit.Substring(0, 10) : unit;
                        if (parsedJSON["YesNoUserField1"] != null) itm.YesNoUserField1 = Convert.ToBoolean(parsedJSON["YesNoUserField1"].ToString() ?? "");
                                if (parsedJSON["YesNoUserField2"] != null) itm.YesNoUserField2 = Convert.ToBoolean(parsedJSON["YesNoUserField2"].ToString() ?? "");
                                if (parsedJSON["YesNoUserField3"] != null) itm.YesNoUserField3 = Convert.ToBoolean(parsedJSON["YesNoUserField3"].ToString() ?? "");
                                _db.Entry(itm).State = System.Data.Entity.EntityState.Modified;
                            }
                            else
                            {
                                ItemsMaster thisitm = new ItemsMaster();
                                if (parsedJSON["Active"] != null) thisitm.Active = Convert.ToBoolean(parsedJSON["Active"].ToString() ?? "");
                                thisitm.AverageCost = Convert.ToDecimal(parsedJSON["AverageCost"] ?? "", CultureInfo.InvariantCulture);
                                thisitm.CategoryDescript = Categ;
                                thisitm.CategoryID = CategID;
                                thisitm.CompanyID = Userdetails.CoID;
                                thisitm.Code = (parsedJSON["Code"] ?? "").ToString();
                                thisitm.Description = (parsedJSON["Description"] ?? "").ToString();
                                thisitm.ID = Convert.ToInt32(parsedJSON["ID"].ToString());
                                thisitm.LastCost = Convert.ToDecimal(parsedJSON["LastCost"] ?? 0, CultureInfo.InvariantCulture);
                                thisitm.NumericUserField1 = Convert.ToDecimal(parsedJSON["NumericUserField1"] ?? 0, CultureInfo.InvariantCulture);
                                thisitm.NumericUserField2 = Convert.ToDecimal(parsedJSON["NumericUserField2"] ?? 0, CultureInfo.InvariantCulture);
                                thisitm.NumericUserField3 = Convert.ToDecimal(parsedJSON["NumericUserField3"] ?? 0, CultureInfo.InvariantCulture);
                                thisitm.Physical = Convert.ToBoolean(parsedJSON["Physical"].ToString() ?? "");
                                thisitm.PriceExclusive = Convert.ToDecimal(parsedJSON["PriceExclusive"] ?? 0, CultureInfo.InvariantCulture);
                                thisitm.PriceInclusive = Convert.ToDecimal(parsedJSON["PriceInclusive"] ?? 0, CultureInfo.InvariantCulture);
                                thisitm.QuantityOnHand = Convert.ToDecimal(parsedJSON["QuantityOnHand"] ?? 0, CultureInfo.InvariantCulture);
                                thisitm.QuantityReserved = Convert.ToDecimal(parsedJSON["QuantityReserved"] ?? 0, CultureInfo.InvariantCulture);
                                thisitm.TextUserField1 = (parsedJSON["TextUserField1"] ?? "").ToString();
                                thisitm.TextUserField2 = (parsedJSON["TextUserField2"] ?? "").ToString();
                                thisitm.TextUserField3 = (parsedJSON["TextUserField3"] ?? "").ToString();
                        string unit = (parsedJSON["Unit"] ?? "").ToString() ?? "";
                        thisitm.Unit = unit.Length > 10 ? unit.Substring(0, 10) : unit;
                        if (parsedJSON["YesNoUserField1"] != null) thisitm.YesNoUserField1 = Convert.ToBoolean(parsedJSON["YesNoUserField1"].ToString() ?? "");
                                if (parsedJSON["YesNoUserField2"] != null) thisitm.YesNoUserField2 = Convert.ToBoolean(parsedJSON["YesNoUserField2"].ToString() ?? "");
                                if (parsedJSON["YesNoUserField3"] != null) thisitm.YesNoUserField3 = Convert.ToBoolean(parsedJSON["YesNoUserField3"].ToString() ?? "");
                                thisitm.IsBOMComponent = false;
                                thisitm.IsKitComponent = false;
                                thisitm.IsFinishedGoods = true;
                                thisitm.IsFromBOM = false;
                                thisitm.IsFromKit = false;
                                thisitm.TaxTypeIdSales = Convert.ToInt32(parsedJSON["TaxTypeIdSales"] ?? 0);
                                thisitm.TaxTypeSalesPerc = _db.TaxTypesMasters.Where(x => x.CompanyID == Userdetails.CoID && x.TaxTypeID == thisitm.TaxTypeIdSales).Select(x => x.TaxPerc).FirstOrDefault();
                                thisitm.TaxTypeIdPurchase = Convert.ToInt32(parsedJSON["TaxTypeIdPurchases"] ?? 0);
                                thisitm.TaxTypePurchPerc = _db.TaxTypesMasters.Where(x => x.CompanyID == Userdetails.CoID && x.TaxTypeID == thisitm.TaxTypeIdPurchase).Select(x => x.TaxPerc).FirstOrDefault();
                        _db.ItemsMasters.Add(thisitm);
                            }  
                    }
                    parsedJSON.RemoveAll();              
                _db.SaveChanges();
            }
        }

        #region Non-Async calls
        /// <summary>
        /// Refreshes ONE item from Sage into ItemsMasters.
        /// Returns TRUE only when Sage actually returned the item and it was written away.
        /// ApiCallNA swallows every transport failure and hands back an empty object, so a
        /// FALSE here means the local QuantityOnHand / AverageCost are STALE - callers that
        /// compute a new average cost from them must not post, or they will push a figure
        /// derived from old data into Sage (which SETS the average).
        /// </summary>
        public bool LoadOneItemNA(long ItemID, UserDetails Userdetails)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string requestUrl = sageurl + "Item/GET/" + ItemID + "?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID;
                JObject parsedJSON = ApiCallNA(requestUrl, Userdetails); // Call the API synchronously

                if (parsedJSON.Count == 0) return false;   // Sage call failed - local copy is stale

                {
                    string Categ = string.Empty;
                    int CategID = 0;
                    if (parsedJSON.ToString().Contains("Category"))
                    {
                        Categ = parsedJSON["Category"]["Description"].ToString();
                        CategID = Convert.ToInt32(parsedJSON["Category"]["ID"].ToString());
                    }
                    string unt = "EACH";
                    if (parsedJSON["Unit"] != null) unt = parsedJSON["Unit"].ToString();

                    var itm = _db.ItemsMasters.FirstOrDefault(x => x.ID == ItemID);
                    if (itm != null)
                    {
                        if (parsedJSON["Active"] != null) itm.Active = Convert.ToBoolean(parsedJSON["Active"].ToString() ?? "");
                        itm.AverageCost = Convert.ToDecimal(parsedJSON["AverageCost"] ?? "", CultureInfo.InvariantCulture);
                        itm.CategoryDescript = Categ;
                        itm.CategoryID = CategID;
                        itm.CompanyID = Userdetails.CoID;
                        itm.Code = (parsedJSON["Code"] ?? "").ToString();
                        itm.Description = (parsedJSON["Description"] ?? "").ToString();
                        itm.LastCost = Convert.ToDecimal(parsedJSON["LastCost"] ?? 0, CultureInfo.InvariantCulture);
                        itm.NumericUserField1 = Convert.ToDecimal(parsedJSON["NumericUserField1"] ?? 0, CultureInfo.InvariantCulture);
                        itm.NumericUserField2 = Convert.ToDecimal(parsedJSON["NumericUserField2"] ?? 0, CultureInfo.InvariantCulture);
                        itm.NumericUserField3 = Convert.ToDecimal(parsedJSON["NumericUserField3"] ?? 0, CultureInfo.InvariantCulture);
                        itm.Physical = Convert.ToBoolean(parsedJSON["Physical"].ToString() ?? "");
                        itm.PriceExclusive = Convert.ToDecimal(parsedJSON["PriceExclusive"] ?? 0, CultureInfo.InvariantCulture);
                        itm.PriceInclusive = Convert.ToDecimal(parsedJSON["PriceInclusive"] ?? 0, CultureInfo.InvariantCulture);
                        itm.QuantityOnHand = Convert.ToDecimal(parsedJSON["QuantityOnHand"] ?? 0, CultureInfo.InvariantCulture);
                        itm.QuantityReserved = Convert.ToDecimal(parsedJSON["QuantityReserved"] ?? 0, CultureInfo.InvariantCulture);
                        itm.TextUserField1 = (parsedJSON["TextUserField1"] ?? "").ToString();
                        itm.TextUserField2 = (parsedJSON["TextUserField2"] ?? "").ToString();
                        itm.TextUserField3 = (parsedJSON["TextUserField3"] ?? "").ToString();
                        string unit = (parsedJSON["Unit"] ?? "").ToString() ?? "";
                        itm.Unit = unit.Length > 10 ? unit.Substring(0, 10) : unit;
                        if (parsedJSON["YesNoUserField1"] != null) itm.YesNoUserField1 = Convert.ToBoolean(parsedJSON["YesNoUserField1"].ToString() ?? "");
                        if (parsedJSON["YesNoUserField2"] != null) itm.YesNoUserField2 = Convert.ToBoolean(parsedJSON["YesNoUserField2"].ToString() ?? "");
                        if (parsedJSON["YesNoUserField3"] != null) itm.YesNoUserField3 = Convert.ToBoolean(parsedJSON["YesNoUserField3"].ToString() ?? "");
                        _db.Entry(itm).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        ItemsMaster thisitm = new ItemsMaster();
                        if (parsedJSON["Active"] != null) thisitm.Active = Convert.ToBoolean(parsedJSON["Active"].ToString() ?? "");
                        thisitm.AverageCost = Convert.ToDecimal(parsedJSON["AverageCost"] ?? "", CultureInfo.InvariantCulture);
                        thisitm.CategoryDescript = Categ;
                        thisitm.CategoryID = CategID;
                        thisitm.CompanyID = Userdetails.CoID;
                        thisitm.Code = (parsedJSON["Code"] ?? "").ToString();
                        thisitm.Description = (parsedJSON["Description"] ?? "").ToString();
                        thisitm.ID = Convert.ToInt32(parsedJSON["ID"].ToString());
                        thisitm.LastCost = Convert.ToDecimal(parsedJSON["LastCost"] ?? 0, CultureInfo.InvariantCulture);
                        thisitm.NumericUserField1 = Convert.ToDecimal(parsedJSON["NumericUserField1"] ?? 0, CultureInfo.InvariantCulture);
                        thisitm.NumericUserField2 = Convert.ToDecimal(parsedJSON["NumericUserField2"] ?? 0, CultureInfo.InvariantCulture);
                        thisitm.NumericUserField3 = Convert.ToDecimal(parsedJSON["NumericUserField3"] ?? 0, CultureInfo.InvariantCulture);
                        thisitm.Physical = Convert.ToBoolean(parsedJSON["Physical"].ToString() ?? "");
                        thisitm.PriceExclusive = Convert.ToDecimal(parsedJSON["PriceExclusive"] ?? 0, CultureInfo.InvariantCulture);
                        thisitm.PriceInclusive = Convert.ToDecimal(parsedJSON["PriceInclusive"] ?? 0, CultureInfo.InvariantCulture);
                        thisitm.QuantityOnHand = Convert.ToDecimal(parsedJSON["QuantityOnHand"] ?? 0, CultureInfo.InvariantCulture);
                        thisitm.QuantityReserved = Convert.ToDecimal(parsedJSON["QuantityReserved"] ?? 0, CultureInfo.InvariantCulture);
                        thisitm.TextUserField1 = (parsedJSON["TextUserField1"] ?? "").ToString();
                        thisitm.TextUserField2 = (parsedJSON["TextUserField2"] ?? "").ToString();
                        thisitm.TextUserField3 = (parsedJSON["TextUserField3"] ?? "").ToString();
                        string unit = (parsedJSON["Unit"] ?? "").ToString() ?? "";
                        thisitm.Unit = unit.Length > 10 ? unit.Substring(0, 10) : unit;
                        if (parsedJSON["YesNoUserField1"] != null) thisitm.YesNoUserField1 = Convert.ToBoolean(parsedJSON["YesNoUserField1"].ToString() ?? "");
                        if (parsedJSON["YesNoUserField2"] != null) thisitm.YesNoUserField2 = Convert.ToBoolean(parsedJSON["YesNoUserField2"].ToString() ?? "");
                        if (parsedJSON["YesNoUserField3"] != null) thisitm.YesNoUserField3 = Convert.ToBoolean(parsedJSON["YesNoUserField3"].ToString() ?? "");
                        thisitm.IsBOMComponent = false;
                        thisitm.IsKitComponent = false;
                        thisitm.IsFinishedGoods = true;
                        thisitm.IsFromBOM = false;
                        thisitm.IsFromKit = false;
                        thisitm.TaxTypeIdSales = Convert.ToInt32(parsedJSON["TaxTypeIdSales"] ?? 0);
                        thisitm.TaxTypeSalesPerc = _db.TaxTypesMasters.Where(x => x.CompanyID == Userdetails.CoID && x.TaxTypeID == thisitm.TaxTypeIdSales).Select(x => x.TaxPerc).FirstOrDefault();
                        thisitm.TaxTypeIdPurchase = Convert.ToInt32(parsedJSON["TaxTypeIdPurchases"] ?? 0);
                        thisitm.TaxTypePurchPerc = _db.TaxTypesMasters.Where(x => x.CompanyID == Userdetails.CoID && x.TaxTypeID == thisitm.TaxTypeIdPurchase).Select(x => x.TaxPerc).FirstOrDefault();
                        _db.ItemsMasters.Add(thisitm);
                    }
                }
                parsedJSON.RemoveAll();
                _db.SaveChanges();
                return true;
            }
        }

        public JObject ApiCallNA(string requestUrl, UserDetails userDetails)
        {
            using (HttpClient client = new HttpClient())
            {
                // Set Basic Authentication Header
                client.Timeout = TimeSpan.FromSeconds(30); // Set the timeout as per your need
                string combined = $"{userDetails.LoginName}:{userDetails.LoginPwd}";
                string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Encoded);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                JObject parsedJSON = new JObject();

                try
                {
                    // Send GET request
                    Console.WriteLine($"Making request to: {requestUrl}");
                    HttpResponseMessage response = client.GetAsync(requestUrl).Result; // Synchronous call
                    Console.WriteLine($"Response Status: {response.StatusCode}");

                    // Check if response was successful
                    if (response.IsSuccessStatusCode)
                    {
                        string content = response.Content.ReadAsStringAsync().Result; // Synchronous call
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            parsedJSON = JObject.Parse(content);
                        }
                    }
                    else
                    {
                        LogErrorToFile($"ApiCallNA request failed: {response.StatusCode} - {response.Content.ReadAsStringAsync().Result} - URL: {requestUrl}");
                    }
                }
                catch (TimeoutException ex)
                {
                    LogErrorToFile($"ApiCallNA timeout: {ex.Message} - URL: {requestUrl}");
                }
                catch (Exception ex)
                {
                    LogErrorToFile($"ApiCallNA error: {ex.Message} - URL: {requestUrl}");
                }

                return parsedJSON;
            }
        }

        public JObject APIPostDocumentNA(string DocType, string JsonStr, UserDetails Userdetails)
        {
            JObject parsedJSON = new JObject();
            string requestUrl = $"{sageurl}{DocType}/Save?apikey={{{APIKey}}}&CompanyID={Userdetails.CoID}";
            var options = new RestClientOptions(requestUrl)
            {
                ThrowOnAnyError = false,
                ThrowOnDeserializationError = false
            };
            var client = new RestClient(options);
            var requ = new RestRequest();
            string combined = $"{Userdetails.LoginName}:{Userdetails.LoginPwd}";
            string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
            requ.AddHeader("Authorization", "Basic " + base64Encoded);
            requ.Method = Method.Post;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            requ.AddJsonBody(JsonStr);

            try
            {
                var response = client.Execute(requ); // Synchronous execution
                if (response.IsSuccessful && !string.IsNullOrWhiteSpace(response.Content))
                {
                    parsedJSON = JObject.Parse(response.Content);
                }
                else
                {
                    parsedJSON["error"] = new JObject
                    {
                        ["statusCode"] = response.StatusCode != null ? (int)response.StatusCode : 0,
                        ["reason"] = response.StatusDescription ?? "Unknown error",
                        ["message"] = response.Content ?? "No content returned"
                    };
                }
            }
            catch (Exception ex)
            {
                parsedJSON["error"] = new JObject
                {
                    ["exception"] = ex.Message, 
                    ["stackTrace"] = ex.StackTrace
                };
            }
            return parsedJSON;
        }

        public async Task<string> UpdateSellingPriceOneItem(long ItemID, decimal newUnitPriceExclusive, UserDetails Userdetails)
        {
            try
            {
                // Get item from API
                string requestUrl = sageurl + $"Item/GET?includeAdditionalItemPrices=false&includeAttachments=false&apikey={APIKey}&$filter=ID eq {ItemID}&CompanyID={Userdetails.CoID}";

                // Use ConfigureAwait(false) to prevent returning to original context
                JObject parsedJSON = await ApiCallAsync(requestUrl, Userdetails).ConfigureAwait(false);

                if (parsedJSON == null)
                {
                    return "Failed to get item from API: Null response";
                }

                // Check for error from ApiCallAsync
                if (parsedJSON["error"] != null)
                {
                    return $"API error: {parsedJSON["error"]["message"]}";
                }

                if (parsedJSON["Results"] == null || !parsedJSON["Results"].HasValues)
                {
                    return $"No item found with ID: {ItemID}";
                }

                // Get the item data
                JObject itemData = (JObject)parsedJSON["Results"].First;

                // Calculate the VAT/tax percentage from original prices
                decimal originalPriceExclusive = itemData["PriceExclusive"]?.Value<decimal>() ?? 0;
                decimal originalPriceInclusive = itemData["PriceInclusive"]?.Value<decimal>() ?? 0;

                decimal vatPercentage = 0;
                if (originalPriceExclusive > 0)
                {
                    vatPercentage = ((originalPriceInclusive / originalPriceExclusive) - 1) * 100;
                }
                else
                {
                    // Default to 15% VAT if can't calculate
                    vatPercentage = 15;
                }

                // Update the prices
                itemData["PriceExclusive"] = Math.Round(newUnitPriceExclusive, 4);
                itemData["PriceInclusive"] = Math.Round(newUnitPriceExclusive * (1 + (vatPercentage / 100)), 4);

                // Remove any properties that might cause issues
                itemData.Remove("Modified");
                itemData.Remove("Created");

                // Convert to JSON string
                string jsonPayload = itemData.ToString(Newtonsoft.Json.Formatting.None);

                // Make POST call - also with ConfigureAwait(false)
                string postUrl = sageurl + $"Item/Save?apikey={{{APIKey}}}&CompanyID={Userdetails.CoID}";

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    // Add authentication
                    string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Userdetails.LoginName}:{Userdetails.LoginPwd}"));
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(postUrl, content).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return $"API update failed. Status: {response.StatusCode}, Error: {errorContent}";
                    }

                    return "OK";
                }
            }
            catch (Exception ex)
            {
                return $"Failed to update selling price for item {ItemID}: {ex.Message}";
            }
        }
        #endregion

        public class POList
        {
            public string SupplierName { get; set; }
            public string DocumentNumber { get; set; }
            public string Reference { get; set; }
            public DateTime DeliveryDate { get; set; }
        }

        #region SalesOrders
        public async Task <JObject> LoadSalesOrders(UserDetails Userdetails)
        {
            lock (_SOsyncLock)
            {
                if (_SOisSyncRunning)
                    return new JObject();
                _SOisSyncRunning = true;
            }

            bool UpdateDate = false;
            double skipQty = 0; float TotQty = 0; int RetQty = 0;
            int usedoc = 0;
            DataSet ds = new DataSet();
            DateTime LastCallDt = DateTime.Now;
            JObject parsedJSON = null;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var LastItmCall = _db.LastCallLogs.Where(x => x.CompanyID == Userdetails.CoID).FirstOrDefault();
                if (LastItmCall != null)
                {
                    if(LastItmCall.LastSODate != null)
                    {
                        SOrdDT = (DateTime)LastItmCall.LastSODate;
                    }
                    else
                    {
                        SOrdDT = DateTime.Today.AddMonths(-12);
                    }     
                  
                }
                else
                {
                    SOrdDT = DateTime.Today.AddMonths(-12);
                }
                LastCallDt = DateTime.Now;

                // get all incomplete POs
                var CompList = _db.DocHeaders
                 .Where(x => x.CompanyID == Userdetails.CoID && x.DocType == 5 && x.Complete == true)
                 .Select(x => x.DocID)
                 .ToList();
                var compDocIds = new HashSet<long>(CompList);

                do
                    {
                        string requestUrl = sageurl + "SalesOrder/GET?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID + "&$skip=" + skipQty + FiltDate(SOrdDT.ToString()) + ")&includeDetail=true&includeCustomerDetails=true";
                        ApiUrlCall api = new ApiUrlCall();
                    try
                    {
                        parsedJSON = await ApiCallAsync(requestUrl, Userdetails);
                    }
                    catch (Exception ex)
                    {
                        _SOisSyncRunning = false;
                        string errMsg = $"CoID: {Userdetails.CoID} + LoadSalesOrders Entity error: Err 2005 - {ex.Message}: {ex.InnerException}";
                        LogErrorToFile(errMsg);
                        parsedJSON = new JObject
                        { 
                            ["error"] = "Err 1643 - " + ex.Message
                        };
                    }

                    if (parsedJSON.Count > 0)
                    {
                        JArray items = (JArray)parsedJSON["Results"];
                        TotQty = Convert.ToInt32(parsedJSON["TotalResults"]);
                        RetQty = Convert.ToInt32(parsedJSON["ReturnedResults"]);

                        string SalesRep = string.Empty; string Ref = string.Empty;
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                long thisdocid = Convert.ToInt64(item["ID"].ToString());
                                if (item["Lines"] != null)
                                {
                                    List<DocumentLine> myObjects = JsonConvert.DeserializeObject<List<DocumentLine>>(item["Lines"].ToString());
                                    foreach (DocumentLine obj in myObjects)
                                    {
                                        if (obj.LineType.ToString() == "0")
                                        {
                                            usedoc = 1;
                                            break;
                                        }
                                    }
                                }
                                if (usedoc == 1)
                                {
                                    var ChkDoc = _db.DocHeaders.Where(x => x.DocID == thisdocid && x.CompanyID == Userdetails.CoID).FirstOrDefault();
                                    if (ChkDoc != null)
                                    {
                                        ChkDoc.DocDate = Convert.ToDateTime(item["Date"].ToString());
                                        ChkDoc.DueDelDate = Convert.ToDateTime(item["DeliveryDate"] ?? "");
                                        ChkDoc.Status = item["Status"].ToString();
                                        ChkDoc.Discount = Convert.ToDecimal(item["Discount"].ToString());
                                        ChkDoc.Exclusive = Convert.ToDecimal(item["Exclusive"].ToString());
                                        ChkDoc.Tax = Convert.ToDecimal(item["Tax"].ToString());
                                        ChkDoc.Rounding = Convert.ToDecimal(item["Rounding"].ToString());
                                        ChkDoc.Total = Convert.ToDecimal(item["Total"].ToString());
                                        ChkDoc.Reference = item["Reference"].ToString();
                                        ChkDoc.Message = item["Message"].ToString();
                                        ChkDoc.DelAddress1 = item["DeliveryAddress01"].ToString() ?? "";
                                        ChkDoc.DelAddress2 = item["DeliveryAddress02"].ToString() ?? "";
                                        ChkDoc.DelAddress3 = item["DeliveryAddress03"].ToString() ?? "";
                                        ChkDoc.DelAddress4 = item["DeliveryAddress04"].ToString() ?? "";
                                        ChkDoc.DelAddress5 = item["DeliveryAddress05"].ToString() ?? "";
                                        ChkDoc.PostAddress5 = item["PostalAddress05"].ToString() ?? "";
                                        if (item.ToString().Contains("SalesRepresentative"))
                                        {
                                        ChkDoc.SalesRepresentativeId = Convert.ToInt64(item["SalesRepresentativeId"].ToString());
                                        ChkDoc.SalesRepName = item["SalesRepresentative"]["Name"].ToString().Replace("'", "''");
                                        }
                                        
                                        ChkDoc.Customer_ExchangeRate = 1;
                                        if (item["Customer_CurrencyId"] != null)
                                        {
                                            ChkDoc.Customer_ExchangeRate = Convert.ToDecimal(item["Customer_ExchangeRate"]);
                                            ChkDoc.Customer_CurrencyId = Convert.ToInt64(item["Customer_CurrencyId"]);
                                        }
                                        _db.Entry(ChkDoc).State = System.Data.Entity.EntityState.Modified;
                                        try
                                        {
                                            _db.SaveChanges();
                                        }
                                        catch (Exception ex) {
                                            string errMsg = $"CoID: {Userdetails.CoID} + LoadSalesOrders Entity error: Err 2078 - {ex.Message}: {ex.InnerException}";
                                            LogErrorToFile(errMsg);
                                            string str = ex.Message; 
                                        }
                                        
                                        await api.LoadSOLines(Convert.ToInt64(item["ID"].ToString()), Userdetails);
                                        //long currentDocId = Convert.ToInt64(item["ID"].ToString());
                                        //if (!compDocIds.Contains(currentDocId))
                                        //{
                                        //    await LoadSOLines(currentDocId, Userdetails);
                                        //}
                                    }
                                    else
                                    {
                                        DocHeader DocH = new DocHeader();
                                        DocH.DocGUID = Guid.NewGuid();
                                        DocH.DocDate = Convert.ToDateTime(item["Date"].ToString());
                                        DocH.DocID = Convert.ToInt64(item["ID"].ToString());
                                        DocH.DueDelDate = Convert.ToDateTime(item["DeliveryDate"] ?? "");
                                        DocH.DocumentNumber = item["DocumentNumber"].ToString().Trim() ?? "";
                                        DocH.CustSuppID = Convert.ToInt64(item["CustomerId"].ToString());
                                        DocH.CustSupName = item["CustomerName"].ToString().Trim() ?? "";
                                        DocH.CompanyID = Convert.ToInt32(Userdetails.CoID);
                                        DocH.Started = false;
                                        DocH.Complete = false;
                                        DocH.Status = item["Status"].ToString().Trim();
                                        DocH.Reference = item["Reference"].ToString().Trim();
                                        DocH.Message = item["Message"].ToString().Trim();
                                        DocH.Discount = Convert.ToDecimal(item["Discount"].ToString());
                                        DocH.Exclusive = Convert.ToDecimal(item["Exclusive"].ToString());
                                        DocH.Tax = Convert.ToDecimal(item["Tax"].ToString());
                                        DocH.Rounding = Convert.ToDecimal(item["Rounding"].ToString());
                                        DocH.Total = Convert.ToDecimal(item["Total"].ToString());
                                        DocH.DocType = 5;
                                        DocH.DelAddress1 = item["DeliveryAddress01"].ToString().Trim() ?? "";
                                        DocH.DelAddress2 = item["DeliveryAddress02"].ToString().Trim() ?? "";
                                        DocH.DelAddress3 = item["DeliveryAddress03"].ToString().Trim() ?? "";
                                        DocH.DelAddress4 = item["DeliveryAddress04"].ToString().Trim() ?? "";
                                        DocH.DelAddress5 = item["DeliveryAddress05"].ToString().Trim() ?? "";
                                        DocH.PostAddress5 = item["PostalAddress05"].ToString().Trim() ?? "";
                                        if (item.ToString().Contains("SalesRepresentative"))
                                        {
                                        DocH.SalesRepresentativeId = Convert.ToInt64(item["SalesRepresentativeId"].ToString());
                                        DocH.SalesRepName = item["SalesRepresentative"]["Name"].ToString().Replace("'", "''").Trim();
                                        }

                                        DocH.Customer_ExchangeRate = 1;
                                        if (item["Customer_CurrencyId"] != null)
                                        {
                                            DocH.Customer_ExchangeRate = Convert.ToDecimal(item["Customer_ExchangeRate"]);
                                            DocH.Customer_CurrencyId = Convert.ToInt64(item["Customer_CurrencyId"]);
                                        }

                                        DocH.Active = true;
                                        _db.DocHeaders.Add(DocH);
                                        try
                                        {
                                            _db.SaveChanges();
                                        }
                                        catch (Exception ex) 
                                        {
                                            string errMsg = $"CoID: {Userdetails.CoID} + LoadSalesOrders error: PO {item["DocumentNumber"].ToString() ?? ""}: {ex.Message}";
                                            LogErrorToFile(errMsg);
                                        }
                                        await api.LoadSOLines(Convert.ToInt64(item["ID"].ToString()), Userdetails);
                                        //long currentDocId = Convert.ToInt64(item["ID"].ToString());
                                        //if (!compDocIds.Contains(currentDocId))
                                        //{
                                        //    await LoadSOLines(currentDocId, Userdetails);
                                        //}

                                        // get all user who need to be notified
                                        long CoID = Convert.ToInt32(Userdetails.CoID);
                                        var UserList = _db.RolesMasters.Where(x => x.CompanyID == CoID && x.NotifyNewSO == true).ToList();
                                        if (UserList.Count > 0)
                                        {
                                            foreach (var usr in UserList)
                                            {
                                                var Notif = new Notification
                                                {
                                                    Message = "New Sales Order Received: " + DocH.DocumentNumber,
                                                    IsRead = false,
                                                    CreatedAt = DateTime.Now,
                                                    CompanyID = Userdetails.CoID,
                                                    UserRoleID = usr.RoleID
                                                };
                                                _db.Notifications.Add(Notif);
                                            }
                                                try
                                                {
                                                    _db.SaveChanges();
                                                }
                                                    catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                                                    {
                                                        var errorMessages = ex.EntityValidationErrors
                                                            .SelectMany(x => x.ValidationErrors)
                                                            .Select(x => x.PropertyName + ": " + x.ErrorMessage);

                                                        string fullErrorMessage = string.Join("; ", errorMessages);
                                                        string code = item?["Code"]?.ToString() ?? "Unknown";
                                                        string name = item?["Description"]?.ToString() ?? "Unknown";

                                                        string errMsg = $"CoID: {Userdetails.CoID} + LoadSalesOrders Entity error: PO {item["DocumentNumber"].ToString() ?? ""}: {fullErrorMessage}";
                                                        LogErrorToFile(errMsg);
                                                    }
                                            catch (Exception ex)
                                            {
                                                string code = item?["Code"]?.ToString() ?? "Unknown";
                                                string name = item?["Description"]?.ToString() ?? "Unknown";
                                                string errMsg = $"CoID: {Userdetails.CoID} + LoadSalesOrders error: PO {item["DocumentNumber"].ToString() ?? ""}: {ex.Message}";
                                                LogErrorToFile(errMsg);
                                            }
                                        }
                                    }
                                }
                                usedoc = 0;
                            }
                        }
                        UpdateDate = true;
                    }
                        parsedJSON.RemoveAll();
                        skipQty = skipQty + RetQty;
                    } while (skipQty < TotQty);
                
                if (UpdateDate == true)
                {
                    if (LastItmCall != null)
                    {
                        LastItmCall.LastSODate = Convert.ToDateTime(LastCallDt.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        LastCallLog newlog = new LastCallLog();
                        newlog.CompanyID = Userdetails.CoID;
                        newlog.LastSODate = Convert.ToDateTime(LastCallDt.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture));
                        _db.LastCallLogs.Add(newlog);
                    }
                    try
                    {
                        _db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        string errMsg = $"CoID: {Userdetails.CoID} + Error saving LastCallDate for Sales Orders: {ex.Message}";
                        LogErrorToFile(errMsg);
                    }
                }
            }
            _SOisSyncRunning = false;
            return parsedJSON;
        }

        public async Task LoadSOLines(long soid, UserDetails Userdetails)
        {
           JObject parsedJSON = null;
           DataSet ds = new DataSet();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // get latest doclines
                string requestUrl = sageurl + "SalesOrder/GET/" + soid + "?includeDetail={True}&includeCustomerDetails={True}&apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID;
               // JObject parsedJSON = await ApiCallAsync(requestUrl, Userdetails);
                try
                {
                    parsedJSON = await ApiCallAsync(requestUrl, Userdetails);
                }
                catch (Exception ex)
                {
                    parsedJSON = new JObject
                    {
                        ["error"] = "Err 2254 - " + ex.Message
                    };
                }

                if (parsedJSON.Count > 0)
                {
                    List<DocumentLine> myObjects = JsonConvert.DeserializeObject<List<DocumentLine>>(parsedJSON["Lines"].ToString());
                    List<DocLine> existLines = _db.DocLines.Where(x => x.DocID == soid).ToList();
                    // remove lines deleted from SBCA
                    var myObjectCodes = myObjects.Select(o => o.ID).ToHashSet();
                    var linesToRemove = existLines.Where(line => !myObjectCodes.Contains(line.SBCALineID) && line.SBCALineID  !=0).ToList();
                    _db.DocLines.RemoveRange(linesToRemove);
                    _db.SaveChanges();

                    foreach (DocumentLine obj in myObjects)
                    {
                        // check if line exists and update, 
                        var Line = _db.DocLines.Where(x => x.SBCALineID == obj.ID).FirstOrDefault();
                        if (Line != null)
                        {
                            Line.Discount = obj.Discount;
                            Line.DiscountPercentage = obj.DiscountPercentage;
                            Line.Exclusive = obj.Exclusive;
                            var itm = _db.ItemsMasters.Where(x => x.ID == obj.SelectionId).FirstOrDefault();
                            if (itm != null) Line.ItemCode = itm.Code;
                            if (itm != null) Line.Unit = itm.Unit ?? "".ToString();
                            Line.ItemDescription = obj.Description ?? "".ToString();
                            Line.Quantity = Convert.ToDecimal(obj.Quantity);
                            Line.SelectionId = obj.SelectionId;
                            Line.Tax = Convert.ToDecimal(obj.Tax);
                            Line.TaxPercentage = Convert.ToDecimal(obj.TaxPercentage);
                            Line.Total = Convert.ToDecimal(obj.Total);
                            Line.UnitCost = Convert.ToDecimal(obj.UnitCost, CultureInfo.InvariantCulture);
                            Line.UnitPriceExclusive = Convert.ToDecimal(obj.UnitPriceExclusive, CultureInfo.InvariantCulture);
                            Line.UnitPriceInclusive = Convert.ToDecimal(obj.UnitPriceInclusive, CultureInfo.InvariantCulture);
                            Line.AnalysisCategoryId1 = obj.AnalysisCategoryId1;
                            Line.AnalysisCategoryId2 = obj.AnalysisCategoryId2;
                            Line.AnalysisCategoryId3 = obj.AnalysisCategoryId3;
                            Line.LineTaxTypeID = obj.TaxTypeId;
                            Line.ExchRate = 1;
                            if (obj.ExchRate != 0)
                            {
                                Line.ExchRate = obj.ExchRate;
                            }    
                            _db.Entry(Line).State = System.Data.Entity.EntityState.Modified;
                        }
                        else
                        {    
                            DocLine dl = new DocLine();
                            dl.SBCALineID = obj.ID;
                            dl.Discount = obj.Discount;
                            dl.DiscountPercentage = obj.DiscountPercentage;
                            dl.Exclusive = obj.Exclusive;
                            dl.ItemCode = obj.SelectionId.ToString();
                            var itm = _db.ItemsMasters.Where(x => x.ID == obj.SelectionId).FirstOrDefault();
                            if (itm != null) dl.ItemCode = itm.Code;
                            if (itm != null) dl.Unit = itm.Unit ?? "".ToString();
                            dl.ItemDescription = obj.Description ?? "".ToString();
                            dl.LineType = obj.LineType;
                            dl.Quantity = Convert.ToDecimal(obj.Quantity);
                            dl.SelectionId = obj.SelectionId;
                            dl.Tax = Convert.ToDecimal(obj.Tax);
                            dl.TaxPercentage = Convert.ToDecimal(obj.TaxPercentage);
                            dl.Total = Convert.ToDecimal(obj.Total);
                            dl.UnitCost = Convert.ToDecimal(obj.UnitCost, CultureInfo.InvariantCulture);
                            dl.UnitPriceExclusive = Convert.ToDecimal(obj.UnitPriceExclusive, CultureInfo.InvariantCulture);
                            dl.UnitPriceInclusive = Convert.ToDecimal(obj.UnitPriceInclusive, CultureInfo.InvariantCulture);
                            dl.DocID = soid;
                            dl.ToReceive = false;
                            dl.ReceiveQty = 0;
                            dl.ReceiveComplete = false;
                            dl.AnalysisCategoryId1 = obj.AnalysisCategoryId1;
                            dl.AnalysisCategoryId2 = obj.AnalysisCategoryId2;
                            dl.AnalysisCategoryId3 = obj.AnalysisCategoryId3;
                            dl.QtyLeft = Convert.ToDecimal(obj.Quantity);
                            dl.LineTaxTypeID = obj.TaxTypeId;
                            dl.CompanyID = Userdetails.CoID;
                            dl.ExchRate = 1;
                            if (obj.ExchRate != 0)
                            {
                                dl.ExchRate = obj.ExchRate;
                            }
                            _db.DocLines.Add(dl);
                        } 
                    }
                    try
                    {
                        _db.SaveChanges();
                    }
                    catch (Exception ex) {
                        string errMsg = $"CoID: {Userdetails.CoID} + Error Ln 2344 - Saving Sales Order Line: {ex.Message}";
                        LogErrorToFile(errMsg);
                    }
                }
            }
        }
        #endregion

        public async Task <JObject> LoadGLAccounts(UserDetails Userdetails)
        {
            double skipQty = 0; float TotQty = 0; int RetQty = 0;
            DataSet ds = new DataSet();
            JObject parsedJSON = null;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                do
                {
                    string requestUrl = sageurl + "Account/GET?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID + "&$skip=" + skipQty;
                    //JObject parsedJSON = await ApiCallAsync(requestUrl, Userdetails);     
                    try
                    {
                        parsedJSON = await ApiCallAsync(requestUrl, Userdetails);
                    }
                    catch (Exception ex)
                    {
                        parsedJSON = new JObject
                        {
                            ["error"] = "Err 1949 - " + ex.Message
                        };
                    }

                    if (parsedJSON.Count > 0)
                    {
                        JArray items = (JArray)parsedJSON["Results"];
                        TotQty = Convert.ToInt32(parsedJSON["TotalResults"]);
                        RetQty = Convert.ToInt32(parsedJSON["ReturnedResults"]);
                        string SalesRep = string.Empty; string Ref = string.Empty;
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                long AcctID = Convert.ToInt64(item["ID"]);
                                var AccHE = _db.AccountsMasters.Where(x => x.CompanyID == Userdetails.CoID && x.AccountID == AcctID).FirstOrDefault();
                                if (AccHE != null)
                                {
                                    AccHE.AccountName = item["Name"].ToString() ?? "";
                                }
                                else
                                {
                                    AccountsMaster AccH = new AccountsMaster();
                                    AccH.CompanyID = Userdetails.CoID;
                                    AccH.AccountID = Convert.ToInt64(item["ID"].ToString());
                                    AccH.AccountName = item["Name"].ToString() ?? "";
                                    AccH.AccountDescr = item["Name"].ToString() ?? "";

                                    if (item["Category"] != null)
                                    {
                                        AccH.AcctCategDescr = item["Category"]["Description"].ToString() ?? "";
                                        AccH.AcctCategID = item["Category"]?["ID"] != null ? Convert.ToInt64(item["Category"]["ID"]) : (long?)null;
                                    }
                                    AccH.AcctDefTaxTypeID = Convert.ToInt64(item["DefaultTaxTypeId"]);
                                    AccH.AcctdefTaxTypePerc = 0;
                                    AccH.AcctBalance = 0;
                                    if (item.ToString().Contains("Percentage"))
                                    {
                                        if (AccH.AcctdefTaxTypePerc != null)
                                        {
                                            AccH.AcctdefTaxTypePerc = Convert.ToDecimal(item["DefaultTaxType"]["Percentage"]);
                                        }
                                    }
                                    AccH.JCUse = false;
                                    AccH.WOUse = false;
                                    AccH.PSUse = false;
                                    AccH.AccountAddCosts = false;
                                    AccH.AccountAddCostsContra = false;
                                    AccH.AccountAddCosts = false;
                                    _db.AccountsMasters.Add(AccH);
                                    try
                                    {
                                        _db.SaveChanges();
                                    }
                                    catch (Exception ex) { string str = ex.Message; }
                                }
                            }   
                        }
                    }
                    parsedJSON.RemoveAll();
                    skipQty = skipQty + RetQty;
                } while (skipQty < TotQty);
            }
            return parsedJSON;
        }

        public static string LogOut(System.Guid UserGuid)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                {
                    var user = _db.UsersMasters.SingleOrDefault(u => u.UserGUID == UserGuid);
                    if (user != null)
                    {
                        user.IsLoggedIn = false;
                        _db.SaveChanges();
                    }
                }
            }

            return "OK";
        }

        public async Task LoadPOAttachments(long thisdocid, UserDetails Userdetails)
        {      
            // get attachments count
            string requestUrl = sageurl + "PurchaseOrderAttachment/GET/" + thisdocid + "?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID;
            JObject parsedJSON = await ApiCallAsync(requestUrl, Userdetails);
            if (parsedJSON.Count > 0)
            {
                if (Convert.ToInt32(parsedJSON["ReturnedResults"]) > 0)
                {
                    JArray items = (JArray)parsedJSON["Results"];
                   using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var CurrAtt = _db.DocHeaderAttachments.Where(x => x.CompanyID == Userdetails.CoID && x.DocID == thisdocid).ToList();
                        _db.DocHeaderAttachments.RemoveRange(CurrAtt);      
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                DocHeaderAttachment dha = new DocHeaderAttachment
                                {
                                    CompanyID = Userdetails.CoID,
                                    AttGUID = (Guid)item["AttachmentUID"],
                                    DocID = thisdocid,
                                    AttName = item["Name"].ToString() ??"",
                                };
                                _db.DocHeaderAttachments.Add(dha);
                            }
                            try
                            {
                                _db.SaveChanges();
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        public static string NumberToDecimal (string Num, int DecPlaces)
        {
            string RetNum = "0";
            string dec = "N" + DecPlaces;
            RetNum = Convert.ToDecimal(Num).ToString(dec);
            return RetNum;
        }

        /////////////////////////////////
        ///
        /// FROM Data Insights - SALES ORDERS
        public static DataSet GetSQLDataFromString(string cmdtext)
        {
            string conString = constrP;
#if DEBUG
            conString = constr;
#endif
            DataSet ds = new DataSet();
            using (SqlConnection con = new SqlConnection(conString))
            {
                SqlCommand cmd = new SqlCommand(cmdtext, con);
                using (var oda = new SqlDataAdapter())
                {
                    con.Open();
                    try
                    {
                        cmd.CommandTimeout = 0;
                        oda.SelectCommand = cmd;
                        oda.Fill(ds);
                    }
                    catch (Exception ex)
                    {
                        var srt = ex.Message;
                    }
                    con.Close();
                }
            }
            return ds;
        }

        public static string SetSQLDataFromString(string cmdtext)
        {
            string conString = constrP;
#if DEBUG
            conString = constr;
#endif
            string srt = "";
            using (SqlConnection con = new SqlConnection(conString))
            {
                SqlCommand cmd = new SqlCommand(cmdtext, con);
                con.Open();
                try
                {
                    cmd.CommandTimeout = 0;
                    cmd.ExecuteNonQuery();
                    srt = "OK";
                }
                catch (Exception ex)
                {
                    srt = ex.Message;
                }
                con.Close();
            }
            return srt;
        }

        public static async Task LoadCustAdjustments(UserDetails Userdetails)
        {
            CultureInfo cultureInfo = CultureInfo.CurrentCulture;
            // Get the Calendar object for the specified culture
            Calendar calendar = cultureInfo.Calendar;
            int weekNumr = 0;
            DateTime thisdt = Convert.ToDateTime(DateTime.Today.AddMonths(-1), CultureInfo.InvariantCulture), duedt = Convert.ToDateTime(DateTime.Today.AddMonths(-1), CultureInfo.InvariantCulture); ;
            double skipQty = 0;
           
            DateTime frmdt = DateTime.Now; double transactVal = 0;
            DataSet MyDS = new DataSet();
            MyDS = GetSQLDataFromString("Select TOP (1) CustAdjUpdate FROM DIUpdateLog WHERE CompanyID = " + Userdetails.CoID + " AND ProfileID = '" + Userdetails.UserGuiD + "'");
              
            if (MyDS.Tables[0].Rows.Count > 0)
            {
                if (MyDS.Tables[0].Rows[0]["CustAdjUpdate"].ToString() != "")
                {
                    try
                    {
                        InvoiceDT = DateTime.ParseExact(MyDS.Tables[0].Rows[0]["CustAdjUpdate"].ToString(), "dd/MM/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                        //InvoiceDT = Convert.ToDateTime(MyDS.Tables[0].Rows[0]["CustAdjUpdate"].ToString(), CultureInfo.InvariantCulture);
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        InvoiceDT = Convert.ToDateTime(thisdt, CultureInfo.InvariantCulture);
                    }
                    catch { }
                }
            }
            else
            {
                try
                {
                    SetSQLDataFromString($"INSERT INTO DIUpdateLog (CompanyID, ProfileID, SalesOrderUpdate) Values ('{Userdetails.CoID}','{Userdetails.UserGuiD}','{InvoiceDT}')");
                }
                catch { }
            }

            MyDS.Tables.Clear();
            MyDS.Dispose();
           
            string conString = constrP;
#if DEBUG
            conString = constr;
#endif

            cTripleDES des = new cTripleDES(key, iv);
            float TotQty = 0; int RetQty = 0;

            using (SqlConnection con = new SqlConnection(conString))
            {
                using (var command = new SqlCommand())
                {
                    con.Open();
                    string FiltStr = "&$filter=Date gt datetime'" + InvoiceDT.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture) + "'";
                    do
                    {
                        string requestUrl = sageurl + "CustomerAdjustment/GET?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID + "&$skip=" + skipQty + FiltStr;
                        ApiUrlCall api = new ApiUrlCall();
                        JObject parsedJSON = await api.ApiCallAsync(requestUrl, Userdetails);
                        if (parsedJSON.Count > 0)
                        {
                            JArray items = (JArray)parsedJSON["Results"];
                            TotQty = Convert.ToInt32(parsedJSON["TotalResults"]);
                            RetQty = Convert.ToInt32(parsedJSON["ReturnedResults"]);

                            string SalesRep = string.Empty; string Ref = string.Empty;
                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    try
                                    {
                                        thisdt = Convert.ToDateTime(item["Date"], CultureInfo.InvariantCulture);
                                    }
                                    catch { }
                                    try
                                    {
                                        duedt = Convert.ToDateTime(item["DueDate"], CultureInfo.InvariantCulture);
                                        weekNumr = calendar.GetWeekOfYear(Convert.ToDateTime(duedt.ToString()), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                                    }
                                    catch { }

                                    double LineValExVat = 0, Line_Tax = 0, LineTotal = 0;
                                    LineValExVat = Convert.ToDouble(item["Exclusive"].ToString(), CultureInfo.InvariantCulture);
                                    Line_Tax = Convert.ToDouble(item["Tax"].ToString(), CultureInfo.InvariantCulture);
                                    LineTotal = Convert.ToDouble(item["Total"].ToString(), CultureInfo.InvariantCulture);

                                    // calculate financial; YE
                                    string FYear = thisdt.AddYears(1).ToString("yyyy");
                                    int MthNum = Convert.ToInt16(thisdt.ToString("MM"));
                                    if (MthNum < 4)
                                    {
                                        FYear = thisdt.AddYears(1).AddMonths(-MthNum).ToString("yyyy");
                                    }

                                    string weeknumS = weekNumr.ToString();
                                    if (weeknumS.ToString().Length == 1)
                                    {
                                        weeknumS = "0" + weeknumS;
                                    }

                                    if (item["Reference"] != null)
                                    {
                                        Ref = item["Reference"].ToString();
                                    }

                                    string custname = string.Empty;
                                    custname = item["Customer"]["Name"]?.ToString().Replace("'", "''");
                                    if (constr.ToLower().Contains("demo"))
                                    {
                                        if (custname.Length > 4)
                                        {
                                            custname = custname.Substring(0, 4) + "***";
                                        }
                                    }

                                    command.CommandText = "INSERT INTO DITransactionsTbl (TransID, From_Document, Number,Type, Date, CustomerID, Customer_Name, Sales_Rep, Reference, Item,  Description, Quantity, Unit_Price_Excl, Discount, Exclusive, Tax, Total, Analysis_Category1, Analysis_Category2, Analysis_Category3,Line_Cost, Line_GP, Year_Month, Year, isConvertedQuote, LineTypeID, LineType, CoID, CoName, Year_Financial, DateDue, WeekNumber, DocumentMessage ,LineMessage, Doc_Discount_Perc, Nett_Line_Total, Doc_Discount_Amount, StatusID, Status, YearWeek, DeliveryAddress01, DeliveryAddress02, DeliveryAddress03, DeliveryAddress04, DeliveryAddress05 )" +
                                         " VALUES ('" + item["ID"] + "'," +
                                         "'" + "" + "', " +
                                         "'" + item["DocumentNumber"]?.ToString() + "', " +
                                         "'" + "Customer_Adjustmnent" + "', " +
                                         "'" + thisdt.ToString("yyyy-MM-dd") + "', " +
                                         "'" + item["CustomerId"]?.ToString().Replace("'", "''") + "', " +
                                         "'" + custname + "', " +
                                         "'" + SalesRep + "', " +
                                          "'" + Ref + "', " +
                                         "'" + "" + "', " +
                                         "'" + item["Description"]?.ToString().Replace("'", "''") + "', " +
                                         "'" + "1" + "', " +
                                         "'" + LineValExVat.ToString() + "', " +
                                         "'" + "0" + "', " +
                                         "'" + LineValExVat.ToString() + "'," +
                                         "'" + Line_Tax.ToString() + "'," +
                                         "'" + LineTotal.ToString() + "'," +
                                         "'" + "" + "'," +
                                         "'" + "" + "'," +
                                         "'" + "" + "'," +
                                         "'" + "" + "'," +
                                         "'" + "0" + "'," +
                                         "'" + thisdt.ToString("yyyy-MM") + "'," +
                                         "'" + thisdt.ToString("yyyy") + "'," +
                                         "'" + 0 + "'," +
                                         "" + "98" + "," +
                                         "'" + "C_Adjust" + "'," +
                                         "'" + Userdetails.CoID + "'," +
                                         "'" + "" + "'," +
                                         "'" + FYear + "'," +
                                         "'" + duedt.ToString("yyyy-MM-dd") + "', " +
                                         "'" + weekNumr + "'," +
                                         "'" + "" + "'," +
                                         "'" + "" + "'," +
                                         "'" + "" + "'," +
                                         "'" + LineValExVat.ToString() + "'," +
                                         "'" + 0 + "'," +
                                         "'" + "" + "'," +
                                         "'" + "" + "'," +
                                         "'" + duedt.ToString("yyyy") + weeknumS + "'," +
                                          "'" + item["Customer"]["DeliveryAddress01"]?.ToString().Replace("'", "''") + "'," +
                                         "'" + item["Customer"]["DeliveryAddress02"]?.ToString().Replace("'", "''") + "'," +
                                         "'" + item["Customer"]["DeliveryAddress03"]?.ToString().Replace("'", "''") + "'," +
                                         "'" + item["Customer"]["DeliveryAddress04"]?.ToString().Replace("'", "''") + "'," +
                                         "'" + item["Customer"]["DeliveryAddress05"]?.ToString().Replace("'", "''") + "')";
                                    try
                                    {
                                        command.ExecuteNonQuery();
                                    }
                                    catch { }

                                }
                            }
                        }
                        string formattedDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
                        SetSQLDataFromString("UPDATE DIUpdateLog SET CustAdjUpdate = '" + formattedDateTime + "' WHERE CompanyID = '" + Userdetails.CoID + "' AND ProfileID = '" + Userdetails.UserGuiD + "'");
                        skipQty = skipQty + RetQty;
                    } while (skipQty < TotQty);
                    con.Close();
                }
            }
            //DsUser.Tables.Clear();
            //DsUser.Dispose();
            return;
        }

        public static async Task LoadInvoices(UserDetails Userdetails)
        {
            CultureInfo cultureInfo = CultureInfo.CurrentCulture;
            // Get the Calendar object for the specified culture
            Calendar calendar = cultureInfo.Calendar;
            int weekNumr = 0;

            DateTime stdate, thisdt = Convert.ToDateTime("01 Jan 2000"), duedt = Convert.ToDateTime("01 Jan 2000");
            cTripleDES des = new cTripleDES(key, iv);

            double skipQty = 0; int i = 0; float TotQty = 0; int RetQty = 0; int isinvfromquote = 0;

            DataSet MyDS = new DataSet();
            MyDS = GetSQLDataFromString("Select TOP (1) TaxInvoiceUpdate FROM DIUpdateLog WHERE CompanyID = " + Userdetails.CoID + " AND ProfileID = '" + Userdetails.UserGuiD + "'");
            if (MyDS.Tables[0].Rows.Count > 0)
            {
                if (MyDS.Tables[0].Rows[0]["TaxInvoiceUpdate"].ToString() != "")
                {
                    try
                    {
                        InvoiceDT = DateTime.ParseExact(MyDS.Tables[0].Rows[0]["TaxInvoiceUpdate"].ToString(),"dd/MM/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                        //InvoiceDT = Convert.ToDateTime(MyDS.Tables[0].Rows[0]["TaxInvoiceUpdate"].ToString(), CultureInfo.InvariantCulture);
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        InvoiceDT = Convert.ToDateTime(DateTime.Today.AddMonths(-1), CultureInfo.InvariantCulture);
                    }
                    catch { }
                }
            }
            else
            {
                try
                {
                    SetSQLDataFromString($"INSERT INTO DIUpdateLog (CompanyID, ProfileID, SalesOrderUpdate) Values ('{Userdetails.CoID}','{Userdetails.UserGuiD}','{InvoiceDT}')");
                }
                catch { }
            }

            MyDS.Tables.Clear();
            MyDS.Dispose();

            string conString = constrP;
#if DEBUG
            conString = constr;
#endif

            using (SqlConnection con = new SqlConnection(conString))
            {
                using (var command = new SqlCommand())
                {
                    con.Open();
                    do
                    {
                        //string requestUrl = sageurl + "TaxInvoice/GET?apikey={" + APIKey + "}&CompanyID=" + CoID + "&$skip=" + skipQty + "&$filter=Date gt datetime'" + stdate.ToString("yyyy-MM-dd") + "T11:59:59" + "'&includeDetail=true&includeCustomerDetails=true";
                        string requestUrl = sageurl + "TaxInvoice/GET?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID + "&$skip=" + skipQty + FiltDate(InvoiceDT.ToString()) + ")&includeDetail=true&includeCustomerDetails=true";
                        ApiUrlCall api = new ApiUrlCall();
                        JObject parsedJSON = await api.ApiCallAsync(requestUrl, Userdetails);
                        if (parsedJSON.Count > 0)
                        {
                            JArray items = (JArray)parsedJSON["Results"];
                            TotQty = Convert.ToInt32(parsedJSON["TotalResults"]);
                            RetQty = Convert.ToInt32(parsedJSON["ReturnedResults"]);

                            string SalesRep = string.Empty; string Ref = string.Empty;
                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    try
                                    {
                                        thisdt = Convert.ToDateTime(item["Date"], CultureInfo.InvariantCulture);
                                    }
                                    catch { }
                                    try
                                    {
                                        duedt = Convert.ToDateTime(item["DueDate"], CultureInfo.InvariantCulture);
                                        weekNumr = calendar.GetWeekOfYear(Convert.ToDateTime(duedt.ToString()), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                                    }
                                    catch { }

                                    // get document discount
                                    double DocDiscPerc = 0;
                                    if (item["DiscountPercentage"].ToString().Length > 0)
                                    {
                                        try
                                        {
                                            DocDiscPerc = Convert.ToDouble(item["DiscountPercentage"].ToString(), CultureInfo.InvariantCulture);
                                        }
                                        catch { }
                                    }

                                    // delete all current lines for this document
                                    command.Connection = con;
                                    command.CommandText = "Delete FROM DITransactionsTbl WHERE Number = '" + item["DocumentNumber"].ToString() + "' AND CoID = '" + Userdetails.CoID + "'";
                                    command.ExecuteNonQuery();
                                    /////////////////////////
                                    ///
                                    // load invoice lines
                                    if (item["Lines"] != null)
                                    {
                                        List<DocumentLine> myObjects = JsonConvert.DeserializeObject<List<DocumentLine>>(item["Lines"].ToString());
                                        foreach (DocumentLine obj in myObjects)
                                        {
                                            SalesRep = string.Empty;
                                            if (item.ToString().Contains("SalesRepresentative"))
                                            {
                                                SalesRep = item["SalesRepresentative"]["Name"].ToString().Replace("'", "''");
                                                if (constr.ToLower().Contains("demo"))
                                                {
                                                    SalesRep = SalesRep.Substring(0, 4) + "***";
                                                }
                                            }

                                            if (item.ToString().Contains("Reference"))
                                            {
                                                if (item["Reference"] != null)
                                                {
                                                    Ref = item["Reference"].ToString().Replace("'", "''");
                                                }
                                            }
                                            string Analis1 = string.Empty;

                                            if (item.ToString().Contains("AnalysisCategoryId1"))
                                            {

                                                if (obj.AnalysisCategoryId1 != 0)
                                                {
                                                    Analis1 = obj.AnalysisCategoryId1.ToString();

                                                }
                                            }
                                            string Analis2 = string.Empty;
                                            if (item.ToString().Contains("AnalysisCategoryId2"))
                                            {
                                                if (obj.AnalysisCategoryId2 != 0)
                                                {
                                                    Analis2 = obj.AnalysisCategoryId2.ToString();
                                                }
                                            }
                                            string Analis3 = string.Empty;
                                            if (item.ToString().Contains("AnalysisCategoryId3"))
                                            {
                                                if (obj.AnalysisCategoryId3 != 0)
                                                {
                                                    Analis3 = obj.AnalysisCategoryId3.ToString();
                                                }
                                            }

                                            string fromdoc = "";
                                            if (item.ToString().Contains("FromDocument"))
                                            {
                                                fromdoc = item["FromDocument"].ToString();
                                            }

                                            string LineType = GetLineType(Convert.ToInt16(obj.LineType.ToString()));

                                            // calculate financial; YE
                                            string FYear = thisdt.AddYears(1).ToString("yyyy");
                                            int MthNum = Convert.ToInt16(thisdt.ToString("MM"));
                                            if (MthNum < 4)
                                            {
                                                FYear = thisdt.AddYears(1).AddMonths(-MthNum).ToString("yyyy");
                                            }

                                            string DocumentMsg = "";
                                            if (item["Message"] != null)
                                            {
                                                DocumentMsg = item["Message"].ToString();
                                            }

                                            string LineMsg = "";
                                            if (obj.Comments != null && obj.Comments.ToString() != "")
                                            {
                                                LineMsg = obj.Comments.ToString();
                                            }

                                            // calculate Nett_Line_Value based on if the document has a Document Discount
                                            double LineValExVat = 0, Line_Nett_Value = 0, Line_Disc_From_Doc_Disc = 0;
                                            if (DocDiscPerc > 0)
                                            {
                                                try
                                                {
                                                    LineValExVat = Convert.ToDouble(obj.Exclusive.ToString(), CultureInfo.InvariantCulture);
                                                    Line_Disc_From_Doc_Disc = LineValExVat * DocDiscPerc;
                                                    Line_Nett_Value = LineValExVat - Line_Disc_From_Doc_Disc;
                                                }
                                                catch { }
                                            }
                                            else
                                            {
                                                LineValExVat = Convert.ToDouble(obj.Exclusive.ToString(), CultureInfo.InvariantCulture);
                                                Line_Nett_Value = LineValExVat;
                                            }

                                            // calculate line GP
                                            double linecost = 0, linegp = 100;
                                            if (item.ToString().Contains("UnitCost"))
                                            {
                                                if (obj.UnitCost > 0 && Line_Nett_Value > 0)
                                                {
                                                    linecost = (double)(obj.UnitCost * obj.Quantity);
                                                    linegp = Math.Round((Line_Nett_Value - linecost) / Line_Nett_Value, 4) * 100;
                                                }
                                            }
                                            string statusid = "99";
                                            if (item["StatusId"] != null)
                                            {
                                                statusid = item["StatusId"].ToString();
                                            }

                                            string weeknumS = weekNumr.ToString();
                                            if (weeknumS.ToString().Length == 1)
                                            {
                                                weeknumS = "0" + weeknumS;
                                            }

                                            string custname = string.Empty;
                                            custname = item["CustomerName"]?.ToString().Replace("'", "''");
                                            if (constr.ToLower().Contains("demo"))
                                            {
                                                if (custname.Length > 4)
                                                {
                                                    custname = custname.Substring(0, 4) + "***";
                                                }
                                            }

                                            // Insert entries in database table
                                            //command.CommandText = "INSERT INTO DITransactionsTbl (TransID, From_Document, Number,Type, Date, CustomerID, Customer_Name, Sales_Rep, Reference, Item,  Description, Quantity, Unit_Price_Excl, Discount, Exclusive, Tax, Total, Analysis_Category1, Analysis_Category2, Analysis_Category3,Line_Cost, Line_GP, Year_Month, Year, isConvertedQuote, LineTypeID, LineType, CoID, CoName, Year_Financial, DateDue, WeekNumber, DocumentMessage ,LineMessage, Doc_Discount_Perc, Nett_Line_Total, Doc_Discount_Amount, StatusID, Status, YearWeek, DeliveryAddress01, DeliveryAddress02, DeliveryAddress03, DeliveryAddress04, DeliveryAddress05 )" +
                                            //" VALUES ('" + obj.ID + "'," +
                                            //"'" + fromdoc + "', " +
                                            //"'" + item["DocumentNumber"]?.ToString() + "', " +
                                            //"'" + "Tax_Invoice" + "', " +
                                            //"'" + thisdt.ToString("yyyy-MM-dd") + "', " +
                                            //"'" + item["CustomerId"]?.ToString().Replace("'", "''") + "', " +
                                            //"'" + custname + "', " +
                                            //"'" + SalesRep + "', " +
                                            // "'" + Ref + "', " +
                                            //"'" + getitemcodefromid(obj.SelectionId.ToString()) + "', " +
                                            //"'" + obj.Description.ToString().Replace("'", "''") ?? "" + "', " +
                                            //"'" + obj.Quantity.ToString() + "', " +
                                            //"'" + obj.UnitPriceExclusive.ToString() + "', " +
                                            //"'" + obj.Discount.ToString() + "', " +
                                            //"'" + obj.Exclusive.ToString() + "'," +
                                            //"'" + obj.Tax.ToString() + "'," +
                                            //"'" + obj.Total.ToString() + "'," +
                                            //"'" + Analis1.Replace("'", "''") + "'," +
                                            //"'" + Analis2.Replace("'", "''") + "'," +
                                            //"'" + Analis3.Replace("'", "''") + "'," +
                                            //"'" + linecost.ToString() + "'," +
                                            //"'" + linegp.ToString() + "'," +
                                            //"'" + thisdt.ToString("yyyy-MM") + "'," +
                                            //"'" + thisdt.ToString("yyyy") + "'," +
                                            //"'" + isinvfromquote + "'," +
                                            //"" + Convert.ToInt16(obj.LineType.ToString()) + "," +
                                            //"'" + LineType + "'," +
                                            //"'" + Userdetails.CoID + "'," +
                                            //"'" + "" + "'," +
                                            //"'" + FYear + "'," +
                                            //"'" + duedt.ToString("yyyy-MM-dd") + "', " +
                                            //"'" + weekNumr + "'," +
                                            //"'" + DocumentMsg.Replace("'", "''") + "'," +
                                            //"'" + LineMsg.Replace("'", "''") + "'," +
                                            //"'" + DocDiscPerc + "'," +
                                            //"'" + Math.Round(Line_Nett_Value, 2) + "'," +
                                            //"'" + Math.Round(Line_Disc_From_Doc_Disc, 2) + "'," +
                                            //"'" + statusid + "'," +
                                            //"'" + item["Status"].ToString() + "'," +
                                            //"'" + duedt.ToString("yyyy") + weeknumS + "'," +
                                            // "'" + item["DeliveryAddress01"]?.ToString().Replace("'", "''") + "'," +
                                            //"'" + item["DeliveryAddress02"]?.ToString().Replace("'", "''") + "'," +
                                            //"'" + item["DeliveryAddress03"]?.ToString().Replace("'", "''") + "'," +
                                            //"'" + item["DeliveryAddress04"]?.ToString().Replace("'", "''") + "'," +
                                            //"'" + item["DeliveryAddress05"]?.ToString().Replace("'", "''") + "')";
                                            // 1.  SQL text (unchanged except for the parameter tokens)

                                            command.Parameters.Clear();

                                            object DbString(string s) => string.IsNullOrWhiteSpace(s) ? DBNull.Value : (object)s;
                                            object DbObj(object o) => o ?? DBNull.Value;
                                           
                                            command.CommandText = @"
                                                    INSERT INTO DITransactionsTbl
                                                    (
                                                        TransID, From_Document, Number, Type, Date, CustomerID, Customer_Name,
                                                        Sales_Rep, Reference, Item, Description, Quantity,
                                                        Unit_Price_Excl, Discount, Exclusive, Tax, Total,
                                                        Analysis_Category1, Analysis_Category2, Analysis_Category3,
                                                        Line_Cost, Line_GP, Year_Month, Year, isConvertedQuote,
                                                        LineTypeID, LineType, CoID, CoName, Year_Financial,
                                                        DateDue, WeekNumber, DocumentMessage, LineMessage,
                                                        Doc_Discount_Perc, Nett_Line_Total, Doc_Discount_Amount,
                                                        StatusID, Status, YearWeek,
                                                        DeliveryAddress01, DeliveryAddress02, DeliveryAddress03,
                                                        DeliveryAddress04, DeliveryAddress05
                                                    )
                                                    VALUES
                                                    (
                                                        @TransID, @From_Document, @Number, @Type, @Date, @CustomerID, @Customer_Name,
                                                        @Sales_Rep, @Reference, @Item, @Description, @Quantity,
                                                        @Unit_Price_Excl, @Discount, @Exclusive, @Tax, @Total,
                                                        @Analysis_Category1, @Analysis_Category2, @Analysis_Category3,
                                                        @Line_Cost, @Line_GP, @Year_Month, @Year, @isConvertedQuote,
                                                        @LineTypeID, @LineType, @CoID, @CoName, @Year_Financial,
                                                        @DateDue, @WeekNumber, @DocumentMessage, @LineMessage,
                                                        @Doc_Discount_Perc, @Nett_Line_Total, @Doc_Discount_Amount,
                                                        @StatusID, @Status, @YearWeek,
                                                        @DeliveryAddress01, @DeliveryAddress02, @DeliveryAddress03,
                                                        @DeliveryAddress04, @DeliveryAddress05
                                                    );";

                                            command.Parameters.Add("@TransID", SqlDbType.BigInt).Value = DbObj(obj.ID);
                                            command.Parameters.Add("@From_Document", SqlDbType.NVarChar, 50).Value = DbString(fromdoc);
                                            command.Parameters.Add("@Number", SqlDbType.NVarChar, 50).Value = DbString(item["DocumentNumber"]?.ToString());
                                            command.Parameters.Add("@Type", SqlDbType.NVarChar, 50).Value = "Tax_Invoice";
                                            command.Parameters.Add("@Date", SqlDbType.Date).Value = thisdt.Date;
                                            command.Parameters.Add("@CustomerID", SqlDbType.NVarChar, 50).Value = DbString(item["CustomerId"]?.ToString());
                                            command.Parameters.Add("@Customer_Name", SqlDbType.NVarChar, 200).Value = DbString(custname);
                                            command.Parameters.Add("@Sales_Rep", SqlDbType.NVarChar, 100).Value = DbString(SalesRep);
                                            command.Parameters.Add("@Reference", SqlDbType.NVarChar, 100).Value = DbString(Ref);
                                            command.Parameters.Add("@Item", SqlDbType.NVarChar, 100).Value = DbString(getitemcodefromid(obj.SelectionId.ToString()));
                                            command.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = DbString(obj.Description?.ToString());
                                            command.Parameters.Add("@Quantity", SqlDbType.Decimal).Value = DbObj(obj.Quantity);
                                            command.Parameters.Add("@Unit_Price_Excl", SqlDbType.Decimal).Value = DbObj(obj.UnitPriceExclusive);
                                            command.Parameters.Add("@Discount", SqlDbType.Decimal).Value = DbObj(obj.Discount);
                                            command.Parameters.Add("@Exclusive", SqlDbType.Decimal).Value = DbObj(obj.Exclusive);
                                            command.Parameters.Add("@Tax", SqlDbType.Decimal).Value = DbObj(obj.Tax);
                                            command.Parameters.Add("@Total", SqlDbType.Decimal).Value = DbObj(obj.Total);
                                            command.Parameters.Add("@Analysis_Category1", SqlDbType.NVarChar, 100).Value = DbString(Analis1);
                                            command.Parameters.Add("@Analysis_Category2", SqlDbType.NVarChar, 100).Value = DbString(Analis2);
                                            command.Parameters.Add("@Analysis_Category3", SqlDbType.NVarChar, 100).Value = DbString(Analis3);
                                            command.Parameters.Add("@Line_Cost", SqlDbType.Decimal).Value = DbObj(linecost);
                                            command.Parameters.Add("@Line_GP", SqlDbType.Decimal).Value = DbObj(linegp);
                                            command.Parameters.Add("@Year_Month", SqlDbType.NVarChar, 10).Value = thisdt.ToString("yyyy-MM");
                                            command.Parameters.Add("@Year", SqlDbType.NVarChar, 4).Value = thisdt.ToString("yyyy");
                                            command.Parameters.Add("@isConvertedQuote", SqlDbType.Bit).Value = isinvfromquote;
                                            command.Parameters.Add("@LineTypeID", SqlDbType.SmallInt).Value = Convert.ToInt16(obj.LineType);
                                            command.Parameters.Add("@LineType", SqlDbType.NVarChar, 50).Value = DbString(LineType);
                                            command.Parameters.Add("@CoID", SqlDbType.NVarChar, 50).Value = DbObj(Userdetails.CoID);
                                            command.Parameters.Add("@CoName", SqlDbType.NVarChar, 100).Value = DBNull.Value;
                                            command.Parameters.Add("@Year_Financial", SqlDbType.NVarChar, 10).Value = DbString(FYear);
                                            command.Parameters.Add("@DateDue", SqlDbType.Date).Value = duedt.Date;
                                            command.Parameters.Add("@WeekNumber", SqlDbType.NVarChar, 10).Value = DbObj(weekNumr);
                                            command.Parameters.Add("@DocumentMessage", SqlDbType.NVarChar, -1).Value = DbString(DocumentMsg);
                                            command.Parameters.Add("@LineMessage", SqlDbType.NVarChar, -1).Value = DbString(LineMsg);
                                            command.Parameters.Add("@Doc_Discount_Perc", SqlDbType.Decimal).Value = DbObj(DocDiscPerc);
                                            command.Parameters.Add("@Nett_Line_Total", SqlDbType.Decimal).Value = Math.Round(Line_Nett_Value, 2);
                                            command.Parameters.Add("@Doc_Discount_Amount", SqlDbType.Decimal).Value = Math.Round(Line_Disc_From_Doc_Disc, 2);
                                            command.Parameters.Add("@StatusID", SqlDbType.Int).Value = DbObj(statusid);
                                            command.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = DbString(item["Status"]?.ToString());
                                            command.Parameters.Add("@YearWeek", SqlDbType.NVarChar, 10).Value = $"{duedt:yyyy}{weeknumS}";
                                            command.Parameters.Add("@DeliveryAddress01", SqlDbType.NVarChar, 100).Value = DbString(item["DeliveryAddress01"]?.ToString());
                                            command.Parameters.Add("@DeliveryAddress02", SqlDbType.NVarChar, 100).Value = DbString(item["DeliveryAddress02"]?.ToString());
                                            command.Parameters.Add("@DeliveryAddress03", SqlDbType.NVarChar, 100).Value = DbString(item["DeliveryAddress03"]?.ToString());
                                            command.Parameters.Add("@DeliveryAddress04", SqlDbType.NVarChar, 100).Value = DbString(item["DeliveryAddress04"]?.ToString());
                                            command.Parameters.Add("@DeliveryAddress05", SqlDbType.NVarChar, 100).Value = DbString(item["DeliveryAddress05"]?.ToString());
                                            try
                                            {
                                                command.ExecuteNonQuery();
                                            }
                                            catch { }
                                        }
                                    }
                                    i++;
                                }
                            }
                        }
                        string formattedDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
                        SetSQLDataFromString("UPDATE DIUpdateLog SET TaxInvoiceUpdate = '" + formattedDateTime + "' WHERE CompanyID = '" + Userdetails.CoID + "' AND ProfileID = '" + Userdetails.UserGuiD + "'");
                        skipQty = skipQty + RetQty;
                    } while (skipQty < TotQty);
                    con.Close();
                }
            }
        }

        public static async Task LoadSalesCreditNotes(UserDetails Userdetails)
        {
            CultureInfo cultureInfo = CultureInfo.CurrentCulture;
            // Get the Calendar object for the specified culture
            Calendar calendar = cultureInfo.Calendar;
            int weekNumr = 0;
            
            DateTime stdate;
            
            cTripleDES des = new cTripleDES(key, iv);
            double skipQty = 0; int i = 0; float TotQty = 0; int RetQty = 0;

            DataSet MyDS = new DataSet();
            MyDS = GetSQLDataFromString("Select TOP (1) CNUpdate FROM DIUpdateLog WHERE CompanyID = " + Userdetails.CoID + " AND ProfileID = '" + Userdetails.UserGuiD + "'");
            if (MyDS.Tables[0].Rows.Count > 0)
            {
                if (MyDS.Tables[0].Rows[0]["CNUpdate"].ToString() != "")
                {
                    try
                    {
                        CNoteDT = DateTime.ParseExact(MyDS.Tables[0].Rows[0]["CNUpdate"].ToString(), "dd/MM/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                        //CNoteDT = Convert.ToDateTime(MyDS.Tables[0].Rows[0]["CNUpdate"].ToString(), CultureInfo.InvariantCulture);
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        CNoteDT = Convert.ToDateTime(DateTime.Today.AddMonths(-3), CultureInfo.InvariantCulture);
                    }
                    catch { }
                }
            }
            else
            {
                try
                {
                    SetSQLDataFromString($"INSERT INTO DIUpdateLog (CompanyID, ProfileID, SalesOrderUpdate) Values ('{Userdetails.CoID}','{Userdetails.UserGuiD}','{CNoteDT}')");
                }
                catch { }
            }

            MyDS.Tables.Clear();
            MyDS.Dispose();

            string conString = constrP;
#if DEBUG
            conString = constr;
#endif

            string SalesRep = string.Empty; string Ref = string.Empty;
            using (SqlConnection con = new SqlConnection(conString))
            {
                using (var command = new SqlCommand())
                {
                    con.Open();
                    do
                    {
                        //string requestUrl = sageurl + "CustomerReturn/GET?apikey={" + APIKey + "}&CompanyID=" + CoID + "&$skip=" + skipQty + "&$filter=Date gt datetime'" + stdate.ToString("yyyy-MM-dd") + "T23:59:59" + "'&includeDetail=true";
                        string requestUrl = sageurl + "CustomerReturn/GET?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID + "&$skip=" + skipQty + FiltDate(CNoteDT.ToString()) + ")&includeDetail=true";
                        ApiUrlCall api = new ApiUrlCall();
                        JObject parsedJSON = await api.ApiCallAsync(requestUrl, Userdetails);
                        if (parsedJSON.Count > 0)
                        {
                            JArray items = (JArray)parsedJSON["Results"];
                            TotQty = Convert.ToInt32(parsedJSON["TotalResults"]);
                            RetQty = Convert.ToInt32(parsedJSON["ReturnedResults"]);
                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    DateTime thisdt = Convert.ToDateTime(item["Date"], CultureInfo.InvariantCulture);
                                    weekNumr = calendar.GetWeekOfYear(Convert.ToDateTime(thisdt.ToString()), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

                                    // get document discount
                                    double DocDiscPerc = 0;
                                    if (item["DiscountPercentage"].ToString().Length > 0)
                                    {
                                        try
                                        {
                                            DocDiscPerc = Convert.ToDouble(item["DiscountPercentage"].ToString(), CultureInfo.InvariantCulture);
                                        }
                                        catch { }
                                    }

                                    // delete all current lines for this document
                                    command.Connection = con;
                                    command.CommandText = "Delete FROM DITransactionsTbl WHERE Number = '" + item["DocumentNumber"].ToString() + "' AND CoID = '" + Userdetails.CoID + "'";
                                    command.ExecuteNonQuery();
                                    /////////////////////////
                                    ///
                                    List<DocumentLine> myObjects = JsonConvert.DeserializeObject<List<DocumentLine>>(item["Lines"].ToString());
                                    foreach (DocumentLine obj in myObjects)
                                    {
                                        SalesRep = string.Empty;
                                        if (item.ToString().Contains("SalesRepresentative"))
                                        {
                                            try
                                            {
                                                SalesRep = item["SalesRepresentative"]["Name"].ToString().Replace("'", "''");
                                                if (constr.ToLower().Contains("demo"))
                                                {
                                                    SalesRep = SalesRep.Substring(0, 4) + "***";
                                                }
                                            }
                                            catch { }

                                        }
                                        Ref = string.Empty;
                                        if (item.ToString().Contains("Reference"))
                                        {
                                            if (item["Reference"] != null)
                                            {
                                                Ref = item["Reference"].ToString().Replace("'", "''");
                                            }
                                        }
                                        string Analis1 = string.Empty;
                                        if (item.ToString().Contains("AnalysisCategoryId1"))
                                        {
                                            if (obj.AnalysisCategoryId1 != null)
                                            {
                                                Analis1 = obj.AnalysisCategoryId1.ToString();
                                            }
                                        }
                                        string Analis2 = string.Empty;
                                        if (item.ToString().Contains("AnalysisCategoryId2"))
                                        {
                                            if (obj.AnalysisCategoryId2 != null)
                                            {
                                                Analis2 = obj.AnalysisCategoryId2.ToString();
                                            }
                                        }
                                        string Analis3 = string.Empty;
                                        if (item.ToString().Contains("AnalysisCategoryId3"))
                                        {
                                            if (obj.AnalysisCategoryId3 != null)
                                            {
                                                Analis3 = obj.AnalysisCategoryId3.ToString();
                                            }
                                        }

                                        decimal linecost = 0, linegp = 100; 
                                        if (item.ToString().Contains("UnitCost"))
                                        {
                                            if (obj.UnitCost > 0 && obj.Exclusive >0)
                                            {
                                                linecost = (decimal)(obj.UnitCost * obj.Quantity * -1);
                                                linegp = Math.Round((obj.Exclusive - linecost) / obj.Exclusive, 4) * 100;
                                            }
                                        }

                                        string fromdoc = "";
                                        if (item.ToString().Contains("FromDocument"))
                                        {
                                            fromdoc = item["FromDocument"].ToString();
                                        }

                                        string LineType = GetLineType(Convert.ToInt16(obj.LineType.ToString()));

                                        // calculate financial; YE
                                        string FYear = thisdt.AddYears(1).ToString("yyyy");
                                        int MthNum = Convert.ToInt16(thisdt.ToString("MM"));
                                        if (MthNum < 4)
                                        {
                                            FYear = thisdt.AddYears(1).AddMonths(-MthNum).ToString("yyyy");
                                        }

                                        string DocumentMsg = "";
                                        if (item["Message"] != null)
                                        {
                                            DocumentMsg = item["Message"].ToString();
                                        }

                                        string LineMsg = "";
                                        if (obj.Comments != null && obj.Comments.ToString() != "")
                                        {
                                            LineMsg = obj.Comments.ToString();
                                        }

                                        // calculate Nett_Line_Value based on if the document has a Document Discount
                                        double LineValExVat = 0, Line_Nett_Value = 0, Line_Disc_From_Doc_Disc = 0;
                                        if (DocDiscPerc > 0)
                                        {
                                            try
                                            {
                                                LineValExVat = (Convert.ToDouble(obj.Exclusive.ToString(), CultureInfo.InvariantCulture)) * -1;
                                                Line_Disc_From_Doc_Disc = LineValExVat * DocDiscPerc;
                                                Line_Nett_Value = LineValExVat - Line_Disc_From_Doc_Disc;
                                            }
                                            catch { }
                                        }
                                        else
                                        {
                                            LineValExVat = (Convert.ToDouble(obj.Exclusive.ToString(), CultureInfo.InvariantCulture)) * -1;
                                            Line_Nett_Value = LineValExVat;
                                        }

                                        string statusid = "99";
                                        if (item["StatusId"] != null)
                                        {
                                            statusid = item["StatusId"].ToString();
                                        }

                                        string weeknumS = weekNumr.ToString();
                                        if (weeknumS.ToString().Length == 1)
                                        {
                                            weeknumS = "0" + weeknumS;
                                        }

                                        string custname = string.Empty;
                                        custname = item["CustomerName"]?.ToString().Replace("'", "''");
                                        if (constr.ToLower().Contains("demo"))
                                        {
                                            if (custname.Length > 4)
                                            {
                                                custname = custname.Substring(0, 4) + "***";
                                            }
                                        }

                                        // Insert entries in database table
                                        command.CommandText = "INSERT INTO DITransactionsTbl (TransID, From_Document, Number,Type, Date, CustomerID, Customer_Name, Sales_Rep, Reference, Item,  Description, Quantity, Unit_Price_Excl, Discount, Exclusive, Tax, Total, Analysis_Category1, Analysis_Category2, Analysis_Category3,Line_Cost, Line_GP, Year_Month, Year, LineTypeID, LineType, CoID, CoName, Year_Financial, DateDue, WeekNumber, DocumentMessage ,LineMessage, Doc_Discount_Perc, Nett_Line_Total, Doc_Discount_Amount, StatusID,YearWeek, DeliveryAddress01, DeliveryAddress02, DeliveryAddress03, DeliveryAddress04, DeliveryAddress05 )" +
                                        " VALUES ('" + obj.ID + "'," +
                                        "'" + fromdoc + "', " +
                                        "'" + item["DocumentNumber"]?.ToString() + "', " +
                                        "'" + "Credit_Note" + "', " +
                                        "'" + thisdt.ToString("yyyy-MM-dd") + "', " +
                                         "'" + item["CustomerId"]?.ToString().Replace("'", "''") + "', " +
                                        "'" + custname + "', " +
                                        "'" + SalesRep + "', " +
                                         "'" + Ref + "', " +
                                         "'" + getitemcodefromid(obj.SelectionId.ToString()) + "', " +
                                        "'" + obj.Description.ToString().Replace("'", "''") + "', " +
                                        "'" + obj.Quantity.ToString() + "', " +
                                        "'-" + obj.UnitPriceExclusive.ToString() + "', " +
                                        "'-" + obj.Discount.ToString() + "', " +
                                        "'-" + obj.Exclusive.ToString() + "'," +
                                        "'-" + obj.Tax.ToString() + "'," +
                                        "'-" + obj.Total.ToString() + "'," +
                                        "'" + Analis1.Replace("'", "''") + "'," +
                                        "'" + Analis2.Replace("'", "''") + "'," +
                                        "'" + Analis3.Replace("'", "''") + "'," +
                                        "'" + linecost.ToString() + "'," +
                                        "'" + linegp.ToString() + "'," +
                                        "'" + thisdt.ToString("yyyy-MM") + "'," +
                                        "'" + thisdt.ToString("yyyy") + "'," +
                                        "" + Convert.ToInt16(obj.LineType.ToString()) + "," +
                                         "'" + LineType + "'," +
                                        "'" + Userdetails.CoID + "'," +
                                        "'" + "" + "'," +
                                        "'" + FYear + "'," +
                                        "'" + thisdt.ToString("yyyy-MM-dd") + "', " +
                                        "'" + weekNumr + "'," +
                                        "'" + DocumentMsg.Replace("'", "''") + "'," +
                                        "'" + LineMsg.Replace("'", "''") + "'," +
                                         "'" + DocDiscPerc + "'," +
                                        "'" + Math.Round(Line_Nett_Value, 2) + "'," +
                                        "'" + Math.Round(Line_Disc_From_Doc_Disc, 2) + "'," +
                                         "'" + statusid + "'," +
                                         "'" + thisdt.ToString("yyyy") + weeknumS + "'," +
                                        "'" + item["DeliveryAddress01"]?.ToString().Replace("'", "''") + "'," +
                                        "'" + item["DeliveryAddress02"]?.ToString().Replace("'", "''") + "'," +
                                        "'" + item["DeliveryAddress03"]?.ToString().Replace("'", "''") + "'," +
                                        "'" + item["DeliveryAddress04"]?.ToString().Replace("'", "''") + "'," +
                                        "'" + item["DeliveryAddress05"]?.ToString().Replace("'", "''") + "')";
                                        try
                                        {
                                            command.ExecuteNonQuery();
                                        }
                                        catch (Exception ex) { string str = ex.Message; }
                                    }
                                    i++;
                                }
                            }
                        }
                        string formattedDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
                        SetSQLDataFromString("UPDATE DIUpdateLog SET CNUpdate = '" + formattedDateTime + "' WHERE CompanyID = '" + Userdetails.CoID + "' AND ProfileID = '" + Userdetails.UserGuiD + "'");
                        skipQty = skipQty + RetQty;
                    } while (skipQty < TotQty);
                    con.Close();
                }
            }
        }

        public static async Task DILoadSalesOrders(UserDetails Userdetails)
        {
            CultureInfo cultureInfo = CultureInfo.CurrentCulture;
            // Get the Calendar object for the specified culture
            Calendar calendar = cultureInfo.Calendar;
            int weekNumr = 0;         
            
            DateTime thisdt = Convert.ToDateTime(DateTime.Today.AddMonths(-1), CultureInfo.InvariantCulture), deldt = Convert.ToDateTime(DateTime.Today.AddMonths(-1), CultureInfo.InvariantCulture);
            cTripleDES des = new cTripleDES(key, iv);

            DataSet MyDS = new DataSet();
            MyDS = GetSQLDataFromString("Select TOP (1) SalesOrderUpdate FROM DIUpdateLog WHERE CompanyID = " + Userdetails.CoID + " AND ProfileID = '" + Userdetails.UserGuiD + "'");
            if (MyDS != null)
            {
                if (MyDS.Tables[0].Rows.Count > 0)
                {
                    if (MyDS.Tables[0].Rows[0]["SalesOrderUpdate"].ToString() != "")
                    {
                        try
                        {
                            SOrdDT = DateTime.ParseExact(MyDS.Tables[0].Rows[0]["SalesOrderUpdate"].ToString(), "dd/MM/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                            //SOrdDT = Convert.ToDateTime(MyDS.Tables[0].Rows[0]["SalesOrderUpdate"].ToString(), CultureInfo.InvariantCulture);
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            SOrdDT = Convert.ToDateTime(thisdt, CultureInfo.InvariantCulture);
                        }
                        catch { }
                    }
                }
                else
                {
                    try
                    {
                        SetSQLDataFromString($"INSERT INTO DIUpdateLog (CompanyID, ProfileID, SalesOrderUpdate) Values ('{Userdetails.CoID}','{Userdetails.UserGuiD}','{SOrdDT}')");
                    }
                    catch { }
                }
            }
            else
            {
                //SetSQLDataFromString("INSERT INTO DIUpdateLog (CompanyID, ProfileID, SalesOrderUpdate) Values "" +,"","")'
            }
            MyDS.Tables.Clear();
            MyDS.Dispose();
            
            double skipQty = 0; int i = 0; float TotQty = 0; int RetQty = 0; int isinvfromquote = 0;
            string conString = constrP;
#if DEBUG
            conString = constr;
#endif

            ApiUrlCall api = new ApiUrlCall();
            using (SqlConnection con = new SqlConnection(conString))
            {
                using (var command = new SqlCommand())
                {
                    con.Open();
                    do
                    {
                        //string requestUrl = sageurl + "SalesOrder/GET?apikey={" + APIKey + "}&CompanyID=" + CoID + "&$skip=" + skipQty + "&$filter=Status eq 'Pending' or Status eq 'Overdue'&includeDetail=true&includeCustomerDetails=true";
                        string requestUrl = sageurl + "SalesOrder/GET?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID + "&$skip=" + skipQty + FiltDate(SOrdDT.ToString()) + ")&includeDetail=true&includeCustomerDetails=true";
                        JObject parsedJSON = await api.ApiCallAsync(requestUrl, Userdetails);
                        if (parsedJSON.Count > 0)
                        {
                            JArray items = (JArray)parsedJSON["Results"];
                            TotQty = Convert.ToInt32(parsedJSON["TotalResults"]);
                            RetQty = Convert.ToInt32(parsedJSON["ReturnedResults"]);
                            string SalesRep = string.Empty; string Ref = string.Empty;
                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    try
                                    {
                                        thisdt = Convert.ToDateTime(item["Date"], CultureInfo.InvariantCulture);
                                        deldt = Convert.ToDateTime(item["DeliveryDate"], CultureInfo.InvariantCulture);
                                        weekNumr = calendar.GetWeekOfYear(Convert.ToDateTime(deldt.ToString()), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                                    }
                                    catch { }

                                    // get document discount
                                    decimal DocDiscPerc = 0;
                                    if (item["DiscountPercentage"].ToString().Length > 0)
                                    {
                                        try
                                        {
                                            DocDiscPerc = Convert.ToDecimal(item["DiscountPercentage"].ToString(), CultureInfo.InvariantCulture);
                                        }
                                        catch { }
                                    }
                                    // delete all current lines for this document    
                                    command.Connection = con;
                                    command.CommandText = "Delete FROM DITransactionsTbl WHERE Number = '" + item["DocumentNumber"].ToString() + "' AND CoID = '" + Userdetails.CoID + "'";
                                    command.ExecuteNonQuery();
                                    /////////////////////////
                                    List<DocumentLine> myObjects = JsonConvert.DeserializeObject<List<DocumentLine>>(item["Lines"].ToString());
                                    foreach (DocumentLine obj in myObjects)
                                    {
                                        SalesRep = string.Empty;
                                        if (item.ToString().Contains("SalesRepresentative"))
                                        {
                                            SalesRep = item["SalesRepresentative"]["Name"].ToString().Replace("'", "''");
                                        }

                                        if (item.ToString().Contains("Reference"))
                                        {
                                            if (item["Reference"] != null)
                                            {
                                                Ref = item["Reference"].ToString().Replace("'", "''");
                                            }
                                        }
                                        string Analis1 = string.Empty;
                                        if (item.ToString().Contains("AnalysisCategoryId1"))
                                        {
                                            Analis1 = obj.AnalysisCategoryId1.ToString();
                                        }
                                        string Analis2 = string.Empty;
                                        if (item.ToString().Contains("AnalysisCategoryId2"))
                                        {
                                                Analis2 = obj.AnalysisCategoryId2.ToString();
                                        }
                                        string Analis3 = string.Empty;
                                        if (item.ToString().Contains("AnalysisCategoryId3"))
                                        {
                                                Analis3 = obj.AnalysisCategoryId3.ToString();
                                        }
                                        string fromdoc = "";
                                        if (item.ToString().Contains("FromDocument"))
                                        {
                                            fromdoc = item["FromDocument"].ToString();
                                        }

                                        string LineType = GetLineType(Convert.ToInt16(obj.LineType.ToString()));

                                        // calculate financial; YE
                                        string FYear = thisdt.AddYears(1).ToString("yyyy");
                                        int MthNum = Convert.ToInt16(thisdt.ToString("MM"));
                                        if (MthNum < 4)
                                        {
                                            FYear = thisdt.AddYears(1).AddMonths(-MthNum).ToString("yyyy");
                                        }

                                        string DocumentMsg = "";
                                        if (item["Message"] != null)
                                        {
                                            DocumentMsg = item["Message"].ToString();
                                        }

                                        string LineMsg = "";
                                        if (obj.Comments != null && obj.Comments.ToString() != "")
                                        {
                                            LineMsg = obj.Comments.ToString();
                                        }

                                        // calculate Nett_Line_Value based on if the document has a Document Discount
                                        decimal LineValExVat = 0, Line_Nett_Value = 0, Line_Disc_From_Doc_Disc = 0;
                                        if (DocDiscPerc > 0)
                                        {
                                            try
                                            {
                                                LineValExVat = Convert.ToDecimal(obj.Exclusive.ToString(), CultureInfo.InvariantCulture);
                                                Line_Disc_From_Doc_Disc = (decimal) LineValExVat * DocDiscPerc;
                                                Line_Nett_Value = LineValExVat - Line_Disc_From_Doc_Disc;
                                            }
                                            catch { }
                                        }
                                        else
                                        {
                                            LineValExVat = Convert.ToDecimal(obj.Exclusive.ToString(), CultureInfo.InvariantCulture);
                                            Line_Nett_Value = LineValExVat;
                                        }

                                        // calculate line GP
                                        decimal linecost = 0, linegp = 100;
                                        if (item.ToString().Contains("UnitCost"))
                                        {
                                            if (obj.UnitCost > 0 && Line_Nett_Value > 0)
                                            {
                                                linecost = obj.UnitCost * obj.Quantity;
                                                linegp = Math.Round((Line_Nett_Value - linecost) / Line_Nett_Value, 4) * 100;
                                            }
                                        }

                                        string statusid = "99";
                                        if (item["StatusId"] != null)
                                        {
                                            statusid = item["StatusId"].ToString();
                                        }

                                        string weeknumS = weekNumr.ToString();
                                        if (weeknumS.ToString().Length == 1)
                                        {
                                            weeknumS = "0" + weeknumS;
                                        }

                                        string custname = string.Empty;
                                        custname = item["CustomerName"]?.ToString().Replace("'", "''");
                                        if (constr.ToLower().Contains("demo"))
                                        {
                                            if (custname.Length > 4)
                                            {
                                                custname = custname.Substring(0, 4) + "***";
                                            }
                                        }
                                        // Insert entries in database table
                                        command.CommandText = "INSERT INTO DITransactionsTbl (TransID, From_Document, Number,Type, Date, CustomerID, Customer_Name, Sales_Rep, Reference, Item,  Description, Quantity, Unit_Price_Excl, Discount, Exclusive, Tax, Total, Analysis_Category1, Analysis_Category2, Analysis_Category3,Line_Cost, Line_GP, Year_Month, Year, isConvertedQuote, LineTypeID, LineType, CoID, CoName, Year_Financial, DateDue, WeekNumber, DocumentMessage ,LineMessage, Doc_Discount_Perc, Nett_Line_Total, Doc_Discount_Amount, StatusID, Status, YearWeek, DeliveryAddress01, DeliveryAddress02, DeliveryAddress03, DeliveryAddress04, DeliveryAddress05)" +
                                        " VALUES ('" + obj.ID + "'," +
                                        "'" + fromdoc + "', " +
                                        "'" + item["DocumentNumber"]?.ToString() + "', " +
                                        "'" + "Sales_Order" + "', " +
                                        "'" + thisdt.ToString("yyyy-MM-dd") + "', " +
                                        "'" + item["CustomerId"]?.ToString().Replace("'", "''") + "', " +
                                        "'" + custname + "', " +
                                        //"'" + item["CustomerName"].ToString() + "', " +
                                        "'" + SalesRep + "', " +
                                         "'" + Ref + "', " +
                                         "'" + getitemcodefromid(obj.SelectionId.ToString()) + "', " +
                                        "'" + obj.Description.ToString().Replace("'", "''") + "', " +
                                        "'" + obj.Quantity.ToString() + "', " +
                                        "'" + obj.UnitPriceExclusive.ToString() + "', " +
                                        "'" + obj.Discount.ToString() + "', " +
                                        "'" + obj.Exclusive.ToString() + "'," +
                                        "'" + obj.Tax.ToString() + "'," +
                                        "'" + obj.Total.ToString() + "'," +
                                        "'" + Analis1.Replace("'", "''") + "'," +
                                        "'" + Analis2.Replace("'", "''") + "'," +
                                        "'" + Analis3.Replace("'", "''") + "'," +
                                        "'" + linecost.ToString() + "'," +
                                        "'" + linegp.ToString() + "'," +
                                        "'" + thisdt.ToString("yyyy-MM") + "'," +
                                        "'" + thisdt.ToString("yyyy") + "'," +
                                        "'" + isinvfromquote + "'," +
                                        "" + Convert.ToInt16(obj.LineType.ToString()) + "," +
                                        "'" + LineType + "'," +
                                        "'" + Userdetails.CoID + "'," +
                                        "'" + "" + "'," +
                                        "'" + FYear + "'," +
                                        "'" + deldt.ToString("yyyy-MM-dd") + "'," +
                                         "'" + weekNumr + "'," +
                                        "'" + DocumentMsg.Replace("'", "''") + "'," +
                                        "'" + LineMsg.Replace("'", "''") + "'," +
                                        "'" + DocDiscPerc + "'," +
                                        "'" + Math.Round(Line_Nett_Value, 2) + "'," +
                                        "'" + Math.Round(Line_Disc_From_Doc_Disc, 2) + "'," +
                                         "'" + statusid + "'," +
                                        "'" + item["Status"].ToString() + "'," +
                                        "'" + deldt.ToString("yyyy") + weeknumS + "'," +
                                         "'" + item["DeliveryAddress01"]?.ToString().Replace("'", "''") + "'," +
                                        "'" + item["DeliveryAddress02"]?.ToString().Replace("'", "''") + "'," +
                                        "'" + item["DeliveryAddress03"]?.ToString().Replace("'", "''") + "'," +
                                        "'" + item["DeliveryAddress04"]?.ToString().Replace("'", "''") + "'," +
                                        "'" + item["DeliveryAddress05"]?.ToString().Replace("'", "''") + "')";
                                        try
                                        {
                                            command.ExecuteNonQuery();
                                        }
                                        catch { }
                                    }
                                    i++;
                                }
                            }
                        }
                        string formattedDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
                        SetSQLDataFromString("UPDATE DIUpdateLog SET SalesOrderUpdate = '" + formattedDateTime + "' WHERE CompanyID = '" +Userdetails.CoID + "' AND ProfileID = '" + Userdetails.UserGuiD + "'");
                        skipQty = skipQty + RetQty;
                    } while (skipQty < TotQty);


                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // find and remove deleted sales orders
                    skipQty = 0;
                    List<string> SOList = new List<string>();
                    List<string> CurrSOList = new List<string>();
                    do
                    {
                       string requestUrl = sageurl + "SalesOrder/GET?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID + "&$skip=" + skipQty + "&$filter=Status ne 'Invoiced'&includeDetail=false&includeCustomerDetails=false";
                        JObject parsedJSON = await api.ApiCallAsync(requestUrl, Userdetails);
                        if (parsedJSON.Count > 0)
                        {
                            JArray items = (JArray)parsedJSON["Results"];
                            TotQty = Convert.ToInt32(parsedJSON["TotalResults"]);
                            RetQty = Convert.ToInt32(parsedJSON["ReturnedResults"]);
                            foreach (var item in items)
                            {
                                SOList.Add(item["DocumentNumber"].ToString());
                            }
                        }
                        skipQty = skipQty + RetQty;
                    } while (skipQty < TotQty);

                    DataSet ds = GetSQLDataFromString("SELECT Number FROM DITransactionsTbl WHERE CoID = '" + Userdetails.CoID + "' AND (Type = '" + "Sales_Order" + "' AND  Status <> '" + "Invoiced" + "')");
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        CurrSOList.Add(dr[0].ToString());
                    }
                    var differences = CurrSOList.Except(SOList).ToList();
                    foreach (var difference in differences)
                    {
                        command.Connection = con;
                        command.CommandText = "Delete FROM DITransactionsTbl WHERE Number = '" + difference.ToString() + "' AND CoID = '" + Userdetails.CoID + "'";
                        command.ExecuteNonQuery();
                    }
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    con.Close();
                    differences.Clear();
                    ds.Clear();
                    ds.Dispose();
                    SOList.Clear();
                    CurrSOList.Clear();
                }
            }
        }

        private static string getitemcodefromid(string itemid)
        {
            // get item code from sqlite db
            string itemcode = "";
            DataSet MyDS = new DataSet();
            MyDS = GetSQLDataFromString("Select TOP (1) Code FROM ItemsMaster WHERE ID = '" + itemid + "'");
            if (MyDS != null) 
            {
                if (MyDS.Tables[0].Rows.Count > 0)
                {
                    itemcode = MyDS.Tables[0].Rows[0][0].ToString();
                }
                MyDS.Clear();
                MyDS.Dispose();
            }
            return itemcode;
        }

        /// FROM Data Insights - PURCHASE ORDERS
        /// 
        public static async Task LoadSuppPurchaseOrders(UserDetails Userdetails)
        {
            CultureInfo cultureInfo = CultureInfo.CurrentCulture;
            // Get the Calendar object for the specified culture
            Calendar calendar = cultureInfo.Calendar;
            int weekNumr = 0;

            DateTime duedt = Convert.ToDateTime(DateTime.Today.AddMonths(-12), CultureInfo.InvariantCulture), deldt = Convert.ToDateTime(DateTime.Today.AddMonths(-1), CultureInfo.InvariantCulture); ;
            cTripleDES des = new cTripleDES(key, iv);

            double skipQty = 0; int i = 0; float TotQty = 0; int RetQty = 0;
            double DocDiscPerc = 0;

            DataSet MyDS = new DataSet();
            MyDS = GetSQLDataFromString("Select TOP (1) POUpdate FROM DIUpdateLog WHERE CompanyID = " + Userdetails.CoID + " AND ProfileID = '" + Userdetails.UserGuiD + "'");
            if (MyDS.Tables[0].Rows.Count > 0)
            {
                if (MyDS.Tables[0].Rows[0]["POUpdate"].ToString() != "")
                {
                    try
                    {
                        PODT = DateTime.ParseExact(MyDS.Tables[0].Rows[0]["POUpdate"].ToString(), "dd/MM/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                       // PODT = Convert.ToDateTime(MyDS.Tables[0].Rows[0]["POUpdate"].ToString(), CultureInfo.InvariantCulture);
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        PODT = Convert.ToDateTime(duedt, CultureInfo.InvariantCulture);
                    }
                    catch { }
                }
            }
            else
            {
                try
                {
                    SetSQLDataFromString($"INSERT INTO DIUpdateLog (CompanyID, ProfileID, SalesOrderUpdate) Values ('{Userdetails.CoID}','{Userdetails.UserGuiD}','{PODT}')");
                }
                catch { }
            }

            MyDS.Tables.Clear();
            MyDS.Dispose();

            string conString = constrP;
#if DEBUG
            conString = constr;
#endif

            ApiUrlCall api = new ApiUrlCall();
            using (SqlConnection con = new SqlConnection(conString))
            {
                using (var command = new SqlCommand())
                {
                    con.Open();
                    do
                    {
                        string requestUrl = sageurl + "PurchaseOrder/GET?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID + "&$skip=" + skipQty + FiltDate(PODT.ToString()) + ")&includeDetail=true&includeSupplierDetails=true";
                        JObject parsedJSON = await api.ApiCallAsync(requestUrl, Userdetails);
                        if (parsedJSON.Count > 0)
                        {
                            JArray items = (JArray)parsedJSON["Results"];
                            TotQty = Convert.ToInt32(parsedJSON["TotalResults"]);
                            RetQty = Convert.ToInt32(parsedJSON["ReturnedResults"]);

                            string SalesRep = string.Empty; string Ref = string.Empty, fromDocNum = ""; ;
                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    DateTime thisdt = Convert.ToDateTime(item["Date"], CultureInfo.InvariantCulture);

                                    try
                                    {
                                        duedt = Convert.ToDateTime(item["DueDate"], CultureInfo.InvariantCulture);
                                        deldt = Convert.ToDateTime(item["DeliveryDate"], CultureInfo.InvariantCulture);
                                        weekNumr = calendar.GetWeekOfYear(Convert.ToDateTime(duedt.ToString()), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                                    }
                                    catch { }

                                    weekNumr = calendar.GetWeekOfYear(Convert.ToDateTime(thisdt.ToString()), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

                                    // delete all current lines for this document
                                    command.Connection = con;
                                    command.CommandText = "Delete FROM DITransactionsTbl WHERE Number = '" + item["DocumentNumber"].ToString().Replace("'", "''") + "' AND CoID = '" + Userdetails.CoID + "'";
                                    command.ExecuteNonQuery();
                                    /////////////////////////
                                    ///
                                    List<DocumentLine> myObjects = JsonConvert.DeserializeObject<List<DocumentLine>>(item["Lines"].ToString());
                                    foreach (DocumentLine obj in myObjects)
                                    {
                                        SalesRep = string.Empty;
                                        Ref = string.Empty;
                                        if (item.ToString().Contains("Reference"))
                                        {
                                            if (item["Reference"] != null)
                                            {
                                                Ref = item["Reference"].ToString().Replace("'", "''");
                                            }
                                        }
                                        string Analis1 = string.Empty;
                                        if (item.ToString().Contains("AnalysisCategoryId1"))
                                        {
                                            if (obj.AnalysisCategoryId1 != null)
                                            {
                                                Analis1 = obj.AnalysisCategoryId1.ToString();
                                            }
                                        }
                                        string Analis2 = string.Empty;
                                        if (item.ToString().Contains("AnalysisCategoryId2"))
                                        {
                                            if (obj.AnalysisCategoryId2 != null)
                                            {
                                                Analis2 = obj.AnalysisCategoryId2.ToString();
                                            }
                                        }
                                        string Analis3 = string.Empty;
                                        if (item.ToString().Contains("AnalysisCategoryId3"))
                                        {
                                            if (obj.AnalysisCategoryId3 != null)
                                            {
                                                Analis3 = obj.AnalysisCategoryId3.ToString();
                                            }
                                        }

                                        double linecost = 0, linegp = 0;
                                        string LineType = GetLineType(Convert.ToInt16(obj.LineType.ToString()));

                                        // calculate financial; YE
                                        string FYear = thisdt.AddYears(1).ToString("yyyy");
                                        int MthNum = Convert.ToInt16(thisdt.ToString("MM"));
                                        if (MthNum < 4)
                                        {
                                            FYear = thisdt.AddYears(1).AddMonths(-MthNum).ToString("yyyy");
                                        }

                                        string DocumentMsg = "";
                                        if (item["Message"] != null)
                                        {
                                            DocumentMsg = item["Message"].ToString();
                                        }

                                        string LineMsg = "";
                                        if (obj.Comments != null && obj.Comments.ToString() != "")
                                        {
                                            LineMsg = obj.Comments.ToString();
                                        }

                                        string statusid = "99";
                                        if (item["StatusId"] != null)
                                        {
                                            statusid = item["StatusId"].ToString();
                                        }
                                        string weeknumS = weekNumr.ToString();
                                        if (weeknumS.ToString().Length == 1)
                                        {
                                            weeknumS = "0" + weeknumS;
                                        }

                                        string suppname = string.Empty;
                                        suppname = item["SupplierName"]?.ToString().Replace("'", "''");
                                        if (constr.ToLower().Contains("demo"))
                                        {
                                            if (suppname.Length > 4)
                                            {
                                                suppname = suppname.Substring(0, 4) + "***";
                                            }
                                        }
                                        // Insert entries in database table
                                        command.CommandText = "INSERT INTO DITransactionsTbl (TransID, From_Document,  Number,Type, Date, CustomerID, Customer_Name, Sales_Rep, Reference, Item,  Description, Quantity, Unit_Price_Excl, Discount, Exclusive, Tax, Total, Analysis_Category1, Analysis_Category2, Analysis_Category3,Line_Cost, Line_GP, Year_Month, Year, LineTypeID, LineType, CoID, CoName, Year_Financial, DateDue, WeekNumber, DocumentMessage, LineMessage, Doc_Discount_Perc, Nett_Line_Total, Doc_Discount_Amount, StatusID, Status, YearWeek)" +
                                        " VALUES ('" + obj.ID + "'," +
                                        "'" + fromDocNum + "', " +
                                        "'" + item["DocumentNumber"]?.ToString().Replace("'", "''") + "', " +
                                        "'" + "Purchase_Order" + "', " +
                                        "'" + thisdt.ToString("yyyy-MM-dd") + "', " +
                                        "'" + item["SupplierId"]?.ToString().Replace("'", "''") + "', " +
                                        "'" + suppname + "', " +
                                        "'" + SalesRep + "', " +
                                            "'" + Ref.Replace("'", "''") + "', " +
                                        "'" + getitemcodefromid(obj.SelectionId.ToString()) + "', " +
                                        "'" + obj.Description.ToString().Replace("'", "''") + "', " +
                                        "'" + obj.Quantity.ToString() + "', " +
                                        "'-" + obj.UnitPriceExclusive.ToString() + "', " +
                                        "'-" + obj.Discount.ToString() + "', " +
                                        "'-" + obj.Exclusive.ToString() + "'," +
                                        "'" + obj.Tax.ToString() + "'," +
                                        "'-" + obj.Total.ToString() + "'," +
                                        "'" + Analis1.Replace("'", "''") + "'," +
                                        "'" + Analis2.Replace("'", "''") + "'," +
                                        "'" + Analis3.Replace("'", "''") + "'," +
                                        "'" + linecost.ToString() + "'," +
                                        "'" + linegp.ToString() + "'," +
                                        "'" + thisdt.ToString("yyyy-MM") + "'," +
                                        "'" + thisdt.ToString("yyyy") + "'," +
                                        "" + Convert.ToInt16(obj.LineType.ToString()) + "," +
                                        "'" + LineType + "'," +
                                        "'" + Userdetails.CoID + "' , " +
                                        "'" + "" + "'," +
                                         "'" + FYear + "'," +
                                         "'" + deldt.ToString("yyyy-MM-dd") + "', " +
                                        "'" + weekNumr + "'," +
                                        "'" + DocumentMsg.Replace("'", "''") + "'," +
                                        "'" + LineMsg.Replace("'", "''") + "'," +
                                        "'" + DocDiscPerc + "'," +
                                        "'-" + obj.Exclusive.ToString() + "'," +
                                        "'-" + obj.Discount.ToString() + "'," +
                                        "'" + statusid + "'," +
                                        "'" + item["Status"].ToString() + "'," +
                                        "'" + deldt.ToString("yyyy") + weeknumS + "')";
                                        try
                                        {
                                            command.ExecuteNonQuery();
                                        }
                                        catch { }
                                    }
                                    i++;
                                }
                            }
                        }
                        string formattedDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
                        SetSQLDataFromString("UPDATE DIUpdateLog SET POUpdate = '" + formattedDateTime + "' WHERE CompanyID = '" + Userdetails.CoID + "' AND ProfileID = '" + Userdetails.UserGuiD + "'");
                        skipQty = skipQty + RetQty;
                    } while (skipQty < TotQty);
                    con.Close();
                }
            }
        }

        public static DataSet GetSQLDataFromStoredProc(string storedProcName, Dictionary<string, object> parameters = null)
        {
            string conString = constrP;
#if DEBUG
            conString = constr;
#endif
            DataSet ds = new DataSet();
            using (SqlConnection con = new SqlConnection(conString))
            {
                using (SqlCommand cmd = new SqlCommand(storedProcName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    using (var oda = new SqlDataAdapter())
                    {
                        con.Open();
                        try
                        {
                            cmd.CommandTimeout = 0;
                            oda.SelectCommand = cmd;
                            oda.Fill(ds);
                        }
                        catch (Exception ex)
                        {
                            var srt = ex.Message;
                        }
                        con.Close();
                    }
                }
            }
            return ds;
        }

        public async Task<List<DocumentLineOrigValues>> LoadOriginalAvCostsAsync(string ItmFilter, UserDetails userDetails)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string requestUrl = $"{sageurl}Item/GET?apikey={APIKey}&CompanyID={userDetails.CoID}&$filter={ItmFilter}";
                JObject parsedJSON = await ApiCallAsync(requestUrl, userDetails);

                if (parsedJSON != null && parsedJSON["TotalResults"] != null)
                {
                    return parsedJSON["Results"].ToObject<List<DocumentLineOrigValues>>();
                }

                return new List<DocumentLineOrigValues>();
            }
        }

        public static decimal NumberToDecimal(decimal value, int decimalPlaces)
        {
            return Math.Round(value, decimalPlaces);
        }

        public static async Task GetOneSalesOrder(UserDetails Userdetails, long DocID)
        {
            double skipQty = 0; float TotQty = 0; int RetQty = 0;
            DataSet ds = new DataSet();
            DateTime LastCallDt = DateTime.Now;
            string SalesRep = string.Empty; string Ref = string.Empty;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var CompList = _db.DocHeaders.Where(x => x.CompanyID == Userdetails.CoID && x.DocType == 5 && x.DocID == DocID).FirstOrDefault();

                string requestUrl = sageurl + "SalesOrder/GET/" + DocID + "?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID + "&includeDetail=true&includeCustomerDetails=true";
                ApiUrlCall api = new ApiUrlCall();
                JObject parsedJSON = await api.ApiCallAsync(requestUrl, Userdetails);
                if (parsedJSON.Count > 0)
                    {
                    DocHeader DocH = new DocHeader();
                    DocH.DocGUID = Guid.NewGuid();
                    DocH.DocDate = Convert.ToDateTime(parsedJSON["Date"].ToString());
                    DocH.DocID = Convert.ToInt64(parsedJSON["ID"].ToString());
                    DocH.DueDelDate = Convert.ToDateTime(parsedJSON["DeliveryDate"] ?? "");
                    DocH.DocumentNumber = parsedJSON["DocumentNumber"].ToString().Trim() ?? "";
                    DocH.CustSuppID = Convert.ToInt64(parsedJSON["CustomerId"].ToString());
                    DocH.CustSupName = parsedJSON["CustomerName"].ToString().Trim() ?? "";
                    DocH.CompanyID = Convert.ToInt32(Userdetails.CoID);
                    DocH.Started = false;
                    DocH.Complete = false;
                    if (parsedJSON["SalesRepresentativeId"] != null) DocH.SalesRepresentativeId = Convert.ToInt64(parsedJSON["SalesRepresentativeId"].ToString()) ;
                    DocH.Status = parsedJSON["Status"].ToString().Trim();
                    DocH.Reference = parsedJSON["Reference"].ToString().Trim();
                    DocH.Message = parsedJSON["Message"].ToString().Trim();
                    DocH.Discount = Convert.ToDecimal(parsedJSON["Discount"].ToString());
                    DocH.Exclusive = Convert.ToDecimal(parsedJSON["Exclusive"].ToString());
                    DocH.Tax = Convert.ToDecimal(parsedJSON["Tax"].ToString());
                    DocH.Rounding = Convert.ToDecimal(parsedJSON["Rounding"].ToString());
                    DocH.Total = Convert.ToDecimal(parsedJSON["Total"].ToString());
                    DocH.DocType = 5;
                    DocH.DelAddress1 = parsedJSON["DeliveryAddress01"].ToString().Trim() ?? "";
                    DocH.DelAddress2 = parsedJSON["DeliveryAddress02"].ToString().Trim() ?? "";
                    DocH.DelAddress3 = parsedJSON["DeliveryAddress03"].ToString().Trim() ?? "";
                    DocH.DelAddress4 = parsedJSON["DeliveryAddress04"].ToString().Trim() ?? "";
                    DocH.DelAddress5 = parsedJSON["DeliveryAddress05"].ToString().Trim() ?? "";
                    DocH.PostAddress5 = parsedJSON["PostalAddress05"].ToString().Trim() ?? "";
                    if (parsedJSON.ToString().Contains("SalesRepresentative"))
                    {
                        DocH.SalesRepName = parsedJSON["SalesRepresentative"]["Name"].ToString().Replace("'", "''").Trim();
                    }
                    DocH.Active = true;
                    _db.DocHeaders.Add(DocH);
                    _db.SaveChanges();

                    if (CompList.Complete == false) await api.LoadSOLines(DocID, Userdetails);
                }
            }
        }
        public async Task<JObject> GetOneSOtoInv(UserDetails Userdetails, long DocID)
        {
            string requestUrl = sageurl + "SalesOrder/GET/" + DocID + "?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID + "&includeDetail=true&includeCustomerDetails=true";
            ApiUrlCall api = new ApiUrlCall();
            JObject parsedJSON = await api.ApiCallAsync(requestUrl, Userdetails);

            if (parsedJSON.Count > 0)
            {
                JObject invoiceJson = new JObject();

                // Map invoice header
                invoiceJson["Date"] = DateTime.Now.ToString("yyyy-MM-dd");
                invoiceJson["DueDate"] = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd");

                // Link back to the SO
                invoiceJson["FromDocument"] = "SalesOrder";
                invoiceJson["FromDocumentId"] = parsedJSON["ID"];
                invoiceJson["FromDocumentTypeId"] = 1;

                // Status-related
                invoiceJson["Status"] = "Posted";
                invoiceJson["AllowOnlinePayment"] = true;
                invoiceJson["Paid"] = false;
                invoiceJson["Locked"] = false;

                // Customer details
                invoiceJson["CustomerId"] = parsedJSON["CustomerId"];
                invoiceJson["CustomerName"] = parsedJSON["CustomerName"];
                invoiceJson["Customer"] = parsedJSON["Customer"]; // full object

                // Customer address details
                invoiceJson["PostalAddress01"] = parsedJSON["PostalAddress01"];
                invoiceJson["PostalAddress02"] = parsedJSON["PostalAddress02"];
                invoiceJson["PostalAddress03"] = parsedJSON["PostalAddress03"];
                invoiceJson["PostalAddress04"] = parsedJSON["PostalAddress04"];
                invoiceJson["PostalAddress05"] = parsedJSON["PostalAddress05"];
                invoiceJson["DeliveryAddress01"] = parsedJSON["DeliveryAddress01"];
                invoiceJson["DeliveryAddress02"] = parsedJSON["DeliveryAddress02"];
                invoiceJson["DeliveryAddress03"] = parsedJSON["DeliveryAddress03"];
                invoiceJson["DeliveryAddress04"] = parsedJSON["DeliveryAddress04"];
                invoiceJson["DeliveryAddress05"] = parsedJSON["DeliveryAddress05"];

                // Sales Rep details
                invoiceJson["SalesRepresentativeId"] = parsedJSON["SalesRepresentativeId"];
                invoiceJson["SalesRepresentative_Description"] = parsedJSON["SalesRepresentative_Description"];
                invoiceJson["SalesRepresentative"] = parsedJSON["SalesRepresentative"];

                // Optional references
                invoiceJson["Reference"] = "From SO " + parsedJSON["DocumentNumber"];
                invoiceJson["Message"] = parsedJSON["Message"];

                // Financial flags
                invoiceJson["Inclusive"] = true;
                invoiceJson["DiscountPercentage"] = parsedJSON["DiscountPercentage"];
                invoiceJson["TaxReference"] = parsedJSON["TaxReference"];

                // Header-level foreign currency
                invoiceJson["Customer_CurrencyId"] = parsedJSON["Customer_CurrencyId"];
                invoiceJson["Customer_ExchangeRate"] = parsedJSON["Customer_ExchangeRate"];

                // Clean + copy line items
                JArray lines = new JArray();
                if (parsedJSON["Lines"] is JArray soLines)
                {
                    foreach (JObject line in soLines)
                    {
                        JObject cleanLine = new JObject
                        {
                            ["SelectionId"] = line["SelectionId"],
                            ["TaxTypeId"] = line["TaxTypeId"],
                            ["Description"] = line["Description"],
                            ["LineType"] = line["LineType"],
                            ["Quantity"] = line["Quantity"],
                            ["UnitPriceExclusive"] = line["UnitPriceExclusive"],
                            ["UnitPriceInclusive"] = line["UnitPriceInclusive"],
                            ["TaxPercentage"] = line["TaxPercentage"],
                            ["DiscountPercentage"] = line["DiscountPercentage"],
                            ["Unit"] = line["Unit"],

                            // Foreign currency details
                            ["CurrencyId"] = line["CurrencyId"],
                            ["UnitCost"] = line["UnitCost"],

                            // Added fields for TaxInvoice
                            ["Exclusive"] = line["Exclusive"] ?? 0.0,
                            ["Discount"] = line["Discount"] ?? 0.0,
                            ["Tax"] = line["Tax"] ?? 0.0,
                            ["Total"] = line["Total"] ?? 0.0
                        };

                        lines.Add(cleanLine);
                    }
                }
                invoiceJson["Lines"] = lines;

                return invoiceJson;
            }

            return new JObject();
        }

        public async Task<JObject> GetOneSOFull(UserDetails Userdetails, long DocID)
        {
            string requestUrl = sageurl + "SalesOrder/GET/" + DocID + "?apikey={" + APIKey + "}&CompanyID=" + Userdetails.CoID + "&includeDetail=true&includeCustomerDetails=true";
            ApiUrlCall api = new ApiUrlCall();
            JObject soJson = await api.ApiCallAsync(requestUrl, Userdetails);

            if (soJson.Count > 0)
            {
                // Clone JSON so original remains untouched
                JObject updateJson = (JObject)soJson.DeepClone();

                // Example: Only update Status
                updateJson["StatusId"] = "4";

                // You can now post this back to Sage to update the SO
                return updateJson;
            }

            return new JObject();
        }

        public void LogErrorToFile(string message)
        {
            try
            {
                string appDataPath = HostingEnvironment.MapPath("~/App_Data");
                if (string.IsNullOrEmpty(appDataPath))
                {
                    appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
                }

                // Ensure folder exists
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                // Build daily log filename: errorlog_YYYY-MM-DD.txt
                string fileName = $"errorlog_{DateTime.Now:yyyy-MM-dd}.txt";
                string fullPath = Path.Combine(appDataPath, fileName);

                // Prepare log entry
                string logEntry = $"{DateTime.Now:HH:mm:ss} - {message}{Environment.NewLine}";

                // Append to file (creates file if it does not exist)
                File.AppendAllText(fullPath, logEntry);
            }
            catch
            {
                // Avoid recursive errors if logging fails
            }
        }

        public async Task<JObject> LoadOneItemJson(string ItmFilter, UserDetails userDetails)
        { 
            string requestUrl = $"{sageurl}Item/GET?apikey={APIKey}&CompanyID={userDetails.CoID}&$filter={ItmFilter}";
            JObject parsedJSON = await ApiCallAsync(requestUrl, userDetails);
            return parsedJSON; 
        }
    }
}