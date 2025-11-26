using iTextSharp.text;
using iTextSharp.text.pdf;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class TransferSlip : BasePage
    {
        int TrfThisID;
       
// DTO for dropdown items
[Serializable]
        public class ItemDropDownDto
        {
            public long CompanyID { get; set; }
            public long ItemID { get; set; }
            public string Item { get; set; }
            public string ItemCode { get; set; }
            public long StoreID { get; set; }
            public decimal QOH { get; set; } // Quantity on hand
            public string LotNum { get; set; }
            public decimal? PriceExclusive { get; set; }
            public decimal? TotalUnitPriceExclInclAdd { get; set; }
            public string Unit { get; set; }
        }
            // Class-level list for dropdown data
            private List<ItemDropDownDto> _itemDropDownList
            {
                get { return ViewState["ItemDropDownList"] as List<ItemDropDownDto>; }
                set { ViewState["ItemDropDownList"] = value; }
            }

        [Serializable]
        public class StoresDto
        {
            public int StoreID { get; set; }
            public string StoreCode { get; set; }
            public string StoreDescript { get; set; }
           
        }
        private List<StoresDto> _stores
        {
            get { return ViewState["StoreList"] as List<StoresDto>; }
            set { ViewState["StoreList"] = value; }
        }

        private UserDetails CurrentUser
            {
                get { return Session["UserDetails"] as UserDetails; }
            }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            TrfThisID = Convert.ToInt32(Request.QueryString["trfid"]);
            if (!IsPostBack)
            {
                LoadWarehouses();
                LoadActiveItems();
                LoadTransfer();
            }
        }

        private void LoadTransfer()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Trf = (from ith in _db.ItemTransferHeaders
                            where ith.CompanyID == CurrentUser.CoID
                            let fromStore = _db.Stores.Where(s => s.StoreID == ith.TrfFromID).Select(s => s.StoreCode).FirstOrDefault()
                            let toStore = _db.Stores.Where(s => s.StoreID == ith.TrfToID).Select(s => s.StoreCode).FirstOrDefault()
                            select new
                            {
                                ith.TrfID,
                                FrmStore = fromStore,
                                ToStore = toStore,
                                ith.TrfStatus,
                                ith.TrfNotes,
                                ith.TrfDate,
                                ith.TrfActive,
                                ith.TrfStarted,
                                ith.TrfComplete,
                                ith.TransferID,
                                ith.TrfFromID,
                                ith.TrfToID,
                                ith.TrfReference, 
                                ith.TrfCompleteDate
                            }).Where(x => x.TrfID == TrfThisID).FirstOrDefault();

                    if (Trf != null)
                    {         
                        lblDocNum.Text = Trf.TransferID.ToString().PadLeft(8, '0');
                        txtxTrfRef.Text = Trf.TrfReference;
                        txtNotes.Text = Trf.TrfNotes;
                        txtDate.Text = Convert.ToDateTime(Trf.TrfDate).ToString("dd MMM yyyy");
                        if (Trf.TrfCompleteDate != null) txtCompleteDate.Text = Convert.ToDateTime(Trf.TrfCompleteDate).ToString("dd MMM yyyy");
                    
                        if (Trf.TrfFromID != null && ddlFromWarehouse.Items.FindByValue(Trf.TrfFromID.ToString()) != null)
                        ddlFromWarehouse.SelectedValue = Trf.TrfFromID.ToString();
                            else
                        ddlFromWarehouse.SelectedIndex = 0; // or handle as needed

                    if (Trf.TrfToID != null && ddlToWarehouse.Items.FindByValue(Trf.TrfToID.ToString()) != null)
                        ddlToWarehouse.SelectedValue = Trf.TrfToID.ToString();
                    else
                        ddlToWarehouse.SelectedIndex = 0; // or handle as needed

                    var TrfLines = _db.ItemTransferLines.Where(x => x.CompanyID == CurrentUser.CoID && x.TrfID == Trf.TrfID).OrderBy(x => x.TrfLID).ToList();
                    if (Trf.TrfStarted == true)
                    {
                        TrfLines = TrfLines.Where(x => x.TrfOutQty > 0).ToList();
                    }
                    
                    foreach (var Trl in TrfLines)
                    {
                        if (Trl.TrfOutQty != null && Trl.TrfOutQty > 0)
                        {
                            string TrfQty = ApiUrlCall.NumberToDecimal(Trl.TrfOutQty.ToString(), CurrentUser.CompanyDecPlaces);
                            Trl.TrfOutQty = Convert.ToDecimal(TrfQty);
                        }
                    }

                    DDStatus.Text = Trf.TrfStatus;
                    if (Trf.TrfStatus == "Started")
                    {
                        lbtnCompleteTrf.Style.Add("display", "inline-block");
                        lbtnStart.Style.Add("display", "none");
                        LbtnSaveEdits.Style.Add("display", "none");
                    }
                    else if (Trf.TrfStatus == "Complete" || Trf.TrfStatus == "Deleted")
                    {
                        lbtnCompleteTrf.Style.Add("display", "none");
                        lbtnStart.Style.Add("display", "none");
                        LbtnSaveEdits.Style.Add("display", "none");
                    }
                    else
                    {
                        lbtnStart.Style.Add("display", "inline-block");
                        LbtnSaveEdits.Style.Add("display", "inline-block");
                        lbtnCompleteTrf.Style.Add("display", "none");
                    }
                    GridTrfLines.DataSource = TrfLines;
                    GridTrfLines.DataBind();
                }
            }
        }
        private void LoadWarehouses()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true && x.StoreCode != "CoR" && x.StoreCode != "CoD").ToList();
                ddlFromWarehouse.DataSource = stores;
                ddlFromWarehouse.DataTextField = "StoreDescript";
                ddlFromWarehouse.DataValueField = "StoreID";
                ddlFromWarehouse.DataBind();
                ddlFromWarehouse.Items.Insert(0, "-Select-");
                ddlToWarehouse.DataSource = stores;
                ddlToWarehouse.DataTextField = "StoreDescript";
                ddlToWarehouse.DataValueField = "StoreID";
                ddlToWarehouse.DataBind();
                ddlToWarehouse.Items.Insert(0, "-Select-");
                // Map to StoresDto for ViewState
                _stores = stores.Select(s => new StoresDto
                {
                    StoreID = s.StoreID,
                    StoreCode = s.StoreCode,
                    StoreDescript = s.StoreDescript
                }).ToList();
            }
        }

        private void LoadActiveItems()
        {
            if (_itemDropDownList == null || _itemDropDownList.Count == 0)
            {
                using (SBMSEntities db = new SBMSEntities(Config.GetConnectionString()))
                {
                    _itemDropDownList = db.GetOpeningBalancesAllStores(CurrentUser.CoID)
                        .Select(x => new ItemDropDownDto
                        {
                            CompanyID = CurrentUser.CoID,
                            ItemID = (long)x.ItemID,
                            Item = (x.ItemCode ?? "") + (string.IsNullOrWhiteSpace(x.ItemCode) ? "" : " - ") + (x.ItemDescription ?? ""),
                            ItemCode = x.ItemCode,
                            StoreID = (long)x.StoreID,
                            QOH = x.QOH ?? 0,
                            LotNum = x.LotNumber,
                            PriceExclusive = (decimal)x.PriceExclusive,
                            TotalUnitPriceExclInclAdd = (decimal)x.TotalUnitPriceExclInclAdd,
                            Unit = x.Unit ?? ""
                        }).ToList();
                }
            }
        }

        protected void GridTrfLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (CurrentUser.CompanyUseLotNumbers == false)
            {
                e.Row.Cells[1].Visible = false;
            }
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                if (ddlFromWarehouse.SelectedIndex <= 0)
                {
                    return;
                }

                if (DDStatus.Text == "Complete" || DDStatus.Text == "Deleted")
                {
                    var lbtnDeleteLine = e.Row.FindControl("lbtnDeleteLine");
                    lbtnDeleteLine.Visible = false;
                    var lbtnLineSave = e.Row.FindControl("lbtnLineSave");
                    lbtnLineSave.Visible = false;
                    TextBox txtQty = (TextBox)e.Row.FindControl("txtQty");
                    txtQty.ReadOnly = true;
                } else
                if (DDStatus.Text == "Started")
                {
                    var lbtnDeleteLine = e.Row.FindControl("lbtnDeleteLine");
                    lbtnDeleteLine.Visible = false;
                    var lbtnLineSave = e.Row.FindControl("lbtnLineSave");
                    lbtnLineSave.Visible = false;
                    TextBox txtQty = (TextBox)e.Row.FindControl("txtQty");
                    txtQty.ReadOnly = true;
                }
                else
                if (DDStatus.Text == "New")
                {
                    var lbtnDeleteLine = e.Row.FindControl("lbtnDeleteLine");
                    lbtnDeleteLine.Visible = true;
                    var lbtnLineSave = e.Row.FindControl("lbtnLineSave");
                    lbtnLineSave.Visible = true;
                    TextBox txtQty = (TextBox)e.Row.FindControl("txtQty");
                    txtQty.ReadOnly = false;
                }

                long Storeid = Convert.ToInt64(ddlFromWarehouse.SelectedValue);
                var ddlGridItem = (DropDownList)e.Row.FindControl("ddlGridItem");
                var DDlotNum = (DropDownList)e.Row.FindControl("DDlotNum");
                var lblAvailQty = (Label)e.Row.FindControl("lblAvailQty");

                if (ddlGridItem != null)
                {
                    // Use distinct items by ItemID and Item
                    ddlGridItem.DataSource = _itemDropDownList
                        .Where(x => x.StoreID == Storeid)
                        .GroupBy(x => new { x.ItemID, x.Item })
                        .Select(g => g.First())
                        .ToList();
                    ddlGridItem.DataTextField = "Item";
                    ddlGridItem.DataValueField = "ItemID";
                    ddlGridItem.DataBind();
                    ddlGridItem.Items.Insert(0, new System.Web.UI.WebControls.ListItem("-Select-", ""));

                    // Set selected value based on the row's ItemSelectionId
                    var dataItem = e.Row.DataItem;
                    long? itemid = null;
                    if (dataItem != null)
                    {
                        var itemSelectionIdProp = dataItem.GetType().GetProperty("ItemSelectionId");
                        if (itemSelectionIdProp != null)
                        {
                            var itemSelectionId = itemSelectionIdProp.GetValue(dataItem, null);
                            if (itemSelectionId != null)
                            {
                                itemid = Convert.ToInt64(itemSelectionId);
                                System.Web.UI.WebControls.ListItem li = ddlGridItem.Items.FindByValue(itemid.ToString());
                                if (li != null) ddlGridItem.SelectedValue = itemid.ToString();
                            }
                        }
                        // Set QOH for the selected item
                        if (itemid != null)
                        {
                            if (CurrentUser.CompanyUseLotNumbers == false)
                            {
                                e.Row.Cells[1].Visible = false;
                                var qoh = _itemDropDownList.Where(x => x.StoreID == Storeid && x.ItemID == itemid).Sum(x => x.QOH);
                                lblAvailQty.Text = ApiUrlCall.NumberToDecimal(qoh.ToString(), CurrentUser.CompanyDecPlaces);
                            }
                            else
                            {
                                if (DDlotNum != null)
                                {
                                    DDlotNum.DataSource = _itemDropDownList
                                        .Where(x => x.StoreID == Storeid && x.ItemID == itemid && !string.IsNullOrEmpty(x.LotNum))
                                        .Select(x => x.LotNum)
                                        .Distinct()
                                        .ToList();
                                    DDlotNum.DataBind();
                                    DDlotNum.Items.Insert(0, new System.Web.UI.WebControls.ListItem("-Select-", ""));
                                    // Set selected value to the LotNumber from the data item
                                    var lotNumberProp = dataItem.GetType().GetProperty("LotNumber");
                                    if (lotNumberProp != null)
                                    {
                                        var lotNumberValue = lotNumberProp.GetValue(dataItem, null) as string;
                                        if (!string.IsNullOrEmpty(lotNumberValue) && DDlotNum.Items.FindByValue(lotNumberValue) != null)
                                        {
                                            DDlotNum.SelectedValue = lotNumberValue;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            lblAvailQty.Text = "0";
                        }
                    }
                }
            }
        }

        protected void ddlFromWarehouse_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddlFromWarehouse.SelectedIndex <= 0)
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var header = _db.ItemTransferHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                    header.TrfFromID = null;
                    _db.SaveChanges();
                }
            }
            else
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var header = _db.ItemTransferHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                    if (header != null && long.TryParse(ddlFromWarehouse.SelectedValue, out long newFromStoreId))
                    {
                        header.TrfFromID = newFromStoreId;
                        _db.SaveChanges();
                    }
                }
            }
            LoadActiveItems();
            LoadTransfer();
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

        protected void lbtnLineSave_Click(object sender, EventArgs e)
        {
            LinkButton lbtn = sender as LinkButton;
            if (lbtn == null) return;

            GridViewRow row = lbtn.NamingContainer as GridViewRow;
            if (row == null) return;

            long Lid = Convert.ToInt32(lbtn.CommandArgument);
            DropDownList ddlGridItem = row.FindControl("ddlGridItem") as DropDownList;
            TextBox txtQty = row.FindControl("txtQty") as TextBox;
            DropDownList DDlotNum = row.FindControl("DDlotNum") as DropDownList;
            Label lblAvailQty = row.FindControl("lblAvailQty") as Label;

            // Check for insufficient stock
            if (lblAvailQty != null && txtQty != null)
            {
                decimal availQty = 0;
                decimal reqQty = 0;
                decimal.TryParse(lblAvailQty.Text, out availQty);
                decimal.TryParse(txtQty.Text, out reqQty);
                if (reqQty > availQty)
                {
                    AlertHelper.ShowSweetAlert(this, "Insufficient stock available for this item.", "error");
                    return;
                }
            }

            if (ddlGridItem.SelectedIndex <= 0) return;

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var lines = _db.ItemTransferLines.Where(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID).ToList();
                if (lines == null) return;

                var Ln = lines.FirstOrDefault(x => x.TrfLID == Lid);
                if (Ln != null)
                {
                    Ln.ItemSelectionId = Convert.ToInt64(ddlGridItem.SelectedValue);
                    try
                    {
                        Ln.ItemDescription = ddlGridItem.SelectedItem.Text.Split('-')[1].Trim();
                        Ln.ItemCode = ddlGridItem.SelectedItem.Text.Split('-')[0].Trim();
                    }
                    catch
                    {
                        Ln.ItemDescription = ddlGridItem.SelectedItem.Text;
                    }
                    
                    try
                    {
                        Ln.TrfOutQty = Convert.ToDecimal(txtQty.Text);
                        Ln.TrfOutQty = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(Ln.TrfOutQty.ToString(), CurrentUser.CompanyDecPlaces)); ;
                    }
                    // set price and total values
                    //Ln.u
                    catch { Ln.TrfOutQty = 0; }
                    if (DDlotNum.SelectedIndex > 0 && DDlotNum.Text != "") Ln.LotNumber = DDlotNum.Text;

                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    /// GET ITEM AND EXTRACT PRICING DETAILS
                    /// 
                    var thisrowItem = _itemDropDownList.FirstOrDefault(x => x.ItemID == Ln.ItemSelectionId && x.StoreID == Convert.ToInt64(ddlFromWarehouse.SelectedValue));
                    Ln.Unit = thisrowItem.Unit;
                    Ln.TotalUnitPriceExclInclAdd = thisrowItem.TotalUnitPriceExclInclAdd ?? 0m;
                    Ln.PriceExclusive = thisrowItem.PriceExclusive ?? 0m;
                    Ln.TotalLineValExcl = Ln.TotalUnitPriceExclInclAdd * Ln.TrfOutQty;
                    Ln.TransactionReference = txtxTrfRef.Text.ToString().Trim();
                    _db.SaveChanges();
                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////                
                }

            // If all lines have ItemSelectionId, add a new blank line
                if (lines.All(x => x.ItemSelectionId != null))
                {
                    var newLine = new ItemTransferLine
                    {
                        CompanyID = CurrentUser.CoID,
                        TrfID = TrfThisID,
                        TransactionReference = txtxTrfRef.Text.ToString().Trim()
                    };
                    _db.ItemTransferLines.Add(newLine);

                    _db.SaveChanges();
                }
            GridTrfLines.DataSource = _db.ItemTransferLines.Where(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID).ToList();
            GridTrfLines.DataBind();
            }
         }

            protected void ddlGridItem_SelectedIndexChanged(object sender, EventArgs e)
            {
                long Storeid, itemid;
                DropDownList ddlGridItem = sender as DropDownList;
                if (ddlGridItem == null) return;

                GridViewRow row = ddlGridItem.NamingContainer as GridViewRow;
                if (row == null) return;

                // Get the TrfLID for this row
                long? trfLid = null;
                var grid = row.NamingContainer as GridView;
                if (grid != null && grid.DataKeys != null && grid.DataKeys.Count > row.RowIndex)
                {
                    trfLid = Convert.ToInt64(grid.DataKeys[row.RowIndex].Value);
                }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var lines = _db.ItemTransferLines.Where(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID).ToList();

                // Set SelectionId in the in-memory line
                //var lines = Session["CurrentTransferLines"] as List<ItemTransferLine>;
                if (lines != null && trfLid != null)
                {
                    var line = lines.FirstOrDefault(x => x.TrfLID == trfLid);
                    if (line != null && ddlGridItem.SelectedIndex > 0)
                    {
                        line.ItemSelectionId = Convert.ToInt64(ddlGridItem.SelectedValue);
                        line.ItemDescription = ddlGridItem.SelectedItem.Text;
                    }
                }
            }
                // Populate DDlotNum for the selected item
                var DDlotNum = row.FindControl("DDlotNum") as DropDownList;
                if (DDlotNum != null && ddlGridItem.SelectedIndex > 0)
                {
                    Storeid = Convert.ToInt64(ddlFromWarehouse.SelectedValue);
                    itemid = Convert.ToInt64(ddlGridItem.SelectedValue);
                    var lotList = _itemDropDownList
                        .Where(x => x.StoreID == Storeid && x.ItemID == itemid && !string.IsNullOrEmpty(x.LotNum))
                        .Select(x => x.LotNum)
                        .Distinct()
                        .ToList();
                    DDlotNum.DataSource = lotList;
                    DDlotNum.DataBind();
                    DDlotNum.Items.Insert(0, new System.Web.UI.WebControls.ListItem("-Select-", ""));
                }

                var lblAvailQty = row.FindControl("lblAvailQty") as Label;
                Storeid = Convert.ToInt64(ddlFromWarehouse.SelectedValue);
                itemid = Convert.ToInt64(ddlGridItem.SelectedValue);

                var qoh = _itemDropDownList.Where(x => x.StoreID == Storeid && x.ItemID == itemid).Sum(x => x.QOH);
                lblAvailQty.Text = ApiUrlCall.NumberToDecimal(qoh.ToString(), CurrentUser.CompanyDecPlaces);
                // Optionally, update QOH display here if needed
            }

            protected void DDlotNum_SelectedIndexChanged(object sender, EventArgs e)
            {
                if (ddlFromWarehouse.SelectedIndex <= 0)
                {
                    return;
                }

                DropDownList dd = sender as DropDownList;
                if (dd == null) return;

                GridViewRow row = dd.NamingContainer as GridViewRow;
                if (row == null) return;
                DropDownList DDlotNum = row.FindControl("DDlotNum") as DropDownList;
                long Storeid = Convert.ToInt64(ddlFromWarehouse.SelectedValue);
                var ddlGridItem = (DropDownList)row.FindControl("ddlGridItem");
                var lblAvailQty = (Label)row.FindControl("lblAvailQty");

                long itemid = Convert.ToInt64(ddlGridItem.SelectedValue);
                string selectedLot = DDlotNum.SelectedItem.Text;

            // Set LotNumber in the in-memory line
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var lines = _db.ItemTransferLines.Where(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID).ToList();

                // var lines = Session["CurrentTransferLines"] as List<ItemTransferLine>;
                if (lines != null)
                {
                    // Find the line by TrfLID (from DataKeys or CommandArgument)
                    int rowIndex = row.RowIndex;
                    var grid = row.NamingContainer as GridView;
                    long? trfLid = null;
                    if (grid != null && grid.DataKeys != null && grid.DataKeys.Count > rowIndex)
                    {
                        trfLid = Convert.ToInt64(grid.DataKeys[rowIndex].Value);
                    }
                    else
                    {
                        // fallback: try to get from a hidden field or other means if available
                    }
                    if (trfLid != null)
                    {
                        var line = lines.FirstOrDefault(x => x.TrfLID == trfLid);
                        if (line != null)
                        {
                            line.LotNumber = selectedLot;
                        }
                    }
                }
            }
                var qoh = _itemDropDownList.Where(x => x.StoreID == Storeid && x.ItemID == itemid && x.LotNum == selectedLot).Sum(x => x.QOH);
                lblAvailQty.Text = ApiUrlCall.NumberToDecimal(qoh.ToString(), CurrentUser.CompanyDecPlaces);
            }

            protected void lbtnDeleteLine_Click(object sender, EventArgs e)
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    LinkButton lbtn = sender as LinkButton;
                    int Lid = Convert.ToInt32(lbtn.CommandArgument);
                    _db.ItemTransferLines.RemoveRange(_db.ItemTransferLines.Where(x => x.CompanyID == CurrentUser.CoID && x.TrfLID == Lid));
                    _db.SaveChanges();
                    LoadTransfer();
                }
            }

            protected void lbtnPrintDN_Click(object sender, EventArgs e)
            {
            CreatePDF();
            Response.Redirect($"~/ViewPDF.aspx?doc=" + CurrentUser.UserGuiD.ToString() + "\\Trf_" + lblDocNum.Text, false);
        }

            protected void lbtnComplete_Click(object sender, EventArgs e)
            {

            }

        protected void lbtnStart_Click(object sender, EventArgs e)
        {
            string result = SaveTransfer();
            if (result != "OK")
            {
                AlertHelper.ShowSweetAlert(this, result, "error");
            }
            else
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var lines = _db.ItemTransferLines.Where(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID).ToList();
                    if (lines.Count == 1)
                    {
                        if (lines[0].ItemSelectionId == null || (lines[0].TrfOutQty == null || (lines[0].TrfOutQty < 1)))
                        {
                            AlertHelper.ShowSweetAlert(this, "No Lines Saved. Please add lines to transfer.", "error");
                            return;
                        }
                    }
                    else
                    {
                        foreach (var ln in lines)
                        {
                            if (ln.ItemSelectionId > 0 && (ln.TrfOutQty == null || (ln.TrfOutQty < 1)))
                            {
                                AlertHelper.ShowSweetAlert(this, "Not All Lines Saved. Please save each line to ensure a successful transfer.", "error");
                                return;
                            }
                        }
                    }

                    var header = _db.ItemTransferHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                    if (header != null)
                    {
                        header.TrfStarted = true;
                        header.TrfStatus = "Started";
                        header.TrfDate = DateTime.Now;
                    }
                    _db.SaveChanges();
                    LoadTransfer();
                    AlertHelper.ShowSweetAlert(this, "Transfer Started", "success");
                }
            }
        }
        

        protected void lbtnDelTrf_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Delete all lines for this transfer
                var lines = _db.ItemTransferLines.Where(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                _db.ItemTransferLines.RemoveRange(lines);

                var header = _db.ItemTransferHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                if (header != null)
                {
                    header.TrfActive = false;
                    header.TrfStatus = "Deleted";
                }
                _db.SaveChanges();
            }
            Response.Redirect("~/TransferHeaders.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void LbtnSaveEdits_Click(object sender, EventArgs e)
        {
            string result = SaveTransfer();
            if (result != "OK")
            {
                AlertHelper.ShowSweetAlert(this, result, "error");
            }
            else
            {
                AlertHelper.ShowSweetAlert(this, "Transfer saved started.", "success");
            }           
        }

        protected string SaveTransfer()
        {
            if (string.IsNullOrWhiteSpace(txtxTrfRef.Text))
            {
                return "Invalid Reference, please enter a reference before continuing";
            }

            // Validate date
            if (!DateTime.TryParse(txtDate.Text, out DateTime trfDate))
            {
                return "Invalid Date format. Please enter a valid date.";
            }

            // Validate From Warehouse
            if (ddlFromWarehouse.SelectedIndex <= 0 || !long.TryParse(ddlFromWarehouse.SelectedValue, out long fromId))
            {
                return "Please select a valid From Warehouse.";
            }

            // Validate To Warehouse
            if (ddlToWarehouse.SelectedIndex <= 0 || !long.TryParse(ddlToWarehouse.SelectedValue, out long toId))
            {
                return "Please select a valid To Warehouse.";
            }

            if (ddlFromWarehouse.SelectedValue == ddlToWarehouse.SelectedValue)
            {
                return "From and To Warehouses cannot be the same.";
            }
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Save header
                var header = _db.ItemTransferHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                if (header != null)
                {
                    header.TrfReference = txtxTrfRef.Text.Trim();
                    header.TrfNotes = txtNotes.Text.Trim();
                    header.TrfDate = trfDate;
                    header.TrfFromID = fromId;
                    header.TrfToID = toId;
                }
                try
                {
                    _db.SaveChanges();
                    return "OK";
                }
                catch (Exception ex)
                {
                    return ex.ToString();
                }
            }
        }
        protected void lbtnCompleteTrf_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // perform Item transfer for each item line
                var header = _db.ItemTransferHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID);
                if (header != null)
                {
                    header.TrfComplete = true;
                    header.TrfStatus = "Complete";
                    header.TrfActive = false;
                    header.TrfCompleteDate = DateTime.Now;
                }

                var lines = _db.ItemTransferLines
                .Where(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID && x.ItemSelectionId != null && x.TrfOutQty > 0)
                .ToList();

                long fromStoreId = header.TrfFromID ?? 0;
                long toStoreId = header.TrfToID ?? 0;

                foreach (var line in lines)
                {
                    line.TrfInQty = line.TrfOutQty;
                    DoAdjustments(header.TrfReference, line, fromStoreId, toStoreId);
                }

                _db.SaveChanges();
                LoadTransfer();
                AlertHelper.ShowSweetAlert(this, "Transfer Completed", "success");
            }
        }

        private void DoAdjustments(string trfref, ItemTransferLine line, long fromStoreId, long toStoreId)
        {
            decimal trfQty = 0, TrfUnitCost = 0;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Record for Receiving store
                ItemTransaction ItemTrans = new ItemTransaction();
                ItemTrans.CompanyID = line.CompanyID;
                ItemTrans.DocumentID = 0;
                ItemTrans.TransactionType = "TRF";
                ItemTrans.ItemID = line.ItemSelectionId;
                ItemTrans.ItemCode = line.ItemCode;
                ItemTrans.ItemDescription = line.ItemDescription;
                ItemTrans.LotNumber = line.LotNumber ?? null;
                ItemTrans.Unit = line.Unit;
                ItemTrans.FromID = toStoreId;
                ItemTrans.ToID = fromStoreId;
                ItemTrans.Qty = line.TrfOutQty * -1;
                trfQty = (decimal)line.TrfOutQty * -1;
                ItemTrans.PriceInclusive = line.PriceInclusive;
                ItemTrans.PriceExclusive = line.TotalUnitPriceExclInclAdd;
                TrfUnitCost = (decimal)ItemTrans.PriceExclusive;
                decimal ToBal = trfQty;
                var QOHIn = (from it2 in _db.ItemTransactions
                                where it2.ItemID == line.ItemSelectionId && it2.CompanyID == line.CompanyID && it2.LotNumber == line.LotNumber && it2.ToID == ItemTrans.ToID
                                orderby it2.TransactionDate descending
                                select new
                                {
                                    it2.TotalUnitPriceExclInclAdd
                                }).FirstOrDefault();
                if (QOHIn != null)
                {
                    if (QOHIn.TotalUnitPriceExclInclAdd.HasValue) ItemTrans.PriceExclusive = QOHIn.TotalUnitPriceExclInclAdd;
                }
                ItemTrans.TransactionDate = DateTime.Now;
                ItemTrans.DocumentType = 4;
                ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid
                                                            // Additional costs ????
                decimal AddCosts = 0, UnitPrInclAddCosts = 0;
                ItemTrans.AdditionalCosts = 0;

                ItemTrans.TotalUnitPriceExclInclAdd = ItemTrans.PriceExclusive;
                if (AddCosts > 0) { ItemTrans.TotalUnitPriceExclInclAdd = ItemTrans.TotalUnitPriceExclInclAdd + (AddCosts / trfQty); }
                UnitPrInclAddCosts = (decimal)ItemTrans.TotalUnitPriceExclInclAdd;

                ItemTrans.TotalLineValExcl = ItemTrans.TotalUnitPriceExclInclAdd * trfQty;
                ItemTrans.TransactionReference = line.ItemCode + " " + trfref + " " + trfQty + " out to " + _stores.Where(x => x.StoreID == toStoreId).Select(x => x.StoreCode).FirstOrDefault();
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
                ItemTrans = new ItemTransaction();
                ItemTrans.CompanyID = line.CompanyID;
                ItemTrans.DocumentID = 0;
                ItemTrans.TransactionType = "TRF";
                ItemTrans.ItemID = line.ItemSelectionId;
                ItemTrans.ItemCode = line.ItemCode;
                ItemTrans.ItemDescription = line.ItemDescription;
                ItemTrans.LotNumber = line.LotNumber ?? null;
                ItemTrans.Unit = line.Unit;
                ItemTrans.FromID = fromStoreId;
                ItemTrans.ToID = toStoreId;
                ItemTrans.Qty = trfQty * -1;
                ItemTrans.DocumentType = 4;
                ItemTrans.TransactionDate = DateTime.Now;
                ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid

                // Additional costs ????
                ItemTrans.AdditionalCosts = 0;
                //if (AddCosts > 0) { ItemTrans.AdditionalCosts = AddCosts / trfQty; }
                ItemTrans.PriceExclusive = TrfUnitCost;
                ItemTrans.TotalUnitPriceExclInclAdd = TrfUnitCost;
                ItemTrans.TotalLineValExcl = ItemTrans.TotalUnitPriceExclInclAdd * (trfQty * -1);
                ItemTrans.TransactionReference = line.ItemCode + " " + trfref + " " + trfQty * -1 + " in from " + _stores.Where(x => x.StoreID == fromStoreId).Select(x => x.StoreCode).FirstOrDefault();
                ItemTrans.ExchRate = 1;
                _db.ItemTransactions.Add(ItemTrans);
                _db.SaveChanges();
            }
        }

        private void CreatePDF()
        {
            string filepath = string.Empty, fname = string.Empty;
            var regfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 10, BaseColor.BLACK);
            var regfontB = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 10, Font.BOLD, BaseColor.BLACK);
            var regfontS = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 8, BaseColor.BLACK);
            var medfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 11, BaseColor.BLACK);
            var headfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 18, BaseColor.BLACK);
          
            //Guid DocGuid = Guid.Parse(docguid);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var TrL = _db.ItemTransferLines.Where(x => x.TrfID == TrfThisID && x.TrfOutQty >0).OrderBy(x=>x.TrfLID).ToList();
                var DH = _db.ItemTransferHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.TrfID == TrfThisID).FirstOrDefault();
                if (TrL != null)
                {
                    iTextSharp.text.Document doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 40, 40);

                    try
                    {
                        if (!Directory.Exists(Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString())))
                        {
                            Directory.CreateDirectory(Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString()));
                        }
                        filepath = Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString() + "\\Trf_" + lblDocNum.Text + ".PDF");
                        if (File.Exists(filepath))
                        {
                            File.Delete(filepath);
                        }
                        if (File.Exists(filepath))
                        {
                            File.Delete(filepath);
                        }
                        PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(filepath, FileMode.Create));
                        writer.SetPdfVersion(PdfWriter.PDF_VERSION_1_7);
                        writer.SetFullCompression();
                        writer.PageEvent = new PDFFooter();
                    }
                    catch { }
                    doc.Open();
                    doc.SetMargins(28f, 28f, 100f, 80f);

                    #region Headerinfo
                    PdfPTable table = new PdfPTable(3);
                    PdfPCell cell;
                    table.SetWidths(new int[] { 150, 285, 150 });
                    table.TotalWidth = doc.PageSize.Width - 80;
                    table.LockedWidth = true;
                    iTextSharp.text.Image gif;
                    string imgpath = Server.MapPath("~/images/CoImages/" + CurrentUser.CoID + ".png");
                    if (File.Exists(imgpath))
                    {
                        gif = iTextSharp.text.Image.GetInstance(imgpath);
                        gif.ScaleToFit(170.0F, 65.0F);
                    }
                    else
                    {
                        gif = null;
                    }

                    try
                    {
                        cell = new PdfPCell(gif);
                    }
                    catch
                    {
                        cell = new PdfPCell(new Phrase(""));
                    }
                    cell.Border = 0;
                    cell.HorizontalAlignment = 0;
                    cell.Rowspan = 5;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Transfer Slip ", headfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(DH.TrfReference.ToString(), headfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("From:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    var fromStoreDto = _stores.FirstOrDefault(x => x.StoreID == DH.TrfFromID);
                    cell = new PdfPCell(new Phrase(fromStoreDto != null ? fromStoreDto.StoreDescript : string.Empty, medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("To:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    var toStoreDto = _stores.FirstOrDefault(x => x.StoreID == DH.TrfToID);
                    cell = new PdfPCell(new Phrase(toStoreDto != null ? toStoreDto.StoreDescript : string.Empty, medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Date:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(Convert.ToDateTime(DH.TrfDate).ToString("dd MMM yyyy"), medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Completed Date:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                   if (DH.TrfCompleteDate != null)
                    {
                        cell = new PdfPCell(new Phrase(Convert.ToDateTime(DH.TrfCompleteDate).ToString("dd MMM yyyy"), medfont));
                    }
                    else
                    {
                        cell = new PdfPCell(new Phrase("", medfont));
                    }
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(DH.TrfNotes, medfont));
                    cell.Colspan = 2;
                    cell.Border = 0;
                    cell.HorizontalAlignment = 0;
                    table.AddCell(cell);

                    

                    doc.Add(table);
                    #endregion

                    //#region messages
                    //PdfPTable tableM = new PdfPTable(3);
                    //PdfPCell cellM;
                    //tableM.SpacingBefore = 15f;
                    //tableM.TotalWidth = doc.PageSize.Width - 80;
                    //tableM.LockedWidth = true;

                    //cellM = new PdfPCell(new Phrase("Picking Notes:- " + Environment.NewLine + (PS.PSPickMessage ?? "").ToString(), regfont));
                    //cellM.HorizontalAlignment = 0;
                    //cellM.FixedHeight = 80f; ;
                    //tableM.AddCell(cellM);

                    //cellM = new PdfPCell(new Phrase("Packing Notes:- " + Environment.NewLine + (PS.PSPackMessage ?? "").ToString(), regfont));
                    //cellM.HorizontalAlignment = 0;
                    //cellM.FixedHeight = 60f; ;
                    //tableM.AddCell(cellM);

                    //cellM = new PdfPCell(new Phrase("Delivery Notes:- " + Environment.NewLine + (PS.PSDeliverMessage ?? "").ToString(), regfont));
                    //cellM.HorizontalAlignment = 0;
                    //cellM.FixedHeight = 60f; ;
                    //tableM.AddCell(cellM);


                    //doc.Add(tableM);
                    //#endregion


                    #region HeaderRow
                    PdfPTable table4 = new PdfPTable(8);
                    PdfPCell cell4;
                    table4.SpacingBefore = 15f;
                    table4.SetWidths(new int[] { 50, 150, 60, 30, 100, 40, 40, 40 });
                    table4.TotalWidth = doc.PageSize.Width - 80;
                    table4.LockedWidth = true;

                    cell4 = new PdfPCell(new Phrase("Item Code", regfont));
                    cell4.HorizontalAlignment = 0;
                    cell4.FixedHeight = 20f; ;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Description", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Bar Code", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Unit", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Lot Number", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Out Qty", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("In Qty", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Note", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    #endregion

                    
                    foreach (var DL in TrL)
                    {
                        cell4 = new PdfPCell(new Phrase(DL.ItemCode ?? "", regfont));
                        cell4.HorizontalAlignment = 0;
                        cell4.FixedHeight = 20f;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(DL.ItemDescription ?? "", regfont));
                        cell4.HorizontalAlignment = 0;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase("", regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(DL.Unit ?? "", regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(DL.LotNumber ?? "", regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        string outQty = "";
                        if (DL.TrfOutQty != null)
                        {
                            outQty = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(DL.TrfOutQty.ToString(), CurrentUser.CompanyDecPlaces)).ToString();
                        }
                        cell4 = new PdfPCell(new Phrase(outQty.ToString(), regfont));
                        //cell4 = new PdfPCell(new Phrase(DL.Quantity.ToString(), regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        string InQty = "";
                        if (DL.TrfInQty != null)
                        {
                            InQty = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(DL.TrfInQty.ToString(), CurrentUser.CompanyDecPlaces)).ToString();
                        }
                        cell4 = new PdfPCell(new Phrase(InQty, regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase("", regfont));
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                    }

                    doc.Add(table4);


                    doc.AddTitle("Transfer Slip: ");
                    doc.AddAuthor("Data Fusion");
                    doc.Close();

                }
            }
        }

    }
}
