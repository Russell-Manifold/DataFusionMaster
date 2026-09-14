using iTextSharp.text;
using iTextSharp.text.pdf;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{ 
    public partial class PickingSlip : BasePage
    {
        long docid = 0;
        string docguid;
        private List<GetLinkedStoredFromItem_Result> _itemST;
        private List<GetActiveLotNumbersLinkedToStores_Result> _ActiveLotNums;
        // SBCALineID -> outstanding SO-line balance (QtyLeft ?? Quantity), for the read-only Qty_Left grid column.
        private Dictionary<long, decimal> _qtyLeftMap;
        protected void Page_Load(object sender, EventArgs e)
        {
            docguid = Request.QueryString["docid"];
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
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

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                _itemST = _db.GetLinkedStoredFromItem(CurrentUser.CoID).Where(x=>x.AllowPicking == true).OrderBy(x => x.StoreCode).ToList();
                _ActiveLotNums = _db.GetActiveLotNumbersLinkedToStores(CurrentUser.CoID).OrderBy(x=>x.LotNumber).ToList();
            }

            if (!IsPostBack)
            {
               if (CurrentUser.UseModule3 == true)
                {
                    DDOptions.Attributes.Add("style", "display:inline-block; color:#4282C1; font-size:1em; border: 1px #4282C1 solid; border-radius:.25em; margin-top:.5em");
                }
                else
                {
                    DDOptions.Style.Add("display", "none");
                }
               
                LoadDelivBy();    
                LoadPSHeader();
                BindGrid();
                LoadHistory();
                if (GridPSLines.Rows.Count == 0)
                {
                    AlertHelper.ShowSweetAlert(this, "No physical item lines to pick", "error");
                }
            }
        }

        protected void LoadPSHeader()
        {
           using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var thisPS = _db.GetOnePickingSlipFromDocHeaderID(CurrentUser.CoID, docguid).FirstOrDefault();
                if (thisPS != null)
                {
                    docid = thisPS.DocID;
                    lblDocID.Text = thisPS.DocID.ToString();
                    txtCustName.Text = thisPS.CustSupName.ToString();
                    lblPSid.Text = thisPS.PSID.ToString();
                    lblDocNum.Text = (thisPS.PSIntNumber ?? "").ToString();
                    txtRef.Text = (thisPS.Reference ?? "").ToString();
                    txtSODate.Text = Convert.ToDateTime(thisPS.DueDelDate).ToString("dd MMM yyyy");
                    txtSONum.Text = thisPS.DocumentNumber ?? "";
                    lblSOStatus.Text = "(" + (thisPS.Status ?? "").ToString() + ")";
                    if (thisPS.Status == "Invoiced")
                    {
                        lblSOStatus.ForeColor = System.Drawing.Color.Red;
                        lblSOStatus.Font.Bold = true; 
                        DDOptions.Attributes.Add("style", "display:none");
                    }
                    else
                        if (thisPS.Status == "Cancelled")
                    {
                        lblSOStatus.ForeColor = System.Drawing.Color.Orange;
                        lblSOStatus.Font.Bold = true;
                        DDOptions.Attributes.Add("style", "display:none");
                    }
                    txtAddress1.Text = thisPS.DelAddress1 ?? "";
                    txtAddress2.Text = thisPS.DelAddress2 ?? "";
                    //txtAddress3.Text = thisPS.DelAddress3 ?? "";
                    if (thisPS.DeliveryBy != null) { DDeliveryBy.Text = thisPS.DeliveryBy.ToString(); }
                    txtWMsg.Text = thisPS.PSPickMessage ?? "";
                    txtDMsg.Text = thisPS.PSDeliverMessage ?? "";
                    txtPMsg.Text = thisPS.PSPackMessage ?? "";
                    txtRep.Text = thisPS.SalesRepName ?? "";
                    txtIssuedTo.Text = thisPS.IssuedTo ?? "";
                    lblstatus.Text = thisPS.PSStatus ?? "";
                    if (thisPS.FromStoreID >0)
                    {
                        txtFromStore.Text = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreID == thisPS.FromStoreID).Select(x => x.StoreCode).FirstOrDefault();
                    }
                    else { txtFromStore.Text = "All"; }

                    if (thisPS.PSStartDate != null)
                    {
                        try
                        {
                            DateTime dt = Convert.ToDateTime(thisPS.PSStartDate, CultureInfo.InvariantCulture);
                            txtIssueDate.Text = dt.ToString("dd MMM yyyy");
                            txtIssuedTo.Text = "Picking";
                        }
                        catch { }

                    }
                    if (thisPS.Complete == true)
                    {
                        LbtnPickSave.Attributes.Add("style", "display:none");
                        LbtnSaveEdits.Attributes.Add("style", "display:none");
                        lbtnDelPS.Attributes.Add("style", "display:none");
                        lbtnIssue.Attributes.Add("style", "display:none");
                    }
                    
                    if (CurrentUser.UseModule3 == true)
                    {
                       if (thisPS.LinkedWONumber != 0)
                        {
                            var item = DDOptions.Items.FindByValue("2");
                            if (item != null) DDOptions.Items.Remove(item);

                            lblWoID.Text = thisPS.LinkedWONumber.ToString();
                            var WONum = _db.WorksOrderHeaders.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == thisPS.LinkedWONumber).WONum;
                            lbtnWOrd.Text = $" Open WO {WONum}";
                            //DDOptions.Style.Add("display", "none");
                        }
                        else
                        {
                            lbtnWOrd.Style.Add("display", "none");
                        }
                    }
                    else
                    {
                        DDOptions.Style.Add("display", "none");
                        lbtnWOrd.Style.Add("display", "none");
                    }
                }
            }
        }

        protected void BindGrid ()
        {
            ApplyLotColumnVisibility();
            long PsID = Convert.ToInt64(lblPSid.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var TempLines = _db.PickSlipLines.Where(x => x.PSID == PsID).OrderBy(x=>x.LineID).ToList();

                // Outstanding balance per SO line for the Qty_Left column (null QtyLeft = nothing fulfilled yet).
                _qtyLeftMap = new Dictionary<long, decimal>();
                if (long.TryParse(lblDocID.Text, out long DocID))
                {
                    _qtyLeftMap = _db.DocLines.Where(x => x.DocID == DocID && x.SBCALineID != 0)
                        .Select(x => new { x.SBCALineID, x.QtyLeft, x.Quantity }).ToList()
                        .GroupBy(x => x.SBCALineID)
                        .ToDictionary(g => g.Key, g => g.First().QtyLeft ?? g.First().Quantity ?? 0);
                }
                foreach (var itm in TempLines)
                {
                    if (itm.Quantity != null)
                    {
                        itm.Quantity = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(itm.Quantity.ToString(), CurrentUser.CompanyDecPlaces));
                    }
                    if (itm.PickQty != null)
                    {
                        itm.PickQty = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(itm.PickQty.ToString(), CurrentUser.CompanyDecPlaces));
                    }
                    else
                    {
                        itm.PickQty = itm.Quantity;
                    }
                }

                GridPSLines.DataSource = TempLines;
                GridPSLines.DataBind();
                // Column 5 = Qty_Left, column 6 = Pick_Qty. Hiding 6 hid the wrong one: Pick_Qty
                // is what the operator needs to see (and on a serial line it is the number of
                // serials ticked). Worse, the column is hidden AFTER DataBind, so txtPickQty was
                // still created but never rendered - it posted back empty, which is why
                // Convert.ToDecimal(txtPickQty.Text) threw and why the serial count never showed.
                GridPSLines.Columns[5].Visible = false;   // Qty_Left hidden - Pick_Qty is the one that matters here
            }
        }

        protected void lbtnLineSave_Click(object sender, EventArgs e)
        {
            long PsID = Convert.ToInt64(lblPSid.Text);
            long DocID = Convert.ToInt64(lblDocID.Text);
            decimal qoh = 0;
            decimal PriceExcl = 0, priceInclAdd = 0;
            LinkButton lbtnLineSave = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnLineSave.NamingContainer;
            DropDownList DDlotNum = (DropDownList)row.FindControl("DDlotNum");
            DropDownList DDStore = (DropDownList)row.FindControl("DDStore");
            TextBox txtPickQty = (TextBox)row.FindControl("txtPickQty");
            CheckBox chkComplete = (CheckBox)row.FindControl("chkComplete");
            Label lblLotNum = (Label)row.FindControl("lblLotNum");

            long ThisLineID = Convert.ToInt64(lbtnLineSave.CommandArgument);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var NewPSLine = _db.PickSlipLines.Where(x => x.LineID == ThisLineID).FirstOrDefault();

                // Resolved ONCE for this line and used throughout: four separate inline copies of
                // this query had to be kept in step, and each was a round-trip on the same row.
                long lineItemId = NewPSLine != null ? NewPSLine.SelectionId : 0;
                bool lineIsSerialItem = lineItemId > 0 && _db.ItemsMasters
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.ID == lineItemId)
                    .Select(x => x.IsSerialTracked).FirstOrDefault();

                int FrmStorid = 0;
                if (NewPSLine.LineType == 0)
                {
                    FrmStorid = _db.Stores.Where(x => x.StoreCode == DDStore.SelectedValue && x.CompanyID == CurrentUser.CoID).Select(x => x.StoreID).FirstOrDefault();

                    #region CheckStockOnHand
                    if (chkComplete.Checked)
                    {

                        // NOT on a serial line. Its lot dropdown is hidden but still bound, and
                        // still holds the line's first picked serial - so this asked "how much of
                        // that ONE serial is on hand?", got 1 (a serial always holds exactly one
                        // unit, unlike a lot), compared it against a pick of 20 and refused. A
                        // serial line has to be measured against the item's total in the store,
                        // which is what the else branch already does.
                        if (!lineIsSerialItem && DDlotNum.SelectedIndex > 0)
                        {
                            var ItmT = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == NewPSLine.SelectionId && x.ToID == FrmStorid && x.DocumentID != PsID && x.LotNumber == DDlotNum.SelectedValue);
                            if (ItmT.Any())
                            {
                                qoh = (decimal)ItmT.Sum(x => x.Qty);
                                var lastTrn = ItmT.OrderByDescending(x => x.TrnID).FirstOrDefault();
                                PriceExcl = (decimal)lastTrn.PriceExclusive;
                                priceInclAdd = (decimal)lastTrn.TotalUnitPriceExclInclAdd;
                            }
                        }
                        else
                        {
                            var ItmT = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == NewPSLine.SelectionId && x.ToID == FrmStorid && x.DocumentID != PsID);
                            if (ItmT.Any())
                            {
                                qoh = (decimal)ItmT.Sum(x => x.Qty);
                                var lastTrn = ItmT.OrderByDescending(x => x.TrnID).FirstOrDefault();
                                PriceExcl = (decimal)lastTrn.PriceExclusive;
                                priceInclAdd = (decimal)lastTrn.TotalUnitPriceExclInclAdd;
                            }
                        }
                        // Back-order: only require enough stock for the quantity actually being picked,
                        // not the full ordered quantity (a short pick is allowed and back-ordered).
                        decimal reqPickQty = (decimal)NewPSLine.Quantity;
                        try { decimal _p = Convert.ToDecimal(txtPickQty.Text.TrimEnd()); if (_p > 0) reqPickQty = _p; } catch { }
                        if (qoh < reqPickQty)
                        {
                            var ItmServ = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == NewPSLine.SelectionId);
                            if (ItmServ.Physical == true)
                            {
                                chkComplete.Checked = false;
                                AlertHelper.ShowSweetAlert(this, $"Insufficient quantity in store for {NewPSLine.ItemCode} only {qoh} available. Unable to save", "error");
                                DDlotNum.SelectedIndex = -1;
                                return;
                            }
                        }

                    }
                    #endregion

                    #region CheckIfLotTracked
                    // update existing line
                    if (NewPSLine.LineType == 0)
                    {
                        if (NewPSLine.IsLotTracked == true)
                        {
                            // A serial line's LotNumber holds its first picked serial, and its
                            // dropdown is hidden but still bound - so this block, which clears
                            // LotNumber when nothing is selected, would wipe it.
                            if (!lineIsSerialItem
                                && (DDlotNum.SelectedItem == null || DDlotNum.SelectedItem.Value.ToLower().Contains("number") || DDlotNum.Items.Count == 0))
                            {
                                if (DDlotNum.SelectedItem.Value.ToLower().Contains("number"))
                                {
                                    NewPSLine.LotNumber = string.Empty;
                                    _db.SaveChanges();
                                }
                                else
                                {
                                    chkComplete.Checked = false;
                                    AlertHelper.ShowSweetAlert(this, row.Cells[1].Text + " is Lot Tracked, select a valid lot number before continuing.", "error");
                                    return;
                                }
                            }
                        }
                    }
                }

                    #endregion

                NewPSLine.PickComplete = chkComplete.Checked;
                decimal pickqty = 0;
                try
                {
                    pickqty = Convert.ToDecimal(txtPickQty.Text.TrimEnd());
                }
                catch
                {
                    if (chkComplete.Checked) pickqty = (decimal)NewPSLine.Quantity;
                }


                NewPSLine.PickQty = pickqty;

                // ── A serial line cannot be completed without its units ────────────────
                // The lot-required guard below is skipped for serial lines (their lot box is
                // hidden), so without this a line could be ticked Complete having never opened
                // the chooser: the outbound then found no serials, posted one lumped movement
                // with a null lot, and left every serial lot still showing on hand.
                //
                // The count must also EQUAL the quantity being picked. The outbound issues one
                // movement per recorded serial while close-off invoices PickQty, so any
                // difference is stock that leaves with no document behind it.
                if (NewPSLine.PickComplete == true && lineIsSerialItem)
                {
                    var lineSerialsNow = SerialPicking.GetLineSerials(_db, CurrentUser.CoID,
                                                                      NewPSLine.PSID, NewPSLine.LineID);
                    if (lineSerialsNow.Count == 0)
                    {
                        chkComplete.Checked = false;
                        AlertHelper.ShowSweetAlert(this,
                            NewPSLine.ItemCode + " is serial tracked - use \"Select serials\" to choose "
                            + "the units for this line before completing it. Nothing was saved.", "error");
                        return;
                    }
                    if (lineSerialsNow.Count != pickqty)
                    {
                        chkComplete.Checked = false;
                        AlertHelper.ShowSweetAlert(this,
                            lineSerialsNow.Count + " serial(s) are selected for " + NewPSLine.ItemCode
                            + " but the pick quantity is " + pickqty.ToString("0.##")
                            + ". They must match - re-open \"Select serials\" or correct the quantity. "
                            + "Nothing was saved.", "error");
                        return;
                    }
                }

                if (NewPSLine.PickComplete == true)
                {
                    if (NewPSLine.IsLotTracked == true)
                    {
                        // Not on a serial line: its LotNumber holds the first picked serial and
                        // the dropdown is hidden but still bound, so this would clobber it.
                        if (!lineIsSerialItem
                            && !DDlotNum.SelectedValue.ToLower().ToString().Contains("number"))
                            NewPSLine.LotNumber = DDlotNum.SelectedValue.ToString();
                    }
                    NewPSLine.PickTime = DateTime.Now;
                    NewPSLine.StoreCodeFrom = DDStore.Text;
                } else
                {
                    if (NewPSLine.IsLotTracked == true)
                    {
                        // Not on a serial line: its LotNumber holds the first picked serial and
                        // the dropdown is hidden but still bound, so this would clobber it.
                        if (!lineIsSerialItem
                            && !DDlotNum.SelectedValue.ToLower().ToString().Contains("number"))
                            NewPSLine.LotNumber = DDlotNum.SelectedValue.ToString();
                        _db.SaveChanges();
                    }
                    //NewPSLine.LotNumber = null;
                    NewPSLine.PickTime = null;
                    //NewPSLine.BarCode = null;
                }
                _db.SaveChanges();
                if (NewPSLine.LineType == 0)
                {
                    #region UpdateItemTransactions - reverse transaction if there is a previous one only
                    // NEED TO REVERSE TRANSACTIONS - NOT DELETE
                    // ── Serial lines: reverse EVERY unit, not just the first ────────────
                    // A serial line moved out as one row per unit, but only the first row's id
                    // is stored on the line. Reversing that alone would put one unit back and
                    // strand the rest: stock permanently short, and those serials stuck as
                    // issued and unpickable.
                    //
                    // What to reverse comes from the LEDGER, not from the line's current serial
                    // selection: the operator may have just changed that selection, and what
                    // has to be undone is what actually went out. Netting the movements for
                    // this slip and item per serial means an already-reversed unit nets to zero
                    // and is left alone - no need to track reversals separately.
                    // ONLY for a serial line. Applying this to an ordinary lot line reversed
                    // its -20 with a single +1 and skipped the correct full reversal below.
                    // Resolved from the database, not from the cached _serialItems set, which
                    // is only loaded on a non-postback and is null here.
                    long revItemId = NewPSLine.SelectionId;
                    var serialsToReverse = new List<string>();
                    if (lineIsSerialItem)
                    {
                        // What has to be put back is what actually WENT OUT, which is not the same
                        // as what the line currently has selected. Re-open the chooser, tick a
                        // different set, and the units issued under the old set are still out -
                        // scoping the reversal to the current selection left them stranded as
                        // issued and unpickable, with the store permanently short.
                        //
                        // So: every unit of this item still out on this slip, MINUS the units that
                        // belong to the slip's other lines. ItemTransaction has no line key, and
                        // this is what stands in for one - it keeps a 20-unit split line from
                        // reversing its neighbours' units while still catching its own strays.
                        var otherLineUnits = SerialPicking.SerialsOnOtherLines(_db, CurrentUser.CoID,
                                                                              NewPSLine.PSID, NewPSLine.LineID);

                        // Netted, so a unit already put back sums to zero and is left alone -
                        // un-picking twice cannot return the same unit twice.
                        serialsToReverse = _db.ItemTransactions
                            .Where(x => x.CompanyID == CurrentUser.CoID
                                     && x.DocumentID == NewPSLine.PSID
                                     && x.ItemID == revItemId
                                     && x.TransactionType == "PS"
                                     && x.LotNumber != null && x.LotNumber != "")
                            .GroupBy(x => x.LotNumber)
                            .Select(g => new { Serial = g.Key, Net = g.Sum(x => x.Qty) ?? 0 })
                            .Where(x => x.Net < 0)
                            .Select(x => x.Serial)
                            .ToList()
                            .Where(x => !otherLineUnits.Contains(x, StringComparer.OrdinalIgnoreCase))
                            .ToList();
                    }

                    if (serialsToReverse.Count > 0)
                    {
                        NewPSLine.ItemTransLineID = null;
                        foreach (string serial in serialsToReverse)
                        {
                            // The outbound movement for this unit on this picking slip.
                            var outTrn = _db.ItemTransactions
                                .Where(x => x.CompanyID == CurrentUser.CoID
                                         && x.DocumentID == NewPSLine.PSID
                                         && x.ItemID == NewPSLine.SelectionId
                                         && x.LotNumber == serial
                                         && x.Qty < 0)
                                .OrderByDescending(x => x.TrnID).FirstOrDefault();
                            if (outTrn == null) continue;

                            decimal backVal;
                            _db.ItemTransactions.Add(new ItemTransaction
                            {
                                CompanyID                 = CurrentUser.CoID,
                                DocumentID                = outTrn.DocumentID,
                                TransactionType           = "PS",
                                ItemID                    = outTrn.ItemID,
                                ItemCode                  = outTrn.ItemCode ?? "",
                                ItemDescription           = outTrn.ItemDescription ?? "",
                                Unit                      = outTrn.Unit,
                                FromID                    = 0,
                                ToID                      = outTrn.ToID ?? FrmStorid,
                                Qty                       = 1,
                                DocumentType              = 10,
                                PriceExclusive            = outTrn.PriceExclusive,
                                TotalUnitPriceExclInclAdd = outTrn.TotalUnitPriceExclInclAdd,
                                TotalLineValExcl          = outTrn.TotalUnitPriceExclInclAdd,
                                TransactionDate           = DateTime.Now,
                                ByRoleID                  = CurrentUser.RoleID,
                                AdditionalCosts           = 0,
                                TransactionReference      = lblDocNum.Text + " Reversal",
                                LotNumber                 = serial,
                                ExchRate                  = 1,
                                // Back in at its original cost, re-blending the store average.
                                StoreAvgCost = StoreCosting.ComputeMovement(_db, CurrentUser.CoID,
                                                   (long)outTrn.ItemID, outTrn.ToID ?? FrmStorid,
                                                   1, outTrn.TotalUnitPriceExclInclAdd ?? 0, out backVal)
                            });
                            _db.SaveChanges();
                        }

                        // Deliberately NOT clearing PickSlipLineSerials here. The outbound
                        // further down reads it to decide what to issue, and wiping it at this
                        // point left that read empty - so every serial pick fell back to a
                        // single lumped movement and the invoice serial note came out blank.
                    }
                    // NOT an else-if: a line can carry a lumped movement from before it was
                    // serialised. Skipping this because serials were reversed would leave that
                    // older movement standing and the stock double-deducted on the next pick.
                    if (NewPSLine.ItemTransLineID != null)
                    {
                        long TrnLineid = 0;
                        try
                        {
                            TrnLineid = Convert.ToInt64(NewPSLine.ItemTransLineID);
                        }
                        catch { }
                        if (TrnLineid > 0)
                        {
                            NewPSLine.ItemTransLineID = null;
                            var ItmR = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.TrnID == TrnLineid).FirstOrDefault();
                            if (ItmR != null)
                            {
                                ItemTransaction ItemTrans = new ItemTransaction();
                                ItemTrans.CompanyID = CurrentUser.CoID;
                                ItemTrans.DocumentID = ItmR.DocumentID;
                                ItemTrans.TransactionType = "PS";
                                ItemTrans.ItemID = ItmR.ItemID;
                                ItemTrans.ItemCode = ItmR.ItemCode ?? "";
                                ItemTrans.ItemDescription = ItmR.ItemDescription ?? "";
                                ItemTrans.Unit = ItmR.Unit;
                                ItemTrans.FromID = 0;
                                ItemTrans.ToID = ItmR.ToID ?? FrmStorid;   // reverse INTO the store it was originally picked from, not the store currently selected in the dropdown
                                ItemTrans.Qty = Convert.ToDecimal(ItmR.Qty) * -1;
                                ItemTrans.DocumentType = 10;
                                ItemTrans.PriceExclusive = ItmR.PriceExclusive;
                                ItemTrans.TotalUnitPriceExclInclAdd = ItmR.TotalUnitPriceExclInclAdd;
                                qoh = 0;
                                if (NewPSLine.IsLotTracked == true)
                                {
                                    if (DDlotNum.SelectedIndex > 0)
                                    {
                                        var ItmT = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == NewPSLine.SelectionId && x.ToID == FrmStorid && x.LotNumber == DDlotNum.SelectedValue).OrderByDescending(x => x.TrnID);
                                        if (ItmT != null)
                                        {
                                            try
                                            {
                                                qoh = (decimal)ItmT.Sum(x => x.Qty);
                                            }
                                            catch { }
                                        }
                                    }
                                }
                                else
                                {
                                    var ItmT = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == NewPSLine.SelectionId && x.ToID == FrmStorid).OrderByDescending(x => x.TrnID);
                                    if (ItmT != null)
                                    {
                                        try
                                        {
                                            qoh = (decimal)ItmT.Sum(x => x.Qty);
                                        }
                                        catch { }
                                    }
                                }
                                ItemTrans.TransactionDate = DateTime.Now;
                                ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid     
                                ItemTrans.AdditionalCosts = 0;
                                ItemTrans.TotalLineValExcl = ItemTrans.TotalUnitPriceExclInclAdd * ItemTrans.Qty;
                                ItemTrans.TransactionReference = lblDocNum.Text + " Reversal";
                                ItemTrans.LotNumber = ItmR.LotNumber;
                                ItemTrans.ExchRate = 1;
                                // Reversal puts the pick back at its original cost and re-blends
                                // the store's running average.
                                decimal revVal;
                                ItemTrans.StoreAvgCost = StoreCosting.ComputeMovement(_db, CurrentUser.CoID, (long)ItemTrans.ItemID, ItmR.ToID ?? FrmStorid, (decimal)ItemTrans.Qty, (decimal)ItemTrans.TotalLineValExcl, out revVal);
                                _db.ItemTransactions.Add(ItemTrans);
                                _db.SaveChanges();
                            }
                        }
                    }
                    #endregion

                    if (chkComplete.Checked)
                    {
                        // add record to item movement table to update on hand balances   
                        ItemTransaction ItemTrans = new ItemTransaction();
                        ItemTrans.CompanyID = CurrentUser.CoID;
                        ItemTrans.DocumentID = Convert.ToInt64(lblPSid.Text);
                        ItemTrans.TransactionType = "PS";
                        ItemTrans.ItemID = Convert.ToInt64(NewPSLine.SelectionId);
                        ItemTrans.ItemCode = NewPSLine.ItemCode ?? "";
                        ItemTrans.ItemDescription = NewPSLine.ItemDescription ?? "";
                        ItemTrans.Unit = NewPSLine.Unit;
                        ItemTrans.FromID = 0;
                        ItemTrans.ToID = FrmStorid;
                        // Back-order: move only the picked quantity out of stock (not the ordered qty).
                        ItemTrans.Qty = Convert.ToDecimal(NewPSLine.PickQty ?? NewPSLine.Quantity) * -1;
                        ItemTrans.DocumentType = 10;
                        // Outbound: stock leaves at the pick store's running weighted average;
                        // an out never changes the store average.
                        decimal storeAvg = StoreCosting.GetStoreAvgCost(_db, CurrentUser.CoID, (long)ItemTrans.ItemID, FrmStorid);
                        if (storeAvg == 0)
                        {
                            storeAvg = (decimal)_db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == ItemTrans.ItemID).AverageCost;
                        }
                        ItemTrans.PriceExclusive = storeAvg;
                        ItemTrans.TotalUnitPriceExclInclAdd = storeAvg;
                        ItemTrans.TransactionDate = DateTime.Now;
                        ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid
                        ItemTrans.AdditionalCosts = 0;
                        ItemTrans.TotalLineValExcl = ItemTrans.TotalUnitPriceExclInclAdd * ItemTrans.Qty;
                        ItemTrans.StoreAvgCost = storeAvg;
                        ItemTrans.TransactionReference = lblDocNum.Text;
                        ItemTrans.LotNumber = string.Empty;
                        if (NewPSLine.IsLotTracked == true)
                        {
                            ItemTrans.LotNumber = NewPSLine.LotNumber;
                        }
                        ItemTrans.ExchRate = 1;

                        // ── Serial lines: one movement per unit ─────────────────────────────
                        // A serial is a lot holding exactly one unit, so a single row of -20
                        // against the first serial would take twenty off a lot that holds one
                        // and leave the other nineteen showing as still on hand. Twenty rows of
                        // -1 deplete each unit properly. Value is unchanged: an outbound leaves
                        // at the store average, so twenty at that average total the same as one.
                        var pickedSerials = SerialPicking.GetLineSerials(_db, CurrentUser.CoID,
                                                                         NewPSLine.PSID, NewPSLine.LineID);
                        if (pickedSerials.Count > 0)
                        {
                            long firstTrn = 0;
                            foreach (string serial in pickedSerials)
                            {
                                ItemTransaction unitTrn = new ItemTransaction
                                {
                                    CompanyID                  = ItemTrans.CompanyID,
                                    DocumentID                 = ItemTrans.DocumentID,
                                    TransactionType            = ItemTrans.TransactionType,
                                    ItemID                     = ItemTrans.ItemID,
                                    ItemCode                   = ItemTrans.ItemCode,
                                    ItemDescription            = ItemTrans.ItemDescription,
                                    Unit                       = ItemTrans.Unit,
                                    FromID                     = ItemTrans.FromID,
                                    ToID                       = ItemTrans.ToID,
                                    Qty                        = -1,
                                    DocumentType               = ItemTrans.DocumentType,
                                    PriceExclusive             = ItemTrans.PriceExclusive,
                                    TotalUnitPriceExclInclAdd  = ItemTrans.TotalUnitPriceExclInclAdd,
                                    TransactionDate            = DateTime.Now,
                                    ByRoleID                   = ItemTrans.ByRoleID,
                                    AdditionalCosts            = 0,
                                    TotalLineValExcl           = ItemTrans.TotalUnitPriceExclInclAdd * -1,
                                    StoreAvgCost               = ItemTrans.StoreAvgCost,
                                    TransactionReference       = ItemTrans.TransactionReference,
                                    LotNumber                  = serial,
                                    ExchRate                   = 1
                                };
                                _db.ItemTransactions.Add(unitTrn);
                                _db.SaveChanges();
                                if (firstTrn == 0) firstTrn = unitTrn.TrnID;
                            }
                            // Kept for the existing reversal path; un-picking a serial line walks
                            // the line's serials rather than this single id.
                            NewPSLine.ItemTransLineID = firstTrn;
                            _db.SaveChanges();
                        }
                        else
                        {
                            _db.ItemTransactions.Add(ItemTrans);
                            _db.SaveChanges();
                            NewPSLine.ItemTransLineID = ItemTrans.TrnID;
                            _db.SaveChanges();
                        }
                    }
                }
            }
            RefreshPickStatus(PsID);
        }

        // PSStatus lifecycle when Pick-Slip Tracking is off: Captured -> Started -> Picked.
        // (Complete is set at close-off; when tracking is on, station names drive PSStatus.)
        private void RefreshPickStatus(long psid)
        {
            if (CurrentUser.UsePickSlipTracking == true) return;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var psm = _db.PickingSlipMasters.FirstOrDefault(x => x.CustomerID == CurrentUser.CoID && x.PSID == (int)psid);
                if (psm == null || psm.PSComplete == true) return;
                var lines = _db.PickSlipLines.Where(l => l.PSID == psid && l.CompanyID == CurrentUser.CoID && l.LineType == 0).ToList();
                if (lines.Count == 0) return;
                string newStatus = lines.All(l => l.PickComplete == true) ? "Picked"
                                 : lines.Any(l => l.PickComplete == true) ? "Started"
                                 : "Captured";
                if (psm.PSStatus != newStatus) { psm.PSStatus = newStatus; _db.SaveChanges(); }
            }
        }

       protected void lbtnComment_Click(object sender, EventArgs e)
        {
            LinkButton lbtnComment = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnComment.NamingContainer;
            long jcline = Convert.ToInt64(lbtnComment.CommandArgument);
            lblLineid.Text = jcline.ToString();
            lblSender.Text = "lbtnComment";
            Button25_ModalPopupExtender.Show();
        }

        protected void lbtnWorksMsg_Click(object sender, EventArgs e)
        {
            lblSender.Text = "lbtnWorksMsg";
            Button25_ModalPopupExtender.Show();
        }

        protected void btnSaveComment_Click(object sender, EventArgs e)
        {
             using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (lblSender.Text == "lbtnComment")
                {
                    long jcl = Convert.ToInt64(lblLineid.Text);
                    var jcline = _db.JobCardLines.Where(x => x.LineID == jcl).FirstOrDefault();
                    jcline.Comments = txtMsgBody.Text.ToString() ?? "";
                    _db.SaveChanges();
                    BindGrid();
                } 
                else
                if (lblSender.Text == "lbtnWorksMsg")
                {
                    var jc = _db.JobCardsMasters.Where(x => x.JCID == docid).FirstOrDefault();
                    jc.JCWorkMessage = txtMsgBody.Text.ToString() ?? "";
                    _db.SaveChanges();
                    LoadPSHeader();
                }
                else
                if (lblSender.Text == "lbtnPackMsg")
                {
                    var jc = _db.JobCardsMasters.Where(x => x.JCID == docid).FirstOrDefault();
                    jc.JCPackMessage = txtMsgBody.Text.ToString() ?? "";
                    _db.SaveChanges();
                    LoadPSHeader();
                }
                else
                if (lblSender.Text == "lbtnDelMsg")
                {
                    var jc = _db.JobCardsMasters.Where(x => x.JCID == docid).FirstOrDefault();
                    jc.JCDeliveryMessage = txtMsgBody.Text.ToString() ?? "";
                    _db.SaveChanges();
                    LoadPSHeader();
                }
                txtMsgBody.Text = "";
            }
        }

        protected void lbtnPackMsg_Click(object sender, EventArgs e)
        {
            lblSender.Text = "lbtnPackMsg";
            Button25_ModalPopupExtender.Show();
        }

        protected void lbtnDelMsg_Click(object sender, EventArgs e)
        {
            lblSender.Text = "lbtnDelMsg";
            Button25_ModalPopupExtender.Show();
        }

        // Items on this slip that are serial tracked. Loaded once per bind rather than a
        // query per row.
        private HashSet<long> _serialItems;

        private void LoadSerialItems()
        {
            _serialItems = new HashSet<long>();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                foreach (long id in _db.ItemsMasters
                                       .Where(x => x.CompanyID == CurrentUser.CoID && x.IsSerialTracked)
                                       .Select(x => x.ID).ToList())
                {
                    _serialItems.Add(id);
                }
            }
        }

        /// <summary>
        /// Blanks the Lot Number heading when every line on the slip is serial tracked - a
        /// serial is finer than a lot, so the column has nothing to say.
        ///
        /// The column itself STAYS. Setting Visible = false on a GridView column stops its
        /// controls being created at all, and DDlotNum is read unguarded in nine places on
        /// this page - the save and complete handlers would throw the moment anyone used a
        /// serial slip. The per-row controls are hidden instead, which leaves the cell empty
        /// and the control present.
        /// </summary>
        // A non-empty marker, so the blanked column can still be found next time without
        // matching genuinely header-less columns.
        private const string LotColumnBlanked = " ";

        private void ApplyLotColumnVisibility()
        {
            if (_serialItems == null) LoadSerialItems();

            int lotColumn = -1;
            for (int i = 0; i < GridPSLines.Columns.Count; i++)
            {
                string h = GridPSLines.Columns[i].HeaderText;
                // Matched on the real header only. A blank-header fallback also matched
                // column 0 (the header-less SelectionID field), and a later mixed bind then
                // wrote "Lot Number" onto that column.
                if (string.Equals(h, "Lot Number", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(h, LotColumnBlanked, StringComparison.Ordinal))
                {
                    lotColumn = i;
                    break;
                }
            }
            if (lotColumn < 0) return;

            bool anyLotLine = false;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                long psid = Convert.ToInt64(lblPSid.Text);
                foreach (long selId in _db.PickSlipLines
                                          .Where(x => x.CompanyID == CurrentUser.CoID && x.PSID == psid)
                                          .Select(x => x.SelectionId).ToList())
                {
                    if (!_serialItems.Contains(selId)) { anyLotLine = true; break; }
                }
            }
            GridPSLines.Columns[lotColumn].HeaderText = anyLotLine ? "Lot Number" : LotColumnBlanked;
        }

        protected void GridPSLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (_serialItems == null) LoadSerialItems();
            long PsID = Convert.ToInt64(lblPSid.Text);
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                LinkButton lbtnLotNumAdd = (LinkButton)e.Row.FindControl("lbtnLotNumAdd");
                DropDownList ddLt = (DropDownList)e.Row.FindControl("DDlotNum");

                long itemID = Convert.ToInt64(e.Row.Cells[0].Text.ToString());
                var item = (PickSlipLine)e.Row.DataItem;

                // Serial lines are picked by choosing units, not by picking a lot from a list.
                // The "+" that opens the chooser is far too subtle to be the only way in, so
                // for these lines it becomes a labelled button and the lot dropdown - which
                // means nothing here - is hidden.
                if (_serialItems != null && _serialItems.Contains(itemID))
                {
                    // A serial is deeper than a lot, so a lot number means nothing on this line.
                    if (ddLt != null) ddLt.Visible = false;
                    Label lblLotHidden = (Label)e.Row.FindControl("lblLotNum");
                    if (lblLotHidden != null) lblLotHidden.Visible = false;
                    if (lbtnLotNumAdd != null)
                    {
                        // Same styling as Save Edits below the grid, so it reads as a normal
                        // button rather than something bolted on. The column it lives in is
                        // sized to 0em for the old "+" icon, so it must not wrap.
                        lbtnLotNumAdd.Text = " Select serials";
                        lbtnLotNumAdd.CssClass = "icon fa-barcode buttonSage";

                        // The markup carries an INLINE style for the "+" icon this button
                        // normally is - font-size:.8em and margin:-1em. Inline beats the class,
                        // so buttonSage was drawn at 80% with a NEGATIVE margin: the glyph sat on
                        // top of the text and the padding collapsed. Clear it before styling.
                        lbtnLotNumAdd.Attributes.Remove("style");
                        lbtnLotNumAdd.Style["white-space"] = "nowrap";
                        lbtnLotNumAdd.Style["margin"] = "0 .4em";
                        lbtnLotNumAdd.Style["padding"] = ".35em .8em";
                        lbtnLotNumAdd.Style["color"] = "#fff";

                        // The column is sized 0em for the icon it usually holds.
                        if (e.Row.Cells.Count > 9)
                        {
                            e.Row.Cells[9].Style["white-space"] = "nowrap";
                            e.Row.Cells[9].Style["width"] = "auto";
                            e.Row.Cells[9].Style["text-align"] = "right";
                        }
                        lbtnLotNumAdd.ToolTip = "Choose the "
                            + ApiUrlCall.NumberToDecimal((item.Quantity ?? 0).ToString(), CurrentUser.CompanyDecPlaces)
                            + " serial number(s) for this line";

                        // A line ticked Done has already issued its units, and re-opening the
                        // chooser used to set PickComplete back to false WITHOUT reversing
                        // anything - stock gone, nothing invoiced, balance out again on a back
                        // order. That is blocked in lbtnLotNumAdd_Click, which SAYS so.
                        //
                        // Deliberately NOT disabled here: a disabled LinkButton renders as plain
                        // text with no postback, so the button just went dead with no message and
                        // read as broken. A refusal the operator can see beats a silent one.
                        if (item.PickComplete == true)
                            lbtnLotNumAdd.ToolTip = "Un-tick Done before changing the serials for this line.";
                    }
                }

                // Read-only outstanding balance on the linked SO line (blank for lot-split lines).
                Label lblQtyLeft = (Label)e.Row.FindControl("lblQtyLeft");
                if (lblQtyLeft != null && _qtyLeftMap != null && (item.SBCALineID ?? 0) != 0
                    && _qtyLeftMap.TryGetValue((long)item.SBCALineID, out decimal qtyLeft))
                {
                    lblQtyLeft.Text = ApiUrlCall.NumberToDecimal(qtyLeft.ToString(), CurrentUser.CompanyDecPlaces);
                }
                DropDownList DDStore = new DropDownList();
                DDStore = (DropDownList)e.Row.FindControl("DDStore");
                DDStore.Items.Clear();
               
                var StoreList = _itemST.Where(x => x.ItemID == itemID && x.AllowPicking==true).ToList();
                
                List<StoreTList> StorList = new List<StoreTList>();
                foreach (var st in StoreList)
                {
                    StoreTList StItem = new StoreTList();
                    StItem.StoreCode = st.StoreCode;   
                    DropDownList DDlotNum = (DropDownList)e.Row.FindControl("DDlotNum");
                    if (!CurrentUser.CompanyUseLotNumbers)
                    {
                        using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                        {
                            //var ItmT = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == st.ItemID && x.ToID == st.StoreID && x.DocumentID != PsID).OrderByDescending(x => x.TrnID).FirstOrDefault();
                            var ItmT = _db.ItemTransactions.Where(it => it.CompanyID == CurrentUser.CoID && it.ItemID == st.ItemID && it.ToID == (int)st.StoreID).Select(it => (decimal?)it.Qty).DefaultIfEmpty(0).Sum() ?? 0;
                            {
                                StItem.QOH = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(ItmT.ToString(), CurrentUser.CompanyDecPlaces));
                                if (StItem.QOH != 0)
                                {
                                    StItem.StoreCnQ = st.StoreCode.Trim() + $"({StItem.QOH.ToString()})";
                                }
                                else
                                {     
                                    StItem.StoreCnQ = st.StoreCode + "(0)";    
                                }
                            }
                        }
                    }
                    else
                    {
                        StItem.QOH = 0;
                        StItem.StoreCnQ = st.StoreCode;
                    }
                        StorList.Add(StItem);
                }

                if (StorList != null && StorList.Count > 0)
                {
                    DDStore.DataSource = StorList;
                    DDStore.DataTextField = "StoreCnQ";
                    DDStore.DataValueField = "StoreCode";
                    DDStore.DataBind();
                    DDStore.Items.Insert(0, "-?-");
                    DDStore.SelectedValue = item.StoreCodeFrom;
                }
                else
                {
                    DDStore.Visible = false;
                    
                    ddLt.Visible = false;
                    lbtnLotNumAdd.Visible = false;
                }
                if (CurrentUser.CompanyUseLotNumbers == true)
                {   
                    if (StoreList.Count == 1 && DDStore.SelectedIndex > -1)
                    {
                        if (item.IsLotTracked == true)
                        {
                            var LotNums = _ActiveLotNums.Where(x => x.StoreCode == DDStore.SelectedItem.ToString() && x.ItemId == itemID).ToList();
                            var lotNumList = new List<LotNumList>();
                            foreach (var lot in LotNums)
                            {
                                lotNumList.Add(new LotNumList
                                {
                                    LotNum = lot.LotNumber,
                                    LotDisplay = lot.LotNumber + " (" + lot.QtyHandToStore.ToString("N2") + ")"
                                });
                            }
                            ddLt.DataSource = lotNumList.ToList();
                            ddLt.DataValueField = "LotNum";
                            ddLt.DataTextField = "LotDisplay";
                            ddLt.DataBind();
                            try
                            {
                                ddLt.SelectedValue = item.LotNumber;
                            }
                            catch { }
                            ddLt.Items.Insert(0, "- Lot Number - ");
                        }
                        else
                        {
                            ddLt.Visible = false;
                            lbtnLotNumAdd.Visible = false;
                        }
                    }
                }
                else
                {
                    ddLt.Visible = false;
                    lbtnLotNumAdd.Visible = false;
                    if (StoreList.Count == 1 || DDStore.SelectedIndex > 0)
                    {
                    }
                }
            }
            if (lblstatus.Text == "Complete")
            {
                CheckBox chkC = (CheckBox)e.Row.FindControl("chkComplete");
                DropDownList DDStore = (DropDownList)e.Row.FindControl("DDStore");
                DropDownList DDlotNum = (DropDownList)e.Row.FindControl("DDlotNum");
                LinkButton lbtnLotNumAdd = (LinkButton)e.Row.FindControl("lbtnLotNumAdd");
               if (chkC != null)
                {
                    chkC.Enabled = false;
                    DDStore.Enabled = false;
                    DDlotNum.Enabled = false;

                    lbtnLotNumAdd.Enabled = false;
                }  
            }
            e.Row.Cells[0].Visible = false;
            if (CurrentUser.CompanyUseLotNumbers == false)
            {
                e.Row.Cells[8].Visible = false;    // Lot Number (barcode column removed)
                e.Row.Cells[9].Visible = false;    // lot-add button
            }
        }

        protected void LbtnPickSave_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                int slipid = Convert.ToInt32(lblPSid.Text);
                long Docid = Convert.ToInt64(lblDocID.Text);

                // ---- Back-order close-off flow --------------------------------------------------
                // Work out whether everything ordered on this slip has actually been picked.
                var LinesForCheck = _db.PickSlipLines.Where(x => x.PSID == slipid).ToList();
                decimal orderedQty = LinesForCheck.Sum(l => (decimal)(l.Quantity ?? 0));
                decimal pickedQty = LinesForCheck.Sum(l => (l.PickComplete == true ? (decimal)(l.PickQty ?? l.Quantity ?? 0) : 0m));
                bool hasShortfall = (orderedQty - pickedQty) > 0.0001m;
                bool anyPicked = pickedQty > 0.0001m;

                string closeChoice = hfCloseChoice.Value;
                hfCloseChoice.Value = "";

                if (string.IsNullOrEmpty(closeChoice))
                {
                    // First pass: ask the picker how to close off, then re-post with the answer.
                    string pb = Page.ClientScript.GetPostBackEventReference(LbtnPickSave, "");
                    string hf = hfCloseChoice.ClientID;
                    string js;
                    if (!hasShortfall)
                    {
                        js = "Swal.fire({title:'Close Picking Slip',text:'Mark this picking slip as fully picked and close it off?',icon:'question',showCancelButton:true,confirmButtonText:'Yes, close off'})"
                           + ".then(function(r){if(r.isConfirmed){document.getElementById('" + hf + "').value='full';" + pb + ";}});";
                    }
                    else if (!anyPicked)
                    {
                        // Nothing picked = nothing to invoice, so there is nothing to close off yet.
                        AlertHelper.ShowSweetAlert(this, "No items have been picked yet. Pick at least one item before closing off, or delete this picking slip.", "warning");
                        return;
                    }
                    else
                    {
                        js = "Swal.fire({title:'Picking Complete?',text:'Some items are short-picked. Is picking complete?',icon:'question',showDenyButton:true,showCancelButton:true,confirmButtonText:'Yes, fully picked',denyButtonText:'No, save as short-picked'})"
                           + ".then(function(r){"
                           + "if(r.isConfirmed){document.getElementById('" + hf + "').value='full';" + pb + ";}"
                           + "else if(r.isDenied){Swal.fire({title:'Keep Unpicked on Back Order?',text:'Invoice the picked items now and create a new Sales Order for the unpicked balance? Choosing No cancels the unpicked balance.',icon:'question',showDenyButton:true,showCancelButton:true,confirmButtonText:'Yes, put balance on back order',denyButtonText:'No, cancel balance'})"
                           + ".then(function(r2){"
                           + "if(r2.isConfirmed){document.getElementById('" + hf + "').value='backorder';" + pb + ";}"
                           + "else if(r2.isDenied){document.getElementById('" + hf + "').value='shortship';" + pb + ";}"
                           + "});}"
                           + "});";
                    }
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "PSCloseFlow", js, true);
                    return;
                }

                // Safety: 'full' is only valid when nothing is short.
                if (closeChoice == "full" && hasShortfall)
                {
                    AlertHelper.ShowSweetAlert(this, "Some lines are not fully picked. Choose Back Order or Cancel Balance.", "error");
                    return;
                }
                // keepBackOrder = record the unpicked balance in QtyLeft; a new Sales Order is created
                // for it when this SO is posted to Sage. full / shortship both close the SO line off
                // with nothing owing (short-ship simply abandons the balance).
                bool keepBackOrder = (closeChoice == "backorder");
                // --------------------------------------------------------------------------------

                long FirstLineID = 0;
                // update SO lines from Picking Slip
                var PSLines = _db.PickSlipLines.Where(x => x.PSID == slipid).OrderBy(x=>x.LineID).ToList();

                // How much of each order line is still allowed to be invoiced on this run.
                //
                // A serial order line is split into batches of 20 when the slip is created, and the
                // split parts carry SBCALineID = 0 - they find their parent through FirstLineID and
                // clamp themselves to its QtyLeft. But the parent is processed FIRST and sets its own
                // QtyLeft to 0 on any close that is not a back order, so every later part clamped to
                // zero: 50 units left the building and 20 were invoiced.
                //
                // Tracking the allowance here, seeded from the balance BEFORE the parent is touched
                // and decremented as each part takes its share, keeps the original protection (a
                // re-closed slip cannot re-invoice a banked quantity) without the parts fighting
                // over a value the parent has already overwritten.
                var lineAllowance = new Dictionary<long, decimal>();
                
                foreach (var PsL in PSLines)
                {
                    // Only lines the picker actually marked complete count as picked this cycle;
                    // everything else is left on back order (pickQty = 0, no stock was moved for it).
                    decimal pickQty = 0;
                    if (PsL.PickComplete == true)
                    {
                        pickQty = (decimal)(PsL.PickQty ?? PsL.Quantity);
                    }

                    // Serials picked against THIS line. They travel on the document line comment,
                    // which is what reaches Sage as the invoice line's Comments - so the customer
                    // can count the serials on a line against that line's quantity.
                    string serialNote = "";
                    var lineSerials = SerialPicking.GetLineSerials(_db, CurrentUser.CoID, PsL.PSID, PsL.LineID);
                    if (lineSerials.Count > 0) serialNote = SerialPicking.SerialNote(lineSerials);
                    if (PsL.SBCALineID != 0)
                    {
                        var SOLine = _db.DocLines.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == Docid && x.SBCALineID == PsL.SBCALineID).FirstOrDefault();
                        if (SOLine != null)
                        {
                            FirstLineID = (long)PsL.SBCALineID;
                            // QtyLeft is the running outstanding balance (mirrors the receiving flow):
                            // null on the first cycle = full ordered qty, then it counts down by what's picked.
                            decimal prevLeft = SOLine.QtyLeft ?? (decimal)SOLine.Quantity;

                            // Seeded once per order line, from the balance as it stands before this
                            // run touches it. Split parts of the same line read and decrement it.
                            if (!lineAllowance.ContainsKey(FirstLineID)) lineAllowance[FirstLineID] = prevLeft;

                            // Clamp to the outstanding balance so re-closing the same slip after a back
                            // order can't re-invoice qty that was already banked in an earlier cycle.
                            if (pickQty > lineAllowance[FirstLineID]) pickQty = lineAllowance[FirstLineID];
                            lineAllowance[FirstLineID] -= pickQty;

                            // What is still owing after every part of this line has taken its share.
                            // The parts are processed after this one, so their quantities are added
                            // to ReceiveQty and taken off QtyLeft as they go.
                            decimal outstanding = lineAllowance[FirstLineID];
                            if (outstanding < 0) outstanding = 0;
                            // QtyLeft = outstanding back-order qty (0 once fully picked, or when the balance is cancelled).
                            SOLine.QtyLeft = keepBackOrder ? outstanding : 0;
                            SOLine.ReceiveQty = pickQty;   // qty to invoice THIS cycle
                            SOLine.ReceiveComplete = (SOLine.QtyLeft == 0);
                            SOLine.StoreCode = PsL.StoreCodeFrom;
                            SOLine.LotNumber = PsL.LotNumber;
                            if (serialNote.Length > 0)
                            {
                                SOLine.Comments = SerialPicking.CapComment(serialNote, PsL.Comments);
                            }
                            SOLine.Exclusive = SOLine.UnitPriceExclusive * pickQty;
                            SOLine.Discount = (SOLine.UnitPriceExclusive * pickQty) * SOLine.DiscountPercentage;
                            // VAT is charged on the discounted-NET amount, not the gross line value.
                            SOLine.Tax = (SOLine.Exclusive - SOLine.Discount) * SOLine.TaxPercentage;
                            SOLine.Total = SOLine.Exclusive - SOLine.Discount + SOLine.Tax;
                            SOLine.LotNumber = PsL.LotNumber;
                            SOLine.ExchRate = 1;
                            SOLine.localCurrLineVal = SOLine.Exclusive - SOLine.Discount;
                        }
                    }
                    else
                    {
                        // add new line
                        DocLine DLn = new DocLine();
                        DLn.DocID = Docid;
                        DLn.SBCALineID = (long)PsL.SBCALineID;
                        DLn.SelectionId = PsL.SelectionId;
                        DLn.ItemCode = PsL.ItemCode;
                        DLn.ItemDescription = PsL.ItemDescription;
                        DLn.Quantity = 0;
                        DLn.ReceiveQty = pickQty;
                        DLn.Comments = serialNote.Length > 0
                            ? SerialPicking.CapComment(serialNote, PsL.Comments)
                            : PsL.Comments;
                        DLn.QtyLeft = 0;
                        DLn.ReceiveQty = pickQty;
                        DLn.ToReceive = true;
                        DLn.ReceiveComplete = true;
                        DLn.StoreCode = PsL.StoreCodeFrom;
                        DLn.LotNumber = PsL.LotNumber;
 
                        // Get values from first line of the same item
                        var FirstSOLine = _db.DocLines.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == Docid && x.SBCALineID == FirstLineID).FirstOrDefault();

                        // Take this part's share out of the line's remaining allowance. Reading the
                        // parent's QtyLeft here instead clamped every part to zero, because the
                        // parent had already been closed off a few lines above.
                        decimal allowLeft;
                        if (!lineAllowance.TryGetValue(FirstLineID, out allowLeft))
                        {
                            allowLeft = FirstSOLine != null ? (FirstSOLine.QtyLeft ?? (decimal)FirstSOLine.Quantity) : pickQty;
                            lineAllowance[FirstLineID] = allowLeft;
                        }
                        if (pickQty > allowLeft) pickQty = allowLeft;
                        lineAllowance[FirstLineID] -= pickQty;

                        // The parent carries the order line's balance, so what this part takes has
                        // to come off it - otherwise the balance still shows units that have gone.
                        if (FirstSOLine != null && pickQty > 0)
                        {
                            decimal parentLeft = FirstSOLine.QtyLeft ?? 0;
                            parentLeft -= pickQty;
                            FirstSOLine.QtyLeft = parentLeft < 0 ? 0 : parentLeft;
                            FirstSOLine.ReceiveComplete = (FirstSOLine.QtyLeft == 0);
                        }

                        DLn.ReceiveQty = pickQty;
                        DLn.UnitPriceExclusive = FirstSOLine.UnitPriceExclusive;
                        DLn.UnitPriceInclusive = FirstSOLine.UnitPriceInclusive;
                        DLn.TaxPercentage = FirstSOLine.TaxPercentage;
                        DLn.DiscountPercentage = FirstSOLine.DiscountPercentage;
                        DLn.Exclusive = DLn.UnitPriceExclusive * pickQty;
                        DLn.Discount = (DLn.UnitPriceExclusive * pickQty) * DLn.DiscountPercentage;
                        // VAT is charged on the discounted-NET amount, not the gross line value.
                        DLn.Tax = (DLn.Exclusive - DLn.Discount) * DLn.TaxPercentage;
                        DLn.Total = DLn.Exclusive - DLn.Discount + DLn.Tax;
                        DLn.AnalysisCategoryId1 = FirstSOLine.AnalysisCategoryId1;
                        DLn.AnalysisCategoryId2 = FirstSOLine.AnalysisCategoryId2;
                        DLn.AnalysisCategoryId3 = FirstSOLine.AnalysisCategoryId3;
                        // Cost of sale = the pick store's running weighted average (feeds the Sage
                        // invoice line UnitCost and, via the API, the line GP).
                        long pickStoreId = _db.Stores.Where(x => x.StoreCode == PsL.StoreCodeFrom && x.CompanyID == CurrentUser.CoID).Select(x => x.StoreID).FirstOrDefault();
                        DLn.UnitCost = StoreCosting.GetStoreAvgCost(_db, CurrentUser.CoID, (long)PsL.SelectionId, pickStoreId);
                        DLn.ItemType = FirstSOLine.ItemType;
                        DLn.LineTaxTypeID = FirstSOLine.LineTaxTypeID;
                        DLn.Unit = FirstSOLine.Unit;
                        DLn.LineType = FirstSOLine.LineType;
                        DLn.ExchRate = 1;
                        DLn.localCurrLineVal = DLn.Exclusive - DLn.Discount;
                        _db.DocLines.Add(DLn);

                        // The parent's balance is decremented where this part's quantity is
                        // decided, above - doing it again here would take the same units off twice.
                    }
                }
                _db.SaveChanges();
                    
                // Back order: this SO closes normally for the picked quantities. Any outstanding
                // balance is recorded in QtyLeft; when the SO is posted to Sage, a NEW Sales Order
                // is created for that balance (linked via Reference) and follows the normal workflow.
                bool hasBackOrder = keepBackOrder && _db.DocLines.Any(x => x.DocID == Docid && (x.QtyLeft ?? 0) > 0.0001m);

                var DocH = _db.DocHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == Docid).FirstOrDefault();
                DocH.Complete = true;
                DocH.Active = true;
                DocH.CompBy = CurrentUser.RoleID;
                DocH.CompleteDate = DateTime.Today;
                if (hasBackOrder)
                {
                    // Cleared back to "Invoiced" once the balance SO has been created at post time.
                    DocH.Status = "Partially Invoiced";
                }

                // get total cost from transactions
                DocH.DocCost = Convert.ToDecimal((_db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.DocumentID == slipid).Sum(x => (decimal?)x.TotalLineValExcl) ?? 0m).ToString("N2"));
                if (DocH.DocCost != 0) DocH.DocCost = DocH.DocCost * -1;
                decimal DocValue = Convert.ToDecimal((_db.DocLines.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == Docid).Sum(x => (decimal?)x.Exclusive) ?? 0m));
                decimal cost = DocH.DocCost ?? 0m;
                decimal gp = 0m;
                if (DocValue != 0)
                {
                    gp = (DocValue - cost) / DocValue;   // e.g. 0.25
                }
                DocH.DocGP = gp;

                var Stat = _db.PickSlipProcesses.Where(ws => ws.CompanyID == CurrentUser.CoID).OrderByDescending(ws => ws.Seq).Select(ws => new { ws.Seq, ws.PSName, ws.PSPID }).FirstOrDefault();
                var PSH = _db.PickingSlipMasters.Where(x => x.CustomerID == CurrentUser.CoID && x.PSID == slipid).FirstOrDefault();
                int fromstat = (int)PSH.PSStationID;
                PSH.PSStationID = Stat.PSPID;
                PSH.PSStatus = (CurrentUser.UsePickSlipTracking == true) ? Stat.PSName.ToString() : "Complete";
                PSH.PSComplete = true;
                PSH.PSCompleteDate = DateTime.Now;
                PSH.PSCompleteBy = CurrentUser.RoleID;
                PSH.PSActive = false;
                PSH.LinkedSOrdID = Docid;

                var PickTransaction = new PickSlipTransaction
                {
                    PSID = (int?)slipid,
                    MoveQty = 1,
                    RejectQty = 0,
                    MoveDate = DateTime.Now,
                    FromStationID = fromstat,
                    ToStationID = Stat.PSPID,
                    CompanyID = CurrentUser.CoID,
                    MoveBy = CurrentUser.RoleID
                    // Set other properties as needed
                };
                _db.PickSlipTransactions.Add(PickTransaction);

                // get users linked to PS notifications
                var PSUsers = _db.RolesMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.NotifyPSMove == true).ToList();
                if (PSUsers.Count > 0)
                {
                    foreach (var usr in PSUsers)
                    {
                        var Notif = new Notification
                        {
                            Message = PSH.PSIntNumber + " Moved to Process " + PSH.PSStatus.ToString(),
                            IsRead = false,
                            CreatedAt = DateTime.Now,
                            CompanyID = CurrentUser.CoID,
                            UserRoleID = usr.RoleID
                        };
                        _db.Notifications.Add(Notif);
                    }
                }
                _db.SaveChanges();
                // Show popup and redirect after confirmation
                string closeMsg = hasBackOrder
                    ? "Picked items closed off. When this Sales Order is updated to Sage, a new Sales Order will be created for the outstanding balance."
                    : "Picking Slip closed off successfully.";
                string script = @"
                        Swal.fire({
                            title: 'Success!',
                            text: '" + closeMsg + @"',
                            icon: 'success'
                        }).then(function() {
                            window.location.href = '" + ResolveUrl("~/SalesOrder.aspx?docid=" + docguid.ToString() + "&autosave=" + CurrentUser.AutoUpdateSageSOs) + @"';
                        }); ";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "CloseOffSuccess", script, true);
                return;
            }
        }

        protected void LbtnSaveEdits_Click(object sender, EventArgs e)
        {
            int slipid = Convert.ToInt32(lblPSid.Text);
            long Docid = Convert.ToInt64(lblDocID.Text);

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (DDeliveryBy.SelectedIndex > 0)
                {
                    var DocH = _db.DocHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == Docid).FirstOrDefault();
                    DocH.DeliveryBy = DDeliveryBy.Text.ToString().Trim().Replace("'", "''");
                }
                
                var PSH = _db.PickingSlipMasters.Where(x => x.CustomerID == CurrentUser.CoID && x.PSID == slipid).FirstOrDefault();
                PSH.PSPickMessage = (txtWMsg.Text ?? "").ToString().Trim().Replace("'", "''");
                PSH.PSPackMessage = (txtPMsg.Text ?? "").ToString().Trim().Replace("'", "''");
                PSH.PSDeliverMessage = (txtDMsg.Text ?? "").ToString().Trim().Replace("'", "''");
                _db.SaveChanges();

                AlertHelper.ShowSweetAlert(this, "Successfully Saved", "success");  
            }
        }

        protected void lbtnIssue_Click(object sender, EventArgs e)
        {
            long PsID = Convert.ToInt64(lblPSid.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var thisPS = _db.PickingSlipMasters.Where(x=>x.CustomerID == CurrentUser.CoID && x.PSID == PsID).FirstOrDefault();
                if (thisPS != null)
                {
                    var newstat = _db.PickSlipProcesses.Where(x => x.CompanyID == CurrentUser.CoID && x.PSName.Contains("Issue")).FirstOrDefault();
                    int frmStatid = (int)thisPS.PSStationID;
                    thisPS.PSStatus = newstat.PSName;
                    thisPS.PSStationID = newstat.PSPID;
                    thisPS.PSStart = true;
                    thisPS.PSStartDate = DateTime.Now;
                    thisPS.PSIssuedTo = _db.RolesMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.IsPicker == true).OrderBy(x=>x.RoleID).Select(x =>x.RoleID).FirstOrDefault();
                    var PickTransaction = new PickSlipTransaction
                    {
                        PSID = (int?)PsID,
                        MoveQty = 1,
                        RejectQty = 0,
                        MoveDate = DateTime.Now,
                        FromStationID = frmStatid,
                        ToStationID = thisPS.PSStationID,
                        CompanyID = CurrentUser.CoID,
                        MoveBy = CurrentUser.RoleID
                        // Set other properties as needed
                    };
                    _db.PickSlipTransactions.Add(PickTransaction);

                    var thisdoc = _db.DocHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.LinkedPSID == PsID).FirstOrDefault();
                    thisdoc.Started = true;

                    _db.SaveChanges();
                    LoadPSHeader();
                    LoadHistory();
                }
            }
        }

        protected void DDStore_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            GridViewRow row = (GridViewRow)ddl.NamingContainer;
            DropDownList DDStore = (DropDownList)row.FindControl("DDStore");
            DropDownList DDItemCode = (DropDownList)row.FindControl("DDItemCode");
            LinkButton lbtnLineSave = (LinkButton)row.FindControl("lbtnLineSave");
            DropDownList DDlotNum = (DropDownList)row.FindControl("DDlotNum");

            if (DDStore.SelectedIndex > 0)
            {
                
                if (!DDlotNum.Visible)
                {
                    CheckBox chkComplete = (CheckBox)row.FindControl("chkComplete");
                    chkComplete.Checked = true;
                    if (lbtnLineSave != null)
                    {
                        lbtnLineSave_Click(lbtnLineSave, e);
                    }
                }
            }
            else
            {
                DDlotNum.SelectedIndex = -1;
                CheckBox chkComplete = (CheckBox)row.FindControl("chkComplete");
                chkComplete.Checked = false;
                if (lbtnLineSave != null)
                {
                    lbtnLineSave_Click(lbtnLineSave, e);
                }
            }


            string StoreCode = DDStore.SelectedValue.ToString();
            long LineID = Convert.ToInt64(lbtnLineSave.CommandArgument);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var PSLine = _db.PickSlipLines.Where(x => x.LineID == LineID).FirstOrDefault();
                PSLine.StoreCodeFrom = StoreCode;
                _db.SaveChanges();
            }
            if (!DDlotNum.Visible)
            {
                long ItemID = Convert.ToInt64(row.Cells[0].Text.ToString());
                PopulateDDLotNumbers(row, StoreCode, ItemID);
            }
        }
        private void PopulateDDLotNumbers(GridViewRow row, string StCode, long ItemID)
        {
            DropDownList DDlotNum = (DropDownList)row.FindControl("DDlotNum");
            DDlotNum.Items.Clear();
            // Example LINQ query based on itemType
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var LotNums = _ActiveLotNums.Where(x => x.ItemId == ItemID && x.StoreCode == StCode).ToList();
                var lotNumList = new List<LotNumList>();
                foreach (var lot in LotNums)
                {
                    lotNumList.Add(new LotNumList
                    {
                        LotNum = lot.LotNumber,
                        LotDisplay = lot.LotNumber + " (" + lot.QtyHandToStore.ToString("N2") + ")"
                    });
                }
                DDlotNum.DataSource = lotNumList.ToList();
                DDlotNum.DataValueField = "LotNum";
                DDlotNum.DataTextField = "LotDisplay";
                DDlotNum.DataBind();
                DDlotNum.Items.Insert(0, "- Lot Number - ");
            }
        }

        private void LoadDelivBy()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var DelBy = _db.DelivMethods.Where(x => x.CompanyID == CurrentUser.CoID && x.DelActive == true).OrderBy(x => x.DelivMethod1).ToList();
                DDeliveryBy.DataSource = DelBy;
                DDeliveryBy.DataTextField = "DelivMethod1";
                DDeliveryBy.DataBind();
                DDeliveryBy.Items.Insert(0, "- Delivery - ");
            }
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

        protected void lbtnPrintPS_Click(object sender, EventArgs e)
        {
            CreatePDF();
            Response.Redirect($"~/ViewPDF.aspx?doc=" + CurrentUser.UserGuiD.ToString() + "\\PS_" + lblDocNum.Text, false);
        }

        private void CreatePDF()
        {
            string filepath = string.Empty, fname = string.Empty;
            var regfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 10, BaseColor.BLACK);
            var regfontB = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 10, Font.BOLD, BaseColor.BLACK);
            var regfontS = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 8, BaseColor.BLACK);
            var medfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 11, BaseColor.BLACK);
            var headfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 18, BaseColor.BLACK);
            docid = Convert.ToInt64(lblPSid.Text);
            Guid DocGuid = Guid.Parse(docguid);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var PS = _db.PickingSlipMasters.Where(x => x.CustomerID == CurrentUser.CoID && x.PSID == docid).FirstOrDefault();
                var DH = _db.DocHeaders.Where(x => x.DocGUID == DocGuid).FirstOrDefault();
                if (PS != null)
                {
                    iTextSharp.text.Document doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 40, 40);
                    PdfWriter writer = null;

                    try
                    {
                        if (!Directory.Exists(Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString())))
                        {
                            Directory.CreateDirectory(Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString()));
                        }
                        filepath = Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString() + "\\PS_" + lblDocNum.Text + ".PDF");
                        if (File.Exists(filepath))
                        {
                            File.Delete(filepath);
                        }
                        if (File.Exists(filepath))
                        {
                            File.Delete(filepath);
                        }
                        writer = PdfWriter.GetInstance(doc, new FileStream(filepath, FileMode.Create));
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
                        gif.ScaleToFit(125.0F, 60.0F);   // must stay inside the 132pt logo column or it overlaps the barcode
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

                    Phrase psHead = new Phrase();
                    if (CurrentUser.MobileModule)
                    {
                        Barcode128 bc = new Barcode128();
                        bc.Code = PS.PSIntNumber;
                        bc.Font = null;
                        iTextSharp.text.Image bcImg = bc.CreateImageWithBarcode(writer.DirectContent, BaseColor.BLACK, BaseColor.BLACK);
                        bcImg.ScaleToFit(90.0F, 24.0F);   // ~90pt is what the header row leaves beside the 18pt title text
                        psHead.Add(new Chunk(bcImg, 0, -8, true));
                        psHead.Add(new Chunk("   ", headfont));
                    }
                    psHead.Add(new Chunk("Picking Slip #", headfont));
                    cell = new PdfPCell(psHead);
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(PS.PSIntNumber, headfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Customer:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);
 
                    cell = new PdfPCell(new Phrase((DH.CustSupName ?? "").ToString(), medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Sales Rep:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase((DH.SalesRepName ?? "").ToString(), medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Due Date:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(Convert.ToDateTime(PS.PSDueDate).ToString("dd MMM yyyy"), medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Delivery:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase((DH.DeliveryBy ?? "").ToString(), medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    doc.Add(table);
                    #endregion

                    #region messages
                    PdfPTable tableM = new PdfPTable(3);
                    PdfPCell cellM;
                    tableM.SpacingBefore = 15f;
                    tableM.TotalWidth = doc.PageSize.Width - 80;
                    tableM.LockedWidth = true;
                   
                    cellM = new PdfPCell(new Phrase("Picking Notes:- " + Environment.NewLine + (PS.PSPickMessage ?? "").ToString(), regfont));
                    cellM.HorizontalAlignment = 0;
                    cellM.FixedHeight = 80f; ;
                    tableM.AddCell(cellM);

                    cellM = new PdfPCell(new Phrase("Packing Notes:- " + Environment.NewLine + (PS.PSPackMessage ??"").ToString(), regfont));
                    cellM.HorizontalAlignment = 0;
                    cellM.FixedHeight = 60f; ;
                    tableM.AddCell(cellM);

                    cellM = new PdfPCell(new Phrase("Delivery Notes:- " + Environment.NewLine + (PS.PSDeliverMessage ?? "").ToString(), regfont));
                    cellM.HorizontalAlignment = 0;
                    cellM.FixedHeight = 60f; ;
                    tableM.AddCell(cellM);


                    doc.Add(tableM);
                    #endregion


                    #region HeaderRow
                    PdfPTable table4 = new PdfPTable(8);
                    PdfPCell cell4;
                    table4.SpacingBefore = 15f;
                    table4.SetWidths(new int[] { 50, 150, 60, 30, 30, 100, 40, 40});
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

                    cell4 = new PdfPCell(new Phrase("Store", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Lot Number", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Qty", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Picked", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    #endregion

                    var DocLines = _db.PickSlipLines.Where(x => x.PSID == PS.PSID).OrderBy(x => x.LineID).ToList();
                    foreach (var DL in DocLines)
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

                        string Barcode = _db.ItemBarCodeLinks.Where(x => x.ItemID == DL.SelectionId && x.QtyPerBarcode == 1).Select(x => x.BarCode).FirstOrDefault() ?? "";
                        cell4 = new PdfPCell(new Phrase(Barcode ?? "", regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(DL.Unit ?? "", regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(DL.StoreCodeFrom ?? "", regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(DL.LotNumber ?? "", regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(Convert.ToDecimal(ApiUrlCall.NumberToDecimal(DL.Quantity.ToString(), CurrentUser.CompanyDecPlaces)).ToString(), regfont));
                        //cell4 = new PdfPCell(new Phrase(DL.Quantity.ToString(), regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase("", regfont));
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                    }

                    doc.Add(table4);
                    

                    doc.AddTitle("Picking Slip: ");
                    //doc.AddSubject("Classroom Review Instrument");
                    doc.AddAuthor("Data Fusion");
                    doc.Close();

                }
            }
        }

        protected void chkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chkSelectAll = (CheckBox)sender;
            Boolean ischeck = chkSelectAll.Checked;

            // Iterate through each row in the GridView
            foreach (GridViewRow row in GridPSLines.Rows)
            {
                // Find the checkbox in each row
                CheckBox chkComplete = (CheckBox)row.FindControl("chkComplete");
                
                // Set the checkbox's checked state to match the "Select All" checkbox
                if (chkComplete != null)
                {
                    chkComplete.Checked = chkSelectAll.Checked;

                    //TextBox txtPickQty = (TextBox)row.FindControl("txtPickQty");
                    //decimal pickqty = 0, OrdQty = 0;
                    //try
                    //{
                    //    pickqty = Convert.ToDecimal(txtPickQty.Text);
                    //}
                    //catch { }
                    //if (chkComplete.Checked && pickqty == 0)
                    //{
                    //    OrdQty = Convert.ToDecimal(row.Cells[4].Text);
                    //    txtPickQty.Text = OrdQty.ToString();
                    //}
                    
                    // Optionally trigger the lbtnLineSave_Click if needed
                    LinkButton lbtnLineSave = (LinkButton)row.FindControl("lbtnLineSave");
                    if (lbtnLineSave != null)
                    {
                        lbtnLineSave_Click(lbtnLineSave, e);
                    }
                }
            }

            // Ensure the "Select All" checkbox retains its checked state
            chkSelectAll.Checked = ischeck;  // This ensures no change to chkSelectAll
        }

        protected void chkComplete_CheckedChanged(object sender, EventArgs e)
        {
            // Handle individual row checkbox change
            CheckBox chkComplete = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chkComplete.NamingContainer;
            DropDownList DDStore = (DropDownList)row.FindControl("DDStore");
            TextBox txtPickQty = (TextBox)row.FindControl("txtPickQty");
            decimal pickqty = 0, OrdQty = 0;
            try
            {
                decimal.TryParse(txtPickQty.Text, out pickqty);
            }
            catch { }
            if (chkComplete.Checked && pickqty == 0)
            {
                OrdQty = CellParse.ToDecimal(row.Cells[4].Text);
                txtPickQty.Text = OrdQty.ToString();
                pickqty = OrdQty;
            }

            // Perform your logic for handling individual checkbox state change
            if (DDStore.Visible == true)
            {
                if (DDStore.SelectedIndex > 0 && DDStore.SelectedValue != "-?-")
                {
                    LinkButton lbtnLineSave = (LinkButton)row.FindControl("lbtnLineSave");
                    if (lbtnLineSave != null)
                    {
                        lbtnLineSave_Click(lbtnLineSave, e);
                    }
                }
                else
                {
                    chkComplete.Checked = false;
                    AlertHelper.ShowSweetAlert(this, "No Store selected, unable to complete line.", "error");
                    return;
                }
            }
            else
            {
                LinkButton lbtnLineSave = (LinkButton)row.FindControl("lbtnLineSave");
                if (lbtnLineSave != null)
                {
                    lbtnLineSave_Click(lbtnLineSave, e);
                }
            }
        }

        private class LotNumList
        {
            public string LotNum { get; set; }
            public string LotDisplay { get; set; }
        }

        private class StoreTList
        {
            public string StoreCode { get; set; }
            public string StoreCnQ { get; set; }
            public decimal QOH { get; set; }
        }
       
        protected void lbtnViewSO_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/SalesOrder.aspx?docid=" + docguid.ToString(), true);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        private void LoadHistory()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                int psid = Convert.ToInt32(lblPSid.Text);
                var HistLines = _db.GetPickSlipMovementTransactions(CurrentUser.CoID, psid).ToList();
                if (HistLines.Count > 0)
                {
                    GridHistLines.DataSource = HistLines;
                    GridHistLines.DataBind();
                }
            }
        }

        protected void lbtnDelPS_Click(object sender, EventArgs e)
        {
            long psid = Convert.ToInt64(lblPSid.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {

                var PSH = _db.PickingSlipMasters.Where(x => x.CustomerID == CurrentUser.CoID && x.PSID == psid).FirstOrDefault();
                _db.PickingSlipMasters.Remove(PSH);

                var PSL = _db.PickSlipLines.Where(x => x.PSID == psid).OrderBy(x => x.LineID).ToList();       
                _db.PickSlipLines.RemoveRange(PSL);

                var JCT = _db.PickSlipTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.PSID == psid).ToList();
                _db.PickSlipTransactions.RemoveRange(JCT);

                var ItmTC = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.DocumentID == psid).ToList();
                _db.ItemTransactions.RemoveRange(ItmTC);

                var DH = _db.DocHeaders.Where(x => x.LinkedPSID == psid).FirstOrDefault();
                DH.LinkedPSID = null;
                DH.Complete = false;

                long Docid = Convert.ToInt64(lblDocID.Text);
                // delete add lines not in SB
                var DelLines = _db.DocLines.Where(x => x.DocID == Docid && x.SBCALineID == 0).ToList();
                _db.DocLines.RemoveRange(DelLines);

               var DelQtyPicked = _db.DocLines.Where(x => x.DocID == Docid).ToList();
               foreach(var dl in DelQtyPicked)
                {
                    dl.ReceiveQty = 0;
                    if (dl.LotNumber != null && dl.LotNumber != "")
                    {
                        dl.LotNumber = null;
                    }   
                }

                _db.SaveChanges();

                GridPSLines.DataSource = null;
                GridPSLines.DataBind();
                string script = @"
                    Swal.fire({
                        title: 'Success!',
                        text: 'Picking Slip successfully deleted.',
                        icon: 'success'
                    }).then(function() {
                        window.location.href = '" + ResolveUrl("~/SalesOrder.aspx?docid=" + docguid.ToString()) + @"';
                    }); ";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "DeleteSuccess", script, true);
                return;
            }
        }

        protected void lbtnLotNumAdd_Click(object sender, EventArgs e)
        {
            GridLotNums.DataSource = null;
            GridLotNums.DataBind();

            LinkButton lbtnLotNumAdd = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnLotNumAdd.NamingContainer;
            //// get item linked stores & populate ddPopWHses
            lblSlipLine.Text = lbtnLotNumAdd.CommandArgument;

            // Belt to the disabled button above. A completed line's units are already out of
            // stock; opening the chooser here is what silently un-completed it.
            long openLineId = 0;
            long.TryParse(lbtnLotNumAdd.CommandArgument, out openLineId);
            if (openLineId > 0)
            {
                using (SBMSEntities _dbOpen = new SBMSEntities(Config.GetConnectionString()))
                {
                    bool lineDone = _dbOpen.PickSlipLines
                        .Where(x => x.LineID == openLineId).Select(x => x.PickComplete).FirstOrDefault() == true;
                    if (lineDone)
                    {
                        AlertHelper.ShowSweetAlert(this,
                            "This line is already ticked Done and its units have been issued. "
                            + "Un-tick Done first, then choose the serials again.", "warning");
                        return;
                    }
                }
            }

            lblLineQty.Text = row.Cells[4].Text.ToString();
            long ItmID = Convert.ToInt64(row.Cells[0].Text);
            loadpopLotNumbers(ItmID);  
            ModalPopupExtender1.Show();
        }

        protected void LbtnLotAddOK_Click(object sender, EventArgs e)
        {
           decimal sellQty = 0;

            // Serials are picked by ticking whole units, so a tick IS a quantity of 1. Writing it
            // back into the same textbox means everything below - the total check and the line
            // split - works on serials exactly as it already does on lots.
            if (SerialPickMode)
            {
                foreach (GridViewRow grv in GridLotNums.Rows)
                {
                    CheckBox chkPick = (CheckBox)grv.FindControl("chkPickSerial");
                    TextBox txtQ = (TextBox)grv.FindControl("txtUseQty");
                    if (chkPick != null && txtQ != null) txtQ.Text = chkPick.Checked ? "1" : "";
                }
            }

            foreach (GridViewRow grv in GridLotNums.Rows)
            {
                TextBox txtUseQty = (TextBox)grv.FindControl("txtUseQty");
                if (txtUseQty.Text != null && txtUseQty.Text != "")
                { 
                    if (Convert.ToDecimal(txtUseQty.Text) > 0) sellQty += Convert.ToDecimal(txtUseQty.Text);
                }
            }

            // PART PICKS ARE ALLOWED, for lots and for serials alike.
            //
            // Less than the line calls for is a short pick: the shortfall goes on back order
            // through the close-off, which works off the SALES ORDER line's outstanding
            // balance rather than anything recorded on the slip. This modal used to demand
            // the exact quantity, which meant a lot line could only be short picked from the
            // grid (type a lower Pick_Qty, choose the lot, tick Done) and never once the
            // stock had to come from more than one lot.
            //
            // More than the line calls for is still refused - that is stock leaving with no
            // order behind it - and so is selecting nothing at all.
            decimal reqLineQty = Convert.ToDecimal(lblLineQty.Text);
            if (sellQty <= 0)
            {
                AlertHelper.ShowSweetAlert(this,
                    SerialPickMode
                        ? "Tick at least one serial for this line. Nothing was saved."
                        : "Enter a quantity against at least one lot. Nothing was saved.",
                    "error");
                return;
            }
            if (sellQty > reqLineQty)
            {
                AlertHelper.ShowSweetAlert(this,
                    "This line is for " + lblLineQty.Text.Trim() + " - " + sellQty.ToString("0.##")
                    + (SerialPickMode ? " serial(s) ticked. Untick the extras." : " selected. Reduce the quantities.")
                    + " Nothing was saved.", "error");
                return;
            } 

            // Expiry is a warning, not a block: the operator can have a good reason to ship
            // short-dated or expired stock, but they must not be able to do it unknowingly.
            var expiredPicked = new List<string>();
            foreach (GridViewRow grv in GridLotNums.Rows)
            {
                TextBox txtQ = (TextBox)grv.FindControl("txtUseQty");
                Label lblExp = (Label)grv.FindControl("lblExpires");
                if (txtQ == null || lblExp == null) continue;
                decimal q;
                if (!decimal.TryParse(txtQ.Text, out q) || q <= 0) continue;
                if ((lblExp.Text ?? "").IndexOf("expired", StringComparison.OrdinalIgnoreCase) >= 0)
                    expiredPicked.Add(grv.Cells[1].Text.Trim());
            }

            long LineIDD = Convert.ToInt64(lblSlipLine.Text);
            decimal UseQty = 0;
            decimal reqqty = Convert.ToDecimal(lblLineQty.Text);

            // ── Serial items ────────────────────────────────────────────────────────────
            // The line was already split to at most 20 units when the picking slip was
            // created, so the serials are recorded AGAINST this line - it is not split
            // again. One line, its quantity, and the units that satisfied it.
            if (SerialPickMode)
            {
                // ONE pass over the grid: what was ticked, and which store each unit is in.
                // Cells are HTML-encoded by the BoundField, so a serial containing & or < would
                // otherwise be stored and later matched in its encoded form.
                var picked = new List<string>();
                var pickedStores = new List<string>();
                foreach (GridViewRow grv in GridLotNums.Rows)
                {
                    if (grv.RowType != DataControlRowType.DataRow) continue;
                    CheckBox chkPick = (CheckBox)grv.FindControl("chkPickSerial");
                    if (chkPick == null || !chkPick.Checked) continue;

                    picked.Add(Server.HtmlDecode(grv.Cells[1].Text).Trim());
                    string st = Server.HtmlDecode(grv.Cells[0].Text).Trim();
                    if (!pickedStores.Contains(st)) pickedStores.Add(st);
                }

                // EVERY check happens before anything is written. SetLineSerials writes with raw
                // SQL, which commits immediately and outside the change tracker - so a guard that
                // runs after it cannot honour its own "Nothing was saved", and the line's previous
                // selection is destroyed by the DELETE regardless.
                //
                // One line issues from ONE store: ticking across two would book every unit out of
                // whichever was found first, leaving the other store showing its unit on hand.
                if (pickedStores.Count > 1)
                {
                    AlertHelper.ShowSweetAlert(this,
                        "The serials ticked are in different stores (" + string.Join(", ", pickedStores)
                        + "). Pick from one store per line. Nothing was saved.", "error");
                    return;
                }

                using (SBMSEntities _dbS = new SBMSEntities(Config.GetConnectionString()))
                {
                    var pslS = _dbS.PickSlipLines.FirstOrDefault(x => x.LineID == LineIDD);
                    if (pslS == null)
                    {
                        AlertHelper.ShowSweetAlert(this, "Picking slip line not found, please refresh.", "error");
                        return;
                    }

                    // A serial already picked onto another line of this slip would mean the same
                    // physical unit despatched twice. The unique index stops it at the database;
                    // this stops it with a message the operator can act on.
                    var already = SerialPicking.PickedElsewhere(_dbS, CurrentUser.CoID, pslS.PSID, LineIDD, picked);
                    if (already.Count > 0)
                    {
                        AlertHelper.ShowSweetAlert(this,
                            "Already picked on another line of this slip: " + string.Join(", ", already)
                            + ". Nothing was saved.", "error");
                        return;
                    }

                    // Everything above passed, so it is safe to write.
                    SerialPicking.SetLineSerials(_dbS, CurrentUser.CoID, pslS.PSID, LineIDD, picked, CurrentUser.RoleID);

                    if (pickedStores.Count == 1) pslS.StoreCodeFrom = pickedStores[0];
                    pslS.PickQty       = picked.Count;
                    pslS.PickTime      = DateTime.Now;
                    pslS.PickComplete  = false;
                    // The line carries its serials in the child table; LotNumber holds the first
                    // so anything that still reads a single lot has something sensible to show.
                    pslS.LotNumber     = picked.Count > 0 ? picked[0] : null;
                    _dbS.SaveChanges();
                }

                BindGrid();
                if (expiredPicked.Count > 0)
                {
                    AlertHelper.ShowSweetAlert(this,
                        "Picked, but this includes stock that has already expired: "
                        + string.Join(", ", expiredPicked)
                        + ". Check before it is despatched.", "warning");
                }
                return;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Psl = _db.PickSlipLines.Where(x => x.LineID == LineIDD).FirstOrDefault();
                // INSERT FIRST EDITED LINE HERE.THEN ADD ADDITIONA LINES AFTER
                bool isfirst = true;
                // add new lines for each lot number selected
                foreach (GridViewRow grv in GridLotNums.Rows)
                {
                    // find next grid line with a useqty
                    if (grv.RowType == DataControlRowType.DataRow)
                    {
                        UseQty = 0;
                        TextBox txtUseQty = (TextBox)grv.FindControl("txtUseQty");
                        if (txtUseQty.Text != null && txtUseQty.Text != "")
                        {
                            try
                            {
                                if (Convert.ToDecimal(txtUseQty.Text) > 0) UseQty = Convert.ToDecimal(txtUseQty.Text);
                            }
                            catch { }
                        }
                               
                        if (UseQty > 0)
                        {
                            if (isfirst)
                            {
                                Psl.StoreCodeFrom = grv.Cells[0].Text.ToString();
                                Psl.LotNumber = grv.Cells[1].Text.ToString();
                                Psl.Quantity = UseQty;
                                Psl.PickQty = UseQty;
                                Psl.PickTime = DateTime.Now;
                                Psl.PickComplete = false;
                                isfirst = false;
                            }
                            else
                            {
                                PickSlipLine PsLn = new PickSlipLine();
                                PsLn.PSID = Psl.PSID;
                                PsLn.SelectionId = Psl.SelectionId;
                                PsLn.ItemCode = Psl.ItemCode;
                                PsLn.ItemDescription = Psl.ItemDescription;
                                PsLn.BarCode = Psl.BarCode;
                                PsLn.LineType = Psl.LineType;
                                PsLn.Unit = Psl.Unit;
                                PsLn.Comments = Psl.Comments;
                                PsLn.StoreCodeFrom = grv.Cells[0].Text.ToString();
                                PsLn.PickBy = Psl.PickBy;
                                PsLn.PickTime = DateTime.Now;
                                PsLn.PickComplete = false;
                                PsLn.SBCALineID = 0;
                                PsLn.CompanyID = CurrentUser.CoID;
                                PsLn.IsLotTracked = Psl.IsLotTracked;
                                PsLn.LotNumber = grv.Cells[1].Text.ToString();
                                PsLn.Quantity = UseQty;
                                PsLn.PickQty = UseQty;
                                _db.PickSlipLines.Add(PsLn);
                            }
                        }
                    }
                }
                _db.SaveChanges();
                BindGrid();
            }

            if (expiredPicked.Count > 0)
            {
                AlertHelper.ShowSweetAlert(this,
                    "Picked, but this includes stock that has already expired: "
                    + string.Join(", ", expiredPicked)
                    + ". Check before it is despatched.", "warning");
            }
        }

        protected string QuantityCheck()
        {
            string RetStr = "OK";
            decimal useqtyT = 0, reqqty = Convert.ToDecimal(lblLineQty.Text);
            foreach (GridViewRow grv in GridLotNums.Rows)
            {
                if (grv.RowType == DataControlRowType.DataRow)
                {
                    TextBox txtUseQty = (TextBox)grv.FindControl("txtUseQty");
                    decimal AvailQty = CellParse.ToDecimal(grv.Cells[1].Text);
                    decimal UseQty = Convert.ToDecimal(txtUseQty.Text);
                    useqtyT += UseQty;
                    if (UseQty > AvailQty)
                    {
                        RetStr =  "Use Qty > Available Qty for " + grv.Cells[0].Text.ToString();
                        break;
                    }
                }
            }
            if (useqtyT != reqqty)
            {
                RetStr = "Use quantity does not match required quantity, unable to continue";
            }
            return RetStr;
        }

        protected void DDOptions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDOptions.SelectedIndex > 0)
            {
                loadstores();
                if (DDOptions.SelectedValue == "0")
                {
                    lblTpe.Text = "Job Card";
                    pnlJCref.Style.Add("display", "inline-block");

                }
                else if (DDOptions.SelectedValue == "2")
                {
                    lblTpe.Text = "Works Order";
                    pnlJCref.Style.Add("display", "none");
                }
                Button2551_ModalPopupExtender.Show();
            }
        }

        private void loadstores()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true && x.StoreCode != "CoD" && x.StoreCode != "CoR" && x.AllowPicking == true).ToList();
                if (Stores.Any())
                {
                    DDStoreH.DataSource = Stores;
                    DDStoreH.DataTextField = "StoreDescript";
                    DDStoreH.DataValueField = "StoreCode";
                    DDStoreH.DataBind();
                    DDStoreH.Items.Insert(0, "- Any/All -");
                }
            }
        }

        protected void lbtnNewWOYes_Click(object sender, EventArgs e)
        {
            WorksOrderHeader WCHead = new WorksOrderHeader();
            WCHead.CompanyID = CurrentUser.CoID;
            WCHead.Status = "NEW";
            WCHead.Active = true;
            WCHead.LinkedDocumentNum = lblDocNum.Text.ToString();
            WCHead.CustSupName = txtCustName.Text.ToString();
            WCHead.Reference = txtRef.Text.ToString();
            WCHead.DueDate = Convert.ToDateTime(txtSODate.Text, CultureInfo.InvariantCulture);
            WCHead.WOrderDate = DateTime.Now;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                _db.WorksOrderHeaders.Add(WCHead);
                _db.SaveChanges();
                int newfcid = WCHead.ID;

                int woNumb = _db.WorksOrderHeaders.Where(x => x.CompanyID == CurrentUser.CoID)
                    .OrderByDescending(x => x.WONum)
                    .Select(x => x.WONum)
                    .FirstOrDefault();
                WCHead.WONum = woNumb + 1;

                long PsID = Convert.ToInt64(lblPSid.Text);
                var TempLines = _db.PickSlipLines.Where(x => x.PSID == PsID && x.ItemCode != null).OrderBy(x => x.LineID).ToList();
                // for each pickingslip row --> add new WO line
                foreach (var tl in TempLines)
                {
                    var _item = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CurrentUser.CoID && i.ID == tl.SelectionId).FirstOrDefault();
                    WorksOrderLine WoL = new WorksOrderLine();
                    WoL.CompanyID = CurrentUser.CoID;
                    WoL.WOID = newfcid;
                    WoL.LineType = 1;
                    if (_item.IsFromBOM == true) WoL.LineType = 2;
                    if (_item.IsFromKit == true) WoL.LineType = 3;
                    WoL.Quantity = tl.Quantity;
                    WoL.ItemCode = tl.ItemCode;
                    WoL.ItemDescription = tl.ItemDescription;
                    WoL.SelectionId = tl.SelectionId;
                    WoL.DueDelDate = Convert.ToDateTime(txtSODate.Text, CultureInfo.InvariantCulture);
                    WoL.Active = true;
                    _db.WorksOrderLines.Add(WoL);
                    _db.SaveChanges();
                    int newWoLid = WoL.LineID;

                    if (_item.IsFromBOM == false && _item.IsFromKit == false)
                    {
                        WorksOrderRMLine RML = new WorksOrderRMLine();
                        RML.LinkedWOLineID = (int)newWoLid;
                        RML.WOID = newfcid;
                        RML.SelectionId = tl.SelectionId;
                        RML.ItemCode = tl.ItemCode;
                        RML.ItemDescription = tl.ItemDescription;
                        RML.Quantity = Convert.ToDecimal(tl.Quantity);
                        RML.CompanyID = CurrentUser.CoID;
                        RML.LinkedFGSelectionID = tl.SelectionId;
                        RML.LinkedFGCode = tl.ItemCode;
                        RML.LinkedFGQty = tl.Quantity;
                        RML.PickComplete = false;
                        RML.Unit = _item.Unit;
                        RML.IsLotTracked = _item.IsLotTracked;
                        _db.WorksOrderRMLines.Add(RML);
                    }
                    else if (_item.IsFromBOM == true)
                    {
                        int bmc = _db.BOMHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.FGCode == tl.ItemCode).Select(x => x.BomHID).FirstOrDefault();
                        if (bmc > 0)
                        {
                            var BomLines = _db.GetBOMLinesFromBomHeaderID(bmc, CurrentUser.CoID);
                            foreach (var bl in BomLines)
                            {
                                if (bl.ItemID != null)
                                {
                                    WorksOrderRMLine RML = new WorksOrderRMLine();
                                    RML.LinkedWOLineID = (int)newWoLid;
                                    RML.WOID = newfcid;
                                    RML.SelectionId = (long)bl.ItemID;
                                    RML.ItemCode = bl.ItemCode;
                                    RML.ItemDescription = bl.Description;
                                    RML.Quantity = Convert.ToDecimal(tl.Quantity) * Convert.ToDecimal(bl.RMQty);
                                    RML.CompanyID = CurrentUser.CoID;
                                    RML.LinkedFGSelectionID = tl.SelectionId;
                                    RML.LinkedFGCode = tl.ItemCode;
                                    RML.LinkedFGQty = tl.Quantity;
                                    RML.PickComplete = false;
                                    var itm = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == tl.SelectionId).FirstOrDefault();
                                    RML.Unit = itm.Unit;
                                    RML.IsLotTracked = itm.IsLotTracked;
                                    _db.WorksOrderRMLines.Add(RML);
                                }
                            }
                        }
                    }
                    else if (_item.IsFromKit == true)
                    {
                        string kmc = _db.KitHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.FGCode == tl.ItemCode).Select(x => x.KitCode).FirstOrDefault();
                        if (kmc != null)
                        {
                            var KitLines = _db.GetKitLinesFromKitCode(kmc, CurrentUser.CoID);
                            foreach (var bl in KitLines)
                            {
                                if (bl.ItemID != null)
                                {
                                    WorksOrderRMLine RML = new WorksOrderRMLine();
                                    RML.LinkedWOLineID = (int)newWoLid;
                                    RML.WOID = newfcid;
                                    RML.SelectionId = (long)bl.ItemID;
                                    RML.ItemCode = bl.ItemCode;
                                    RML.ItemDescription = bl.Description;
                                    RML.Quantity = Convert.ToDecimal(tl.Quantity) * Convert.ToDecimal(bl.FGQty);
                                    RML.CompanyID = CurrentUser.CoID;
                                    RML.LinkedFGSelectionID = tl.SelectionId;
                                    RML.LinkedFGCode = tl.ItemCode;
                                    RML.LinkedFGQty = tl.Quantity;
                                    RML.PickComplete = false;
                                    RML.IsLotTracked = (bool)bl.IsLotTracked;
                                    RML.Unit = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == bl.ItemID).Unit;
                                    _db.WorksOrderRMLines.Add(RML);
                                }
                            }
                        }
                    }
                }
                int PslipID = Convert.ToInt32(lblPSid.Text);
                var thisPS = _db.PickingSlipMasters.Where(x => x.CustomerID == CurrentUser.CoID && x.PSID == PslipID).FirstOrDefault();
                thisPS.LinkedWONumber = newfcid;
                _db.SaveChanges();
                Response.Redirect($"~/WorksOrdersDetailed.aspx?woid={newfcid}", false);
            }
        }

        protected void lbtnNewWOCancel_Click(object sender, EventArgs e)
        {
            DDOptions.SelectedIndex = 0;
        }

        protected void lbtnWOrd_Click(object sender, EventArgs e)
        {
            int newfcid = Convert.ToInt32(lblWoID.Text);
            Response.Redirect($"~/WorksOrdersDetailed.aspx?woid={newfcid}", false);
        }

        protected void lbtnHome_Click1(object sender, EventArgs e)
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

        protected void DDlotNum_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Handle individual row checkbox change
            DropDownList DDlotNum = (DropDownList)sender;
            GridViewRow row = (GridViewRow)DDlotNum.NamingContainer;
            LinkButton lbtnLineSave = (LinkButton)row.FindControl("lbtnLineSave");
            CheckBox chkComplete = (CheckBox)row.FindControl("chkComplete");
            TextBox txtPickQty = (TextBox)row.FindControl("txtPickQty");
            decimal OrdQty = CellParse.ToDecimal(row.Cells[4].Text);
            
            chkComplete.Checked = true;
            if (DDlotNum.SelectedIndex == 0) chkComplete.Checked = false;
            if (lbtnLineSave != null)
            {
                txtPickQty.Text = OrdQty.ToString();
                lbtnLineSave_Click(lbtnLineSave, e);
            }
        }

       /// <summary>
       /// Fills the lot/serial chooser for one picking-slip line.
       ///
       /// Rows come back FEFO - earliest expiry first, undated last - so the stock that has to
       /// move first is the stock at the top of the list. Serial items pick whole units, so the
       /// quantity box is swapped for a tick and a scan box appears above the grid.
       /// </summary>
       private void loadpopLotNumbers(long itemid)
        {
            // Label.Text round-trips in ViewState, so without this the previous line's prompt -
            // including its required count, or a "cannot be picked as serials" error - is still
            // on screen for the line just opened.
            lblSerialNeed.Text = "";

            var LotNums = _ActiveLotNums.Where(x => x.ItemId == itemid && x.AllowPicking == true).ToList();

            // Expiry lives on LotTrackingMaster, not on the stored procedure result, so it is
            // stitched on here for display and for the FEFO sort.
            var expiry = new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);
            bool isSerial = false;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                foreach (var l in _db.LotTrackingMasters
                                    .Where(x => x.CompanyID == CurrentUser.CoID && x.ItemId == itemid)
                                    .Select(x => new { x.LotNumber, x.UseByDate }).ToList())
                {
                    if (l.LotNumber != null) expiry[l.LotNumber] = l.UseByDate;
                }

                // Item driven: an item that holds serialised stock keeps behaving like one
                // whether or not the company-level module is currently switched on.
                isSerial = _db.ItemsMasters
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.ID == itemid)
                    .Select(x => x.IsSerialTracked).FirstOrDefault();
            }

            // Serial mode picks whole units, so only quantity-1 rows belong here. An item can
            // still hold stock received BEFORE it was flagged serial - a lot of 10 under one
            // number - and ticking that would issue ten units as though it were one.
            if (isSerial)
            {
                int before = LotNums.Count;
                LotNums = LotNums.Where(x => x.QtyHandToStore == 1).ToList();
                if (LotNums.Count == 0 && before > 0)
                {
                    // Stock exists but none of it is single units - typically received before
                    // the item was flagged serial. Silence here just looks broken.
                    lblSerialNeed.Text = "This item has stock, but none of it is held as single "
                        + "serialised units - it was received before serial tracking was switched on. "
                        + "It cannot be picked as serials.";
                }
            }

            var rows = LotNums.Select(x => new LotPickRow
            {
                StoreCode       = x.StoreCode,
                LotNumber       = x.LotNumber,
                QtyHandToStore  = x.QtyHandToStore,
                UseByDate       = (x.LotNumber != null && expiry.ContainsKey(x.LotNumber)) ? expiry[x.LotNumber] : null
            })
            // FEFO: dated stock first, oldest first; undated last so it never jumps the queue.
            .OrderBy(x => x.UseByDate.HasValue ? 0 : 1)
            .ThenBy(x => x.UseByDate ?? DateTime.MaxValue)
            .ThenBy(x => x.LotNumber)
            .ToList();

            SerialPickMode = isSerial;
            pnlSerialScan.Visible = isSerial;
            lblLotModalHead.Text = isSerial
                ? "Select Serial Numbers For This Line"
                : "Fulfill Line Item With Multiple Lot Numbers";

            // The rows ARE serials here, so the column should say so - located by its current
            // header rather than by index, since the two headers alternate.
            for (int i = 0; i < GridLotNums.Columns.Count; i++)
            {
                string h = GridLotNums.Columns[i].HeaderText;
                if (string.Equals(h, "Lot Number", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(h, "Serial", StringComparison.OrdinalIgnoreCase))
                {
                    GridLotNums.Columns[i].HeaderText = isSerial ? "Serial" : "Lot Number";
                    break;
                }
            }
            if (isSerial && lblSerialNeed.Text.Length == 0)
            {
                // Only when the filter above has not already explained why the list is empty.
                lblSerialNeed.Text = "Tick up to " + lblLineQty.Text.Trim()
                                   + " serial(s), earliest expiry first. Fewer is a part pick - "
                                   + "the balance goes on back order.";

            }

            GridLotNums.DataSource = rows;
            GridLotNums.DataBind();
        }

        /// <summary>Row shown in the lot/serial chooser. Carries the expiry the proc does not return.</summary>
        public class LotPickRow
        {
            public string StoreCode { get; set; }
            public string LotNumber { get; set; }
            public decimal QtyHandToStore { get; set; }
            public Nullable<DateTime> UseByDate { get; set; }
        }

        // Whether the open chooser is picking serials (tick, qty always 1) or lots (type a qty).
        private bool SerialPickMode
        {
            get { return ViewState["SerialPickMode"] != null && (bool)ViewState["SerialPickMode"]; }
            set { ViewState["SerialPickMode"] = value; }
        }

        /// <summary>
        /// Shows a tick instead of a quantity box for serials, and flags expired / short-dated
        /// rows. Expiry is a WARNING, never a block - the operator decides.
        /// </summary>
        protected void GridLotNums_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var chk = e.Row.FindControl("chkPickSerial") as CheckBox;
            var qty = e.Row.FindControl("txtUseQty") as TextBox;
            if (chk != null) chk.Visible = SerialPickMode;
            if (qty != null)
            {
                // A serial is one unit, so the quantity is not a decision - but leaving the
                // cell blank reads as missing. Show the 1 and lock it; the tick still drives
                // what is picked, and OK writes that same 1 back before validating.
                if (SerialPickMode)
                {
                    qty.Text = "1";
                    qty.ReadOnly = true;
                    qty.Width = System.Web.UI.WebControls.Unit.Pixel(34);
                    qty.Style.Add("background", "#f2f5f7");
                    qty.Style.Add("color", "#8a99a6");
                }
                else
                {
                    qty.ReadOnly = false;
                }
            }

            var row = e.Row.DataItem as LotPickRow;
            if (row == null || !row.UseByDate.HasValue) return;

            DateTime due = row.UseByDate.Value.Date;
            var lbl = e.Row.FindControl("lblExpires") as Label;
            if (due < DateTime.Today)
            {
                e.Row.BackColor = System.Drawing.Color.MistyRose;
                if (lbl != null) { lbl.ForeColor = System.Drawing.Color.Firebrick; lbl.Font.Bold = true; lbl.Text += " (expired)"; }
            }
            else if (due <= DateTime.Today.AddDays(30))
            {
                e.Row.BackColor = System.Drawing.Color.LightGoldenrodYellow;
                if (lbl != null) { lbl.ForeColor = System.Drawing.Color.DarkGoldenrod; lbl.Font.Bold = true; }
            }
        }

        protected void lbtnAutoCreate_Click(object sender, EventArgs e)
        {
            docid = Convert.ToInt64(lblDocID.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var JcD = _db.JobCardsMasters.Where(x => x.CustomerID == CurrentUser.CoID && x.JCNumber == lblDocNum.Text.Replace("SO", "JC")).FirstOrDefault();
                if (JcD != null)
                {
                    var JcDL = _db.JobCardLines.Where(x => x.JCID == JcD.JCID).ToList();
                    _db.JobCardLines.RemoveRange(JcDL);
                    _db.JobCardsMasters.Remove(JcD);
                }

                JobCardsMaster JCN = new JobCardsMaster();
                JCN.JCNumber = lblDocNum.Text.Replace("SO", "JC");
                JCN.JCGUID = Guid.NewGuid();
                JCN.CustomerID = CurrentUser.CoID;
                JCN.JCCreatedDate = DateTime.Now;
                JCN.JCActive = true;
                var WStation = _db.WorkStations.Where(x => x.CompanyID == CurrentUser.CoID && x.Seq == 1).Select(x => new { x.WSID, x.WSName }).FirstOrDefault();
                if (WStation == null)
                {
                    string message = "Workstations have not been configuired yet. Please use the settings to set them up before continuing";
                    AlertHelper.ShowSweetAlert(this, message, "warning");
                    return;
                }

                JCN.JCWSID = WStation.WSID;
                JCN.JCStatus = WStation.WSName;
                var POLines = _db.DocLines.Where(x => x.DocID == docid).OrderBy(x => x.LineID).ToList();
                JCN.JCSummary = POLines[0].ItemDescription;
                JCN.JCQtyOfItems = POLines[0].Quantity;
                _db.JobCardsMasters.Add(JCN);
                _db.SaveChanges();

                // get DocLines and add them to the Jobcard
                foreach (var Ln in POLines)
                {
                    Boolean iskit = false;
                    int HLineType = 0;
                    int LLineType = 0;

                    JobCardLine Jcl = new JobCardLine();
                    Jcl.JCID = JCN.JCID;
                    Jcl.SelectionId = Ln.SelectionId;
                    Jcl.ItemCode = Ln.ItemCode;
                    Jcl.ItemDescription = Ln.ItemDescription;
                    Jcl.LinePickDate = _db.DocHeaders.Where(x => x.DocID == docid).Select(x => x.DueDelDate).FirstOrDefault();
                    Jcl.SBCALineID = Ln.SBCALineID;
                    var bc = _db.ItemBarCodeLinks.Where(x => x.ItemID == Ln.SelectionId).FirstOrDefault();
                    if (bc != null) Jcl.BarCode = bc.BarCode;
                    Jcl.Unit = Ln.Unit;
                    Jcl.UnitPriceExclusive = Ln.UnitPriceExclusive;
                    Jcl.UnitPriceInclusive = Ln.UnitPriceInclusive;
                    Jcl.DiscountPercentage = Ln.DiscountPercentage;
                    Jcl.TaxPercentage = Ln.TaxPercentage;
                    Jcl.LineTaxTypeID = Ln.LineTaxTypeID;
                    Jcl.Exclusive = Ln.Exclusive;
                    Jcl.Discount = Ln.Discount;
                    Jcl.Tax = Ln.Tax;
                    Jcl.Total = Ln.Total;
                    Jcl.Quantity = Ln.Quantity;
                    Jcl.Comments = Ln.Comments;
                    Jcl.UnitCost = Ln.UnitCost;
                    Jcl.isBundle = false;
                    Jcl.isBundleLine = false;
                    Jcl.CompanyID = CurrentUser.CoID;
                    Jcl.LineType = Ln.LineType;
                    Jcl.IsLotTracked = false;
                    if (Ln.LineType == 1) Jcl.LineType = 2;

                    var ThisItem = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == Ln.SelectionId).FirstOrDefault();
                    if (ThisItem != null)
                    {
                        Jcl.Physical = ThisItem.Physical;
                        Jcl.isKit = ThisItem.IsFromKit;
                        Jcl.isKitLine = ThisItem.IsKitComponent;
                        Jcl.IsLotTracked = ThisItem.IsLotTracked;

                        if (ThisItem.IsFromKit != null && (bool)ThisItem.IsFromKit)
                        {
                            var Store = _db.GetItemLinkedStores(CurrentUser.CoID, Jcl.SelectionId).ToList().FirstOrDefault();
                            Jcl.StoreCodeFrom = Store.StoreCode.ToString();
                            Jcl.PickComplete = true;
                        }
                    }
                    else
                    {
                        Jcl.Physical = false;
                        Jcl.isKit = false;
                        Jcl.isKitLine = false;
                        Jcl.IsLotTracked = false;
                    }

                    _db.JobCardLines.Add(Jcl);
                    _db.SaveChanges();
                    long JClineid = Jcl.LineID;
                    // check if this line is a kit item and add kit lines
                    if (ThisItem != null && ThisItem.IsFromKit != null && ThisItem.IsFromKit == true)
                    {
                        iskit = true;
                        HLineType = 3;
                        var KitLines = _db.GetKitLinesFromKitCode(Ln.ItemCode, CurrentUser.CoID).Where(x => x.ItemID != null && x.ItemID > 0 && x.FGQty > 0).ToList();
                        if (KitLines != null && KitLines.Count > 0)
                        {
                            foreach (var KitL in KitLines)
                            {
                                if ((long)KitL.ItemID > 0)
                                {
                                    JobCardLine JclL = new JobCardLine();
                                    JclL.JCID = JCN.JCID;
                                    JclL.LineType = LLineType;
                                    JclL.SelectionId = (long)KitL.ItemID;
                                    JclL.ItemCode = KitL.ItemCode;
                                    JclL.ItemDescription = KitL.Description;
                                    JclL.Physical = ThisItem.Physical;
                                    JclL.LinePickDate = Jcl.LinePickDate;
                                    JclL.SBCALineID = 0;

                                    JclL.Unit = KitL.Unit;
                                    JclL.UnitPriceExclusive = 0;
                                    JclL.UnitPriceInclusive = 0;
                                    JclL.DiscountPercentage = 0;
                                    JclL.TaxPercentage = 0;
                                    JclL.LineTaxTypeID = 0;
                                    JclL.Exclusive = 0;
                                    JclL.Discount = 0;
                                    JclL.Tax = 0;
                                    JclL.Total = 0;

                                    JclL.Quantity = Ln.Quantity * KitL.FGQty;
                                    JclL.UnitCost = 0;
                                    JclL.Comments = "";
                                    JclL.isKit = false;
                                    JclL.isKitLine = true;
                                    JclL.isBundle = false;
                                    JclL.isBundleLine = false;
                                    JclL.CompanyID = CurrentUser.CoID;
                                    JclL.IsLotTracked = KitL.IsLotTracked ?? false;
                                    _db.JobCardLines.Add(JclL);
                                }
                            }
                        }
                        var thisjcl = _db.JobCardLines.Where(x => x.LineID == JClineid).FirstOrDefault();
                        thisjcl.isKit = iskit;
                        thisjcl.LineType = HLineType;
                    }
                }

                // insert jobcardtransaction record
                JobTransaction JTract = new JobTransaction
                {
                    CompanyID = CurrentUser.CoID,
                    JCID = JCN.JCID,
                    FromStationID = WStation.WSID,
                    ToStationID = WStation.WSID,
                    MoveDate = DateTime.Now,
                    MoveQty = JCN.JCQtyOfItems,
                    MoveBy = CurrentUser.RoleID,
                    RejectQty = 0
                };
                _db.JobTransactions.Add(JTract);

                var thispo = _db.DocHeaders.Where(x => x.DocID == docid).FirstOrDefault();
                if (thispo != null)
                {
                    thispo.LinkedJCID = JCN.JCID;
                    //thispo.Started = true;
                    docguid = thispo.DocGUID.ToString();
                }
                try
                {
                    _db.SaveChanges();
                }
                catch 
                {
                }
                Response.Redirect("~/JobCard.aspx?docid=" + docguid.ToString());
            }
        }

        protected void btnSaveConfirm_Click(object sender, EventArgs e)
        {
            docid = Convert.ToInt64(lblDocID.Text);
            string stor = DDStoreH.SelectedValue.ToString();
            if (lblTpe.Text == "Job Card")
            {
                if (txtMsgBody.Text.Length < 4)
                {
                    string message = "Please capture a Job Card # longer than 4 characters";
                    AlertHelper.ShowSweetAlert(this, message, "warning");
                    return;
                }
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    string jcnum = txtMsgBody.Text.ToString().Trim();
                    var jcChk = _db.JobCardsMasters.Where(x => x.JCNumber == jcnum).FirstOrDefault();
                    if (jcChk != null)
                    {
                        string message = "JC Number already in use, please create a new one";
                        AlertHelper.ShowSweetAlert(this, message, "warning");
                        Button2551_ModalPopupExtender.Show();
                        return;
                    }

                    JobCardsMaster JCN = new JobCardsMaster();
                    JCN.CustomerID = CurrentUser.CoID;
                    JCN.JCNumber = txtMsgBody.Text.ToString();
                    JCN.JCGUID = Guid.NewGuid();
                    JCN.JCCreatedDate = DateTime.Now;
                    var WStation = _db.WorkStations.Where(x => x.CompanyID == CurrentUser.CoID && x.Seq == 1).Select(x => new { x.WSID, x.WSName }).FirstOrDefault();
                    JCN.JCWSID = WStation.WSID;
                    JCN.JCStatus = WStation.WSName;
                    JCN.JCActive = true;
                    var POLines = _db.DocLines.Where(x => x.DocID == docid).OrderBy(x => x.LineID).ToList();
                    JCN.JCSummary = POLines[0].ItemDescription;
                    JCN.JCQtyOfItems = POLines[0].Quantity;
                    _db.JobCardsMasters.Add(JCN);
                    _db.SaveChanges();
                    // get DocLines and add them to the Jobcard    
                    foreach (var Ln in POLines)
                    {
                        Boolean iskit = false;
                        int HLineType = 0;
                        int LLineType = 0;

                        JobCardLine Jcl = new JobCardLine();
                        Jcl.JCID = JCN.JCID;
                        Jcl.SelectionId = Ln.SelectionId;
                        Jcl.ItemCode = Ln.ItemCode;
                        Jcl.ItemDescription = Ln.ItemDescription;
                        Jcl.LinePickDate = _db.DocHeaders.Where(x => x.DocID == docid).Select(x => x.DueDelDate).FirstOrDefault();
                        Jcl.SBCALineID = Ln.SBCALineID;
                        var bc = _db.ItemBarCodeLinks.Where(x => x.ItemID == Ln.SelectionId).FirstOrDefault();
                        if (bc != null) Jcl.BarCode = bc.BarCode;
                        Jcl.Unit = Ln.Unit;
                        Jcl.UnitPriceExclusive = Ln.UnitPriceExclusive;
                        Jcl.UnitPriceInclusive = Ln.UnitPriceInclusive;
                        Jcl.DiscountPercentage = Ln.DiscountPercentage;
                        Jcl.TaxPercentage = Ln.TaxPercentage;
                        Jcl.LineTaxTypeID = Ln.LineTaxTypeID;
                        Jcl.Exclusive = Ln.Exclusive;
                        Jcl.Discount = Ln.Discount;
                        Jcl.Tax = Ln.Tax;
                        Jcl.Total = Ln.Total;
                        Jcl.Quantity = Ln.Quantity;
                        Jcl.Comments = Ln.Comments;
                        Jcl.UnitCost = Ln.UnitCost;
                        Jcl.isBundle = false;
                        Jcl.isBundleLine = false;
                        Jcl.CompanyID = CurrentUser.CoID;
                        Jcl.LineType = Ln.LineType;
                        Jcl.IsLotTracked = false;
                        if (Ln.LineType == 1) Jcl.LineType = 2;
                        var ThisItem = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == Ln.SelectionId).FirstOrDefault();
                        if (ThisItem != null)
                        {
                            Jcl.Physical = ThisItem.Physical;
                            Jcl.isKit = ThisItem.IsFromKit;
                            Jcl.isKitLine = ThisItem.IsKitComponent;
                            Jcl.IsLotTracked = ThisItem.IsLotTracked;

                            if (ThisItem.IsFromKit != null && (bool)ThisItem.IsFromKit)
                            {
                                var Store = _db.GetItemLinkedStores(CurrentUser.CoID, Jcl.SelectionId).ToList().FirstOrDefault();
                                Jcl.StoreCodeFrom = Store.StoreCode.ToString();
                                Jcl.PickComplete = true;
                            }
                        }
                        else
                        {
                            Jcl.Physical = false;
                            Jcl.isKit = false;
                            Jcl.isKitLine = false;
                        }

                        _db.JobCardLines.Add(Jcl);
                        _db.SaveChanges();
                        long JClineid = Jcl.LineID;
                        // check if this line is a kit item and add kit lines

                        if (ThisItem != null)
                        {
                            if (ThisItem.IsFromKit != null && ThisItem.IsFromKit == true)
                            {
                                iskit = true;
                                HLineType = 3;
                                var KitLines = _db.GetKitLinesFromKitCode(Ln.ItemCode, CurrentUser.CoID).Where(x => x.ItemID != null && x.ItemID > 0 && x.FGQty > 0).ToList();
                                if (KitLines != null && KitLines.Count > 0)
                                {
                                    foreach (var KitL in KitLines)
                                    {
                                        JobCardLine JclL = new JobCardLine();
                                        JclL.JCID = JCN.JCID;
                                        JclL.LineType = LLineType;
                                        JclL.SelectionId = (long)KitL.ItemID;
                                        JclL.ItemCode = KitL.ItemCode;
                                        JclL.ItemDescription = KitL.Description;
                                        JclL.LinePickDate = Jcl.LinePickDate;
                                        JclL.SBCALineID = 0;
                                        var bcL = _db.ItemBarCodeLinks.Where(x => x.ItemID == KitL.ItemID).FirstOrDefault();
                                        if (bcL != null) JclL.BarCode = bcL.BarCode;
                                        JclL.Unit = KitL.Unit;
                                        JclL.UnitPriceExclusive = 0;
                                        JclL.UnitPriceInclusive = 0;
                                        JclL.DiscountPercentage = 0;
                                        JclL.TaxPercentage = 0;
                                        Jcl.LineTaxTypeID = 0;
                                        JclL.Exclusive = 0;
                                        JclL.Discount = 0;
                                        JclL.Tax = 0;
                                        JclL.Total = 0;

                                        JclL.Quantity = Ln.Quantity * KitL.FGQty;
                                        Jcl.UnitCost = 0;
                                        JclL.Comments = "";
                                        JclL.isKit = false;
                                        JclL.isKitLine = true;
                                        JclL.isBundle = false;
                                        JclL.isBundleLine = false;
                                        JclL.CompanyID = CurrentUser.CoID;
                                        JclL.IsLotTracked = KitL.IsLotTracked ?? false;
                                        _db.JobCardLines.Add(JclL);
                                    }
                                }
                            }
                            var thisjcl = _db.JobCardLines.Where(x => x.LineID == JClineid).FirstOrDefault();
                            thisjcl.isKit = iskit;
                            thisjcl.LineType = HLineType;
                        }
                    }
                    // insert jobcardtransaction record
                    JobTransaction JTract = new JobTransaction
                    {
                        CompanyID = CurrentUser.CoID,
                        JCID = JCN.JCID,
                        FromStationID = WStation.WSID,
                        ToStationID = WStation.WSID,
                        MoveDate = DateTime.Now,
                        MoveQty = JCN.JCQtyOfItems,
                        MoveBy = CurrentUser.RoleID,
                        RejectQty = 0
                    };
                    _db.JobTransactions.Add(JTract);

                    var thispo = _db.DocHeaders.Where(x => x.DocID == docid).FirstOrDefault();
                    if (thispo != null)
                    {
                        thispo.LinkedJCID = JCN.JCID;
                        docguid = thispo.DocGUID.ToString();
                        _db.SaveChanges();
                    }
                    Response.Redirect("~/JobCard.aspx?docid=" + docguid.ToString());
                }
            }
            else if (lblTpe.Text == "Works Order")
            {
                WorksOrderHeader WCHead = new WorksOrderHeader();
                WCHead.CompanyID = CurrentUser.CoID;
                WCHead.Status = "NEW";
                WCHead.Active = true;
                WCHead.LinkedDocumentNum = lblDocNum.Text.ToString();
                WCHead.CustSupName = txtCustName.Text.ToString();
                WCHead.Reference = txtRef.Text.ToString();
                WCHead.DueDate = Convert.ToDateTime(txtSODate.Text, CultureInfo.InvariantCulture);
                WCHead.WOrderDate = DateTime.Now;
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    _db.WorksOrderHeaders.Add(WCHead);
                    _db.SaveChanges();
                    int newfcid = WCHead.ID;

                    int woNumb = _db.WorksOrderHeaders.Where(x => x.CompanyID == CurrentUser.CoID)
                        .OrderByDescending(x => x.WONum)
                        .Select(x => x.WONum)
                        .FirstOrDefault();
                    WCHead.WONum = woNumb + 1;


                    var TempLines = _db.DocLines.Where(x => x.DocID == docid && x.ItemCode != null).OrderBy(x => x.LineID).ToList();
                    //_db.DocLines.Where(x => x.DocID == docid).OrderBy(x => x.LineID).ToList();
                    // for each pickingslip row --> add new WO line
                    foreach (var tl in TempLines)
                    {
                        var _item = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CurrentUser.CoID && i.ID == tl.SelectionId).FirstOrDefault();
                        WorksOrderLine WoL = new WorksOrderLine();
                        WoL.CompanyID = CurrentUser.CoID;
                        WoL.WOID = newfcid;
                        WoL.LineType = 1;
                        if (_item.IsFromBOM == true) WoL.LineType = 2;
                        if (_item.IsFromKit == true) WoL.LineType = 3;
                        WoL.Quantity = tl.Quantity;
                        WoL.ItemCode = tl.ItemCode;
                        WoL.ItemDescription = tl.ItemDescription;
                        WoL.SelectionId = tl.SelectionId;
                        WoL.DueDelDate = Convert.ToDateTime(txtSODate.Text, CultureInfo.InvariantCulture);
                        WoL.Active = true;
                        _db.WorksOrderLines.Add(WoL);
                        _db.SaveChanges();
                        int newWoLid = WoL.LineID;

                        if (_item.IsFromBOM == false && _item.IsFromKit == false)
                        {
                            WorksOrderRMLine RML = new WorksOrderRMLine();
                            RML.LinkedWOLineID = (int)newWoLid;
                            RML.WOID = newfcid;
                            RML.SelectionId = tl.SelectionId;
                            RML.ItemCode = tl.ItemCode;
                            RML.ItemDescription = tl.ItemDescription;
                            RML.Quantity = Convert.ToDecimal(tl.Quantity);
                            RML.CompanyID = CurrentUser.CoID;
                            RML.LinkedFGSelectionID = tl.SelectionId;
                            RML.LinkedFGCode = tl.ItemCode;
                            RML.LinkedFGQty = tl.Quantity;
                            RML.PickComplete = false;
                            RML.Unit = _item.Unit;
                            RML.IsLotTracked = _item.IsLotTracked;
                            _db.WorksOrderRMLines.Add(RML);
                        }
                        else if (_item.IsFromBOM == true)
                        {
                            int bmc = _db.BOMHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.FGCode == tl.ItemCode).Select(x => x.BomHID).FirstOrDefault();
                            if (bmc > 0)
                            {
                                var BomLines = _db.GetBOMLinesFromBomHeaderID(bmc, CurrentUser.CoID);
                                foreach (var bl in BomLines)
                                {
                                    if (bl.ItemID != null)
                                    {
                                        WorksOrderRMLine RML = new WorksOrderRMLine();
                                        RML.LinkedWOLineID = (int)newWoLid;
                                        RML.WOID = newfcid;
                                        RML.SelectionId = (long)bl.ItemID;
                                        RML.ItemCode = bl.ItemCode;
                                        RML.ItemDescription = bl.Description;
                                        RML.Quantity = Convert.ToDecimal(tl.Quantity) * Convert.ToDecimal(bl.RMQty);
                                        RML.CompanyID = CurrentUser.CoID;
                                        RML.LinkedFGSelectionID = tl.SelectionId;
                                        RML.LinkedFGCode = tl.ItemCode;
                                        RML.LinkedFGQty = tl.Quantity;
                                        RML.PickComplete = false;
                                        var itm = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == tl.SelectionId).FirstOrDefault();
                                        RML.Unit = itm.Unit;
                                        RML.IsLotTracked = itm.IsLotTracked;
                                        _db.WorksOrderRMLines.Add(RML);
                                    }
                                }
                            }
                        }
                        else if (_item.IsFromKit == true)
                        {
                            string kmc = _db.KitHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.FGCode == tl.ItemCode).Select(x => x.KitCode).FirstOrDefault();
                            if (kmc != null)
                            {
                                var KitLines = _db.GetKitLinesFromKitCode(kmc, CurrentUser.CoID);
                                foreach (var bl in KitLines)
                                {
                                    if (bl.ItemID != null)
                                    {
                                        WorksOrderRMLine RML = new WorksOrderRMLine();
                                        RML.LinkedWOLineID = (int)newWoLid;
                                        RML.WOID = newfcid;
                                        RML.SelectionId = (long)bl.ItemID;
                                        RML.ItemCode = bl.ItemCode;
                                        RML.ItemDescription = bl.Description;
                                        RML.Quantity = Convert.ToDecimal(tl.Quantity) * Convert.ToDecimal(bl.FGQty);
                                        RML.CompanyID = CurrentUser.CoID;
                                        RML.LinkedFGSelectionID = tl.SelectionId;
                                        RML.LinkedFGCode = tl.ItemCode;
                                        RML.LinkedFGQty = tl.Quantity;
                                        RML.PickComplete = false;
                                        RML.IsLotTracked = (bool)bl.IsLotTracked;
                                        RML.Unit = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == bl.ItemID).Unit;
                                        _db.WorksOrderRMLines.Add(RML);
                                    }
                                }
                            }
                        }
                    }

                    var thisdoc = _db.DocHeaders.Where(x => x.DocID == docid).FirstOrDefault();
                    thisdoc.LinkedWOID = newfcid;
                    _db.SaveChanges();
                    Response.Redirect($"~/WorksOrdersDetailed.aspx?woid={newfcid}", false);
                }
            }
            else
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    PickingSlipMaster PSN = new PickingSlipMaster();
                    PSN.CustomerID = CurrentUser.CoID;
                    // Back order: a Sales Order can have more than one picking slip. Suffix repeats (-2, -3 ...)
                    // so the internal number (also used as the barcode) stays unique.
                    string psIntNumber = lblDocNum.Text.ToString().Replace("SO", "PS");
                    int priorSlips = _db.PickingSlipMasters.Count(x => x.CustomerID == CurrentUser.CoID && x.LinkedSOrdID == docid);
                    if (priorSlips > 0) psIntNumber = psIntNumber + "-" + (priorSlips + 1);
                    PSN.PSIntNumber = psIntNumber;
                    PSN.PSGUID = Guid.NewGuid();
                    PSN.PSCreatedDate = DateTime.Now;
                    PSN.PSCreatedByRoleID = 0;
                    PSN.PSStatus = "Captured";
                    PSN.PSStationID = _db.PickSlipProcesses.Where(x => x.CompanyID == CurrentUser.CoID && x.Seq == 1).Select(x => x.PSPID).FirstOrDefault();
                    PSN.PSActive = true;
                    PSN.PSDueDate = (DateTime)Convert.ToDateTime(txtSODate.Text);
                    PSN.LinkedSOrdID = docid;
                    PSN.LinkedWONumber = 0;
                    if (DDStoreH.SelectedIndex > 0)
                    {
                        PSN.FromStoreID = Convert.ToInt64(_db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == stor).Select(x => x.StoreID).FirstOrDefault());
                    }
                    else { PSN.FromStoreID = 0; }

                    _db.PickingSlipMasters.Add(PSN);
                    try
                    {
                        _db.SaveChanges();
                    }
                    catch { }


                    // get DocLines and add them to the Jobcard
                    var POLines = _db.DocLines.Where(x => x.DocID == docid && x.ItemCode != null).OrderBy(x => x.LineID).ToList();
                    foreach (var Ln in POLines)
                    {
                        PickSlipLine PsL = new PickSlipLine();
                        PsL.PSID = PSN.PSID;
                        PsL.SBCALineID = Ln.SBCALineID;
                        PsL.SelectionId = Ln.SelectionId;
                        PsL.ItemCode = Ln.ItemCode;
                        PsL.ItemDescription = Ln.ItemDescription;
                        var bc = _db.ItemBarCodeLinks.Where(x => x.ItemID == Ln.SelectionId).FirstOrDefault();
                        if (bc != null) PsL.BarCode = bc.BarCode;
                        PsL.Quantity = Ln.Quantity;
                        PsL.Comments = Ln.Comments;
                        PsL.PickComplete = false;
                        if (DDStoreH.SelectedIndex > 0)
                        {
                            PsL.StoreCodeFrom = DDStoreH.SelectedValue.ToString();
                        }
                        else { PsL.StoreCodeFrom = ""; }

                        PsL.LineType = Ln.LineType;
                        PsL.CompanyID = CurrentUser.CoID;
                        PsL.IsLotTracked = false;
                        bool thisItemIsSerial = false;
                        var ThisItem = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == Ln.SelectionId).FirstOrDefault();
                        if (ThisItem != null)
                        {
                            PsL.IsLotTracked = ThisItem.IsLotTracked;
                            thisItemIsSerial = ThisItem.IsSerialTracked;   // item driven, not a company setting
                        }
                        // Serial items are split into lines of 20 here, so each line's serials can
                        // be counted against its quantity on the invoice. 50 becomes 20 + 20 + 10.
                        SerialPicking.AddPickSlipLines(_db, PsL, thisItemIsSerial);
                    }

                    // insert Picking Slip Transaction record
                    PickSlipTransaction PSract = new PickSlipTransaction
                    {
                        CompanyID = CurrentUser.CoID,
                        PSID = PSN.PSID,
                        FromStationID = PSN.PSStationID,
                        ToStationID = PSN.PSStationID,
                        MoveDate = DateTime.Now,
                        MoveQty = 1,
                        MoveBy = CurrentUser.RoleID,
                        RejectQty = 0
                    };
                    _db.PickSlipTransactions.Add(PSract);

                    var thispo = _db.DocHeaders.Where(x => x.DocID == docid).FirstOrDefault();
                    if (thispo != null)
                    {
                        thispo.LinkedPSID = PSN.PSID;
                        //thispo.Started = true;
                        docguid = thispo.DocGUID.ToString();
                    }
                    _db.SaveChanges();
                    Response.Redirect("~/PickingSlip.aspx?docid=" + docguid.ToString());
                }
            }
        }

    }
}
