using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web.UI;
using static SBMS.TransferSelect;

namespace SBMS
{
    public partial class StockMovement : BasePage
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
                loadstores();
                LoadItems();
                if (CurrentUser.CompanyUseLotNumbers == false)
                {
                    PnlLotNum.Attributes.Add("style", "display:none");
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
        protected void LoadItems()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var itmList = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.Active == true).OrderBy(g=>g.Code).Select(d => new ItmList
                {
                    ID = d.ID,
                    Description = d.Code + " - " + d.Description
                }).ToList();
                dditem.DataSource = itmList;
                dditem.DataTextField = "Description";
                dditem.DataValueField = "ID";
                dditem.DataBind();
                // Give the placeholder an explicit "0" value so the searchable picker treats it
                // as a placeholder and keeps it out of the results. Every selection test on this
                // page uses SelectedIndex, so the added value changes nothing else.
                dditem.Items.Insert(0, new System.Web.UI.WebControls.ListItem("-Select Item-", "0"));
            }
        }

        private class ItmList
        {
            public long ID { get; set; }
            public string Description { get; set; }
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
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

        protected void dditem_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CurrentUser.CompanyUseLotNumbers == true)
            {
                // Selection cleared: drop the lot list and the results with it, otherwise the
                // previous item's lot numbers stay on screen against no item.
                if (dditem.SelectedIndex == 0)
                {
                    ddLotNum.Items.Clear();
                    lbtnsearch_Click(sender, e);
                    return;
                }

                if (dditem.SelectedIndex > 0)
                {
                    long itmID = Convert.ToInt64(dditem.SelectedItem.Value);
                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var LotNumList = _db.ItemTransactions
                          .Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == itmID)
                          .GroupBy(x => x.LotNumber)
                          .Select(g => g.OrderBy(x => x.TransactionDate).FirstOrDefault())
                          .Where(x => x != null && x.LotNumber != null) // Filter out nulls
                          .Select(x => x.LotNumber)
                          .ToList();
                        

                        
                        ddLotNum.DataSource = LotNumList;
                        ddLotNum.DataBind();
                        ddLotNum.Items.Insert(0, "-Select Lot Number-");
                    }
                }
            }
            else
            {
                lbtnsearch_Click(sender, e);
            }
        }

        protected void lbtnsearch_Click(object sender, EventArgs e)
        {
            if (dditem.SelectedIndex == 0)
            {
                GridItemTrans.DataSource = "";
                GridItemTrans.DataBind();
                return;
            }
            decimal TotQty = 0, totVal = 0;
            List<TransLine> TLL = GetTransactions();
            if (ddLotNum.Items.Count > 0 && ddLotNum.SelectedIndex > 0)
            {
                string lotN = ddLotNum.Text;
                TLL = TLL.Where(x=>x.LotNumber == lotN).ToList();
            }
            if (TLL.Count > 0)
            {
                if (ddStore.SelectedIndex > 0)
                {
                    string storeCde = ddStore.SelectedItem.Text;
                    TLL = TLL.Where(x => x.Store == storeCde).ToList();
                }
                foreach (var Trn in TLL)
                {
                    Trn.Qty = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(Trn.Qty.ToString(), CurrentUser.CompanyDecPlaces));
                    TotQty += (decimal)Trn.Qty;
                    totVal += (decimal)Trn.TotalLineValExcl;
                }    
                GridItemTrans.DataSource = TLL.ToList();
                GridItemTrans.DataBind();
                if (TLL.Count > 0)
                {
                    GridItemTrans.FooterRow.Cells[0].Text = "Totals";
                    GridItemTrans.FooterRow.Cells[4].Text = TotQty.ToString("N2");
                    GridItemTrans.FooterRow.Cells[7].Text = totVal.ToString("N2");
                }         
            } else
            {
                TLL = new List<TransLine>();
                GridItemTrans.DataSource = TLL.ToList();
                GridItemTrans.DataBind();
                ShowMessage(sender, EventArgs.Empty, "No records available");
            }
        }

        protected void lbtnDownload_Click(object sender, EventArgs e)
        {    
            if (dditem.SelectedIndex == 0)
            {
                return;
            }
            DataTable planLinesDataTable = DataTableHelper.ConvertToDataTable(GetTransactions());
            ExcelHelper.ExportToExcel(planLinesDataTable, "Item_Lot_Movement", "Item_Lot_Movement");
        }

        public List<TransLine> GetTransactions()
        {
            decimal TotQty = 0, totVal = 0;
            long itmID = Convert.ToInt64(dditem.SelectedItem.Value);
            List<TransLine> TLL = new List<TransLine>();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                 int recordsToTake = int.Parse(ddRecCount.SelectedValue); // Parse the selected value as an integer
                var TransList = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == itmID).OrderBy(x => x.TransactionDate).Take(recordsToTake).ToList();
                foreach (var Trn in TransList)
                {
                    TransLine TL = new TransLine();
                    TL.Code = Trn.ItemCode;
                    TL.TransactionType = Trn.TransactionType;
                    TL.ExchRate = (decimal)Trn.ExchRate;
                    if (Trn.DocumentID.ToString() != "0" && Trn.DocumentID != null)
                    {
                        long Docid = (long)Trn.DocumentID;
                        if (Trn.TransactionType.Contains("JC"))
                        {
                            try
                            {
                                TL.Document = _db.JobCardsMasters.FirstOrDefault(x => x.CustomerID == CurrentUser.CoID && x.JCID == Trn.DocumentID).JCNumber;
                            } catch{ }
                        }
                        else
                        if (Trn.TransactionType == "PS")
                        {
                            try
                            {
                                TL.Document = _db.PickingSlipMasters.FirstOrDefault(x => x.CustomerID == CurrentUser.CoID && x.PSID == Trn.DocumentID).PSIntNumber;
                            }
                            catch { }
                        }
                        else
                        {
                            try
                            {
                                TL.Document = _db.DocHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.DocID == Trn.DocumentID).DocumentNumber;
                            }
                            catch { }
                        }
                    }
                    TL.ItemDescription = Trn.ItemDescription;
                    TL.LotNumber = Trn.LotNumber;

                   TL.Qty = (decimal)Trn.Qty;
                   
                    if (Trn.PriceExclusive != null)TL.PriceExclusive = (decimal)Trn.PriceExclusive;
                    if (Trn.AdditionalCosts != null) TL.AdditionalCosts = (decimal)Trn.AdditionalCosts;
                   if (Trn.TotalLineValExcl != null)  TL.TotalLineValExcl = (decimal)Trn.TotalLineValExcl;
                    if (Trn.ToID == 0)
                    {
                        TL.Store = "-";
                    }
                    else
                    {
                        try
                        {
                            TL.Store = _db.Stores.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StoreID == Trn.ToID).StoreCode ?? "";
                        }
                        catch { TL.Store = "?"; }  
                    }  
                    if (Trn.TransactionDate != null) TL.TransactionDate = (DateTime)Trn.TransactionDate;
                    var RoleBy = _db.RolesMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.RoleID == Trn.ByRoleID);
                    if (RoleBy != null) TL.ByRole = RoleBy.RoleName.ToString();
                    
                    TL.TransactionReference = Trn.TransactionReference ?? "".ToString();
                    TLL.Add(TL);
                    TotQty += (decimal)Trn.Qty;
                    if (Trn.TotalLineValExcl != null) totVal += (decimal)Trn.TotalLineValExcl;
                }
            }
            return TLL;
        }
        public class TransLine
        {
            public string Code { get; set; }
            public string TransactionType { get; set; }
            public string Document { get; set; }
           public string ItemDescription { get; set; }
           public string LotNumber { get; set; }
            public decimal Qty { get; set; }
            public decimal PriceExclusive { get; set; }
            public decimal AdditionalCosts { get; set; }
            public decimal TotalLineValExcl { get; set; }
            public decimal ExchRate { get; set; }
            public decimal LocalCurrValue { get; set; }
            public string Store { get; set; }
            public DateTime TransactionDate { get; set; }
            public string ByRole { get; set; }
            public string TransactionReference { get; set; }
            
        }

        protected void ShowMessage(object sender, EventArgs e, string msg)
        {
            string message = "alert('" + msg + "')";
            ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
        }

        protected void GridItemTrans_RowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e)
        {
            if (CurrentUser.CompanyUseLotNumbers == false)
            {
                e.Row.Cells[3].Visible = false;
            }
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

        protected void imgItemAdjust_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/ItemStkAdjustment.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }

        }

        protected void lbtnStockMove_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/StockMovement.aspx?user=" + CurrentUser.UserGuiD, false);
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

        protected void ddStore_SelectedIndexChanged(object sender, EventArgs e)
        {
            dditem_SelectedIndexChanged(sender, EventArgs.Empty);
        }
        private void loadstores()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true && x.StoreCode != "CoD" && x.StoreCode != "CoR").ToList();
                ddStore.DataSource = Stores;
                ddStore.DataTextField = "StoreCode";
                ddStore.DataValueField = "StoreID";
                ddStore.DataBind();
                ddStore.Items.Insert(0, "-Select Store-");
            }
        }
    }
}