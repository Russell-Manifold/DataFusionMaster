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
    /// <summary>
    /// Edit Components - tailors the raw-material list of ONE works order line.
    ///
    /// The BOM/Kit master is a template: it seeds the component list when the works order
    /// line is created. This screen edits the copy held on the works order (WorksOrderRMLines)
    /// so an operator can match the components to what is actually being supplied - change a
    /// quantity, add an item the standard recipe does not carry, or substitute one item for
    /// another. The BOM/Kit master is never written to, so other works orders are unaffected.
    ///
    /// Saving sets WorksOrderLine.RMCustomised, which stops WorksOrdersDetailed rebuilding
    /// this line from the BOM/Kit on the next line save and wiping the edits.
    ///
    /// Editing stops once anything has been drawn: components that have already moved stock
    /// (and posted to Sage) must not be altered underneath the manufacture screen.
    /// </summary>
    public partial class WorksOrdersRMEdit : BasePage
    {
        long CoID;
        long woid = 0;
        int woLineID = 0;
        bool IsLocked = false;

        // Component types offered on this screen. Stored on WorksOrderRMLine.LineType.
        private const int TypeItem = 1;
        private const int TypeBOM = 2;
        private const int TypeKit = 3;
        private const int TypeService = 4;
        private const int TypeAccount = 5;

        private class PickListEntry
        {
            public long ID { get; set; }
            public string DisplayText { get; set; }
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
            if (CurrentUser.ExpiryDate <= DateTime.Now)
            {
                Response.Redirect("~/Dashboard.aspx?exp=true", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }

            CoID = CurrentUser.CoID;

            // Both ids are required - without the works order LINE there is no component list
            // to edit, and guessing one would edit the wrong line's materials.
            if (!long.TryParse(Request.QueryString["woid"], out woid) ||
                !int.TryParse(Request.QueryString["line"], out woLineID) ||
                woid <= 0 || woLineID <= 0)
            {
                Response.Redirect("~/WorksOrdersHeaders.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                LoadHeader();
                BindGrid();
            }
            else
            {
                // The lock state gates the save/delete handlers on postback too, so it has to
                // be re-established on every request, not just the first.
                SetLockState();
            }
        }

        /// <summary>
        /// Read-only once any component on this line has been drawn, or the line/order is closed.
        /// </summary>
        private void SetLockState()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var line = _db.WorksOrderLines.FirstOrDefault(x => x.CompanyID == CoID && x.LineID == woLineID && x.WOID == woid);
                if (line == null)
                {
                    IsLocked = true;
                    return;
                }

                bool drawn = _db.WorksOrderRMLines.Any(x => x.CompanyID == CoID
                                                         && x.LinkedWOLineID == woLineID
                                                         && (x.PickComplete == true || x.ItemTransLineID != null));
                bool lineDone = line.Complete == true;
                bool orderClosed = _db.WorksOrderHeaders.Any(x => x.CompanyID == CoID && x.ID == woid && x.Active == false);

                IsLocked = drawn || lineDone || orderClosed;

                if (IsLocked)
                {
                    lblLocked.Text = drawn
                        ? "Components have already been drawn against this line - they can no longer be edited."
                        : (lineDone
                            ? "This works order line has been manufactured and can no longer be edited."
                            : "This works order is closed, no further changes allowed.");
                    lbtnSave.Visible = false;
                    lbtnRebuild.Visible = false;
                    GridRMLines.Enabled = false;
                }
            }
        }

        private void LoadHeader()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var header = _db.WorksOrderHeaders.FirstOrDefault(x => x.CompanyID == CoID && x.ID == woid);
                woheader.InnerText = header != null ? "WO-" + header.WONum + "  -  Edit Components" : "Edit Components";

                var line = _db.WorksOrderLines.FirstOrDefault(x => x.CompanyID == CoID && x.LineID == woLineID && x.WOID == woid);
                if (line != null)
                {
                    lblFGCode.Text = line.ItemCode ?? "";
                    lblFGDescription.Text = line.ItemDescription ?? "";
                    lblFGQty.Text = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(
                        Convert.ToDecimal(line.Quantity ?? 0, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces)).ToString("N2");
                    lblDueDate.Text = line.DueDelDate != null
                        ? Convert.ToDateTime(line.DueDelDate, CultureInfo.InvariantCulture).ToString("dd MMM yyyy")
                        : "";
                    lblCustomised.Text = line.RMCustomised ? "(edited for this works order)" : "(as per BOM/Kit)";
                }
            }

            SetLockState();
        }

        /// <summary>
        /// Binds the saved components plus one blank row for adding. The blank row is a
        /// placeholder only (LineID 0) and is never written to the database until an item is
        /// chosen on it - a half-filled component row would otherwise reach the manufacture
        /// screen with no item behind it.
        /// </summary>
        private void BindGrid()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var rmLines = _db.WorksOrderRMLines
                    .Where(x => x.CompanyID == CoID && x.LinkedWOLineID == woLineID)
                    .OrderBy(x => x.LineID)
                    .ToList();

                foreach (var rm in rmLines)
                {
                    rm.Quantity = ApiUrlCall.NumberToDecimal(
                        Convert.ToDecimal(rm.Quantity ?? 0, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
                }

                if (!IsLocked)
                {
                    rmLines.Add(new WorksOrderRMLine
                    {
                        LineID = 0,
                        WOID = woid,
                        CompanyID = CoID,
                        LinkedWOLineID = woLineID,
                        LineType = TypeItem,
                        ItemDescription = "",
                        Unit = "",
                        Quantity = null
                    });
                }

                GridRMLines.DataSource = rmLines;
                GridRMLines.DataBind();
            }
        }

        protected void GridRMLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            // Column 0 carries LineID for the handlers; never shown.
            e.Row.Cells[0].Visible = false;

            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var rml = e.Row.DataItem as WorksOrderRMLine;
            if (rml == null) return;

            var ddType = e.Row.FindControl("DDLineType") as DropDownList;
            var ddItem = e.Row.FindControl("DDItemCode") as DropDownList;
            var lblOnHand = e.Row.FindControl("lblOnHand") as Label;
            var lblShort = e.Row.FindControl("lblShort") as Label;
            var lbtnDelete = e.Row.FindControl("lbtnDeleteLine") as LinkButton;

            int lineType = rml.LineType ?? TypeItem;
            if (ddType != null && ddType.Items.FindByValue(lineType.ToString()) != null)
            {
                ddType.SelectedValue = lineType.ToString();
            }

            if (ddItem != null)
            {
                PopulateItemDropdown(ddItem, lineType);
                string sel = rml.SelectionId.ToString();
                ddItem.SelectedValue = ddItem.Items.FindByValue(sel) != null ? sel : "0";
            }

            // The blank add-row has nothing to delete yet.
            if (rml.LineID == 0 && lbtnDelete != null) lbtnDelete.Visible = false;

            // On Hand / Short: stock-holding items only. Services and GL accounts hold no stock,
            // so a shortage figure against them would be meaningless.
            bool physical = rml.Physical ?? (lineType != TypeService && lineType != TypeAccount);
            if (rml.LineID == 0 || !physical)
            {
                if (lblOnHand != null) lblOnHand.Text = "";
                if (lblShort != null) lblShort.Text = "";
                return;
            }

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

        /// <summary>
        /// The pick list behind a component row. BOM and Kit list the finished-good ITEM behind
        /// each recipe, so the stored SelectionId is always an ItemsMaster id - except for a GL
        /// account, which is an AccountsMaster id and never touches stock.
        /// </summary>
        private void PopulateItemDropdown(DropDownList ddl, int lineType)
        {
            if (ddl == null) return;

            var list = new List<PickListEntry>();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                switch (lineType)
                {
                    case TypeBOM:
                        list = _db.BOMHeaders.Where(x => x.CompanyID == CoID && x.BomActive == true)
                            .OrderBy(x => x.BOMCode)
                            .Select(x => new PickListEntry { ID = x.FGID ?? 0, DisplayText = x.FGCode + " - " + x.FGDescript })
                            .ToList();
                        break;

                    case TypeKit:
                        list = _db.KitHeaders.Where(x => x.CompanyID == CoID && x.KitActive == true)
                            .OrderBy(x => x.KitCode)
                            .Select(x => new PickListEntry { ID = x.FGID ?? 0, DisplayText = x.FGCode + " - " + x.FGDescript })
                            .ToList();
                        break;

                    case TypeService:
                        list = _db.ItemsMasters.Where(x => x.CompanyID == CoID && x.Active == true && x.Physical == false)
                            .OrderBy(x => x.Code)
                            .Select(x => new PickListEntry { ID = x.ID, DisplayText = x.Code + " - " + x.Description })
                            .ToList();
                        break;

                    case TypeAccount:
                        list = _db.AccountsMasters.Where(x => x.CompanyID == CoID)
                            .OrderBy(x => x.AccountName)
                            .Select(x => new PickListEntry { ID = x.AccountID ?? 0, DisplayText = x.AccountName })
                            .ToList();
                        break;

                    default: // TypeItem
                        list = _db.ItemsMasters.Where(x => x.CompanyID == CoID && x.Active == true && x.Physical == true)
                            .OrderBy(x => x.Code)
                            .Select(x => new PickListEntry { ID = x.ID, DisplayText = x.Code + " - " + x.Description })
                            .ToList();
                        break;
                }
            }

            ddl.DataSource = list;
            ddl.DataTextField = "DisplayText";
            ddl.DataValueField = "ID";
            ddl.CssClass = "item-search";   // searchable by code, description or keyword
            ddl.DataBind();
            ddl.Items.Insert(0, new ListItem("- Select -", "0"));
        }

        protected void DDLineType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var ddl = sender as DropDownList;
            if (ddl == null) return;
            var row = ddl.NamingContainer as GridViewRow;
            if (row == null) return;

            var ddItem = row.FindControl("DDItemCode") as DropDownList;
            var txtDescription = row.FindControl("txtDescription") as TextBox;
            var lblUnit = row.FindControl("lblUnit") as Label;

            int lineType;
            if (!int.TryParse(ddl.SelectedValue, out lineType)) lineType = TypeItem;

            // Only this row is touched - rebinding the grid here would discard quantities the
            // operator has typed into the other rows but not yet saved.
            PopulateItemDropdown(ddItem, lineType);
            if (txtDescription != null) txtDescription.Text = "";
            if (lblUnit != null) lblUnit.Text = "";
        }

        protected void DDItemCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            var ddl = sender as DropDownList;
            if (ddl == null) return;
            var row = ddl.NamingContainer as GridViewRow;
            if (row == null) return;

            var ddType = row.FindControl("DDLineType") as DropDownList;
            var txtDescription = row.FindControl("txtDescription") as TextBox;
            var lblUnit = row.FindControl("lblUnit") as Label;

            long selectionId;
            if (!long.TryParse(ddl.SelectedValue, out selectionId) || selectionId <= 0) return;

            int lineType;
            if (ddType == null || !int.TryParse(ddType.SelectedValue, out lineType)) lineType = TypeItem;

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (lineType == TypeAccount)
                {
                    var acct = _db.AccountsMasters.FirstOrDefault(x => x.CompanyID == CoID && x.AccountID == selectionId);
                    if (acct != null && txtDescription != null) txtDescription.Text = acct.AccountName ?? "";
                    if (lblUnit != null) lblUnit.Text = "";
                }
                else
                {
                    var itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CoID && x.ID == selectionId);
                    if (itm != null)
                    {
                        if (txtDescription != null) txtDescription.Text = itm.Description ?? "";
                        if (lblUnit != null) lblUnit.Text = itm.Unit ?? "";
                    }
                }
            }
        }

        protected void lbtnLineSave_Click(object sender, EventArgs e)
        {
            var lbtn = sender as LinkButton;
            if (lbtn == null) return;
            var row = lbtn.NamingContainer as GridViewRow;
            if (row == null) return;

            if (IsLocked)
            {
                AlertHelper.ShowSweetAlert(this, "These components can no longer be edited.", "warning");
                return;
            }

            string err;
            if (!SaveRow(row, out err))
            {
                if (!string.IsNullOrEmpty(err)) AlertHelper.ShowSweetAlert(this, err, "warning");
                return;
            }

            MarkCustomised();
            LoadHeader();
            BindGrid();
        }

        protected void lbtnSave_Click(object sender, EventArgs e)
        {
            if (IsLocked)
            {
                AlertHelper.ShowSweetAlert(this, "These components can no longer be edited.", "warning");
                return;
            }

            foreach (GridViewRow row in GridRMLines.Rows)
            {
                string err;
                if (!SaveRow(row, out err) && !string.IsNullOrEmpty(err))
                {
                    AlertHelper.ShowSweetAlert(this, err, "warning");
                    return;
                }
            }

            MarkCustomised();
            Response.Redirect("~/WorksOrdersDetailed.aspx?woid=" + woid, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        /// <summary>
        /// Saves one grid row. Returns false with an empty message when the row is simply the
        /// untouched blank add-row (nothing to do, not an error).
        /// </summary>
        private bool SaveRow(GridViewRow row, out string error)
        {
            error = "";

            var ddType = row.FindControl("DDLineType") as DropDownList;
            var ddItem = row.FindControl("DDItemCode") as DropDownList;
            var txtDescription = row.FindControl("txtDescription") as TextBox;
            var txtQty = row.FindControl("txtQty") as TextBox;
            var lbtnSaveRow = row.FindControl("lbtnLineSave") as LinkButton;

            if (ddType == null || ddItem == null || txtDescription == null || txtQty == null || lbtnSaveRow == null)
            {
                error = "One or more controls are missing.";
                return false;
            }

            int rmLineID;
            int.TryParse(lbtnSaveRow.CommandArgument, out rmLineID);

            long selectionId;
            long.TryParse(ddItem.SelectedValue, out selectionId);

            bool isNew = rmLineID == 0;

            // Untouched blank add-row: nothing chosen, nothing typed - skip it silently.
            if (isNew && selectionId <= 0 && string.IsNullOrWhiteSpace(txtQty.Text)) return false;

            if (selectionId <= 0)
            {
                error = "Please select an item before saving this component.";
                return false;
            }

            decimal qty;
            if (!decimal.TryParse(txtQty.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out qty) || qty <= 0)
            {
                error = "Please capture a quantity greater than zero for " + txtDescription.Text.Trim() + ".";
                return false;
            }

            int lineType;
            if (!int.TryParse(ddType.SelectedValue, out lineType)) lineType = TypeItem;

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var parent = _db.WorksOrderLines.FirstOrDefault(x => x.CompanyID == CoID && x.LineID == woLineID && x.WOID == woid);
                if (parent == null)
                {
                    error = "Works order line not found.";
                    return false;
                }

                WorksOrderRMLine rm;
                if (isNew)
                {
                    rm = new WorksOrderRMLine
                    {
                        WOID = woid,
                        CompanyID = CoID,
                        LinkedWOLineID = woLineID,
                        LinkedFGSelectionID = parent.SelectionId,
                        LinkedFGCode = parent.ItemCode,
                        LinkedFGQty = parent.Quantity,
                        PickComplete = false,
                        UseQty = 0,
                        ScrapQty = 0,
                        RejectQty = 0
                    };
                    _db.WorksOrderRMLines.Add(rm);
                }
                else
                {
                    rm = _db.WorksOrderRMLines.FirstOrDefault(x => x.CompanyID == CoID && x.LineID == rmLineID && x.LinkedWOLineID == woLineID);
                    if (rm == null)
                    {
                        error = "Component line not found.";
                        return false;
                    }
                    // Belt and braces: never rewrite a row that has already moved stock, even if
                    // the page-level lock was somehow bypassed.
                    if (rm.PickComplete == true || rm.ItemTransLineID != null)
                    {
                        error = "That component has already been drawn and cannot be changed.";
                        return false;
                    }
                }

                rm.LineType = lineType;
                rm.SelectionId = selectionId;
                rm.Quantity = qty;
                rm.ItemDescription = txtDescription.Text.Trim();

                if (lineType == TypeAccount)
                {
                    var acct = _db.AccountsMasters.FirstOrDefault(x => x.CompanyID == CoID && x.AccountID == selectionId);
                    rm.ItemCode = acct != null ? acct.AccountID.ToString() : selectionId.ToString();
                    if (string.IsNullOrWhiteSpace(rm.ItemDescription) && acct != null) rm.ItemDescription = acct.AccountName;
                    rm.Unit = "";
                    rm.Physical = false;      // holds no stock, so nothing is ever drawn for it
                    rm.IsLotTracked = false;
                    rm.UnitCost = 0;
                }
                else
                {
                    var itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CoID && x.ID == selectionId);
                    if (itm == null)
                    {
                        error = "Selected item not found.";
                        return false;
                    }
                    rm.ItemCode = itm.Code ?? "";
                    if (string.IsNullOrWhiteSpace(rm.ItemDescription)) rm.ItemDescription = itm.Description ?? "";
                    rm.Unit = itm.Unit ?? "";
                    rm.Physical = itm.Physical ?? false;
                    rm.IsLotTracked = itm.IsLotTracked;
                    rm.BarCode = itm.BarCode;
                    // Same cost basis the standard BOM explosion uses (WorksOrdersDetailed).
                    rm.UnitCost = (itm.UOMConvert != 0 ? (itm.AverageCost / itm.UOMConvert) : itm.AverageCost) ?? 0;
                }

                _db.SaveChanges();
            }

            return true;
        }

        protected void lbtnDeleteLine_Click(object sender, EventArgs e)
        {
            var lbtn = sender as LinkButton;
            if (lbtn == null) return;

            if (IsLocked)
            {
                AlertHelper.ShowSweetAlert(this, "These components can no longer be edited.", "warning");
                return;
            }

            int rmLineID;
            if (!int.TryParse(lbtn.CommandArgument, out rmLineID) || rmLineID <= 0) return;

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var rm = _db.WorksOrderRMLines.FirstOrDefault(x => x.CompanyID == CoID && x.LineID == rmLineID && x.LinkedWOLineID == woLineID);
                if (rm == null) return;
                if (rm.PickComplete == true || rm.ItemTransLineID != null)
                {
                    AlertHelper.ShowSweetAlert(this, "That component has already been drawn and cannot be removed.", "warning");
                    return;
                }
                _db.WorksOrderRMLines.Remove(rm);
                _db.SaveChanges();
            }

            MarkCustomised();
            LoadHeader();
            BindGrid();
        }

        /// <summary>
        /// Flags the works order line so WorksOrdersDetailed stops re-exploding it from the
        /// BOM/Kit master. Without this the next line save there wipes everything edited here.
        /// </summary>
        private void MarkCustomised()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var line = _db.WorksOrderLines.FirstOrDefault(x => x.CompanyID == CoID && x.LineID == woLineID && x.WOID == woid);
                if (line != null && !line.RMCustomised)
                {
                    line.RMCustomised = true;
                    _db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Discards the tailored components and rebuilds them from the BOM/Kit master - the way
        /// back out if an operator edits the wrong line.
        /// </summary>
        protected void lbtnRebuild_Click(object sender, EventArgs e)
        {
            if (IsLocked)
            {
                AlertHelper.ShowSweetAlert(this, "These components can no longer be edited.", "warning");
                return;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var line = _db.WorksOrderLines.FirstOrDefault(x => x.CompanyID == CoID && x.LineID == woLineID && x.WOID == woid);
                if (line == null) return;

                var existing = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LinkedWOLineID == woLineID).ToList();
                if (existing.Any(x => x.PickComplete == true || x.ItemTransLineID != null))
                {
                    AlertHelper.ShowSweetAlert(this, "Components have already been drawn against this line - it cannot be reset.", "warning");
                    return;
                }
                if (existing.Any()) _db.WorksOrderRMLines.RemoveRange(existing);

                var fgItem = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CoID && x.ID == line.SelectionId);
                decimal fgQty = line.Quantity ?? 0;

                // Mirrors the explosion in WorksOrdersDetailed.lbtnLineSave_Click:
                // type 1 draws the item itself, type 2 the BOM recipe, type 3 the Kit recipe.
                if (line.LineType == 1 && fgItem != null)
                {
                    _db.WorksOrderRMLines.Add(NewRmLine(line, fgItem.ID, fgItem.Code, line.ItemDescription, fgQty,
                                                        fgItem.Unit, fgItem.IsLotTracked, fgItem.Physical ?? false,
                                                        (fgItem.UOMConvert != 0 ? (fgItem.AverageCost / fgItem.UOMConvert) : fgItem.AverageCost) ?? 0));
                }
                else if (line.LineType == 2)
                {
                    int bmc = _db.BOMHeaders.Where(x => x.CompanyID == CoID && x.FGID == line.SelectionId).Select(x => x.BomHID).FirstOrDefault();
                    var bomLines = _db.GetBOMLinesFromBomHeaderID(bmc, CoID).ToList();
                    foreach (var bl in bomLines)
                    {
                        if (bl.ItemID == null) continue;
                        var itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CoID && x.ID == bl.ItemID);
                        _db.WorksOrderRMLines.Add(NewRmLine(line, (long)bl.ItemID, bl.ItemCode, bl.Description,
                                                            fgQty * Convert.ToDecimal(bl.RMQty),
                                                            itm != null ? itm.Unit : "", itm != null && itm.IsLotTracked,
                                                            itm != null && (itm.Physical ?? false),
                                                            itm != null ? ((itm.UOMConvert != 0 ? (itm.AverageCost / itm.UOMConvert) : itm.AverageCost) ?? 0) : 0));
                    }
                }
                else if (line.LineType == 3)
                {
                    string kmc = _db.KitHeaders.Where(x => x.CompanyID == CoID && x.FGID == line.SelectionId).Select(x => x.KitCode).FirstOrDefault();
                    if (kmc != null)
                    {
                        var kitLines = _db.GetKitLinesFromKitCode(kmc, CoID).Where(x => x.ItemID != null && x.ItemID > 0 && x.FGQty > 0).ToList();
                        foreach (var kl in kitLines)
                        {
                            var itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CoID && x.ID == kl.ItemID);
                            _db.WorksOrderRMLines.Add(NewRmLine(line, (long)kl.ItemID, kl.ItemCode, kl.Description,
                                                                fgQty * Convert.ToDecimal(kl.FGQty),
                                                                itm != null ? itm.Unit : "", itm != null && itm.IsLotTracked,
                                                                itm != null && (itm.Physical ?? false),
                                                                itm != null ? ((itm.UOMConvert != 0 ? (itm.AverageCost / itm.UOMConvert) : itm.AverageCost) ?? 0) : 0));
                        }
                    }
                }

                line.RMCustomised = false;
                _db.SaveChanges();
            }

            LoadHeader();
            BindGrid();
            AlertHelper.ShowSweetAlert(this, "Components reset to the BOM/Kit master.", "success");
        }

        private WorksOrderRMLine NewRmLine(WorksOrderLine parent, long selectionId, string itemCode, string description,
                                           decimal qty, string unit, bool isLotTracked, bool physical, decimal unitCost)
        {
            return new WorksOrderRMLine
            {
                WOID = parent.WOID,
                CompanyID = CoID,
                LinkedWOLineID = parent.LineID,
                SelectionId = selectionId,
                ItemCode = itemCode,
                ItemDescription = description,
                Quantity = qty,
                Unit = unit,
                IsLotTracked = isLotTracked,
                Physical = physical,
                UnitCost = unitCost,
                LinkedFGSelectionID = parent.SelectionId,
                LinkedFGCode = parent.ItemCode,
                LinkedFGQty = parent.Quantity,
                PickComplete = false,
                UseQty = 0,
                ScrapQty = 0,
                RejectQty = 0
            };
        }

        protected void lbtnBack_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/WorksOrdersDetailed.aspx?woid=" + woid, false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
