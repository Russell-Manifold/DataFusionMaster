using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class Login : System.Web.UI.Page
    {
        UserDetails userDets;
        public static byte[] key = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 15, 11, 12, 77, 14, 15, 16, 17, 18, 91, 20, 21, 22, 23, 24 };
        public static byte[] iv = { 8, 7, 6, 5, 4, 3, 2, 1 };
        protected void Page_Load(object sender, EventArgs e)
        {
            lblErr.Text = "";
            // Mobile device detection (server-side)
            //string userAgent = Request.UserAgent ?? "";
            //bool isMobile = userAgent.ToLower().Contains("iphone") ||
            //                userAgent.ToLower().Contains("android") ||
            //                userAgent.ToLower().Contains("ipad") ||
            //                userAgent.ToLower().Contains("mobile");

            //if (isMobile && !Request.Url.AbsolutePath.ToLower().Contains("loginm.aspx"))
            //{
            //    Response.Redirect("~/SBMSMobile/LoginM.aspx", true);
            //    return;
            //}
            //// for mobile testing
            //Response.Redirect("~/SBMSMobile/LoginM.aspx", true);

            if (!IsPostBack)
            {
                chkRememberMe.Checked = false;

                var loginCookie = Request.Cookies["Login"];
                if (loginCookie != null)
                {
                    // Pre-fill username
                    txtUsername.Text = loginCookie["Username"];

                    // Pre-fill password from cookie
                    string encPwd = loginCookie["Password"];
                    if (!string.IsNullOrEmpty(encPwd))
                    {
                        txtPwd.Attributes["value"] = Encoding.UTF8.GetString(Convert.FromBase64String(encPwd));
                    }

                    // Silent login if LastLoginDate is today
                    if (DateTime.TryParse(loginCookie["LastLoginDate"], out DateTime lastLogin))
                    {
                        chkRememberMe.Checked = true;
                        if (lastLogin.Date == DateTime.Now.Date)
                        {
                            string username = loginCookie["Username"];
                            if (!string.IsNullOrEmpty(username))
                            {
                                FormsAuthentication.SetAuthCookie(username, true);
                                lbtnlogin_Click(sender, e);
                            }
                        }
                    }
                }
            }
        }

        protected async void lbtnlogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPwd.Text;

            if (password == "")
            {
                try
                {
                    password = Encoding.UTF8.GetString(Convert.FromBase64String(Request.Cookies["Login"]["Password"]));
                }
                catch { };
            }

            if (UserLogin(username, password))
            {
                // Remember Me: save username, password, last login date
                if (chkRememberMe.Checked)
                {
                    Response.Cookies["Login"]["Username"] = username;
                    Response.Cookies["Login"]["Password"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
                    Response.Cookies["Login"]["LastLoginDate"] = DateTime.Now.ToString("yyyy-MM-dd");
                    Response.Cookies["Login"].Expires = DateTime.Now.AddDays(30);
                }
                else
                {
                    Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
                }

                // Set FormsAuthentication cookie
                FormsAuthentication.SetAuthCookie(username, true);
                
                   // Retrieve user details
                userDets = Session["UserDetails"] as UserDetails;

                #region uservalidation
                string jsonString = "{ \"Username\": \"" + username + "\", \"Password\": \"" + password + "\" }";
                ApiUrlCall Api = new ApiUrlCall();
                string RetStr = await Api.ValidateUserAsync("Company", jsonString, userDets);
                              
                bool isValid = true;
                lnkSage.Visible = false;
                if (RetStr != "OK")
                {
                    lblErr.Text = "Sage User Authorisation error. Unable to validate user with Sage: " + RetStr;
                    lnkSage.Visible = true;
                    return;
                }

                #endregion

                if (isValid)
                {
                    // Check for first use
                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var LastCall = _db.LastCallLogs.FirstOrDefault(x => x.CompanyID == userDets.CoID);
                        if (LastCall == null)
                        {
                            PnlNewP.Style.Add("display", "inline-block");
                            PnlNewUser.Style.Add("display", "none");
                            Panel1.Style.Add("display", "none");
                            return;
                        }

                        // Handle returnUrl
                        string returnUrl = Request.QueryString["returnUrl"];
                        if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/"))
                            Response.Redirect(returnUrl, false);
                        else
                            Response.Redirect("~/Dashboard.aspx?user=" + userDets.UserGuiD, false);
                    }
                }
                else
                {
                    lblErr.Text = "Invalid Sage Login Credentials, Unable to continue";
                    return;
                }
            }
            else
            {
                lblErr.Text = "Email address not validated, Unable to continue";
                return;
            }
        }

        public bool UserLogin(string username, string pwd)
        {
            bool rolecaneditlots = false;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Get all active user records for this email
                var userList = _db.UsersMasters.Where(x => x.Useremail == username && x.Active == true).ToList();
                if (userList.Count > 1)
                {
                    // Multiple companies found for this user, show company selection modal
                    // Populate DDCompanyList with CompanyID and companyName from CompanyMaster
                    var companyIds = userList.Select(u => u.CompanyID).ToList();
                    var companies = _db.CompanyMasters.Where(c => c.SBCACoID.HasValue && companyIds.Contains(c.SBCACoID.Value)).Select(c => new { c.SBCACoID, c.CompanyName }).ToList();
                    DDCompanyList.DataSource = companies;
                    DDCompanyList.DataTextField = "CompanyName";
                    DDCompanyList.DataValueField = "SBCACoID";
                    DDCompanyList.DataBind();
                    // Show the modal popup
                    ModalSelectCompany.Show();
                    return false;
                }
                else
                {
                    if (CheckUserLogginedIn(username, pwd)) {
                        if (userList.Count > 0)
                        {
                            setUserDetails(username, pwd, userList.First().CompanyID);
                            return true;
                        }
                        else
                        {
                            AlertHelper.ShowSweetAlert(this, "User not linked to a company, please contact system administrator", "error", "Login Error");
                            return false;
                        }                
                    }
                    else
                    {
                        return false;  
                    }
                }
            }  
        }

        protected bool CheckUserLogginedIn(string username, string pwd)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var user = _db.UsersMasters.Where(x => x.Useremail == username && x.Active == true).FirstOrDefault();
                if (user != null)
                {
                    var loginCookie = Request.Cookies["Login"];
                    if (loginCookie != null)
                    {
                        if (DateTime.TryParse(loginCookie["LastLoginDate"], out DateTime lastLogin))
                        {
                            if (lastLogin.Date != DateTime.Now.Date)
                            {
                                if (user.IsLoggedIn)
                                {
                                    if (user.LoggedInSessionID != null)
                                    {
                                        if (Session.SessionID != user.LoggedInSessionID)
                                        {
                                            Button25_ModalPopupExtender.Show();
                                            return false;
                                        }
                                    }
                                }
                                else { }
                            }
                        }
                    }
                    else
                    {
                        if (user.IsLoggedIn)
                        {
                            if (user.LoggedInSessionID != null)
                            {
                                if (Session.SessionID != user.LoggedInSessionID)
                                {
                                    Button25_ModalPopupExtender.Show();
                                    return false;
                                }
                            }
                        }
                        else { }
                    }         
                }
                return true;
            }
        }

        protected bool setUserDetails(string username, string pwd, long CompanyID)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {

                var user = _db.UsersMasters.Where(x => x.Useremail == username && x.Active == true && x.CompanyID == CompanyID).FirstOrDefault(); 
                UserDetails userDetails = new UserDetails
                {
                    UserName = user.FirstName,
                    LoginName = user.Useremail,
                    LoginPwd = pwd,
                    CoID = user.CompanyID,
                    UserGuiD = (Guid)user.UserGUID,
                    RoleID = (int)user.RoleId,
                    isSuperUser = (bool)user.IsSuperUser,

                };

                if ((int)user.RoleId > 0)
                {
                    // get role details
                    var Role = _db.RolesMasters.Where(x => x.CompanyID == userDetails.CoID && x.RoleID == userDetails.RoleID).FirstOrDefault();
                    if (Role != null)
                    {
                        userDetails.UseGenericLogin = Role.UseGenericLogin;
                        userDetails.CanReceive = Role.CanReceive;
                        userDetails.CanTransfer = Role.CanReceive;
                        userDetails.CanViewPickSlips = Role.CanViewPickSlips;
                        userDetails.CanTrackPickSlips = Role.CanTrackPickSlips;
                        userDetails.CanSalesForecast = Role.CanSalesForecast;
                        userDetails.CanViewJobCards = Role.CanViewJobCards;
                        userDetails.CanTrackJobCards = Role.CanTrackJobCards;
                        userDetails.CanSeeFGDemands = Role.CanSeeFGDemands;
                        userDetails.CanCreateBomKit = Role.CanCreateBomKit;
                        userDetails.CanStockControl = Role.CanStockControl;
                        userDetails.CanViewWorksOrders = Role.CanViewWorksOrders;
                        userDetails.CanFillWorksOrders = Role.CanFillWorksOrders;
                        userDetails.CanViewRMD = Role.CanViewRMD;
                        userDetails.CanEditLotNumbers = Role.UseEditLotNumbers;
                    }
                }


                var GenLogIn = _db.CompanyMasters
                        .Where(x => x.SBCACoID == userDetails.CoID)
                        .Select(x => new
                        {
                            x.CoGenericLoginEmail,
                            x.CoGenericLoginPwd,
                            x.UseModule2,
                            x.UseModule3,
                            x.UATMode,
                            x.SendMessages,
                            x.UseLotTracking,
                            x.UseLotAddDetails,
                            x.ItemQtyDecPlaces,
                            x.UseAutoManf,
                            x.UsePickSlipTracking,
                            x.UseBarcodes,
                            x.AutoUpdateSageSOs,
                            x.AutoGenTaxInvoice,
                            x.UsePacks,
                            x.UseEndDate,
                            x.ShowManfCosts,
                            x.SageWeightField
                        })
                        .FirstOrDefault();

                if (GenLogIn != null)
                {
                    userDetails.UseModule2 = GenLogIn.UseModule2;
                    userDetails.UseModule3 = GenLogIn.UseModule3;
                    userDetails.UATMode = (bool)GenLogIn.UATMode;
                    userDetails.SendMessages = (bool)GenLogIn.SendMessages;
                    userDetails.UsePickSlipTracking = (bool)GenLogIn.UsePickSlipTracking;
                    userDetails.UseBarcodes = (bool)GenLogIn.UseBarcodes;
                    userDetails.AutoUpdateSageSOs = (bool)GenLogIn.AutoUpdateSageSOs;
                    userDetails.AutoGenTaxInvoice = (bool)GenLogIn.AutoGenTaxInvoice;
                    userDetails.UseAutoManf = GenLogIn.UseAutoManf;
                    userDetails.UsePacks = GenLogIn.UsePacks;
                    userDetails.ExpiryDate = GenLogIn.UseEndDate != null ? (DateTime)GenLogIn.UseEndDate : DateTime.MaxValue;
                    userDetails.ShowManfCosts = GenLogIn.ShowManfCosts;
                    userDetails.SageWeightField = GenLogIn.SageWeightField ?? "";
                    if (GenLogIn.UseLotTracking == true)
                    {
                        userDetails.CompanyUseLotNumbers = true;
                    }
                    else
                    {
                        userDetails.CompanyUseLotNumbers = false;
                    }

                    userDetails.CompanyUseLotAddDetails = GenLogIn.UseLotAddDetails;
                    //if (userDetails.UseGenericLogin)
                    //{
                    //    userDetails.LoginName = GenLogIn.CoGenericLoginEmail;
                    //    userDetails.LoginPwd = GenLogIn.CoGenericLoginPwd;
                    //}
                    userDetails.CompanyDecPlaces = GenLogIn.ItemQtyDecPlaces;

                }

                userDetails.LoggedInSessionID = Session.SessionID;
                user.IsLoggedIn = true;
                user.LastActivity = DateTime.Now;
                user.LoggedInSessionID = Session.SessionID;

                try
                {
                    _db.SaveChanges();
                }
                catch { }

                Session["UserDetails"] = userDetails;
                Session["UserID"] = userDetails.UserGuiD;
                Session["RoleID"] = userDetails.RoleID;
                Session["CoID"] = userDetails.CoID;
                return true;
            }
        }

        protected void btnLogUserOut_Click(object sender, EventArgs e)
        {
            string RsTr = LogUserOut(txtUsername.Text);
            if (RsTr  == "OK")
            {
                lblErr.Text ="Successfully Logged Out";
            } else
            {
                lblErr.Text = RsTr;
            }
        }

        public string LogUserOut(string username)
        {
            string retStr = "OK";
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                try
                {
                    var user = _db.UsersMasters.Where(x => x.Useremail == username).FirstOrDefault();
                    if (user != null)
                    {
                        user.LoggedInSessionID = null;
                        user.IsLoggedIn = false;
                        try
                        {
                            _db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            retStr = ex.Message;
                        }
                    }
                }
                catch { }
            }
            return retStr;
        }

        protected void lbtnCancel_Click(object sender, EventArgs e)
        {
            Panel1.Style.Add("display", "inline-block");
            PnlNewP.Style.Add("display", "none");
            PnlNewUser.Style.Add("display", "none");
        }

        protected async void btnSaveYes_Click(object sender, EventArgs e)
        {
            userDets = Session["UserDetails"] as UserDetails;
            long superuserid;
            btnSaveYes.Style.Add("display", "none");
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
               int roles = _db.RolesMasters.Where(x => x.CompanyID == userDets.CoID).Count();
                if (roles < 1)
                {
                    // add default roles
                    RolesMaster rlm = new RolesMaster();
                    rlm.RoleName = "Super User";
                    rlm.CompanyID = userDets.CoID;
                    rlm.CanReceive = true;
                    rlm.CanTransfer = true;
                    rlm.CanViewPickSlips = true;
                    rlm.CanTrackPickSlips = true;
                    rlm.CanSalesForecast = true;
                    rlm.CanViewJobCards = true;
                    rlm.CanTrackJobCards = true;
                    rlm.CanSeeFGDemands = true;
                    rlm.NotifyGRN = false;
                    rlm.NotifyTransfer = true;
                    rlm.NotifySOComplete = true;
                    rlm.CanCreateBomKit = true;
                    rlm.CanInvoice = true;
                    rlm.CanStockControl = true;
                    rlm.CanViewWorksOrders = true;
                    rlm.CanFillWorksOrders = true;
                    rlm.CanViewRMD = true;
                    rlm.IsJobCards = false;
                    rlm.IsPicker = false;
                    rlm.IsProduction = false;
                    _db.RolesMasters.Add(rlm);
                    try
                    {
                        _db.SaveChanges();
                    }
                    catch { }
                    superuserid = rlm.RoleID;
                    var usr = _db.UsersMasters.Where(x => x.UserGUID == userDets.UserGuiD).FirstOrDefault();
                    usr.RoleId = (int?)superuserid;
                    try
                    {
                        _db.SaveChanges();
                    }
                    catch { }


                    rlm = new RolesMaster();
                    rlm.RoleName = "Receiving";
                    rlm.CompanyID = userDets.CoID;
                    rlm.CanReceive = true;
                    rlm.CanViewPickSlips = true;
                    rlm.IsJobCards = false;
                    rlm.IsPicker = false;
                    rlm.IsProduction = false;
                    _db.RolesMasters.Add(rlm);

                    rlm = new RolesMaster();
                    rlm.RoleName = "Stock Controller";
                    rlm.CompanyID = userDets.CoID;
                    rlm.CanTransfer = true;
                    rlm.CanViewPickSlips = true;
                    rlm.CanTrackPickSlips = true;
                    rlm.NotifyPSMove = true;
                    rlm.IsJobCards = false;
                    rlm.IsPicker = false;
                    rlm.IsProduction = false;
                    _db.RolesMasters.Add(rlm);
                   
                    try
                    {
                        _db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        string str = ex.Message;
                    }
                }
                   
                int StorCount = _db.Stores.Where(x => x.CompanyID == userDets.CoID).Count();
                if (StorCount < 1)
                {
                    // add default stores
                    Store stor = new Store();
                    stor.CompanyID = userDets.CoID;
                    stor.StoreCode = "RM";
                    stor.StoreDescript = "Raw Materials";
                    stor.StoreActive = true;
                    stor.AllowReceiving = true;
                    stor.AllowPicking = false;
                    stor.StorePerm = true;
                    _db.Stores.Add(stor);

                    try
                    {
                        _db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        string str = ex.Message;
                    }

                    stor = new Store();
                    stor.CompanyID = userDets.CoID;
                    stor.StoreCode = "WIP";
                    stor.StoreDescript = "Work In Progress";
                    stor.StoreActive = true;
                    stor.AllowReceiving = false;
                    stor.AllowPicking = false;
                    stor.StorePerm = true;
                    _db.Stores.Add(stor);

                    stor = new Store();
                    stor.CompanyID = userDets.CoID;
                    stor.StoreCode = "FG";
                    stor.StoreDescript = "Finished Goods";
                    stor.StoreActive = true;
                    stor.AllowReceiving = false;
                    stor.AllowPicking = true;
                    stor.StorePerm = true;
                    _db.Stores.Add(stor);

                    stor = new Store();
                    stor.CompanyID = userDets.CoID;
                    stor.StoreCode = "SCR";
                    stor.StoreDescript = "Scrap";
                    stor.StoreActive = true;
                    stor.AllowReceiving = false;
                    stor.AllowPicking = false;
                    stor.StorePerm = true;
                    _db.Stores.Add(stor);

                    stor = new Store();
                    stor.CompanyID = userDets.CoID;
                    stor.StoreCode = "CoR";
                    stor.StoreDescript = "Company Receiving";
                    stor.StoreActive = true;
                    stor.AllowReceiving = false;
                    stor.AllowPicking = false;
                    stor.StorePerm = true;
                    _db.Stores.Add(stor);

                    stor = new Store();
                    stor.CompanyID = userDets.CoID;
                    stor.StoreCode = "CoD";
                    stor.StoreDescript = "Company Dispatch";
                    stor.StoreActive = true;
                    stor.AllowReceiving = false;
                    stor.AllowPicking = false;
                    stor.StorePerm = true;
                    _db.Stores.Add(stor);

                    try
                    {
                        _db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        string str = ex.Message;
                    }
                }
                
                int PSCount = _db.PickSlipProcesses.Where(x => x.CompanyID == userDets.CoID).Count();
                if (PSCount < 1)
                {
                    // add default picking prcesses
                    PickSlipProcess psp = new PickSlipProcess();
                    psp.CompanyID = userDets.CoID;
                    psp.PSName = "Captured";
                    psp.PSActive = true;
                    psp.Seq = 0;
                    _db.PickSlipProcesses.Add(psp);

                    psp = new PickSlipProcess();
                    psp.CompanyID = userDets.CoID;
                    psp.PSName = "Issued";
                    psp.PSActive = true;
                    psp.Seq = 1;
                    _db.PickSlipProcesses.Add(psp);

                    psp = new PickSlipProcess();
                    psp.CompanyID = userDets.CoID;
                    psp.PSName = "Picked";
                    psp.PSActive = true;
                    psp.Seq = 2;
                    _db.PickSlipProcesses.Add(psp);

                    psp = new PickSlipProcess();
                    psp.CompanyID = userDets.CoID;
                    psp.PSName = "Packed";
                    psp.PSActive = true;
                    psp.Seq = 3;
                    _db.PickSlipProcesses.Add(psp);

                    psp = new PickSlipProcess();
                    psp.CompanyID = userDets.CoID;
                    psp.PSName = "Checked";
                    psp.PSActive = true;
                    psp.Seq = 4;
                    _db.PickSlipProcesses.Add(psp);

                    psp = new PickSlipProcess();
                    psp.CompanyID = userDets.CoID;
                    psp.PSName = "Dispatch";
                    psp.PSActive = true;
                    psp.Seq = 5;
                    _db.PickSlipProcesses.Add(psp);

                    try
                    {
                        _db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        string str = ex.Message;
                    }
                }

                int wsCount = _db.WorkStations.Where(x => x.CompanyID == userDets.CoID).Count();
                if (wsCount < 1)
                {
                    WorkStation wst = new WorkStation
                    {
                        CompanyID = userDets.CoID,
                        WSName = "Captured",
                        WSActive = true,
                        Seq = 1,
                        IsWIP = false
                    };
                    _db.WorkStations.Add(wst);

                    //LastCallLog lcm = new LastCallLog();
                    //lcm.CompanyID = userDets.CoID;
                    //_db.LastCallLogs.Add(lcm);

                    //try
                    //{
                    //    _db.SaveChanges();
                    //}
                    //catch (Exception ex)
                    //{
                    //    string str = ex.Message;
                    //}
                }
                
                ApiUrlCall api = new ApiUrlCall();   
                var errors = await api.LoadItems(userDets);
                if (errors.Count > 0)
                {
                    string message = string.Join("\n", errors);
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "alert", $"alert('{message}');", true);
                }
                else
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "alertSuccess", "alert('All items successfully sync\\'ed with Sage successfully');", true);
                }

                var LastItmCall = _db.LastCallLogs.FirstOrDefault(x => x.CompanyID == userDets.CoID);
                if (LastItmCall != null)
                {
                    LastItmCall.LastItemDate = DateTime.Now.AddYears(-6);
                }
                else
                {
                    LastCallLog LCall = new LastCallLog();
                    LCall.LastItemDate = DateTime.Now.AddYears(-6);
                    LCall.LastSODate = DateTime.Now.AddYears(-6);
                    LCall.LastPODate = DateTime.Now.AddYears(-6);
                    LCall.CompanyID = userDets.CoID;

                    _db.LastCallLogs.Add(LCall);
                }
                _db.SaveChanges();

                JObject GLResult = await api.LoadGLAccounts(userDets);
                if (GLResult != null && GLResult["error"] != null)
                {
                    // Handle the error
                    lblErr.Text = $"Error loadingGLAccounts: {GLResult["error"]}";
                }

                JObject SOResult = await api.LoadSalesOrders(userDets);
                if (SOResult != null && SOResult["error"] != null)
                {
                    lblErr.Text = $"Error loading Sales Orders: {SOResult["error"]}";
                }

                JObject POResult = await api.LoadPurchaseOrders(userDets);
                if (POResult != null && POResult["error"] != null)
                {
                    lblErr.Text = $"Error loading Sales Orders: {POResult["error"]}";
                }

                JObject taxTypeResult = await api.GetTaxType(userDets);
                if (taxTypeResult != null && taxTypeResult["error"] != null)
                {
                    // Handle the error
                    lblErr.Text = $"Error loading tax types: {taxTypeResult["error"]}";
                }

                var Accts = _db.AccountsMasters.Where(x => x.CompanyID == userDets.CoID && x.AccountName.ToLower().Contains("unallocated expense")).FirstOrDefault();
                if (Accts != null)
                {
                    Accts.AccountAddCosts = true;
                    _db.SaveChanges();
                }

                var itemslist = _db.ItemsMasters.Where(x => x.CompanyID == userDets.CoID && x.QuantityOnHand != 0).ToList();
                // link all items to all stores
                var stores = _db.Stores.Where(x => x.CompanyID == userDets.CoID && x.AllowReceiving == true).ToList();
                List<ItemStoreLinkMaster> bulkInsertList = new List<ItemStoreLinkMaster>();
                foreach (var store in stores)
                {
                    foreach (var item in itemslist)
                    {
                        bulkInsertList.Add(new ItemStoreLinkMaster
                        {
                            CompanyID = userDets.CoID,
                            ItemID = item.ID,
                            StoreID = store.StoreID,
                            Active = true
                        });
                    }
                }
                // Use AddRange to insert all at once
                _db.ItemStoreLinkMasters.AddRange(bulkInsertList);
                try
                {
                    _db.SaveChanges(); // One save operation instead of multiple calls
                }
                catch (Exception ex)
                {
                    lblErr.Text = $"Error linking items to stores, unable to continue: {ex.Message}";
                }
             
                // get RM store ID
                //int rmstore = _db.Stores.Where(x => x.AllowReceiving == true && x.CompanyID == userDets.CoID).Select(x => x.StoreID).FirstOrDefault();
                //// update opening balancs and add transaction records
                //int recnum = GetLotNum(userDets.CoID);
                //foreach (var item in itemslist)
                //{
                //    ItemTransaction ItemTrans = new ItemTransaction();
                //    ItemTrans.CompanyID = userDets.CoID;
                //    ItemTrans.DocumentID = 0;
                //    ItemTrans.TransactionType = "OPN";
                //    ItemTrans.ItemID = Convert.ToInt64(item.ID);
                //    ItemTrans.ItemCode = item.Code;
                //    ItemTrans.ItemDescription = item.Description;
                //    ItemTrans.Unit = item.Unit;
                //    ItemTrans.FromID = 0;
                //    ItemTrans.ToID = rmstore;
                //    ItemTrans.Qty = item.QuantityOnHand;
                //    ItemTrans.DocumentType = 1;
                //    ItemTrans.BalOnHandToStore = ItemTrans.Qty;
                //    ItemTrans.BalOnHandFromStore = 0;
                //    ItemTrans.TransactionDate = DateTime.Now;
                //    ItemTrans.ByRoleID = userDets.RoleID; // roleid
                //    ItemTrans.PriceExclusive = item.AverageCost;
                //    ItemTrans.AdditionalCosts = 0;
                //    ItemTrans.TotalUnitPriceExclInclAdd = item.AverageCost;
                //    ItemTrans.TotalLineValExcl = ItemTrans.PriceExclusive * ItemTrans.Qty;
                //    ItemTrans.TransactionReference = "Opening Balance";
                //    // create Lot Number
                //    if (item.IsLotTracked == true)
                //    {
                //        ItemTrans.LotNumber = DateTime.Today.ToString("ddMMyyyy") + "RM" + recnum.ToString();
                //         // save new Lot Number to db
                //        LotTrackingMaster LtNew = new LotTrackingMaster();
                //        LtNew.LotNumber = ItemTrans.LotNumber;
                //        LtNew.CreatedDate = DateTime.Now;
                //        LtNew.CompanyID = userDets.CoID;
                //        LtNew.ItemCode = ItemTrans.ItemCode;
                //        LtNew.ItemId = ItemTrans.ItemID;
                //        LtNew.LotActive = true;
                //        LtNew.LotQuantity = item.QuantityOnHand;
                //        LtNew.LotTotUnitPrice = (decimal)ItemTrans.TotalUnitPriceExclInclAdd;
                //        _db.LotTrackingMasters.Add(LtNew);
                //        recnum++;
                //    }
                //    _db.ItemTransactions.Add(ItemTrans);
                //        /////////////////////////////////////////////////////////
                //    }
                //try
                //{
                //    _db.SaveChanges();
                //}
                //catch (Exception ex)
                //{
                //    lblErr.Text = $"Error creating opening balanaces and transactions, transactions incomplete:- {ex.Message}";
                //    return;
                //}
            }
            lblErr.Text = $"Data Update Successful, Log in";
            PnlNewUser.Style.Add("display", "inline-block");
            Panel1.Style.Add("display", "none");
            PnlNewP.Style.Add("display", "none");
        }
        
        protected int GetLotNum(long CoID)
        {
            int lotno = 0;
            DateTime dtY = DateTime.Today.AddDays(-1);
            DateTime dtT = DateTime.Today.AddDays(1);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var count = _db.LotTrackingMasters
                        .Where(it => it.CompanyID == CoID && it.CreatedDate > dtY && it.CreatedDate < dtT)
                        .Count();
                lotno = count + 1;
            }
            return lotno;
        }

       protected void lbtnPnlNewclose_Click(object sender, EventArgs e)
        {
            PnlNewUser.Style.Add("display", "none");
            Panel1.Style.Add("display", "inline-block");
        }

        protected void lbtnNewProfile_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/DataFusionOnboard.aspx", true);
        }

        protected async void LinkCoYes_Click(object sender, EventArgs e)
        {

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string username = txtUsername.Text.Trim();
                string password = txtPwd.Text;

                var user = _db.UsersMasters.Where(x => x.Useremail == username && x.Active == true).FirstOrDefault();
                if (user != null)
                {
                    var loginCookie = Request.Cookies["Login"];
                    if (loginCookie != null)
                    {
                        if (DateTime.TryParse(loginCookie["LastLoginDate"], out DateTime lastLogin))
                        {
                            if (lastLogin.Date != DateTime.Now.Date)
                            {
                                if (user.IsLoggedIn)
                                {
                                    if (user.LoggedInSessionID != null)
                                    {
                                        if (Session.SessionID != user.LoggedInSessionID)
                                        {
                                            Button25_ModalPopupExtender.Show();
                                            return;
                                        }
                                    }
                                }
                                else { }
                            }
                        }
                    }
                    else
                    {
                        if (user.IsLoggedIn)
                        {
                            if (user.LoggedInSessionID != null)
                            {
                                if (Session.SessionID != user.LoggedInSessionID)
                                {
                                    Button25_ModalPopupExtender.Show();
                                    return;
                                }
                            }
                        }
                        else { }
                    }
                }
            }
            LinkCoGo();
        }

       protected async void LinkCoGo() 
        {
            string username = txtUsername.Text.Trim();
            string password = txtPwd.Text;
            if (setUserDetails(username, password, Convert.ToInt64(DDCompanyList.SelectedValue)))
                {

               // Remember Me: save username, password, last login date
                if (chkRememberMe.Checked)
                {
                    Response.Cookies["Login"]["Username"] = username;
                    Response.Cookies["Login"]["Password"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
                    Response.Cookies["Login"]["LastLoginDate"] = DateTime.Now.ToString("yyyy-MM-dd");
                    Response.Cookies["Login"].Expires = DateTime.Now.AddDays(30);
                }
                else
                {
                    Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
                }

                // Set FormsAuthentication cookie
                FormsAuthentication.SetAuthCookie(username, true);

                // Retrieve user details
                userDets = Session["UserDetails"] as UserDetails;

                #region uservalidation
                string jsonString = "{ \"Username\": \"" + username + "\", \"Password\": \"" + password + "\" }";
                ApiUrlCall Api = new ApiUrlCall();
                string RetStr = await Api.ValidateUserAsync("Company", jsonString, userDets);
                bool isValid = true;
                lnkSage.Visible = false;
                if (RetStr != "OK")
                {
                    lblErr.Text = "Sage User Authorisation error. Unable to validate user with Sage: " + RetStr;
                    lnkSage.Visible = true;
                    return;
                }

                #endregion

                if (isValid)
                {
                    // Check for first use
                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var LastCall = _db.LastCallLogs.FirstOrDefault(x => x.CompanyID == userDets.CoID);
                        if (LastCall == null)
                        {
                            PnlNewP.Style.Add("display", "inline-block");
                            PnlNewUser.Style.Add("display", "none");
                            Panel1.Style.Add("display", "none");
                            return;
                        }

                        // Handle returnUrl
                        string returnUrl = Request.QueryString["returnUrl"];
                        if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/"))
                            Response.Redirect(returnUrl, false);
                        else
                            Response.Redirect("~/Dashboard.aspx?user=" + userDets.UserGuiD, false);
                    }
                }
                else
                {
                    lblErr.Text = "Invalid Sage Login Credentials, Unable to continue";
                    return;
                }
            }
            else
            {
                lblErr.Text = "Email address not validated, Unable to continue";
                return;
            }
        }
    }
}
