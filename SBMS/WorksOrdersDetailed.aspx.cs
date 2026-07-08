using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class WorksOrdersDetailed : BasePage
    {
        long CoID;
        private List<ItemsMaster> _items;
        private List<BOMHeader> _boms;
        private List<KitHeader> _kits;

        long woid = 0;

        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            UserDetails userDetails = CurrentUser;
            if (CurrentUser == null) Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
            woid = Convert.ToInt64(Request.QueryString["woid"].ToString());
            CoID = CurrentUser.CoID;
            if (!IsPostBack)
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        _items = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CoID && i.IsFinishedGoods == true).OrderBy(x => x.Code).ToList();
                        _boms = _db.BOMHeaders.Where(i => i.BomActive == true && i.CompanyID == CoID).OrderBy(x => x.BOMCode).ToList();
                        _kits = _db.KitHeaders.Where(i => i.KitActive == true && i.CompanyID == CoID).OrderBy(x => x.KitCode).ToList();
                    }
           
                LoadWorkStations();
                LoadWOHeader();
                LoadWOLines();
            }      
        }

        protected void LoadWOHeader()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var WOHeader = _db.WorksOrderHeaders.Where(x => x.CompanyID == CoID && x.ID == woid).FirstOrDefault();
                if (WOHeader != null)
                {
                    // Once any batch has been part-manufactured, lock the order from editing
                    // here so the ordered quantities can't drift out of sync with what has
                    // already been posted to stock/Sage.
                    bool started = _db.WorksOrderLines.Any(x => x.CompanyID == CoID && x.WOID == woid && x.Complete == true);
                    if (WOHeader.Active == false || started)
                    {
                        GridWOLines.Enabled = false;
                        chkCompl.Checked = true;
                        LbtnSaveWO.Enabled = false;
                        LbtnSaveWO.Visible = false;
                        LbtnSaveWO.ToolTip = started
                            ? "This Works Order has been part-manufactured and can no longer be edited."
                            : "This Works Order is Closed, no further changes allowed.";
                    }
                    woheader.InnerText = "WO-" + WOHeader.WONum;
                    if (WOHeader.CustSupName != null) lblFCCustName.Text = WOHeader.CustSupName.ToString() ?? "";
                    if (WOHeader.Reference != null) lblFCRef.Text = WOHeader.Reference.ToString() ?? "";
                    lblcreatedDate.Text = Convert.ToDateTime(WOHeader.WOrderDate, CultureInfo.InvariantCulture).ToString("dd MMM yyyy");
                    if (WOHeader.DueDate != null)
                    {
                        txtDueDate.Text = Convert.ToDateTime(WOHeader.DueDate, CultureInfo.InvariantCulture).ToString("dd MMM yyyy");
                    } else
                    {
                        txtDueDate.Text = DateTime.Today.ToString("dd MMM yyyy");
                    }
                    if (WOHeader.LinkedDocumentNum != null) txtLinkedDoc.Text = WOHeader.LinkedDocumentNum ?? "";
                    if (WOHeader.Message != null) txtwomsg.Text = WOHeader.Message.ToString() ?? "";
                    if (WOHeader.WOrderBy != null) lblCreatedBy.Text = WOHeader.WOrderBy.ToString() ?? "";
                    DDStatus.SelectedValue=WOHeader.Status ??"";
                }
            }
        }

        protected void LoadWOLines()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var WOLines = _db.WorksOrderLines.Where(x => x.CompanyID == CoID && x.WOID == woid).OrderBy(x=> x.LineID).ToList();
                if (!WOLines.Any())
                {
                    WorksOrderLine NewWOLine = new WorksOrderLine();
                    NewWOLine.WOID = woid;
                    NewWOLine.CompanyID = CoID;
                    NewWOLine.Active = true;
                    _db.WorksOrderLines.Add(NewWOLine);
                    _db.SaveChanges();
                }
                else
                {
                    var lastLine = WOLines.Last();
                    if (lastLine != null && lastLine.ItemDescription != null) // Check if NewJCLine and JCID are not null
                    {
                        WorksOrderLine NewWOLine = new WorksOrderLine();
                        NewWOLine.WOID = woid;
                        NewWOLine.CompanyID = CoID;
                        NewWOLine.Active = true;
                        _db.WorksOrderLines.Add(NewWOLine);
                        _db.SaveChanges();
                    }
                }
                WOLines = _db.WorksOrderLines.Where(x => x.CompanyID == CoID && x.WOID == woid).ToList();
                foreach (var TL in WOLines)
                {
                    if (TL.Quantity != null)
                    {
                        TL.Quantity = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(TL.Quantity.ToString(), CurrentUser.CompanyDecPlaces));
                    }
                }
               if (chkCompl.Checked)
                {
                    WOLines = WOLines.Where(x => x.ItemDescription != null && x.Quantity != null).ToList();
                }
                GridWOLines.DataSource = WOLines.OrderBy(x=>x.LineID);
                GridWOLines.DataBind();       
            }
        }
        protected void LbtnSaveWO_Click(object sender, EventArgs e)
        {
            if (lblFCRef.Text.ToString().Trim().Length < 1)
            {
                string message = "Please capture a Reference before continuing.";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }
            
            try
            {
                DateTime dt = Convert.ToDateTime(Convert.ToDateTime(txtDueDate.Text));
            }
                catch
                {
                    string message = "Invalid Due Date, please select a valid date";
                    AlertHelper.ShowSweetAlert(this, message, "warning");
                    return;
                }

            // SAVE LAST FILLED WORK ORDER LINE FIRST
            SaveLastFilledWOLine();

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var FCHeader = _db.WorksOrderHeaders.Where(x => x.CompanyID == CoID && x.ID == woid).FirstOrDefault();
                if (FCHeader != null)
                {
                    FCHeader.Reference = lblFCRef.Text.ToString().Trim();
                    FCHeader.CustSupName = lblFCCustName.Text.ToString();
                    FCHeader.WOrderBy = lblCreatedBy.Text.ToString();
                    FCHeader.LinkedDocumentNum = txtLinkedDoc.Text.ToString();
                    FCHeader.Message = txtwomsg.Text.ToString();
                    FCHeader.DueDate = Convert.ToDateTime(txtDueDate.Text);
                    if (DDStatus.Text.Contains("- Select -"))
                    {
                        FCHeader.Status = "New";
                    }
                    else
                        FCHeader.Status = DDStatus.Text;
                    _db.SaveChanges();
                    string message = "Successfully Saved";
                    AlertHelper.ShowSweetAlert(this, message, "success");
                }
            }
        }

        // Read-only availability on the components grid: total on-hand (all stores) and the
        // shortfall (Required - On-hand). Sub-assemblies (IsFromBOM) that are short are tagged
        // "(make)" since the fix is to manufacture them, not buy more. Display only.
        protected void GridUseBom_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var rml = e.Row.DataItem as WorksOrderRMLine;
            if (rml == null) return;

            Label lblOnHand = e.Row.FindControl("lblOnHand") as Label;
            Label lblShort = e.Row.FindControl("lblShort") as Label;
            if (lblOnHand == null && lblShort == null) return;

            long itemId = rml.SelectionId;
            decimal required = rml.Quantity ?? 0;

            decimal onHand;
            bool isFromBom;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                onHand = _db.ItemTransactions
                    .Where(x => x.CompanyID == CoID && x.ItemID == itemId)
                    .Select(x => (decimal?)x.Qty).DefaultIfEmpty(0).Sum() ?? 0;
                isFromBom = _db.ItemsMasters
                    .Where(x => x.CompanyID == CoID && x.ID == itemId)
                    .Select(x => x.IsFromBOM).FirstOrDefault() ?? false;
            }
            onHand = ApiUrlCall.NumberToDecimal(onHand, CurrentUser.CompanyDecPlaces);
            decimal shortQty = required - onHand;

            if (lblOnHand != null) lblOnHand.Text = onHand.ToString("N2");

            if (shortQty > 0)
            {
                // Highlight the On Hand cell (consistent with the manufacture grid).
                TableCell ohCell = lblOnHand != null ? lblOnHand.Parent as TableCell : null;
                if (ohCell != null)
                {
                    ohCell.BackColor = System.Drawing.Color.MistyRose;
                    ohCell.ForeColor = System.Drawing.Color.Firebrick;
                    ohCell.Font.Bold = true;
                }
                if (lblShort != null) lblShort.Text = shortQty.ToString("N2") + (isFromBom ? " (make)" : "");
            }
            else if (lblShort != null)
            {
                lblShort.Text = "-";
            }
        }

        protected void GridWOLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
            if (CurrentUser.CompanyUseLotNumbers == false)
            {
                e.Row.Cells[8].Visible = false;
            }

            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var item = (WorksOrderLine)e.Row.DataItem;
                var ddlItemCode = (DropDownList)e.Row.FindControl("DDItemCode");
                var ddlLineType = (DropDownList)e.Row.FindControl("DDBOMKIT");
                if (item.Active == false)
                {
                    var lbtn = (LinkButton)e.Row.FindControl("lbtnDeleteLine");
                    lbtn.Visible = false;
                }
                if (item.LineType == 1)
                {
                    if (_items == null)
                    {
                        using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                        {
                            _items = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CoID && i.Physical == true && i.IsFinishedGoods == true).OrderBy(x => x.Code).ToList();
                        }
                    }
                if (ddlItemCode != null)
                    {
                        ddlItemCode.DataSource = _items.Select(i => new {i.ID, DisplayText = i.Code + " - " + i.Description }).ToList();
                        ddlItemCode.DataTextField = "DisplayText";
                        ddlItemCode.DataValueField = "ID";
                        ddlItemCode.DataBind();
                        ddlItemCode.Items.Insert(0, new ListItem("Select", "0"));
                        string selectionIdStr = item.SelectionId.ToString();
                        if (ddlItemCode.Items.FindByValue(selectionIdStr) != null)
                        {
                            ddlItemCode.SelectedValue = selectionIdStr;
                        }
                        else
                        {
                            ddlItemCode.SelectedIndex = 0; // Default to "Select"
                        }
                        ddlLineType.SelectedValue = Convert.ToInt16(item.LineType).ToString();
                        ddlLineType.SelectedValue = Convert.ToInt16(item.LineType).ToString();
                    }
                } 
                else if (item.LineType==2)
                {
                    if (_boms== null)
                    {
                        using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                        {
                            _boms = _db.BOMHeaders.Where(i => i.BomActive == true && i.CompanyID == CoID).OrderBy(x => x.BOMCode).ToList();
                        }
                    }
                    if (ddlItemCode != null)
                    {
                        ddlItemCode.DataSource = _boms.Select(i => new { ID=i.FGID, DisplayText = i.FGCode + " - " + i.FGDescript }).ToList();
                        ddlItemCode.DataTextField = "DisplayText";
                        ddlItemCode.DataValueField = "ID";
                        ddlItemCode.DataBind();
                        ddlItemCode.Items.Insert(0, new ListItem("Select", "0"));
                        ddlItemCode.SelectedValue = item.SelectionId.ToString();
                        ddlLineType.SelectedValue = Convert.ToInt16(item.LineType).ToString();
                    }
                }
                else if (item.LineType==3)
                {
                    if (_kits == null)
                    {
                        using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                        {
                                _kits = _db.KitHeaders.Where(i => i.KitActive == true && i.CompanyID == CoID).OrderBy(x => x.KitCode).ToList();
                        }
                    }
                if (ddlItemCode != null)
                    {
                        ddlItemCode.DataSource = _kits.Select(i => new { ID = i.FGID, DisplayText = i.FGCode + " - " + i.FGDescript }).ToList(); ;
                        ddlItemCode.DataTextField = "DisplayText";
                        ddlItemCode.DataValueField = "ID";
                        ddlItemCode.DataBind();
                        ddlItemCode.Items.Insert(0, new ListItem("Select", "0"));
                        ddlItemCode.SelectedValue = item.SelectionId.ToString();
                        ddlLineType.SelectedValue = Convert.ToInt16(item.LineType).ToString();
                    }
                }    
            }
        }

        protected void DDItemCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            GridViewRow row = (GridViewRow)ddl.NamingContainer;
            long selectedItemid = Convert.ToInt64(ddl.SelectedValue);
            if (selectedItemid > 0)
            {
                PopulateItemDetails(row, selectedItemid);
            }
        }

        private void PopulateItemDetails(GridViewRow row, long itemid)
        {
            // Example LINQ query based on itemCode
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var item = _db.ItemsMasters.FirstOrDefault(i => i.ID == itemid);
                if (item != null)
                {
                    TextBox txtDescription = (TextBox)row.FindControl("txtDescription");
                    TextBox txtQty = (TextBox)row.FindControl("txtQty");
                    txtDescription.Text = item.Description;
                    txtQty.Text = "1"; // Adjust based on your item properties
                }
            }
        }

        protected void lbtnLineSave_Click(object sender, EventArgs e)
        {
            LinkButton lbtn = sender as LinkButton;
            if (lbtn == null) return;

            GridViewRow row = lbtn.NamingContainer as GridViewRow;
            if (row == null) return;

            DropDownList ddlt = row.FindControl("DDBOMKIT") as DropDownList;
            TextBox txtQty = row.FindControl("txtQty") as TextBox;
            DropDownList ddl = row.FindControl("DDItemCode") as DropDownList;
            TextBox txtDescription = row.FindControl("txtDescription") as TextBox;
            TextBox txtLotNum = row.FindControl("txtLotNum") as TextBox;

            if (ddlt == null || txtQty == null || ddl == null || txtDescription == null || txtLotNum == null)
            {
                AlertHelper.ShowSweetAlert(this, "One or more controls are missing.", "error");
                return;
            }

            DateTime dt;
            if (!DateTime.TryParse(txtDueDate.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                AlertHelper.ShowSweetAlert(this, "Invalid Due Date, Unable to continue.", "warning");
                return;
            }

            if (ddl.SelectedIndex == 0)
            {
                AlertHelper.ShowSweetAlert(this, "Invalid Item Selected, Unable to continue.", "warning");
                return;
            }

            decimal qty;
            if (!decimal.TryParse(txtQty.Text, out qty))
            {
                AlertHelper.ShowSweetAlert(this, "Invalid Quantity captured. Unable to continue.", "warning");
                return;
            }

            long rowid, ItemID;
            if (!long.TryParse(lbtn.CommandArgument.ToString(), out rowid) || !long.TryParse(ddl.SelectedValue, out ItemID))
            {
                AlertHelper.ShowSweetAlert(this, "Invalid row or item ID.", "error");
                return;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var ThisFCLine = _db.WorksOrderLines.FirstOrDefault(x => x.CompanyID == CoID && x.LineID == rowid);
                if (ThisFCLine == null)
                {
                    AlertHelper.ShowSweetAlert(this, "Works order line not found.", "error");
                    return;
                }

                ThisFCLine.SelectionId = ItemID;
                ThisFCLine.LineType = (short)(ddlt.SelectedValue != null ? Convert.ToInt16(ddlt.SelectedValue) : 0);
                ThisFCLine.CompanyID = CoID;
                ThisFCLine.Quantity = qty;
                
                ThisFCLine.ItemDescription = txtDescription.Text?.Trim() ?? "";
                ThisFCLine.DueDelDate = dt;
                ThisFCLine.Active = true;

                if (!string.IsNullOrWhiteSpace(txtLotNum.Text))
                {
                    string lotNumN = txtLotNum.Text.Trim();
                    var thislot = _db.LotTrackingMasters.FirstOrDefault(x => x.CompanyID == CoID && x.LotNumber == lotNumN);
                    if (thislot == null)
                    {
                        ThisFCLine.LotNumber = lotNumN;
                        LotTrackingMaster LtNew = new LotTrackingMaster
                        {
                            LotNumber = ThisFCLine.LotNumber,
                            CreatedDate = DateTime.Now,
                            CompanyID = CurrentUser.CoID,
                            ItemCode = ThisFCLine.ItemCode,
                            ItemId = ItemID,
                            LotActive = true,
                            LotQuantity = 0
                        };
                        _db.LotTrackingMasters.Add(LtNew);
                    }
                    else
                    {
                        AlertHelper.ShowSweetAlert(this, $"Lot number {lotNumN} already in use. Please enter a different one", "error");
                        return;
                    }
                }
                else
                {
                    ThisFCLine.LotNumber = null;
                }

                try
                {
                    var item = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CoID && x.ID == ItemID);
                    ThisFCLine.ItemCode = item.Code.ToString()?? "";
                    ThisFCLine.IsLotTracked = item != null ? item.IsLotTracked : false;
                }
                catch { ThisFCLine.IsLotTracked = false; }

                // Remove old RM lines
                var Rmd = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LinkedWOLineID == rowid).ToList();
                if (Rmd.Any())
                    _db.WorksOrderRMLines.RemoveRange(Rmd);

                // Add new RM lines based on LineType
                if (ThisFCLine.LineType == 1)
                {
                    var itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CoID && x.ID == ThisFCLine.SelectionId);
                    WorksOrderRMLine RML = new WorksOrderRMLine
                    {
                        LinkedWOLineID = (int)rowid,
                        WOID = woid,
                        SelectionId = ThisFCLine.SelectionId,
                        ItemCode = itm.Code,
                        ItemDescription = ThisFCLine.ItemDescription,
                        Quantity = ThisFCLine.Quantity,
                        CompanyID = CoID,
                        LinkedFGSelectionID = ThisFCLine.SelectionId,
                        LinkedFGCode = ThisFCLine.ItemCode,
                        LinkedFGQty = ThisFCLine.Quantity,
                        PickComplete = false,
                        Unit = itm.Unit,
                        IsLotTracked = itm.IsLotTracked,
                        UnitCost = (itm.AverageCost / itm.UOMConvert) ?? 0,
                        Physical = itm?.Physical ?? false
                    };
                    _db.WorksOrderRMLines.Add(RML);
                }
                if (ThisFCLine.LineType == 2)
                {
                    int bmc = _db.BOMHeaders.Where(x => x.CompanyID == CoID && x.FGID == ThisFCLine.SelectionId).Select(x => x.BomHID).FirstOrDefault();
                    var BomLines = _db.GetBOMLinesFromBomHeaderID(bmc, CoID);
                    if (BomLines != null)
                    {
                        foreach (var bl in BomLines)
                        {
                            if (bl.ItemID != null)
                            {
                                var itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CoID && x.ID == bl.ItemID);
                                decimal AvCostPerUnit = 0;
                                try
                                {
                                    AvCostPerUnit = itm != null ? (itm.AverageCost / itm.UOMConvert) ?? 0 : 0;
                                }
                                catch { }
                                WorksOrderRMLine RML = new WorksOrderRMLine
                                {
                                    LinkedWOLineID = (int)rowid,
                                    WOID = woid,
                                    SelectionId = (long)bl.ItemID,
                                    ItemCode = bl.ItemCode ?? "",
                                    ItemDescription = bl.Description ?? "",
                                    Quantity = Convert.ToDecimal(ThisFCLine.Quantity) * Convert.ToDecimal(bl.RMQty),
                                    CompanyID = CoID,
                                    LinkedFGSelectionID = ThisFCLine.SelectionId,
                                    LinkedFGCode = ThisFCLine.ItemCode,
                                    LinkedFGQty = ThisFCLine.Quantity,
                                    PickComplete = false,
                                    Unit = itm?.Unit ?? "",
                                    IsLotTracked = itm?.IsLotTracked ?? false,
                                    UnitCost = AvCostPerUnit,
                                    Physical = itm?.Physical ?? false
                                };
                                _db.WorksOrderRMLines.Add(RML);
                            }
                        }
                    }
                }
                if (ThisFCLine.LineType == 3)
                {
                    string kmc = _db.KitHeaders.Where(x => x.CompanyID == CoID && x.FGID == ThisFCLine.SelectionId).Select(x => x.KitCode).FirstOrDefault();
                    var KitLines = _db.GetKitLinesFromKitCode(kmc, CoID);
                    if (KitLines != null)
                    {
                        foreach (var bl in KitLines)
                        {
                            var itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CoID && x.ID == bl.ItemID);
                            WorksOrderRMLine RML = new WorksOrderRMLine
                            {
                                LinkedWOLineID = (int)rowid,
                                WOID = woid,
                                SelectionId = (long)bl.ItemID,
                                ItemCode = bl.ItemCode ?? "",
                                ItemDescription = bl.Description ?? "",
                                Quantity = Convert.ToDecimal(ThisFCLine.Quantity) * Convert.ToDecimal(bl.FGQty),
                                CompanyID = CoID,
                                LinkedFGSelectionID = ThisFCLine.SelectionId,
                                LinkedFGCode = ThisFCLine.ItemCode,
                                LinkedFGQty = ThisFCLine.Quantity,
                                PickComplete = false,
                                Unit = itm?.Unit ?? "",
                                IsLotTracked = itm?.IsLotTracked ?? false,
                                UnitCost = (itm.AverageCost / itm.UOMConvert)  ?? 0,
                                Physical = itm?.Physical ?? false
                            };
                            _db.WorksOrderRMLines.Add(RML);
                        }
                    }
                }

                try
                {
                    _db.SaveChanges();
                }
                catch (Exception ex)
                {
                    AlertHelper.ShowSweetAlert(this, "Error saving changes: " + ex.Message, "error");
                    return;
                }

                LoadWOLines();
                if (string.IsNullOrWhiteSpace(lblFCRef.Text))
                {
                    AlertHelper.ShowSweetAlert(this,
                        "Line saved. Please capture a Reference for this Works Order before exiting.",
                        "warning");
                }
                else
                {
                    AlertHelper.ShowSweetAlert(this, "Successfully Saved.", "success");
                }
            }
        }

        protected void lbtnDeleteLine_Click(object sender, EventArgs e)
        {
            LinkButton lbtn = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtn.NamingContainer;
            long rowid = Convert.ToInt32(lbtn.CommandArgument);
           
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var ThisWOLine = _db.WorksOrderLines.Where(x => x.CompanyID == CoID && x.LineID == rowid);
                _db.WorksOrderLines.RemoveRange(ThisWOLine);

                var Rmd = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LinkedWOLineID == rowid).ToList();
                _db.WorksOrderRMLines.RemoveRange(Rmd);

                _db.SaveChanges();
                LoadWOLines();
                string message = "Line successfully Deleted";
                AlertHelper.ShowSweetAlert(this, message, "success");
            }
        }

      protected void lbtnComment_Click(object sender, EventArgs e)
        {
            LinkButton lbtn = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtn.NamingContainer;
            long rowid = Convert.ToInt32(lbtn.CommandArgument);
            lblLineid.Text = rowid.ToString();
            Button25_ModalPopupExtender.Show();
        }

        protected void btnSaveComment_Click(object sender, EventArgs e)
        {
            long rowid = Convert.ToInt32(lblLineid.Text);

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var ThisWOLine = _db.WorksOrderLines.Where(x => x.CompanyID == CoID && x.LineID == rowid).FirstOrDefault();
                ThisWOLine.Comments = txtMsgBody.Text.Trim().ToString();
                _db.SaveChanges();
                LoadWOLines();
            }
        }

        protected void lbtnShowBOM_Click(object sender, EventArgs e)
        {
            LinkButton lbtn = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtn.NamingContainer;
            TextBox txtQty = (TextBox)row.FindControl("txtQty");
            TextBox txtDescription = (TextBox)row.FindControl("txtDescription");
            DropDownList ddT = (DropDownList)row.FindControl("DDBOMKIT");

            int lineid = Convert.ToInt32(lbtn.CommandArgument);
            decimal ManfQty;

            try
            {
                ManfQty = Convert.ToDecimal(txtQty.Text);
            } catch
            {
                string message = "Invalid manufacturing quantity, Unable to calculate.";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var MLines = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LinkedWOLineID == lineid).ToList();
                foreach (var ln in MLines)
                {
                    ln.Quantity = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(ln.Quantity, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
                    ln.ScrapQty = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(ln.ScrapQty, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
                    ln.UseQty = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(ln.UseQty, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
                }
                GridUseBom.DataSource = MLines;
                GridUseBom.DataBind();

                lblItem.Text = txtDescription.Text.ToString();
                lblQty.Text = "Qty: " + ManfQty.ToString();
                ModalPopupExtender1.Show();
            }
        }

        private class BoMLine
        {
            public long BLID { get; set; }
            public string ItemCode { get; set; }
            public string BomCode { get; set; }
            public long ItemID { get; set; }
            public string Description { get; set; }
            public decimal RMQty { get; set; }
            public decimal AvCost { get; set; }
            public decimal AvRMCost { get; set; }

            public decimal UseQty { get; set; }

        }

        protected void LinkButton2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(lblFCRef.Text))
            {
                AlertHelper.ShowSweetAlert(this,
                    "Please capture a Reference for this Works Order before exiting.",
                    "warning");
                return;
            }
            Response.Redirect("~/WorksOrdersHeaders.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(lblFCRef.Text))
            {
                AlertHelper.ShowSweetAlert(this,
                    "Please capture a Reference for this Works Order before exiting.",
                    "warning");
                return;
            }
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(lblFCRef.Text))
            {
                AlertHelper.ShowSweetAlert(this,
                    "Please capture a Reference for this Works Order before exiting.",
                    "warning");
                return;
            }
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

        protected void LoadWorkStations()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var WoProcs = _db.WorkStations.Where(x => x.CompanyID == CurrentUser.CoID).OrderBy(x => x.Seq).ToList();
                if (WoProcs != null)
                {
                    DDStatus.DataSource = WoProcs;
                    DDStatus.DataTextField = "WSName";
                    DDStatus.DataValueField = "WSName";
                    DDStatus.DataBind();
                    DDStatus.Items.Insert(0, "- Select -");
                    DDStatus.Items.Insert(1, "NEW");
                }
            }
        }

        protected void DDBOMKIT_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            GridViewRow row = (GridViewRow)ddl.NamingContainer;
            DropDownList ddlItemCode = new DropDownList();
            ddlItemCode = (DropDownList)row.FindControl("DDItemCode");

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (ddl.SelectedValue == "1")
                {
                    if (ddlItemCode != null)
                    {
                        _items = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CoID && i.Physical == true && i.IsFinishedGoods == true).OrderBy(x => x.Code).ToList();
                        ddlItemCode.DataSource = _items.Select(i => new { ID = i.ID, DisplayText = i.Code + " - " + i.Description }).ToList();
                        ddlItemCode.DataTextField = "DisplayText";
                        ddlItemCode.DataValueField = "ID";
                        ddlItemCode.DataBind();
                        ddlItemCode.Items.Insert(0, new ListItem("Select", "0"));
                    }
                }
                else if (ddl.SelectedValue == "2")
                {
                    if (ddlItemCode != null)
                    {
                        _boms = _db.BOMHeaders.Where(i => i.BomActive == true && i.CompanyID == CoID).OrderBy(x => x.BOMCode).ToList();
                        ddlItemCode.DataSource = _boms.Select(i => new { ID = i.FGID, DisplayText = i.FGCode + " - " + i.FGDescript }).ToList();
                        ddlItemCode.DataTextField = "DisplayText";
                        ddlItemCode.DataValueField = "ID";
                        ddlItemCode.DataBind();
                        ddlItemCode.Items.Insert(0, new ListItem("Select", "0"));
                    }
                }
                else if (ddl.SelectedValue == "3")
                {
                    if (ddlItemCode != null)
                    {
                        _kits = _db.KitHeaders.Where(i => i.KitActive == true && i.CompanyID == CoID).OrderBy(x => x.KitHID).ToList();
                        ddlItemCode.DataSource = _kits.Select(i => new { ID = i.FGID, DisplayText = i.FGCode + " - " + i.FGDescript }).ToList(); ;
                        ddlItemCode.DataTextField = "DisplayText";
                        ddlItemCode.DataValueField = "ID";
                        ddlItemCode.DataBind();
                        ddlItemCode.Items.Insert(0, new ListItem("Select", "0"));
                    }
                }
            }

        }

        protected void lbtnMLineSave_Click(object sender, EventArgs e)
        {
            LinkButton lbtn = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtn.NamingContainer;

            TextBox txtMQty = (TextBox)row.FindControl("txtMQty");
            Decimal ManfQty = 0;
            try
            {
                ManfQty = Convert.ToDecimal(txtMQty.Text);
            }
            catch
            {
                string message = "Invalid quantity, Unable to calculate.";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }

            int Mlineid = Convert.ToInt32(lbtn.CommandArgument);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var MLine = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LineID == Mlineid).FirstOrDefault();
                MLine.Quantity = ManfQty;
                _db.SaveChanges();
                ModalPopupExtender1.Show();
            }
        }

        protected void lbtnMRPThis_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var MLines = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.WOID == woid).ToList();
                foreach (var ln in MLines)
                {
                    ln.Quantity = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(ln.Quantity, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
                    ln.ScrapQty = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(ln.ScrapQty, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
                    ln.UseQty = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(ln.UseQty, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
                }
                GridUseBom.DataSource = MLines;
                GridUseBom.DataBind();
                lblItem.Visible = false;
                lblQty.Visible = false;
                ModalPopupExtender1.Show();
            }
        }

        protected void lbtnWOPrint_Click(object sender, EventArgs e)
        {
           
            if (lblFCRef.Text.ToString().Trim().Length < 1)
            {
                string message = "Please capture a Reference before continuing.";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }

            try
            {
                DateTime dt = Convert.ToDateTime(Convert.ToDateTime(txtDueDate.Text));
            }
            catch
            {
                string message = "Invalid Due Date, please select a valid date";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }

            // SAVE LAST FILLED WORK ORDER LINE FIRST
            SaveLastFilledWOLine();

            LbtnSaveWO_Click(sender, EventArgs.Empty);
            Response.Redirect($"~/WorksOrderPDFCreate.aspx?woid={woid}", true);
        }

        private void SaveLastFilledWOLine()
        {
            GridViewRow lastFilledRow = null;

            foreach (GridViewRow row in GridWOLines.Rows)
            {
                // Adjust field names exactly as they exist in your grid
                TextBox txtQty = row.FindControl("txtQty") as TextBox;
                DropDownList DDItemCode = row.FindControl("DDItemCode") as DropDownList;

                if (txtQty.Text != null && DDItemCode != null)
                {
                    bool hasQty = !string.IsNullOrWhiteSpace(txtQty.Text);
                    bool hasItem = DDItemCode.SelectedIndex > 0;

                    if (hasQty && hasItem)
                    {
                        lastFilledRow = row; // keep overwriting until LAST filled row
                    }
                }
            }

            if (lastFilledRow != null)
            {
                LinkButton btnSave = lastFilledRow.FindControl("lbtnLineSave") as LinkButton;
                if (btnSave != null)
                {
                    // Fire the SAME logic as an actual Save Line click
                    lbtnLineSave_Click(btnSave, EventArgs.Empty);
                }
            }
        }
    }
}