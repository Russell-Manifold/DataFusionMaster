using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    /// <summary>
    /// Stock Control -> Returnables. Log a return (stock comes back in, and Sage is adjusted)
    /// or an opening balance against a customer, see who is holding what, and choose which
    /// items are returnable. The arithmetic and SQL live in Classes/Returnables.cs.
    /// </summary>
    public partial class ReturnablesPage : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            if (CurrentUser.CanStockControl != true)
            {
                Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
                return;
            }
            if (!IsPostBack)
            {
                lblUsername.Text = $":.. {CurrentUser.UserName}..:  ";
                string imgPath = $"~/images/CoImages/{CurrentUser.CoID}.png";
                imgCoImg.ImageUrl = ResolveUrl(File.Exists(Server.MapPath(imgPath)) ? imgPath : "~/images/CoImages/0000.png");
                txtDate.Text = DateTime.Today.ToString("yyyy-MM-dd");
                LoadAll();
                ShowType();
                // Nothing set up yet: open on the Setup tab, since that is the only thing to do.
                if (GridItems.Rows.Count == 0) hfTab.Value = "setup";
            }
        }

        /// <summary>Everything on the page. If the tables are not there yet, say so instead of failing.</summary>
        private void LoadAll()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                try
                {
                    var items = Returnables.Items(_db, CurrentUser.CoID);
                    GridItems.DataSource = items;
                    GridItems.DataBind();
                    pnlNoItems.Visible = items.Count == 0;

                    string keepItem = DDItem.SelectedValue;
                    DDItem.Items.Clear();
                    DDItem.Items.Add(new ListItem("- Select -", ""));
                    foreach (var i in items)
                        DDItem.Items.Add(new ListItem((i.ItemCode ?? "") + " - " + (i.ItemDescription ?? ""), i.ItemID.ToString()));
                    if (DDItem.Items.FindByValue(keepItem) != null) DDItem.SelectedValue = keepItem;
                    else if (items.Count == 1) DDItem.SelectedIndex = 1;

                    var balances = Returnables.Balances(_db, CurrentUser.CoID);
                    GridOut.DataSource = chkShowZero.Checked ? balances : balances.Where(x => x.Outstanding != 0).ToList();
                    GridOut.DataBind();

                    GridRecent.DataSource = Returnables.Recent(_db, CurrentUser.CoID, 50);
                    GridRecent.DataBind();
                }
                catch
                {
                    pnlNotReady.Visible = true;
                    pnlMain.Visible = false;
                    return;
                }

                long coId = CurrentUser.CoID;
                if (DDCustomer.Items.Count == 0)
                {
                    // Customers are whoever this company has Sales Orders for.
                    var customers = _db.DocHeaders
                        .Where(x => x.CompanyID == coId && x.DocType == 5 && x.CustSuppID != null)
                        .GroupBy(x => x.CustSuppID)
                        .Select(g => new { ID = g.Key, Name = g.Max(x => x.CustSupName) })
                        .OrderBy(x => x.Name).ToList();
                    DDCustomer.Items.Add(new ListItem("- Select -", ""));
                    foreach (var c in customers)
                        DDCustomer.Items.Add(new ListItem(c.Name ?? c.ID.ToString(), c.ID.ToString()));
                }
                if (DDStore.Items.Count == 0)
                {
                    var stores = _db.Stores.Where(x => x.CompanyID == coId && x.StoreActive != false)
                                           .OrderBy(x => x.StoreCode).Select(x => new { x.StoreID, x.StoreCode }).ToList();
                    DDStore.Items.Add(new ListItem("- Select -", ""));
                    foreach (var s in stores) DDStore.Items.Add(new ListItem(s.StoreCode, s.StoreID.ToString()));
                    if (stores.Count == 1) DDStore.SelectedIndex = 1;
                }
            }
        }

        private void ShowType()
        {
            bool isReturn = DDType.SelectedValue == "R";
            lblStore.Visible = isReturn;
            DDStore.Visible = isReturn;
            lblTypeHint.Text = isReturn
                ? "Puts the items back into stock and adjusts Sage stock. No credit note is raised."
                : "Sets the starting figure only. No stock moves and nothing goes to Sage.";
        }

        protected void DDType_SelectedIndexChanged(object sender, EventArgs e) { ShowType(); }

        protected void chkShowZero_CheckedChanged(object sender, EventArgs e) { LoadAll(); }

        protected async void lbtnSave_Click(object sender, EventArgs e)
        {
            bool isReturn = DDType.SelectedValue == "R";

            long customerId, itemId;
            if (!long.TryParse(DDCustomer.SelectedValue, out customerId) || !long.TryParse(DDItem.SelectedValue, out itemId))
            {
                AlertHelper.ShowSweetAlert(this, "Select a customer and an item.", "error");
                return;
            }
            decimal qty;
            if (!decimal.TryParse(txtQty.Text.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out qty) || qty <= 0)
            {
                AlertHelper.ShowSweetAlert(this, "Enter a quantity greater than zero, using a decimal point.", "error");
                return;
            }
            DateTime moveDate;
            if (!DateTime.TryParseExact(txtDate.Text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out moveDate))
            {
                AlertHelper.ShowSweetAlert(this, "Enter a valid date.", "error");
                return;
            }
            if (moveDate.Date > DateTime.Today)
            {
                AlertHelper.ShowSweetAlert(this, "The date cannot be in the future.", "error");
                return;
            }
            int storeId = 0;
            if (isReturn && (!int.TryParse(DDStore.SelectedValue, out storeId) || storeId <= 0))
            {
                AlertHelper.ShowSweetAlert(this, "Select the store the items were received into.", "error");
                return;
            }

            string customerName = DDCustomer.SelectedItem.Text;
            string reference = txtRef.Text.Trim();
            long coId = CurrentUser.CoID;
            string sageWarning = "";

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (!Returnables.Items(_db, coId).Any(x => x.ItemID == itemId))
                {
                    AlertHelper.ShowSweetAlert(this, "That item is no longer on the returnable list.", "error");
                    return;
                }

                if (!isReturn)
                {
                    var it = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == coId && x.ID == itemId);
                    Returnables.AddMovement(_db, coId, customerId, customerName, itemId,
                        it != null ? it.Code : null, it != null ? it.Description : null,
                        qty, "O", moveDate, null, reference, CurrentUser.RoleID, null);
                }
                else
                {
                    // A return cannot exceed what the customer is holding. If they held more from
                    // before tracking started, that is an opening balance, entered first.
                    var bal = Returnables.Balances(_db, coId, customerId).FirstOrDefault(x => x.ItemID == itemId);
                    decimal outstanding = bal != null ? bal.Outstanding : 0;
                    if (qty > outstanding)
                    {
                        AlertHelper.ShowSweetAlert(this,
                            "This customer has " + outstanding.ToString("0.##") + " outstanding, so " + qty.ToString("0.##") +
                            " cannot be returned. If they were holding more before tracking started, add an opening balance first.", "error");
                        return;
                    }

                    // Sage's current average, so the adjustment below does not revalue the item.
                    ApiUrlCall api = new ApiUrlCall();
                    try { await api.LoadOneItem(itemId, CurrentUser); } catch { }

                    var itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == coId && x.ID == itemId);
                    if (itm == null)
                    {
                        AlertHelper.ShowSweetAlert(this, "Item not found.", "error");
                        return;
                    }
                    if (itm.IsLotTracked || itm.IsSerialTracked)
                    {
                        AlertHelper.ShowSweetAlert(this, "Returns are not supported for lot or serial tracked items. Nothing was saved.", "error");
                        return;
                    }

                    // Stock back IN at the store's running average (the item's own average when the
                    // store holds none), so a return never changes what the item is worth.
                    decimal unit = StoreCosting.GetStoreAvgCost(_db, coId, itemId, storeId);
                    if (unit == 0) unit = itm.AverageCost ?? 0;
                    decimal lineVal;
                    decimal newAvg = StoreCosting.ComputeMovement(_db, coId, itemId, storeId, qty, unit * qty, out lineVal);

                    string trnRef = "Returned by " + customerName + (reference.Length > 0 ? " - " + reference : "");
                    if (trnRef.Length > 100) trnRef = trnRef.Substring(0, 100);

                    ItemTransaction t = new ItemTransaction();
                    t.CompanyID = coId;
                    t.DocumentID = 0;
                    t.DocumentType = 1;
                    t.TransactionType = "ADJ";
                    t.ItemID = itemId;
                    t.ItemCode = itm.Code;
                    t.ItemDescription = itm.Description;
                    t.Unit = itm.Unit;
                    t.FromID = 0;
                    t.ToID = storeId;
                    t.LotNumber = null;
                    t.Qty = qty;
                    t.TransactionDate = DateTime.Now;
                    t.ByRoleID = CurrentUser.RoleID;
                    t.PriceExclusive = unit;
                    t.AdditionalCosts = 0;
                    t.TotalUnitPriceExclInclAdd = unit;
                    t.TotalLineValExcl = lineVal;
                    t.StoreAvgCost = newAvg;
                    t.TransactionReference = trnRef;
                    t.ExchRate = 1;
                    // The stock movement and the return record go in together or not at all.
                    using (var tx = _db.Database.BeginTransaction())
                    {
                        _db.ItemTransactions.Add(t);
                        _db.SaveChanges();
                        Returnables.AddMovement(_db, coId, customerId, customerName, itemId, itm.Code, itm.Description,
                            qty, "R", moveDate, storeId, reference, CurrentUser.RoleID, t.TrnID);
                        tx.Commit();
                    }

                    // The invoice took these out of Sage stock; put them back. Same average = no revaluation.
                    if (CurrentUser.UATMode == false)
                    {
                        try
                        {
                            ItemAdjustment iAdj = new ItemAdjustment();
                            iAdj.Date = DateTime.Now;
                            iAdj.ItemID = itemId;
                            iAdj.AverageCost = itm.AverageCost ?? 0;
                            iAdj.Quantity = qty;
                            iAdj.Reason = trnRef;
                            iAdj.Created = DateTime.Now;
                            JObject res = await api.APIPostDocumentAsync("ItemAdjustment", JsonConvert.SerializeObject(iAdj, Formatting.Indented), CurrentUser);
                            if (res == null || !res.ContainsKey("ID"))
                                sageWarning = " The return is saved here, but Sage did not confirm the stock adjustment - check the item in Sage.";
                        }
                        catch (Exception ex)
                        {
                            sageWarning = " The return is saved here, but the Sage stock adjustment failed - check the item in Sage.";
                            new ApiUrlCall().LogErrorToFile($"CoID:{coId} Returnables Sage adjustment failed ItemID:{itemId} Qty:{qty} - {ex.Message}");
                        }
                    }
                }
            }

            txtQty.Text = "";
            txtRef.Text = "";
            LoadAll();
            ShowType();
            if (sageWarning.Length > 0)
                AlertHelper.ShowSweetAlert(this, "Saved." + sageWarning, "warning");
            else
                AlertHelper.ShowSweetAlert(this, isReturn ? "Return saved. Stock has been put back." : "Opening balance saved.", "success");
        }

        protected void lbtnAddItem_Click(object sender, EventArgs e)
        {
            string code = txtItemCode.Text.Trim();
            if (code.Length == 0)
            {
                AlertHelper.ShowSweetAlert(this, "Enter the item code.", "error");
                return;
            }
            long coId = CurrentUser.CoID;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == coId && x.Code == code);
                if (itm == null)
                {
                    AlertHelper.ShowSweetAlert(this, "No item with code " + code.Replace("'", "") + " was found.", "error");
                    return;
                }
                if (itm.IsLotTracked || itm.IsSerialTracked)
                {
                    AlertHelper.ShowSweetAlert(this, "Lot or serial tracked items cannot be made returnable.", "error");
                    return;
                }
                Returnables.AddItem(_db, coId, itm.ID);
            }
            txtItemCode.Text = "";
            LoadAll();
            AlertHelper.ShowSweetAlert(this, code + " is now a returnable item. Deliveries are counted from today.", "success");
        }

        protected void GridItems_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "RemoveItem") return;
            long itemId;
            if (!long.TryParse(Convert.ToString(e.CommandArgument), out itemId)) return;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                Returnables.RemoveItem(_db, CurrentUser.CoID, itemId);
            }
            LoadAll();
        }

        protected void lbtnDash_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnStockControl_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/StockControl.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }
    }
}
