using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Utilities.Collections;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class Transfer : BasePage
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
            if (CurrentUser.UsePickSlipTracking != true) ibtnPickTrack.Style.Add("display", "none");
            lblUsername.Text = $":.. {CurrentUser.UserName} ..:";
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
                showhidebuttons();
                loadstores();
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

        private void loadstores()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive==true && x.StoreCode != "CoD" && x.StoreCode != "CoR" && x.StoreCode.ToLower() != "scr").OrderBy(x=>x.StoreDescript).ToList();
                DDStoreFrom.DataSource = Stores;
                DDStoreFrom.DataTextField = "StoreDescript";
                DDStoreFrom.DataValueField = "StoreCode";
                DDStoreFrom.DataBind();
                DDStoreFrom.Items.Insert(0, "-Select-");
            }
        }

        protected void DDStoreFrom_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDStoreFrom.SelectedIndex > 0)
            {
                GridFromItems.DataSource = "";
                GridFromItems.DataBind();

                GridToItems.DataSource = "";
                GridToItems.DataBind();

                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var Stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true & x.StoreCode != DDStoreFrom.SelectedValue && x.StoreCode != "CoD" && x.StoreCode != "CoR" && x.StoreCode.ToLower() != "scr").OrderBy(x => x.StoreDescript).ToList();
                    DDStoreTo.DataSource = Stores;
                    DDStoreTo.DataTextField = "StoreDescript";
                    DDStoreTo.DataValueField = "StoreCode";
                    DDStoreTo.DataBind();
                    DDStoreTo.Items.Insert(0, "-Select-");      
                }
            }
            lblerr.Text = "Please Select TO Store -->";
        }

        private void LoadOpeningBalances()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string storeCode = DDStoreFrom.SelectedValue.ToString();
                string findstr = txtFindFrom.Text.ToLower().Trim();

                if (CurrentUser.CompanyUseLotNumbers)
                {
                    // Use SP as-is with LotNumber
                    var query = _db.GetOpeningBalancesAllStores(CurrentUser.CoID)
                        .Where(x => x.StoreCode == storeCode &&
                                   (x.ItemDescription.ToLower().Contains(findstr) ||
                                    x.ItemCode.ToLower().Contains(findstr)))
                        .ToList();

                    var Qry2 = query.OrderBy(x => x.ItemCode).ToList();
                    foreach (var itm in Qry2)
                    {
                        if (string.IsNullOrEmpty(itm.LotNumber))
                            itm.LotNumber = "---Select---";

                        if (itm.QOH != null)
                            itm.QOH = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(itm.QOH.ToString(), CurrentUser.CompanyDecPlaces));
                    }

                    GridFromItems.DataSource = Qry2.ToList() ;
                    GridFromItems.DataBind();
                }
                else
                {
                    // Ignore LotNumber: group by Item/Store and sum QOH, pick latest price fields
                    var query = _db.GetOpeningBalancesAllStores(CurrentUser.CoID)
                        .Where(x => x.StoreCode == storeCode &&
                                   (x.ItemDescription.ToLower().Contains(findstr) ||
                                    x.ItemCode.ToLower().Contains(findstr)))
                        .GroupBy(x => new { x.ItemID, x.ItemCode, x.ItemDescription, x.StoreID, x.StoreCode })
                        .Select(g =>
                        {
                            var latest = g.OrderByDescending(x => x.TransID).First();
                            return new
                            {
                                latest.ItemID,
                                latest.ItemCode,
                                latest.ItemDescription,
                                latest.StoreID,
                                latest.StoreCode,
                                QOH = g.Sum(x => x.QOH),
                                TotalOnHand = latest.TotalOnHand,
                                PriceExclusive = latest.PriceExclusive,
                                TotalUnitPriceExclInclAdd = latest.TotalUnitPriceExclInclAdd,
                                Unit = latest.Unit,
                                TransID = latest.TransID,
                                LotNumber = "---Select---"   // no lot numbers
                            };
                        })
                        .ToList();

                    GridFromItems.DataSource = query.OrderBy(x => x.ItemCode).ToList();
                    GridFromItems.DataBind();
                }
            }
        }

        private void LoadToBalances()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string storeCode = DDStoreTo.SelectedValue.ToString();

                if (CurrentUser.CompanyUseLotNumbers)
                {
                    // Use SP as-is with LotNumber
                    var query = _db.GetOpeningBalancesAllStores(CurrentUser.CoID)
                        .Where(x => x.StoreCode == storeCode)
                        .ToList();

                    var Qry2 = query.OrderBy(x => x.ItemCode).ToList();

                    foreach (var itm in Qry2)
                    {
                        if (string.IsNullOrEmpty(itm.LotNumber))
                            itm.LotNumber = "---Select---";

                        if (itm.QOH != null)
                            itm.QOH = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(itm.QOH.ToString(), CurrentUser.CompanyDecPlaces));
                    }

                    GridToItems.DataSource = Qry2.ToList();
                    GridToItems.DataBind();
                }
                else
                {
                    // Ignore LotNumber: group by Item/Store and sum QOH, pick latest price fields
                    var query = _db.GetOpeningBalancesAllStores(CurrentUser.CoID)
                        .Where(x => x.StoreCode == storeCode)
                        .GroupBy(x => new { x.ItemID, x.ItemCode, x.ItemDescription, x.StoreID, x.StoreCode })
                        .Select(g =>
                        {
                            var latest = g.OrderByDescending(x => x.TransID).First();
                            return new
                            {
                                latest.ItemID,
                                latest.ItemCode,
                                latest.ItemDescription,
                                latest.StoreID,
                                latest.StoreCode,
                                QOH = g.Sum(x => x.QOH),
                                TotalOnHand = latest.TotalOnHand,
                                PriceExclusive = latest.PriceExclusive,
                                TotalUnitPriceExclInclAdd = latest.TotalUnitPriceExclInclAdd,
                                Unit = latest.Unit,
                                TransID = latest.TransID,
                                LotNumber = "---Select---"   // no lot numbers
                            };
                        })
                        .ToList();

                    GridToItems.DataSource = query.OrderBy(x => x.ItemCode).ToList();
                    GridToItems.DataBind();
                }
            }
        }

        protected void GridFromItems_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
        }

         protected async void btnApprovYes_Click(object sender, EventArgs e)
        {
            lblerr.Text = "";
            decimal trfQty = 0; 
            long TranID = Convert.ToInt64(TransID.Text);
            try
            {
                trfQty = Convert.ToDecimal(txtQtyToTrf.Text);
            }
            catch { }

            decimal AvailQty = 0;
            try
            {
                AvailQty = Convert.ToDecimal(lblMax.Text);
            }
            catch { }

            if (trfQty > AvailQty)
            { 
                AlertHelper.ShowSweetAlert(this, "Insufficient stock available for this item.", "error");
                return;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {  
                // Record for Receiving store
                var trfitem = _db.ItemTransactions.Where(it => it.TrnID == TranID).FirstOrDefault(); 
                ItemTransaction ItemTrans = new ItemTransaction();
                ItemTrans.CompanyID = trfitem.CompanyID;
                ItemTrans.DocumentID = 0;
                ItemTrans.TransactionType = "TRF";
                ItemTrans.ItemID = trfitem.ItemID;
                ItemTrans.ItemCode = trfitem.ItemCode;
                ItemTrans.ItemDescription = trfitem.ItemDescription;
                ItemTrans.LotNumber = trfitem.LotNumber ?? null;
                ItemTrans.Unit = trfitem.Unit;
                long fromId = getstoreid(lblFromStore.Text);
                long toId = getstoreid(lblToStore.Text);
                ItemTrans.FromID = fromId;
                ItemTrans.ToID = toId;
                ItemTrans.Qty = trfQty;
                ItemTrans.PriceInclusive = trfitem.PriceInclusive;
                ItemTrans.TransactionDate = DateTime.Now;
                ItemTrans.DocumentType = 4;
                ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid

                decimal AddCosts = 0;
                try { AddCosts = Convert.ToDecimal(txtTrfAddCosts.Text); } catch { }
                decimal freightPerUnit = AddCosts > 0 ? AddCosts / trfQty : 0m;

                // IN leg (receiving store): arrives at source store average + freight per unit,
                // and the destination's running weighted average re-blends against stock already there.
                decimal srcAvg = StoreCosting.GetStoreAvgCost(_db, CurrentUser.CoID, (long)trfitem.ItemID, fromId);
                decimal unitLanded = srcAvg + freightPerUnit;
                decimal lineVal;
                decimal destAvg = StoreCosting.ComputeMovement(_db, CurrentUser.CoID, (long)trfitem.ItemID, toId, trfQty, unitLanded * trfQty, out lineVal);
                ItemTrans.PriceExclusive = srcAvg;
                ItemTrans.AdditionalCosts = freightPerUnit;
                ItemTrans.TotalUnitPriceExclInclAdd = unitLanded;
                ItemTrans.TotalLineValExcl = lineVal;
                ItemTrans.StoreAvgCost = destAvg;
                ItemTrans.TransactionReference = trfitem.ItemCode + " Quick Trf " + trfQty + " in from " + lblFromStore.Text;
                ItemTrans.ExchRate = 1;

                _db.ItemTransactions.Add(ItemTrans);

                // check for itemstore link
                long itmID = Convert.ToInt64(ItemTrans.ItemID);
                var ItS = _db.ItemStoreLinkMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreID == ItemTrans.ToID && x.ItemID == itmID).FirstOrDefault();
                if (ItS == null)
                {
                    // create item/store link
                    ItemStoreLinkMaster isL = new ItemStoreLinkMaster
                    {
                        CompanyID = CurrentUser.CoID,
                        ItemID = itmID,
                        StoreID = (int?)ItemTrans.ToID,
                        Active = true,
                    };
                    _db.ItemStoreLinkMasters.Add(isL);
                }

                //// Record for Issuing store
                ItemTransaction ItemTransOUT = new ItemTransaction();
                ItemTransOUT.CompanyID = trfitem.CompanyID;
                ItemTransOUT.DocumentID = 0;
                ItemTransOUT.TransactionType = "TRF";
                ItemTransOUT.ItemID = trfitem.ItemID;
                ItemTransOUT.ItemCode = trfitem.ItemCode;
                ItemTransOUT.ItemDescription = trfitem.ItemDescription;
                ItemTransOUT.LotNumber = trfitem.LotNumber ?? null;
                ItemTransOUT.Unit = trfitem.Unit;
                ItemTransOUT.ToID = fromId;
                ItemTransOUT.FromID = toId;
                ItemTransOUT.Qty = trfQty * -1;
                ItemTransOUT.DocumentType = 4;
                ItemTransOUT.TransactionDate = DateTime.Now;
                ItemTransOUT.ByRoleID = CurrentUser.RoleID; // roleid

                // OUT leg (issuing store): leaves at the source store's running average, unchanged.
                ItemTransOUT.AdditionalCosts = 0;
                ItemTransOUT.PriceExclusive = srcAvg;
                ItemTransOUT.TotalUnitPriceExclInclAdd = srcAvg;
                ItemTransOUT.TotalLineValExcl = srcAvg * (trfQty * -1);
                ItemTransOUT.StoreAvgCost = srcAvg;
                ItemTransOUT.TransactionReference = trfitem.ItemCode + " Quick Trf " + trfQty + " out to " + lblToStore.Text;
                ItemTransOUT.ExchRate = 1;

                _db.ItemTransactions.Add(ItemTransOUT);
                _db.SaveChanges();

                string itemtransnum = ItemTrans.TrnID.ToString();
                #region updateAveragePriceInSage
                if (chkSageUpdate.Checked)
                {
                    #region AdjustItemOut
                    ItemAdjustment iAdj = new ItemAdjustment();
                    iAdj.Date = DateTime.Now;
                    iAdj.ItemID = (long) trfitem.ItemID;
                    var Itm = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == trfitem.ItemID).FirstOrDefault();
                    decimal TotQtyOnHand = (decimal) Itm.QuantityOnHand;
                    iAdj.AverageCost = (decimal)Itm.AverageCost;
                    iAdj.Quantity = (decimal)trfQty * -1;
                    iAdj.Reason = "ADJ Out Trf ID: " + itemtransnum + " - " + txtAddCostsReason.Text.ToString();
                    iAdj.Created = DateTime.Now;
                    string jsonBody = JsonConvert.SerializeObject(iAdj, Formatting.Indented);
                    if (CurrentUser.UATMode == false)
                    {
                        await SendItemAdjustment(jsonBody);
                    }
                        #endregion
                    // --------------------------------------------------
                        #region AdjustItemIn
                        // adjust items back in at new price including additional costs
                        iAdj = new ItemAdjustment();
                        iAdj.Date = DateTime.Now;
                        iAdj.ItemID = (long)trfitem.ItemID;
                    
                    decimal OldTotVal = TotQtyOnHand * (decimal)Itm.AverageCost;
                    decimal NewTotValue = OldTotVal + AddCosts;
                    decimal NewAvCost = NewTotValue/ TotQtyOnHand;

                    iAdj.AverageCost = NewAvCost;
                    iAdj.Quantity = (decimal)trfQty;
                    iAdj.Reason = "ADJ IN Trf ID: " + itemtransnum + " - " + txtAddCostsReason.Text.ToString();
                    iAdj.Created = DateTime.Now;
                    jsonBody = JsonConvert.SerializeObject(iAdj, Formatting.Indented);
                    if (CurrentUser.UATMode == false)
                    {
                        await SendItemAdjustment(jsonBody);
                    }
                        #endregion
                    #endregion
                }
                txtQtyToTrf.Text = null;
                txtAddCostsReason.Text = null;
                txtTrfAddCosts.Text = null;
                chkSageUpdate.Checked = false;

                lblerr.Text = "Transfer Successful";
                LoadOpeningBalances();
                LoadToBalances();
            }
        }

        public async Task SendItemAdjustment(string Item)
        {
            string doctype = "";
            doctype = "ItemAdjustment";
            ApiUrlCall Api = new ApiUrlCall();
            JObject parsedJSON = await Api.APIPostDocumentAsync(doctype, Item, CurrentUser);
        }

        protected void DDStoreTo_SelectedIndexChanged(object sender, EventArgs e)
        {
            GridFromItems.DataSource = "";
            GridFromItems.DataBind();

            GridToItems.DataSource = "";
            GridToItems.DataBind();
            lblerr.Text = "&nbsp" ;
           LoadOpeningBalances();
            LoadToBalances();
        }

        protected void GridToItems_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
            if (CurrentUser.CompanyUseLotNumbers == false)
            {
                e.Row.Cells[3].Visible = false;
            }
        }

        protected long getstoreid(string stcode)
        {
            long storeid = 0;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var store = _db.Stores.Where(x => x.StoreCode == stcode && x.CompanyID == CurrentUser.CoID).FirstOrDefault();
                storeid = store.StoreID;
            }
            return storeid;
        }

        protected void lbtnfind_Click(object sender, EventArgs e)
        {
            LoadOpeningBalances();
        }

        protected void lbtnTrf_Click(object sender, EventArgs e)
        {

            lblerr.Text = "";
            if (DDStoreTo.SelectedIndex > 0)
            {
                decimal TrfQty = 0;
                LinkButton lbtnItmC = (LinkButton)sender;
                GridViewRow row = (GridViewRow)lbtnItmC.NamingContainer;
                long lineid = Convert.ToInt64(lbtnItmC.CommandArgument);

                lblMax.ForeColor = System.Drawing.Color.Black;
                btnApprovYes.Visible = true;
                try
                {
                    TrfQty = Convert.ToDecimal(row.Cells[5].Text.ToString());
                }
                catch { btnApprovYes.Visible = false; }

                if (TrfQty == 0)
                {
                    btnApprovYes.Visible = false;
                    lblMax.ForeColor = System.Drawing.Color.Red;
                }
                TransID.Text = row.Cells[0].Text.ToString();
                
                string LotNum = lbtnItmC.Text.ToString();
                lblMax.Text = row.Cells[5].Text.ToString();
                lblFromStore.Text = DDStoreFrom.SelectedValue.ToString();
                lblToStore.Text = DDStoreTo.SelectedValue.ToString();
                lblItem.Text = "Transfer " + row.Cells[1].Text.ToString() + ": Lot Num" + LotNum;
                if (lbtnItmC.Text.ToString().Contains("---")) lblItem.Text = "Transfer " + row.Cells[1].Text.ToString();
                Button25_ModalPopupExtender.Show();
            }
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

        protected void imgbTrf_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/Transfer.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
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
    }
}