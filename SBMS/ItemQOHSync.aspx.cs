using SBMS.Classes;
using SBMS.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ItemQOHSync : BasePage
    {
        int lotno = 0;
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }
        protected async void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                string returnUrl = HttpUtility.UrlEncode(Request.RawUrl);
                Response.Redirect("~/Login.aspx?returnUrl=" + returnUrl, false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            string imgname = CurrentUser.CoID + ".png";
            string imgPath = $"~/images/CoImages/{imgname}";
            if (File.Exists(Server.MapPath(imgPath)))
            {
               imgCoImg.ImageUrl = ResolveUrl(imgPath);
            }
            else
            {
                imgCoImg.ImageUrl = ResolveUrl("~/images/CoImages/0000.png");
            }

            if (!IsPostBack)
            {
                ApiUrlCall api = new ApiUrlCall();
                var errors = await api.LoadItems(CurrentUser);
                if (errors.Count > 0)
                {
                    string message = string.Join("\n", errors);
                    AlertHelper.ShowSweetAlert(this, message, "error");
                }
                else
                {
                    string message = "All items successfully sync\\'ed with Sage successfully.";
                    AlertHelper.ShowSweetAlert(this, message, "success");
                }
                LoadItems(); // Load data initially
                loadstores();
                if (CurrentUser.CoID.ToString() == "739032")
                {
                    lbtmDLUpdate.Visible = true;
                }
            }
        }

        protected void GridItems_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortExpression = e.SortExpression;
            string sortDirection = GetSortDirection(sortExpression);

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                IQueryable<ItemsMaster> Items = _db.ItemsMasters
                                                    .Where(x => x.CompanyID == CurrentUser.CoID && x.Active == true && x.Physical == true);

                // Apply sorting based on sortExpression and sortDirection
                Items = ApplySorting(Items, sortExpression, sortDirection);

                GridItems.DataSource = Items.ToList();
                GridItems.DataBind();
            }
        }

        private IQueryable<ItemsMaster> ApplySorting(IQueryable<ItemsMaster> query, string sortExpression, string sortDirection)
        {
            switch (sortExpression)
            {
                case "Description":
                    query = (sortDirection == "ASC") ? query.OrderBy(x => x.Description) : query.OrderByDescending(x => x.Description);
                    break;
                case "BarCode":
                    query = (sortDirection == "ASC") ? query.OrderBy(x => x.BarCode) : query.OrderByDescending(x => x.BarCode);
                    break;
                case "QuantityOnHand":
                    query = (sortDirection == "ASC") ? query.OrderBy(x => x.QuantityOnHand) : query.OrderByDescending(x => x.QuantityOnHand);
                    break;
                // Add more cases for other sortable columns as needed
                default:
                    break;
            }

            return query;
        }

        private string GetSortDirection(string column)
        {
            string defaultSortDirection = "ASC";
            string sortDirection = ViewState["SortDirection"] as string;

            if (sortDirection == null || (ViewState["SortExpression"] as string) != column)
            {
                sortDirection = defaultSortDirection;
            }
            else
            {
                sortDirection = sortDirection == "ASC" ? "DESC" : "ASC";
            }

            ViewState["SortDirection"] = sortDirection;
            ViewState["SortExpression"] = column;

            return sortDirection;
        }

        protected void LoadItems()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                IQueryable<ItemsMaster> Items = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.Active == true && x.Physical == true);
                if (txtfind.Text.ToString().Length > 0)
                {
                    Items = Items.Where(x => x.CategoryDescript.ToLower().Contains(txtfind.Text.ToLower()) || x.Description.ToLower().Contains(txtfind.Text.ToLower()) || x.Code.ToLower().Contains(txtfind.Text.ToLower()));
                }
                foreach (ItemsMaster Itm in Items)
                {
                    try
                    {
                        Itm.TotQOH_MDF = _db.ItemTransactions.Where(it => it.CompanyID == CurrentUser.CoID && it.ItemID == Itm.ID).Sum(it => it.Qty);
                        Itm.TotQOH_MDF = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(Itm.TotQOH_MDF, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
                        Itm.QuantityOnHand = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(Itm.QuantityOnHand, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
                    }
                    catch { }                   
                }

                if (chkDiscrep.Checked)
                {
                    Items = Items.Where(x => x.QuantityOnHand != x.TotQOH_MDF);
                }
                
                if (chkZeroOnly.Checked)
                {
                    Items = Items.Where(x => x.QuantityOnHand == 0);
                }

                if (chkzero.Checked)
                {
                    Items = Items.Where(x => x.QuantityOnHand != 0);
                }

                GridItems.DataSource = Items.ToList();
                GridItems.DataBind();
            }
        }

        protected void lbtnBOM_Click(object sender, EventArgs e)
        {
            LinkButton lbtnBOM = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnBOM.NamingContainer;
            int bomid = Convert.ToInt32(lbtnBOM.CommandArgument);
            Response.Redirect("~/ItemEdit.aspx?itm=" + lbtnBOM.CommandArgument);
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

        protected void GridItems_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            decimal SBCAQty = 0, MDFQty = 0;
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                try { SBCAQty = Convert.ToDecimal(e.Row.Cells[3].Text); } catch { }
                try { MDFQty = Convert.ToDecimal(e.Row.Cells[4].Text); } catch { }

                if (SBCAQty != MDFQty)
                {
                    e.Row.Cells[4].BackColor = System.Drawing.Color.Red;
                    e.Row.Cells[4].ForeColor = System.Drawing.Color.White;
                }
            }
            //if (CurrentUser.UseModule2 == false)
            //{
             //   e.Row.Cells[5].Visible = false;
                //e.Row.Cells[6].Visible = false;
                //e.Row.Cells[7].Visible = false;
                //e.Row.Cells[8].Visible = false;
                //e.Row.Cells[9].Visible = false;
          //  }
        }

        private void loadstores()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true && x.StoreCode != "CoD" && x.StoreCode != "CoR" && x.StoreCode.ToLower() != "scr").ToList();
                DDStoreTo.DataSource = Stores;
                DDStoreTo.DataTextField = "StoreDescript";
                DDStoreTo.DataValueField = "StoreCode";
                DDStoreTo.DataBind();
                DDStoreTo.Items.Insert(0, "-Select Store-");
            }
        }

        protected void lbtnUpdateYes_Click(object sender, EventArgs e)
        {
            int Reccount = 0, StorScr = 0;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var StorScp = _db.Stores.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StoreDescript.ToLower().Contains("scrap"));
               if (StorScp != null)
                {
                    StorScr = StorScp.StoreID; // StoreID of Scrap Store
                }
                foreach (GridViewRow gvr in GridItems.Rows)
                {
                    CheckBox chkb = new CheckBox();
                    chkb = (CheckBox)gvr.FindControl("chkSelect");
                    if (chkb.Checked == true)
                    {
                        decimal SBCAQty = 0, MDFQty = 0;
                        try { SBCAQty = Convert.ToDecimal(gvr.Cells[3].Text); } catch { }
                        try { MDFQty = Convert.ToDecimal(gvr.Cells[4].Text); } catch { }
                        if (MDFQty != SBCAQty)
                        {
                            string itmCode = HttpUtility.HtmlDecode(gvr.Cells[0].Text);
                            string StorCode = DDStoreTo.SelectedValue.ToString();
                            var itm = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.Code == itmCode).FirstOrDefault();
                            decimal qoh = 0;
                            if (gvr?.Cells != null && gvr.Cells.Count > 3 && !string.IsNullOrWhiteSpace(gvr.Cells[3].Text))
                            {
                                decimal.TryParse(gvr.Cells[3].Text.Trim(), out qoh);
                            }
                            itm.TotQOH_MDF = qoh;

                            // remove all historic transactions
                            var RemoveList = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemCode == itmCode).ToList();
                            _db.ItemTransactions.RemoveRange(RemoveList);
                            // remove Lot Tracking history
                            var RemoveLots = _db.LotTrackingMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemCode == itmCode).ToList();
                            _db.LotTrackingMasters.RemoveRange(RemoveLots);
                            // _db.SaveChanges();

                            ItemTransaction ItemTrans = new ItemTransaction();
                            ItemTrans.CompanyID = CurrentUser.CoID;
                            ItemTrans.DocumentID = 0;
                            ItemTrans.TransactionType = "OPN";
                            ItemTrans.ItemID = Convert.ToInt64(itm.ID);
                            ItemTrans.ItemCode = itm.Code;
                            ItemTrans.ItemDescription = itm.Description;
                            ItemTrans.Unit = itm.Unit;
                            ItemTrans.FromID = 0;
                            ItemTrans.ToID = _db.Stores.Where(x => x.StoreCode == StorCode && x.CompanyID == CurrentUser.CoID).Select(x => x.StoreID).FirstOrDefault();
                            ItemTrans.Qty = Convert.ToDecimal(gvr.Cells[3].Text);
                            ItemTrans.DocumentType = 1;
                            ItemTrans.TransactionDate = DateTime.Now;
                            ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid
                            ItemTrans.PriceExclusive = itm.AverageCost;
                            ItemTrans.AdditionalCosts = 0;
                            ItemTrans.TotalUnitPriceExclInclAdd = itm.AverageCost;
                            ItemTrans.TotalLineValExcl = ItemTrans.PriceExclusive * ItemTrans.Qty;
                            ItemTrans.TransactionReference = "Opening Balance";
                            ItemTrans.ExchRate = 1;
                            // create Lot Number
                            if (itm.IsLotTracked == true)
                            {
                                //int recnum = GetLotNum(CurrentUser.CoID);
                                if (lotno == 0) { lotno = GetLotNum(CurrentUser.CoID); } else { lotno = lotno + 1; }
                                ItemTrans.LotNumber = DateTime.Today.ToString("ddMMyyyy") + StorCode + lotno.ToString();

                                // check for itemstore link
                                long itmID = Convert.ToInt64(ItemTrans.ItemID);
                                var ItS = _db.ItemStoreLinkMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreID == ItemTrans.ToID && x.ItemID == itmID).FirstOrDefault();
                                if (ItS == null)
                                {
                                    // create item/store link
                                    ItemStoreLinkMaster isL = new ItemStoreLinkMaster
                                    {
                                        CompanyID = CurrentUser.CoID,
                                        ItemID = Convert.ToInt64(itm.ID),
                                        StoreID = (int?)ItemTrans.ToID,
                                        Active = true,
                                    };
                                    _db.ItemStoreLinkMasters.Add(isL);


                                    ItS = _db.ItemStoreLinkMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreID == StorScr && x.ItemID == itmID).FirstOrDefault();
                                    if (ItS == null)
                                    {
                                        if (StorScr > 0)
                                        {
                                            // Link all items to scrap store
                                            isL = new ItemStoreLinkMaster
                                            {
                                                CompanyID = CurrentUser.CoID,
                                                ItemID = Convert.ToInt64(itm.ID),
                                                StoreID = _db.Stores.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StoreDescript.ToLower().Contains("scrap")).StoreID,
                                                Active = true,
                                            };
                                        }
                                        _db.ItemStoreLinkMasters.Add(isL);
                                    }
                                }
                                // save new Lot Number to db
                                LotTrackingMaster LtNew = new LotTrackingMaster();
                                LtNew.LotNumber = ItemTrans.LotNumber;
                                LtNew.CreatedDate = DateTime.Now;
                                LtNew.CompanyID = CurrentUser.CoID;
                                LtNew.ItemCode = ItemTrans.ItemCode;
                                LtNew.ItemId = ItemTrans.ItemID;
                                LtNew.LotActive = true;
                                LtNew.LotTotUnitPrice = (decimal)ItemTrans.TotalUnitPriceExclInclAdd;
                                LtNew.LotQuantity = ItemTrans.Qty ?? 0m;
                                _db.LotTrackingMasters.Add(LtNew);
                            }
                            _db.ItemTransactions.Add(ItemTrans);
                            //try
                            //{
                            //  _db.SaveChanges();
                            Reccount++;
                            // }
                            // catch (Exception ex) { }
                        }
                    }
                }
                try
                {
                    _db.SaveChanges();
                    Reccount++;
                }
                catch (Exception ex) { }
                LoadItems();
                ShowMessage(sender, EventArgs.Empty, Reccount -1 + " Items successfuilly updated.");
            }
        }

        protected int GetLotNum(long CoID)
        {
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

        protected void lbtnfind_Click(object sender, EventArgs e)
        {
            LoadItems();
        }

        protected void chkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chks = new CheckBox();
            chks = (CheckBox)sender;

            CheckBox chkb = new CheckBox();
            foreach (GridViewRow grv in GridItems.Rows)
            {
                chkb = (CheckBox)grv.FindControl("chkSelect");
                if (chks.Checked)
                {
                    chkb.Checked = true;
                }
                else
                {
                    chkb.Checked = false;
                }
            }
        }

        protected void ShowMessage(object sender, EventArgs e, string msg)
        {
            string message = "alert('" + msg + "')";
            ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
        }

        protected void lbtmDLUpdate_Click(object sender, EventArgs e)
        {
            int i = 0;
            string StorCode = string.Empty;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var itmL = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.QuantityOnHand >0).ToList(); 
                foreach (var itm in itmL) {
                    if (itm.TextUserField1 != null && itm.TextUserField1.ToString() != "")
                    {
                        StorCode = itm.TextUserField1;
                        // remove all historic transactions
                        var RemoveList = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemCode == itm.Code).ToList();
                        _db.ItemTransactions.RemoveRange(RemoveList);
                        _db.SaveChanges();

                        ItemTransaction ItemTrans = new ItemTransaction();
                        ItemTrans.CompanyID = CurrentUser.CoID;
                        ItemTrans.DocumentID = 0;
                        ItemTrans.TransactionType = "OPNT";
                        ItemTrans.ItemID = Convert.ToInt64(itm.ID);
                        ItemTrans.ItemCode = itm.Code;
                        ItemTrans.ItemDescription = itm.Description;
                        ItemTrans.Unit = itm.Unit;
                        ItemTrans.FromID = 0;
                        ItemTrans.ToID = _db.Stores.Where(x => x.StoreCode == StorCode && x.CompanyID == CurrentUser.CoID).Select(x => x.StoreID).FirstOrDefault();
                        ItemTrans.Qty = itm.QuantityOnHand;
                        ItemTrans.DocumentType = 1;
                        ItemTrans.TransactionDate = DateTime.Now;
                        ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid
                        ItemTrans.PriceExclusive = itm.AverageCost;
                        ItemTrans.AdditionalCosts = 0;
                        ItemTrans.TotalUnitPriceExclInclAdd = itm.AverageCost;
                        ItemTrans.TotalLineValExcl = ItemTrans.PriceExclusive * ItemTrans.Qty;
                        ItemTrans.TransactionReference = "Transfer To Initial Bin";
                        ItemTrans.ExchRate = 1;
                        // create Lot Number
                        if (itm.IsLotTracked == true)
                        {
                            int recnum = GetLotNum(CurrentUser.CoID);
                            ItemTrans.LotNumber = DateTime.Today.ToString("ddMMyyyy") + StorCode + recnum.ToString();

                            // check for itemstore link
                            long itmID = Convert.ToInt64(ItemTrans.ItemID);
                            var ItS = _db.ItemStoreLinkMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreID == ItemTrans.ToID && x.ItemID == itmID).FirstOrDefault();
                            if (ItS == null)
                            {
                                // create item/store link
                                ItemStoreLinkMaster isL = new ItemStoreLinkMaster
                                {
                                    CompanyID = CurrentUser.CoID,
                                    ItemID = Convert.ToInt64(itm.ID),
                                    StoreID = (int?)ItemTrans.ToID,
                                    Active = true,
                                };
                                _db.ItemStoreLinkMasters.Add(isL);
                            }
                            // save new Lot Number to db
                            LotTrackingMaster LtNew = new LotTrackingMaster();
                            LtNew.LotNumber = ItemTrans.LotNumber;
                            LtNew.CreatedDate = DateTime.Now;
                            LtNew.CompanyID = CurrentUser.CoID;
                            LtNew.ItemCode = ItemTrans.ItemCode;
                            LtNew.ItemId = ItemTrans.ItemID;
                            LtNew.LotActive = true;
                            LtNew.LotTotUnitPrice = (decimal)ItemTrans.TotalUnitPriceExclInclAdd;
                            LtNew.LotQuantity = ItemTrans.Qty ?? 0m;
                            _db.LotTrackingMasters.Add(LtNew);
                        }
                        _db.ItemTransactions.Add(ItemTrans);
                        try
                        {
                            _db.SaveChanges();
                            i++;
                            if (i == 1597) 
                            { string str = ""; }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
            }
        }

        protected void chkzero_CheckedChanged(object sender, EventArgs e)
        {
            LoadItems();
        }
    }
}