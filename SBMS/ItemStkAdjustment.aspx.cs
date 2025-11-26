using AjaxControlToolkit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ItemStkAdjustment : BasePage
    {
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            lblerr.Text = "";
            if (!IsPostBack)
            {
                if (CurrentUser.UsePickSlipTracking != true) ibtnPickTrack.Style.Add("display", "none");

                lblUsername.Text = $":.. {CurrentUser.UserName}..:  ";

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

                showhidebuttons();
                LoadItems();
                if (CurrentUser.CompanyUseLotNumbers == false)
                {
                    DDLotNum.Enabled = false;
                }
            }
        }

        private void showhidebuttons()
        {
            if (CurrentUser.CanReceive != true) ibtmWorksOrders.Style.Add("display", "none");
            if (CurrentUser.CanViewPickSlips != true) ibtnPickSlips.Style.Add("display", "none");
            if (CurrentUser.CanTrackPickSlips != true) ibtnPickTrack.Style.Add("display", "none");
            if (CurrentUser.CanStockControl != true) ibtnStckCtl.Style.Add("display", "none");
            if (CurrentUser.UseModule2 == true)
            {
                if (CurrentUser.CanSalesForecast != true) ibtnFCasts.Style.Add("display", "none");
                if (CurrentUser.CanSeeFGDemands != true) ibtnmrp.Style.Add("display", "none");
                if (CurrentUser.CanTrackJobCards != true) ibtnJobTrack.Style.Add("display", "none");
            }
            else
            {
                ibtnFCasts.Style.Add("display", "none");
                ibtnmrp.Style.Add("display", "none");
                ibtnJobTrack.Style.Add("display", "none");
            }
            if (CurrentUser.UseModule3 == true)
            {
                if (CurrentUser.CanViewWorksOrders != true) ibtmWorksOrders.Style.Add("display", "none");
                if (CurrentUser.CanFillWorksOrders != true) ibtmWOrdMgment.Style.Add("display", "none");
                if (CurrentUser.CanViewRMD != true) ibtnRMD.Style.Add("display", "none");
            }
            else
            {
                ibtmWorksOrders.Style.Add("display", "none");
                ibtmWOrdMgment.Style.Add("display", "none");
                ibtnRMD.Style.Add("display", "none");
            }
        }
        private void LoadItems()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var items = _db.ItemsMasters
                 .Where(x => x.CompanyID == CurrentUser.CoID && x.Active == true)
                 .AsEnumerable()
                 .Select(x => new
                 {
                     id = x.ID.ToString() + "|" + x.Code,
                     text = x.Code + " - " + x.Description
                 })
                 .OrderBy(x => x.text)
                 .ToList();

                // Store as JSON for client-side filtering
                var serializer = new JavaScriptSerializer();
                hdnAllItems.Value = serializer.Serialize(items);

                // Optionally keep the original data binding for fallback
                DDItemList.DataSource = items;
                DDItemList.DataTextField = "text";
                DDItemList.DataValueField = "id";
                DDItemList.DataBind();
                DDItemList.Items.Insert(0, new ListItem("- Select -", ""));
            }
        }
        //private void LoadItems()
        //{
        //    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //    {
        //        var items = _db.ItemsMasters
        //         .Where(x => x.CompanyID == CurrentUser.CoID && x.Active == true)
        //         .AsEnumerable() // Switch to LINQ to Objects
        //         .Select(x => new ItemList
        //         {
        //             itemID = x.ID.ToString() + "|" + x.Code,
        //             ItemDescript = x.Code + " - " + x.Description
        //         })
        //         .OrderBy(x => x.ItemDescript)
        //         .ToList();
        //        DDItemList.DataSource = items;
        //        DDItemList.DataTextField = "ItemDescript";
        //        DDItemList.DataValueField = "itemID";
        //        DDItemList.DataBind();
        //        DDItemList.Items.Insert(0, "- Select -");
        //    }
        //}

        public class ItemList
        {
            public string itemID { get; set; }
            public string ItemDescript { get; set; }
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

        protected async void lbtnSave_Click(object sender, EventArgs e)
        {
            string message = "";
            if (DDItemList.SelectedIndex == 0 || DDInOut.SelectedIndex == 0 || DDStore.SelectedIndex == 0 || DDAdjYesNo.SelectedIndex == 0)
            {
                message = "One or more dropdown selectors have not been selected, unable to continue";
                AlertHelper.ShowSweetAlert(this, message, "error");
                return;
            }
            decimal AdjQty;
            try
            {
                AdjQty = Convert.ToDecimal(txtAdjQty.Text);
            }
            catch
            {
                message = "Invalid Quantity, unable to continue";
                AlertHelper.ShowSweetAlert(this, message, "error");
                return;
            }

            if (AdjQty == 0)
            {
                message = "Invalid Quantity, unable to continue";
                AlertHelper.ShowSweetAlert(this, message, "error");
                return;
            }

            if (txtAdjReason.Text.ToString().Trim().Replace("'", "''").Length == 0)
            {
                message = "Please enter a reason for the adjustment";
                AlertHelper.ShowSweetAlert(this, message, "error");
                return;
            }

            decimal ThisAvCost;
            try
            {
                ThisAvCost = Convert.ToDecimal(txtAvCost.Text);
            }
            catch (Exception ex)
            {
                message = "Invalid Average Cost - Please enter a valid value: \" + ex.Message";
                AlertHelper.ShowSweetAlert(this, message, "error");
                return;
            }


            long itmid = Convert.ToInt64(DDItemList.SelectedValue.ToString().Split('|')[0].ToString());
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // get current bal on hand in selected store of selected item - to calc total after adjustment
                decimal QOH = 0; string LotNum; decimal UnitC = 0;
                if (CurrentUser.CompanyUseLotNumbers == true)
                {
                    if (DDLotNum.Items.Count > 1)
                    {
                        if (DDLotNum.SelectedIndex ==0)
                        {
                            // create new lot number and link it to the store
                            lblLotNum.Text = string.Empty;
                            Button25_ModalPopupExtender.Show();
                            return;
                        }
                        else
                        {
                            LotNum = DDLotNum.Text;
                        }
                    }
                    else
                    {
                        LotNum = null;
                    }
                } else
                {
                    LotNum = null;
                }

                // get store id
               long storeid = _db.Stores.Where(x => x.StoreCode == DDStore.Text && x.CompanyID == CurrentUser.CoID).Select(x => x.StoreID).FirstOrDefault();

                var result = _db.ItemTransactions
                .Where(item => item.ItemID == itmid && item.CompanyID == CurrentUser.CoID && item.ToID == storeid)
                .OrderByDescending(item => item.TrnID);

                if (DDInOut.SelectedValue.ToString() == "1")
                {
                    if (result.Sum(x=>x.Qty)  < Convert.ToDecimal(txtAdjQty.Text))
                    {
                        message = $"Only {result.Sum(x => x.Qty).ToString()} units available to adjust. You cannot adjust {txtAdjQty.Text} units down. Unable to Continue";
                        AlertHelper.ShowSweetAlert(this, message, "error");
                        return;
                    }
                }

                if (result != null) 
                {
                    if (result.Sum(x => x.Qty) != 0)
                        try
                        {
                            QOH = (decimal)result.Sum(x => x.Qty);
                            var lastTrn = result.OrderByDescending(x => x.TrnID).FirstOrDefault();
                            if (lastTrn.TotalUnitPriceExclInclAdd != 0) UnitC = (decimal)lastTrn.TotalUnitPriceExclInclAdd;
                        }
                        catch
                        {
                            QOH = 0;
                            UnitC = 0;
                        }
                }
                // update from Sage
                
                 

                ApiUrlCall api = new ApiUrlCall();
                await api.LoadOneItem(itmid, CurrentUser);

                var itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == itmid);

                
                // calculate new average cost
                decimal CurrVal = (decimal)itm.QuantityOnHand * (decimal)itm.AverageCost;
                decimal LotMoveVal = ThisAvCost * Convert.ToDecimal(txtAdjQty.Text);
                decimal NewStckTotVal = CurrVal + LotMoveVal;
                decimal NewStckQty = Convert.ToDecimal(txtAdjQty.Text) + QOH;
                decimal NewAvCost = NewStckTotVal / NewStckQty;
                if (DDInOut.SelectedIndex == 2)
                {
                    NewStckTotVal = CurrVal - LotMoveVal;
                    NewStckQty = QOH - Convert.ToDecimal(txtAdjQty.Text);
                    if (NewStckQty != 0)
                    {
                        NewAvCost = NewStckTotVal / NewStckQty;
                    }
                    else
                    {
                        NewAvCost = 0; // Avoid division by zero if all stock is removed
                    }
                }

                ItemTransaction ItemTrans = new ItemTransaction();
                ItemTrans.CompanyID = CurrentUser.CoID;
                ItemTrans.DocumentID = 0;
                ItemTrans.TransactionType = "ADJ";
                ItemTrans.ItemID = Convert.ToInt64(itm.ID);
                ItemTrans.ItemCode = itm.Code;
                ItemTrans.ItemDescription = itm.Description;
                ItemTrans.Unit = itm.Unit;
                ItemTrans.FromID = 0;
                ItemTrans.LotNumber = LotNum;
                ItemTrans.ToID = storeid;
                ItemTrans.Qty = Convert.ToDecimal(txtAdjQty.Text);
                if (DDInOut.SelectedIndex == 2) ItemTrans.Qty = (Convert.ToDecimal(txtAdjQty.Text) * -1);
                ItemTrans.DocumentType = 1;
                ItemTrans.TransactionDate = DateTime.Now;
                ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid
                ItemTrans.PriceExclusive = ThisAvCost;
                ItemTrans.AdditionalCosts = 0;
                ItemTrans.TotalUnitPriceExclInclAdd = ThisAvCost;
                ItemTrans.TotalLineValExcl = LotMoveVal;
                ItemTrans.TransactionReference = txtAdjReason.Text.ToString().Trim().Replace("'", "''");
                ItemTrans.ExchRate = 1; 
                _db.ItemTransactions.Add(ItemTrans);
                try
                {
                    _db.SaveChanges();
                } catch (Exception ex)
                {
                    string str = ex.Message;
                }
               

                if (DDAdjYesNo.SelectedValue == "0")
                {
                    // item adjustment in SBCA
                    ItemAdjustment iAdj = new ItemAdjustment();
                    iAdj.Date = DateTime.Now;
                    iAdj.ItemID = itmid;
                    iAdj.AverageCost = (decimal)NewAvCost;
                    iAdj.Quantity = (decimal)ItemTrans.Qty;
                    iAdj.Reason = "Adjustment: " + DateTime.Today + " - " + txtAdjReason.Text.ToString();
                    iAdj.Created = DateTime.Now;
                    string jsonBody = JsonConvert.SerializeObject(iAdj, Formatting.Indented);
                    if (CurrentUser.UATMode == false)
                    {
                        await SendItemAdjustment(jsonBody);
                    }
                }
            }
            DDItemList.SelectedIndex = 0;
            DDLotNum.Items.Clear();
            txtAdjQty.Text = "1";
            DDInOut.SelectedIndex = 0;
            DDStore.SelectedIndex = 0;
            txtAdjReason.Text = string.Empty;
            message = "Adjustment Complete";
            AlertHelper.ShowSweetAlert(this, message, "success");
        }

        public async Task SendItemAdjustment(string Item)
        {
            string doctype = "";
            doctype = "ItemAdjustment";
            ApiUrlCall Api = new ApiUrlCall();
            JObject parsedJSON = await Api.APIPostDocumentAsync(doctype, Item, CurrentUser);
        }
        protected void DDItemList_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (DDItemList.SelectedIndex > 0)
            {
                long ItmID = Convert.ToInt64(DDItemList.SelectedValue.ToString().Split('|')[0].ToString());

                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var stores = _db.GetLinkedStoredFromItem(CurrentUser.CoID).Where(x => x.ItemID == ItmID && x.StoreCode != "CoR" && x.StoreCode != "CoD").OrderBy(x => x.StoreCode).ToList();
                    DDStore.DataSource = stores;
                    DDStore.DataTextField = "StoreCode";
                    DDStore.DataValueField = "StoreCode";
                    DDStore.DataBind();
                    DDStore.Items.Insert(0, "- Select -");

                    if (CurrentUser.CompanyUseLotNumbers == true)
                    {
                        var LotNums = _db.LotTrackingMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemId == ItmID && x.LotActive == true).GroupBy(x => x.LotNumber).Select(g => g.Key).ToList();
                        DDLotNum.DataSource = LotNums;
                        DDLotNum.DataBind();
                        DDLotNum.Items.Insert(0, "- Select -");
                    }
                    else
                    {
                        DDLotNum.Items.Clear();
                        DDLotNum.Items.Insert(0, "- N/A -");
                        DDLotNum.Enabled = false;
                    }
                    var itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == ItmID);
                    if (itm != null)
                    {
                        decimal AvCst = (decimal) itm.AverageCost;
                        txtAvCost.Text = ApiUrlCall.NumberToDecimal(AvCst, CurrentUser.CompanyDecPlaces).ToString();
                    }
                    else txtAvCost.Text = "0";
                }
            }
        }

         protected void imgbRec_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/OSPurchaseOrders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnPickSlips_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/OSSalesOrders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnPickTrack_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/PickingSlipTracking.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnStckCtl_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/StockControl.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void lbtnSOH_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/StockEnquiry.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnFCasts_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/ForeCastHeaders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnmrp_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/FGDemands.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnJobTrack_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/JobTracking.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtmWorksOrders_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/WorksOrdersHeaders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtmWOrdMgment_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/WorksOrdersManfHeaders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnRMD_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/ProductionRMD.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }
        protected void imgdash_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
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
        protected void lbtnNewLot_Click(object sender, EventArgs e)
        {
            if (DDStore.SelectedIndex > 0)
            {
                int recnum = GetLotNum(CurrentUser.CoID);
                lblLotNum.Text = DateTime.Today.ToString("ddMMyyyy") + DDStore.SelectedValue.ToString() + recnum.ToString();
                Button25_ModalPopupExtender.Show();
            }
            else
            {
                string message = "Please select a store before creating a new lot number";
                AlertHelper.ShowSweetAlert(this, message, "error");
                return;
            }
        }

        protected void btnAddNewLot_Click(object sender, EventArgs e)
        {
            if (DDItemList.SelectedIndex == -1)
            {
                string message = "Please select an Item before creating a new lot number";
                AlertHelper.ShowSweetAlert(this, message, "error");
                return;
            }
            if (DDStore.SelectedIndex == -1)
            {
                string message = "Please select a store before creating a new lot number";
                AlertHelper.ShowSweetAlert(this, message, "error");
                return;
            }
            LotTrackingMaster LtNew = new LotTrackingMaster();
            LtNew.LotNumber = lblLotNum.Text;
            LtNew.CreatedDate = DateTime.Now;
            LtNew.CompanyID = CurrentUser.CoID;
            LtNew.ItemCode = DDItemList.SelectedItem.Value.ToString().Split('|')[1].ToString();
            LtNew.ItemId = Convert.ToInt64(DDItemList.SelectedItem.Value.ToString().Split('|')[0].ToString());
            LtNew.LotActive = true;
            LtNew.LotTotUnitPrice = 0;
            LtNew.LotQuantity = 1;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                _db.LotTrackingMasters.Add(LtNew);
                try
                {
                    _db.SaveChanges();
                } catch (Exception ex)
                {
                    string str = "";
                }
             
                var LotNums = _db.LotTrackingMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemId == LtNew.ItemId && x.LotActive == true).GroupBy(x => x.LotNumber).Select(g => g.Key).ToList();
                DDLotNum.DataSource = LotNums;
                DDLotNum.DataBind();
                DDLotNum.Items.Insert(0, "- Select -");
                DDLotNum.SelectedValue = lblLotNum.Text;
            }
        }

        protected void lblLotNum_TextChanged(object sender, EventArgs e)
        {

            string newLotNumber = lblLotNum.Text;
            string originalLotNumber = hfOriginalLotNumber.Value;

            if (string.IsNullOrEmpty(originalLotNumber))
            {
                // New record: Check if the lot number already exists in the database
                if (CheckLotNumberExists(newLotNumber))
                {
                    // Lot number exists, show an error message
                    LotNumCheck.Text = "Lot number already exists. Record not updated";
                }
            }
            else
            {
                // Editing an existing record
                if (!newLotNumber.Equals(originalLotNumber, StringComparison.OrdinalIgnoreCase))
                {
                    // Lot number has changed: Check if the new lot number already exists in the database
                    if (CheckLotNumberExists(newLotNumber))
                    {
                        // Lot number exists, show an error message
                        LotNumCheck.Text = "Lot number already exists. Record not updated";
                    }
                }
            }
        }
        private bool CheckLotNumberExists(string lotnumber)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                return _db.LotTrackingMasters.Any(l => l.LotNumber == lotnumber && l.CompanyID == CurrentUser.CoID);
            }
        }
    }
}
