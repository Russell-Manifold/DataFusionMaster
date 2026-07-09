using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Services.Description;
using System.Web.UI;
using System.Web.UI.WebControls;
using Document = SBMS.Models.Document;
using Font = iTextSharp.text.Font;

namespace SBMS
{
    public partial class Receiving : BasePage
    {
        long docid = 0;
        Guid docguid;
        decimal RecValue = 0, RecTax = 0, RecEx = 0;
        private string LotNumCheck = "";
        decimal addcosts = 0;
        decimal recqty = 0; 
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }

        protected override void Render(System.Web.UI.HtmlTextWriter writer)
        {
            if (GridPOLines.Rows.Count > 0)
            {
                foreach (GridViewRow row in GridPOLines.Rows)
                {
                    if (row.RowType == DataControlRowType.DataRow)
                    {
                        row.Attributes.Add("onclick", Page.ClientScript.GetPostBackEventReference(GridPOLines, "Select$" + row.RowIndex, true));
                    }
                }
            }
            base.Render(writer);
        }
        protected async void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!Guid.TryParse(Request.QueryString["docid"], out docguid))
            {
                Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            LotNumCheck = "OK";
            if (!IsPostBack)
            {
                CalendarExtender2.StartDate = DateTime.Today.AddDays(1);
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

                CalendarExtender1.EndDate = DateTime.Today;
                txtRecDate.Text = DateTime.Today.ToString("dd MMM yyyy");
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var thispo = _db.DocHeaders.Where(x => x.DocGUID == docguid).FirstOrDefault();
                    if (thispo != null)
                    {
                        docid = thispo.DocID;
                        lblDocID.Text = docid.ToString();
                        txtSuppName.Text = thispo.CustSupName.ToString();
                        lblSupplierID.Text = thispo.CustSuppID.ToString();
                        txtDocNum.Text = (thispo.DocumentNumber ?? "").ToString();
                        txtRef.Text = (thispo.Reference ?? "").ToString();
                        txtPODate.Text = Convert.ToDateTime(thispo.DocDate).ToString("dd MMM yyyy");
                        txtMsg.Text = thispo.Message ?? "";
                        decimal exchRate = thispo.Supplier_ExchangeRate ?? 1m;
                        txtExRate.Text = exchRate.ToString("0.####");
                        if (exchRate == 1m)
                        {
                            txtExRate.Visible = false;
                            lblExRate.Visible = false;
                        }

                        if (thispo.CompleteDate != null) txtRecDate.Text = Convert.ToDateTime(thispo.CompleteDate).ToString("dd MMM yyyy");
                        if (thispo.Complete != null)
                        {
                            if (thispo.Complete == true)
                            {
                                chkReceiveComplete.Checked = (Boolean)thispo.Complete;
                                // Allow re-opening a completed PO for further receiving via the status
                                // radio ONLY when something is still outstanding. Otherwise lock it.
                                // NB: "if(!confirm())return false" - NOT "return confirm()" - so that a
                                // confirmed prompt still falls through to the auto-postback.
                                if (HasOutstandingReceiving(_db, docid))
                                {
                                    RBpoStatus.Items[1].Attributes["onclick"] =
                                        "if(!confirm('Re-open this PO for receiving? The Receiving Complete status will be cleared so you can continue receiving the outstanding balance.'))return false;";
                                }
                                else
                                {
                                    RBpoStatus.Enabled = false;   // nothing outstanding - genuinely complete
                                }
                                //lbtnReset.Style.Add("display", "none");
                                lbtnReceiveFinish.Style.Add("display", "none");
                                lbtnRecAll.Style.Add("display", "none");
                                PnlAddCosts.Style.Add("display", "none");
                                txtDNNum.ReadOnly = true;
                                txtInvNum.ReadOnly = true;
                                txtRecDate.ReadOnly = true;
                            }
                        }
                        txtDNNum.Text = thispo?.DNNum?.ToString() ?? "";
                        txtInvNum.Text = thispo?.InvNum?.ToString() ?? "";
                        lblRecBy.Text = ""; // roleid
                        thispo.Started = true;
                        _db.SaveChanges();

                        if (CurrentUser.CompanyUseLotNumbers == false)
                        {
                            lblLotNumH.Attributes.Add("style", "display:none");
                            lblLotNum.Attributes.Add("style", "display:none");
                            hfOriginalLotNumber.Visible=false;
                        }
                        // check if lines already exist
                        int islines = _db.DocLines.Where(x => x.DocID == docid).Count();
                        LoadOtherDetails();
                        ApiUrlCall api = new ApiUrlCall();
                        POReconcileSummary refreshSummary = null;
                        if (islines > 0)
                        {
                            if (Convert.ToBoolean(Request.QueryString["updt"]) == true)
                            {
                                if (thispo.Complete != true)
                                {
                                    refreshSummary = await api.LoadPOLines(docid, CurrentUser);
                                }
                                LoadLines();
                                BindGrid();
                            }
                            else
                            {
                                var TempLines = _db.TempDocLines.Where(x => x.DocID == docid).ToList();
                                if (TempLines.Count == 0)
                                {
                                    LoadLines();
                                }
                                TempLines.Clear();
                                BindGrid();
                            }
                        }
                        else
                        {
                            if (thispo.Complete != true)
                            {
                                refreshSummary = await api.LoadPOLines(docid, CurrentUser);
                            }
                            LoadLines();
                            BindGrid();
                        }

                        // Surface what changed during the Sage refresh so the user can see
                        // why lines may have appeared / disappeared / shifted in quantity.
                        if (refreshSummary != null && refreshSummary.HasChanges)
                        {
                            lblRefreshSummary.Text = refreshSummary.ToBannerText();
                            lblRefreshSummary.Style["display"] = "inline-block";
                        }
                        LoadStores();
                        LoadAttachments(docid);
                        if (DDStore.Items.Count == 0)
                        {
                            btnApprovYes.Attributes.Add("style", "display:none");
                            lbtnReceive.Attributes.Add("style", "display:none");
                            string message = "No stores available for receiving, please go to settings and allow at least 1 store to receive goods";
                            AlertHelper.ShowSweetAlert(this, message, "error");
                        }
                    }
                }
            }
        }

        protected void lbtnRecAll_Click(object sender, EventArgs e)
        {
            docid = Convert.ToInt64(lblDocID.Text);

            // validate store selection up-front so we never write "-Select-" as a StoreCode
            string selectedStore = DDStore.SelectedValue?.ToString() ?? "";
            string selectedStoreText = DDStore.SelectedItem?.Text ?? "";
            if (string.IsNullOrWhiteSpace(selectedStore) || selectedStoreText == "-Select-" || selectedStore == "-Select-")
            {
                AlertHelper.ShowSweetAlert(this, "Please select a valid Store before receiving all items.", "warning");
                return;
            }

            int recnum = GetLotNum(CurrentUser.CoID);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                long.TryParse(lblSupplierID.Text, out long supplierIdParsed);

                var Lines = _db.TempDocLines.Where(x => x.DocID == docid).OrderBy(x => x.LineID).ToList();
                try
                {
                    foreach (TempDocLine dl in Lines)
                {
                    decimal qtyToRec = dl.QtyLeft ?? (dl.Quantity ?? 0);
                    if (qtyToRec <= 0) continue;

                    // Preserve manually-typed ReceiveQty — Select All only fills empty lines.
                    if ((dl.ReceiveQty ?? 0) > 0) continue;

                    if (dl.ItemType == 0)
                    {
                        dl.ReceiveQty = qtyToRec;
                        dl.QtyLeft = 0;
                        dl.ToReceive = true;
                        dl.StoreCode = selectedStore;
                        var itmMaster = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == dl.SelectionId);
                        bool Itm = itmMaster != null && (itmMaster.IsLotTracked == true);
                        if (Itm == true)
                        {
                            // ensure a unique lot number even under concurrent receive activity
                            string candidate;
                            do
                            {
                                candidate = DateTime.Today.ToString("ddMMyyyy") + selectedStore + recnum.ToString();
                                if (!CheckLotNumberExists(candidate)) break;
                                recnum++;
                            } while (true);

                                dl.LotNumber = candidate;
                            LotTrackingMaster LtNew = new LotTrackingMaster();
                            LtNew.LotNumber = dl.LotNumber;
                            LtNew.CreatedDate = DateTime.Now;
                            LtNew.CompanyID = CurrentUser.CoID;
                            LtNew.ItemCode = dl.ItemCode;
                            LtNew.ItemId = dl.SelectionId;
                            LtNew.LotActive = true;
                            LtNew.LotQuantity = qtyToRec;
                            _db.LotTrackingMasters.Add(LtNew);
                            recnum++;
                        }
                    }
                    else
                    {
                        dl.ReceiveQty = qtyToRec;
                        dl.QtyLeft = 0;
                        dl.ToReceive = true;
                        dl.StoreCode = selectedStore;
                    }

                    // Within-session double-click protection only. We match by
                    // TempDocLines.LineID which is stable WITHIN a session but freshly
                    // minted each page load, so prior-cycle OS rows never match here.
                    // That's exactly what we want: each new receive cycle inserts a
                    // fresh OS row instead of overwriting earlier receipt history.
                    // (Cross-session sums use SBCALineID elsewhere - see LoadLines.)
                    long dlSbcaLineId = dl.SBCALineID;
                    var existingOS = _db.ReceivingOutstandings.FirstOrDefault(x =>
                            x.PODocID == docid
                         && x.Archive == false
                         && x.LineID == dl.LineID);
                    if (existingOS != null)
                    {
                        existingOS.RecQty = qtyToRec;
                        existingOS.QtyLeft = 0;
                        existingOS.CreatedBy = CurrentUser.RoleID;
                        existingOS.CreatedDate = DateTime.Now;
                        // Back-fill stable identity if this within-session row lacks one.
                        if (existingOS.SBCALineID == null) existingOS.SBCALineID = dlSbcaLineId;
                    }
                    else
                    {
                        ReceivingOutstanding or = new ReceivingOutstanding
                        {
                            CompanyID = CurrentUser.CoID,
                            PONumber = txtDocNum.Text,
                            PODocID = docid,
                            Supplier = txtSuppName.Text,
                            SupplierID = supplierIdParsed,
                            ItemCode = dl.ItemCode,
                            SelectionId = dl.SelectionId,
                            ItemDescription = dl.ItemDescription,
                            OrigQty = dl.Quantity ?? 0,
                            RecQty = qtyToRec,
                            QtyLeft = 0,
                            LineID = dl.LineID,
                            SBCALineID = dlSbcaLineId,
                            CreatedBy = CurrentUser.RoleID,
                            CreatedDate = DateTime.Now,
                            Archive = false
                        };
                        _db.ReceivingOutstandings.Add(or);
                    }
                }
                
                    _db.SaveChanges();
                }
                //catch (Exception ex)
                //{
                //    new ApiUrlCall().LogErrorToFile(ex.ToString());
                //   // AlertHelper.ShowSweetAlert(this, "Receive All failed: " + ex.Message, "error");
                //    Exception inner = ex;
                //   while (inner.InnerException != null) inner = inner.InnerException;

                //    AlertHelper.ShowSweetAlert(this, "Receive All failed: " + inner.Message, "error");
                //    return;
                //}
                catch (Exception ex)
                {
                    new ApiUrlCall().LogErrorToFile(ex.ToString());

                    Exception inner = ex;
                    while (inner.InnerException != null) inner = inner.InnerException;

                    string msg = "Receive All failed: " + inner.Message;

                    // collapse to a single safe line: no quotes, no backslashes, no line breaks
                    var sb = new System.Text.StringBuilder(msg.Length);
                    foreach (char c in msg)
                    {
                        if (c == '\r' || c == '\n' || c == '\t') sb.Append(' ');
                        else if (c == '\'' || c == '"' || c == '\\' || c == '`') sb.Append(' ');
                        else if (c < 32) continue;            // drop other control chars
                        else sb.Append(c);
                    }
                    msg = sb.ToString();

                    AlertHelper.ShowSweetAlert(this, msg, "error");
                    return;
                }
            }
            BindGrid();
        }
        protected void LoadLines()
        {
            docid = Convert.ToInt64(lblDocID.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Retrieve the records to be deleted
                var tempLinesToDelete = _db.TempDocLines.Where(x => x.DocID == docid).ToList();
                _db.TempDocLines.RemoveRange(tempLinesToDelete);
                _db.SaveChanges();

                // get lines from DocLines. Lines that were removed from the Sage PO
                // after part-receiving stay active locally - the receipt is finished
                // through this app and reconciled in Sage manually afterwards.
                var Lines = _db.DocLines
                              .Where(x => x.DocID == docid)
                              .OrderBy(x => x.LineID)
                              .ToList();

                foreach (var line in Lines)
                {
                    // Sum receivings against this line. New rows are joined by SBCALineID
                    // (the stable Sage identity); legacy rows with NULL SBCALineID fall back
                    // to (PODocID + ItemCode) for the same PO.
                    decimal Prerecqty = _db.ReceivingOutstandings
                             .Where(x => x.PODocID == docid
                                      && x.Archive == false
                                      && (x.SBCALineID == line.SBCALineID
                                          || (x.SBCALineID == null && x.ItemCode == line.ItemCode)))
                             .Sum(x => (decimal?)x.RecQty) ?? 0;

                    decimal newQtyLeft = (line.Quantity ?? 0) - Prerecqty;
                    if (newQtyLeft < 0) newQtyLeft = 0;
                    line.QtyLeft = newQtyLeft;
                    // Do NOT wipe ReceiveQty here. It carries the in-progress staged quantity
                    // (captured on the scanner, or in a prior desktop session) into TempDocLines
                    // below, so receiving resumes and hands off across devices. The finalize resets
                    // it on DocLines after a successful submit, so there is no stale re-receive.
                }
                _db.SaveChanges();

                // Copy them into the TempDocLines

                var tempLines = Lines.Select(line => new TempDocLine
                {
                    DocID = line.DocID,
                    SBCALineID = line.SBCALineID,
                    SelectionId = line.SelectionId,
                    ItemCode = line.ItemCode,
                    ItemDescription = line.ItemDescription,
                    LineType = line.LineType,
                    Quantity = line.Quantity,
                    UnitPriceExclusive = line.UnitPriceExclusive,
                    UnitPriceInclusive = line.UnitPriceInclusive,
                    TaxPercentage = line.TaxPercentage,
                    DiscountPercentage = line.DiscountPercentage,
                    Exclusive = line.Exclusive,
                    Discount = line.Discount,
                    Tax = line.Tax,
                    Total = line.Total,
                    Comments = line.Comments,
                    // Show outstanding AFTER the staged accept + reject (e.g. scanner counts), so a
                    // line counted 5-of-5 shows Qty_Left 0. DocLine.QtyLeft stays as committed-remaining.
                    QtyLeft = Math.Max(0m, (line.QtyLeft ?? 0) - (line.ReceiveQty ?? 0) - line.RejectQty),
                    ReceiveQty = line.ReceiveQty,
                    RejectQty = line.RejectQty,
                    ToReceive = line.ToReceive,
                    ReceiveComplete = line.ReceiveComplete,
                    StoreCode = line.StoreCode,
                    LotNumber = line.LotNumber,
                    ItemType = line.ItemType, 
                    LineTaxTypeID = line.LineTaxTypeID,
                    ExchRate = (decimal) line.ExchRate,
                }).ToList();

                // Insert the new list of entities into the TempDocLines table
                _db.TempDocLines.AddRange(tempLines);

                // Save changes to the database
                _db.SaveChanges();
                LoadAddCosts();
            }
        }

        protected void BindGrid()
        {
            docid = Convert.ToInt64(lblDocID.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                CalcTotals();
                var TempLines = _db.TempDocLines.Where(x => x.DocID == docid).ToList();
                bool containsServ = false; PnlServices.Style.Add("display", "none"); ViewState["pnlServicesDisplay"] = "none";
                // Option A: the estimated-costs modal is for POs WITHOUT in-PO service lines only.
                // Default it visible; hidden below if a service line is detected (those use Branch A).
                LbtnAddCosts.Enabled = true; LbtnAddCosts.Style.Add("display", "inline-block");
                foreach (var TL in TempLines)
                {
                    // detect service / addcost lines (but keep iterating so EVERY row gets formatted)
                    if (containsServ == false && (TL.ItemType == 1 || TL.ItemType == 2))
                    {
                        RBAllocateCosts.Enabled = true;
                        PnlServices.Style.Add("display", "inline-block");
                        ViewState["pnlServicesDisplay"] = "inline-block";
                        PnlServices.Style.Add("max-width", "50%");
                        containsServ = true;
                        LbtnAddCosts.Enabled = false; LbtnAddCosts.Style.Add("display", "none");   // service line present -> hide estimates modal
                        if (TL.ItemType == 2) RBAllocateCosts.Enabled = false;
                    }
                    if (TL.Quantity != null)
                    {
                        TL.Quantity = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(TL.Quantity.ToString(), CurrentUser.CompanyDecPlaces));
                    }
                    if (TL.ReceiveQty != null)
                    {
                        TL.ReceiveQty = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(TL.ReceiveQty.ToString(), CurrentUser.CompanyDecPlaces));
                    }
                    if (TL.QtyLeft != null)
                    {
                        TL.QtyLeft = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(TL.QtyLeft.ToString(), CurrentUser.CompanyDecPlaces));
                    }
                    // Reject uses the same decimal places as the other qty fields; default 0.
                    TL.RejectQty = Convert.ToDecimal(ApiUrlCall.NumberToDecimal((TL.RejectQty ?? 0).ToString(), CurrentUser.CompanyDecPlaces));
                }
                GridPOLines.DataSource = TempLines;
                GridPOLines.DataBind();
                lblSubTotal.Text = RecEx.ToString("N2");
                lblTotVat.Text = RecTax.ToString("N2");
                lblTotal.Text = RecValue.ToString("N2");
            }
        }

        protected void CalcTotals()
        {
            docid = Convert.ToInt64(lblDocID.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var TempLines = _db.TempDocLines.Where(x => x.DocID == docid).ToList();
                // do all other calcs here before binding
                foreach (TempDocLine tl in TempLines)
                {
                    // reset per-line — never inherit the previous row's values
                    decimal OrdQty = tl.Quantity ?? 0;
                    decimal recqty = tl.ReceiveQty ?? 0;
                    decimal LineVal = tl.Total ?? 0;
                    decimal LineTax = tl.Tax ?? 0;
                    decimal LineExTax = (tl.Exclusive != null && tl.ItemType != 2) ? (decimal)tl.Exclusive : 0;

                    if (OrdQty > 0)
                    {
                        tl.ReceiveTotal = (LineVal / OrdQty) * recqty;
                        tl.ReceiveTotalTax = (LineTax / OrdQty) * recqty;
                        tl.ReceiveTotalExcl = (LineExTax / OrdQty) * recqty;
                    }
                    else
                    {
                        tl.ReceiveTotal = 0;
                        tl.ReceiveTotalTax = 0;
                        tl.ReceiveTotalExcl = 0;
                    }

                    if (recqty > 0)
                    {
                        tl.QtyVar = Math.Round(OrdQty / recqty, 2);
                    }
                    else
                    {
                        tl.QtyVar = 0;
                    }

                    if (OrdQty > 0)
                    {
                        RecValue += tl.ReceiveTotal ?? 0;
                        RecTax += tl.ReceiveTotalTax ?? 0;
                        RecEx += tl.ReceiveTotalExcl ?? 0;
                    }
                }
                _db.SaveChanges();
            }
        }
        protected void LoadStores()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true && x.AllowReceiving == true && x.StoreCode != "CoR" && x.StoreCode != "CoD" && x.StoreCode.ToLower() != "scr").ToList(); // && x.StoreCode != "Co"
                if (stores.Count > 0)
                {
                    DDStore.DataSource = stores;
                    DDStore.DataTextField = "StoreDescript";
                    DDStore.DataValueField = "StoreCode";
                    DDStore.DataBind();


                    DDStoreEdit.DataSource = stores;
                    DDStoreEdit.DataTextField = "StoreDescript";
                    DDStoreEdit.DataValueField = "StoreCode";
                    DDStoreEdit.DataBind();
                    DDStoreEdit.Items.Insert(0, "-Select-");

                    if (stores.Count > 1)
                    {
                        DDStore.Items.Insert(0, "-Select-");
                    }
                    btnApprovYes.Attributes.Add("style", "inline-block");
                    lbtnReceive.Attributes.Add("style", "inline-block");
                }
            }
        }

        protected void GridPOLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            //e.Row.Cells[0].Visible = false;
            if (CurrentUser.CompanyUseLotNumbers == false)
            {
                e.Row.Cells[12].Visible = false;
            }
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                if(Convert.ToDecimal(e.Row.Cells[5].Text.ToString()) > (decimal)0)
                {
                    e.Row.Cells[5].BackColor = System.Drawing.Color.Honeydew;
                    e.Row.Cells[5].Font.Bold = true;
                }


                if (e.Row.Cells[4].Text != e.Row.Cells[10].Text)
                {
                    e.Row.Cells[10].BackColor = System.Drawing.Color.AntiqueWhite;
                }
                if (Convert.ToDecimal(e.Row.Cells[14].Text.ToString()) > (decimal)1.05 || Convert.ToDecimal(e.Row.Cells[14].Text.ToString()) < (decimal)0.95) e.Row.Cells[8].Style.Add("border", "1px solid red");
                if (e.Row.Cells[16].Text.ToString() == "2")
                {
                    LinkButton lbtn = new LinkButton();
                    lbtn = (LinkButton)e.Row.FindControl("lbtnItmC");
                    lbtn.Enabled = false;
                    lbtn.ForeColor = System.Drawing.Color.DarkGray;
                }
                if (e.Row.Cells[12].Visible == true && e.Row.Cells[12].Text.ToString().Trim().Replace("&nbsp;","") != "")
                {
                    LinkButton lbtnLotNumAdd = new LinkButton();
                    lbtnLotNumAdd = (LinkButton)e.Row.FindControl("lbtnLotNumAdd");
                    lbtnLotNumAdd.Visible = true;
                }
                recqty += Convert.ToDecimal(e.Row.Cells[8].Text.ToString());
            }
            else if (e.Row.RowType == DataControlRowType.Footer)
            {
                e.Row.Cells[8].Text = recqty.ToString();
            }
            e.Row.Cells[16].Visible = false;
            e.Row.Cells[15].Visible = false;
        }

        protected void lbtnReceive_Click(object sender, EventArgs e)
        {
            if (lblLotNum.Enabled == true)
            {
                if (LotNumCheck != "OK")
                {
                    string message = LotNumCheck.ToString();
                    AlertHelper.ShowSweetAlert(this, message, "error");
                    return;
                }
            }
            
            decimal QtyRec = 0, QtyOrd = 0, QtyLeft = 0;
            try
            {
                QtyRec = decimal.TryParse(txtQtyReceive.Text.Replace(" ", "").Replace("\u00A0", "").Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal resultRec) ? resultRec : 0;
            } catch (Exception ex)
            {
                string message = "Receiving Qty Error - Unable to continue: " + ex.Message;
                AlertHelper.ShowSweetAlert(this, message, "error");
                return;
            }
           
            long lineid = Convert.ToInt64(lblLineID.Text);
            
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Docline = _db.TempDocLines.Where(x => x.LineID == lineid).FirstOrDefault();
                if (Docline == null)
                {
                    AlertHelper.ShowSweetAlert(this, "Receiving line not found, please refresh and try again.", "error");
                    return;
                }
                long docid = Convert.ToInt64(lblDocID.Text);

                QtyOrd = Docline.Quantity ?? 0;

                // Sum receivings against this line by SBCALineID; fall back to ItemCode
                // for legacy rows that pre-date the SBCALineID column.
                long sbcaLineId = Docline.SBCALineID;
                decimal Prerecqty = _db.ReceivingOutstandings
                         .Where(x => x.PODocID == docid
                                  && x.Archive == false
                                  && (x.SBCALineID == sbcaLineId
                                      || (x.SBCALineID == null && x.ItemCode == Docline.ItemCode)))
                         .Sum(x => (decimal?)x.RecQty) ?? 0;

                QtyLeft = QtyOrd - (Prerecqty + QtyRec);
                if (QtyLeft < 0) QtyLeft = 0;
                Docline.QtyLeft = QtyLeft;

                if (QtyLeft > 0)
                {
                    RBpoStatus.SelectedIndex = 1;
                }

                if (QtyRec > 0)
                {
                    ReceivingOutstanding or = new ReceivingOutstanding
                    {
                        CompanyID = CurrentUser.CoID,
                        PONumber = txtDocNum.Text,
                        PODocID = Convert.ToInt64(lblDocID.Text),
                        Supplier = txtSuppName.Text,
                        SupplierID = Convert.ToInt64(lblSupplierID.Text), // get supplier id from docheader
                        ItemCode = Docline.ItemCode,
                        SelectionId = Convert.ToInt64(Docline.SelectionId),
                        ItemDescription = Docline.ItemDescription,
                        OrigQty = QtyOrd,
                        RecQty = QtyRec,
                        QtyLeft = QtyLeft,
                        LineID = Convert.ToInt64(lblLineID.Text),
                        SBCALineID = sbcaLineId,
                        CreatedBy = CurrentUser.RoleID,
                        CreatedDate = DateTime.Now,
                        Archive = false
                    };

                    _db.ReceivingOutstandings.Add(or);
                }
                
                if (chkAddLotNum.Checked == true)
                {
                    // add new row to the document safely by creating a fresh record instead of an object reference
                    TempDocLine Tdl = new TempDocLine
                    {
                        DocID = Docline.DocID,
                        SBCALineID = Docline.SBCALineID,
                        SelectionId = Docline.SelectionId,
                        ItemCode = Docline.ItemCode,
                        ItemDescription = Docline.ItemDescription,
                        LineType = Docline.LineType,
                        Quantity = Docline.Quantity,
                        UnitPriceExclusive = Docline.UnitPriceExclusive,
                        UnitPriceInclusive = Docline.UnitPriceInclusive,
                        TaxPercentage = Docline.TaxPercentage,
                        DiscountPercentage = Docline.DiscountPercentage,
                        Exclusive = Docline.Exclusive,
                        Discount = Docline.Discount,
                        Tax = Docline.Tax,
                        Total = Docline.Total,
                        Comments = Docline.Comments,
                        QtyLeft = Docline.QtyLeft,
                        ToReceive = Docline.ToReceive,
                        ReceiveComplete = Docline.ReceiveComplete,
                        StoreCode = Docline.StoreCode,
                        ItemType = Docline.ItemType,
                        LineTaxTypeID = Docline.LineTaxTypeID,
                        ExchRate = Docline.ExchRate,
                        LotNumber = lblLotNum.Text,
                        ReceiveQty = QtyRec
                    };
                    _db.TempDocLines.Add(Tdl);
                    _db.SaveChanges();
                    lineid = Tdl.LineID;
                }
                _db.SaveChanges();
            }

            if (DDStore.Enabled == true)
            {
                if (DDStore.Items.Count > 1)
                {
                    if (DDStoreEdit.SelectedIndex == 0)
                    {
                        string message = "Invalid Store Selected, Unable to continue.";
                        AlertHelper.ShowSweetAlert(this, message, "error");
                        return;
                    }
                }
                else
                if (DDStore.Items.Count == 1)
                {
                    if (DDStoreEdit.SelectedItem.Text == "-Select-")
                    {
                        string message = "Invalid Store Selected, Unable to continue.";
                        AlertHelper.ShowSweetAlert(this, message, "error");
                        return;
                    }
                }
                else
                {
                    string message = "Invalid Store Selected, Unable to continue.";
                    AlertHelper.ShowSweetAlert(this, message, "error");
                    return;
                }
            }
          
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Docline = _db.TempDocLines.Where(x => x.LineID == lineid).FirstOrDefault();
                if (Docline == null)
                {
                    AlertHelper.ShowSweetAlert(this, "Receiving line not found, please refresh and try again.", "error");
                    return;
                }
                if(DDStore.Enabled == true) Docline.StoreCode = DDStoreEdit.SelectedValue.ToString();
                Docline.ReceiveQty = QtyRec;
                Docline.QtyLeft = QtyLeft < 0 ? 0 : QtyLeft;
                if (lblLotNum.Enabled == true) Docline.LotNumber = lblLotNum.Text;
                Docline.ToReceive = true;
                Docline.ReceiveComplete = false;
                if (lblLotNum.Enabled == true)
                {
                    // save new Lot Number to db
                    LotTrackingMaster LtNew = new LotTrackingMaster();
                    LtNew.LotNumber = Docline.LotNumber;
                    LtNew.CreatedDate = DateTime.Now;
                    LtNew.CompanyID = CurrentUser.CoID;
                    LtNew.ItemCode = Docline.ItemCode;
                    LtNew.ItemId = Docline.SelectionId;
                    LtNew.LotActive = true;
                    
                    decimal LotQty = 1;
                    try
                    {
                        LotQty = Convert.ToDecimal(txtNumPieces.Text);
                    }
                    catch { }
                    LtNew.LotQuantity = LotQty;
                    if (txtLotNote.Text.ToString().Trim().Length > 0)
                    {
                        LtNew.LotUserDefined = txtLotNote.Text.ToString().Trim();
                    }
                    try
                    {
                        DateTime ubDate = Convert.ToDateTime(txtRecDate.Text);
                        LtNew.UseByDate = ubDate;
                    }
                    catch { }

                    _db.LotTrackingMasters.Add(LtNew);
                }
                _db.SaveChanges();
                lblLineID.Text = "";
            }
            BindGrid();
        }

        private bool CheckLotNumberExists(string lotnumber)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                return _db.LotTrackingMasters.Any(l => l.LotNumber == lotnumber && l.CompanyID == CurrentUser.CoID);
            }

        }
        protected async void lbtnReceiveFinish_Click(object sender, EventArgs e)
        {
            docid = Convert.ToInt64(lblDocID.Text);
            decimal exchRate =1;
            string SuppInvN = "";

            // require at least one of Delivery Note # or Supplier Invoice # to be a real value (>= 3 chars)
            string dnTrim = (txtDNNum.Text ?? "").Trim();
            string invTrim = (txtInvNum.Text ?? "").Trim();
            if (dnTrim.Length < 3 && invTrim.Length < 3)
            {
                string warnMsg = "Please capture a Delivery Note # or Supplier Invoice # of at least 3 characters.";
                AlertHelper.ShowSweetAlert(this, warnMsg, "warning");
                return;
            }
            DateTime dt1 = DateTime.Today;
            if (!DateTime.TryParse(txtRecDate.Text, out dt1))
            {
                string warnMsg = "Please capture a valid Receiving Date.";
                AlertHelper.ShowSweetAlert(this, warnMsg, "warning");
                return;
            }

            try
            {
                DateTime dt = Convert.ToDateTime(txtRecDate.Text);
            }
            catch
            {
                string warnMsg = "Invalid Receive Date, unable to continue.";
                AlertHelper.ShowSweetAlert(this, warnMsg, "warning");
                return;
            }

            // ---------------------------------------------------------------------
            // Partial-receive guard.
            //
            // If the user has flagged Receiving Complete = Yes BUT some line on the
            // PO still has ordered > received-so-far, we interrupt with a confirm.
            // This prevents accidentally closing a PO in Sage when only part has
            // been received. The user can override by clicking Yes on the prompt -
            // we set hfPartialOverride = "1" client-side and re-post.
            //
            // Receivings for the current batch are already persisted as
            // ReceivingOutstandings by per-line Save or Select All before we get
            // here, so the sum below is the full picture.
            // ---------------------------------------------------------------------
            if (RBpoStatus.SelectedValue == "0" && hfPartialOverride.Value != "1")
            {
                bool isPartial;
                using (SBMSEntities _dbChk = new SBMSEntities(Config.GetConnectionString()))
                {
                    isPartial = _dbChk.DocLines
                        .Where(dl => dl.DocID == docid)
                        .Any(dl =>
                            (dl.Quantity ?? 0) >
                            (_dbChk.ReceivingOutstandings
                                   .Where(r => r.PODocID == docid
                                            && r.Archive == false
                                            && (r.SBCALineID == dl.SBCALineID
                                                || (r.SBCALineID == null && r.ItemCode == dl.ItemCode)))
                                   .Sum(r => (decimal?)r.RecQty) ?? 0));
                }

                if (isPartial)
                {
                    string confirmJs =
                        "Swal.fire({" +
                        "  icon: 'warning'," +
                        "  title: 'PO not fully received'," +
                        "  html: 'You have received only <b>part</b> of this PO but flagged <b>Receiving Complete = Yes</b>.<br/><br/>" +
                                "Continuing will close the PO in Sage and any unreceived lines will be removed.<br/><br/>" +
                                "Are you sure?'," +
                        "  showCancelButton: true," +
                        "  confirmButtonText: 'Yes, close anyway'," +
                        "  cancelButtonText: 'No, keep PO open'," +
                        "  reverseButtons: true," +
                        "  focusCancel: true" +
                        "}).then(function(result) {" +
                        "  if (result.isConfirmed) {" +
                        "    document.getElementById('" + hfPartialOverride.ClientID + "').value = '1';" +
                        "    __doPostBack('" + lbtnReceiveFinish.UniqueID + "', '');" +
                        "  } else {" +
                        "    if (typeof enableReceiveButtons === 'function') enableReceiveButtons();" +
                        "  }" +
                        "});";

                    ScriptManager.RegisterStartupScript(
                        this, this.GetType(), "PartialReceiveConfirm", confirmJs, true);
                    return;
                }
            }

            // Reset the override so a subsequent receive starts clean.
            hfPartialOverride.Value = "";

            try
            {
                ApiUrlCall api = new ApiUrlCall();
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {

                    // get DocHeader
                    var Head = _db.DocHeaders.Where(x => x.DocID == docid).FirstOrDefault();

                    // check if Supplier Inv Num has already been received
                    // prefer the supplier invoice # when present, otherwise fall back to the delivery note #
                    if (invTrim.Length >= 1)
                    {
                        SuppInvN = invTrim;
                    }
                    else
                    {
                        SuppInvN = dnTrim;
                    }
                    // check and update Foreign Currency rate
                    int SuppInvNumb = _db.DocHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.SupplierInvNum == SuppInvN).Count();
                    if (SuppInvNumb > 0)
                    {
                        string warnMsg = "Supplier Invoice number already used, unable to duplicate.";
                        AlertHelper.ShowSweetAlert(this, warnMsg, "warning");
                        return;
                    }

                    var FLines = _db.TempDocLines.Where(x => x.DocID == docid && x.ToReceive == true && x.ReceiveComplete == false).ToList();
                    if (FLines.Count == 0)
                    {
                        string message = "Please receive line items before continuing.";
                        AlertHelper.ShowSweetAlert(this, message, "warning");
                        return;
                    }

                    #region create new documents preparing for Sage
                    // 3) Save supplier Invoice in SBCA.
                    Document Doc = new Document();
                    // add document header
                    SupplierInvoiceHeader DocH = new SupplierInvoiceHeader();

                    //ID = Head.DocID,
                    DocH.DueDate = dt1;
                    DocH.SupplierId = (long)Head.CustSuppID;
                    DocH.SupplierName = Head.CustSupName.ToString();
                    DocH.StatusId = 1;
                    DocH.Date = dt1;
                    DocH.Inclusive = (bool)Head.Inclusive;
                    DocH.DiscountPercentage = (decimal)Head.DiscountPercentage;
                    DocH.TaxReference = Head.TaxReference.ToString();
                    DocH.Reference = SuppInvN.ToString();
                    DocH.Message = Head.Message.ToString();
                    DocH.FromDocument = Head.DocumentNumber.ToString();
                    if (Head.Supplier_CurrencyId != null)
                    {
                        try
                        {
                            DocH.Supplier_CurrencyId = (long)Head.Supplier_CurrencyId;
                            exchRate = Convert.ToDecimal(txtExRate.Text);
                            DocH.Supplier_ExchangeRate = (decimal)exchRate;
                        }
                        catch { }
                    }

                    List<DocumentLine> documentLines = new List<DocumentLine>();
                    List<DocumentItemList> dil = new List<DocumentItemList>();
                    foreach (var dl in FLines)
                    {
                        if (dl.ItemType == 0)
                        {
                            if (dl.StoreCode == null || dl.StoreCode.ToString() == "")
                            {
                                string message = "Invalid Store Code for " + dl.ItemDescription + ". Unable to continue.";
                                AlertHelper.ShowSweetAlert(this, message, "error");
                                return;
                            }
                        }

                        if (dl.ItemType < 2)
                        {
                            DocumentItemList dilF = new DocumentItemList();
                            dilF.SelectionId = dl.SelectionId;
                            dilF.CompanyID = CurrentUser.CoID;
                            dil.Add(dilF);

                            DocumentLine DL = new DocumentLine();
                            DL.SelectionId = dl.SelectionId;
                            DL.TaxTypeId = (int)dl.LineTaxTypeID;
                            DL.Description = dl.ItemDescription;
                            DL.LineType = (int)dl.LineType;  // 0 = Inventory Item
                            DL.Quantity = (decimal)dl.ReceiveQty + (dl.RejectQty ?? 0);   // bill accept + reject; reject is split to the reject store after the GRN
                            DL.UnitPriceExclusive = (decimal)dl.UnitPriceExclusive;
                            DL.UnitPriceInclusive = (decimal)dl.UnitPriceInclusive;
                            DL.Unit = dl.Unit;
                            DL.DiscountPercentage = (decimal)dl.DiscountPercentage;
                            DL.TaxPercentage = (decimal)dl.TaxPercentage;
                            DL.TaxTypeId = (int)dl.LineTaxTypeID;
                            DL.Exclusive = (decimal)dl.Exclusive;
                            DL.Discount = (decimal)dl.Discount;
                            DL.Tax = (decimal)dl.Tax;
                            DL.Total = (decimal)dl.Total;
                            DL.ExchRate = (decimal)exchRate;
                            DL.localCurrLineVal = (DL.Quantity * DL.UnitPriceExclusive) / exchRate;
                            if (dl.LotNumber != null && dl.LotNumber.ToString() != "" && dl.StoreCode != null && dl.StoreCode.ToString() != "")
                            {
                                DL.Comments = "Store: " + dl.StoreCode + " - Lot # " + dl.LotNumber + " : " + dl.Comments;
                            }
                            else if (dl.StoreCode != null && dl.StoreCode.ToString() != "")
                            {
                                DL.Comments = "Store: " + dl.StoreCode + " : " + dl.Comments;
                            }
                            else if (dl.LotNumber != null && dl.LotNumber.ToString() != "")
                            {
                                DL.Comments = "Lot # " + dl.LotNumber + " : " + dl.Comments;
                            }
                            else
                            {
                                DL.Comments = dl.Comments;
                            }
                            if (dl.AnalysisCategoryId1 != null) DL.AnalysisCategoryId1 = (long)dl.AnalysisCategoryId1;
                            if (dl.AnalysisCategoryId2 != null) DL.AnalysisCategoryId2 = (long)dl.AnalysisCategoryId2;
                            if (dl.AnalysisCategoryId3 != null) DL.AnalysisCategoryId3 = (long)dl.AnalysisCategoryId3;
                            documentLines.Add(DL);
                        }
                    } ;
                    #endregion

                    #region get and store original average costs for later use
                    // call dil list of items to get all original Average costs and quantities beforew Sage receiving
                    // build filter from dil list
                    string avcostfiltster = "";
                    foreach (var dilItem in dil)
                    {
                        avcostfiltster += "ID eq " + dilItem.SelectionId.ToString() + " or ";
                    }
                    List<DocumentLineOrigValues> avcostlist;
                    if (avcostfiltster.Length > 4)
                    {
                        avcostfiltster = avcostfiltster.Substring(0, avcostfiltster.Length - 4);
                        avcostlist = await api.LoadOriginalAvCostsAsync(avcostfiltster, CurrentUser);
                    }
                    else
                    {
                        avcostlist = new List<DocumentLineOrigValues>();
                    }
                    #endregion

                    #region CollateDocForsending
                    string SupInv = string.Empty; string SuppInvNum = ""; string jsonBody;
                    if (CurrentUser.UATMode == false)
                    {
                        Doc.Header = DocH;
                        Doc.Lines = documentLines;
                        object jsonObject;

                        if (Doc.Header.Supplier_ExchangeRate == 1)
                        {
                            jsonObject = new
                            {
                                //Doc.Header.ID,
                                Doc.Header.DueDate,
                                Doc.Header.SupplierId,
                                Doc.Header.SupplierName,
                                Doc.Header.StatusId,
                                Doc.Header.Date,
                                Doc.Header.Inclusive,
                                Doc.Header.DiscountPercentage,
                                Doc.Header.TaxReference,
                                Doc.Header.Reference,
                                Doc.Header.Message,
                                Doc.Header.FromDocument,
                                Doc.Lines
                            };
                        }
                        else
                        {
                            jsonObject = new
                            {
                                //Doc.Header.ID,
                                Doc.Header.DueDate,
                                Doc.Header.SupplierId,
                                Doc.Header.SupplierName,
                                Doc.Header.StatusId,
                                Doc.Header.Date,
                                Doc.Header.Inclusive,
                                Doc.Header.DiscountPercentage,
                                Doc.Header.TaxReference,
                                Doc.Header.Reference,
                                Doc.Header.Message,
                                Doc.Header.FromDocument,
                                Doc.Header.Supplier_ExchangeRate,
                                Doc.Header.Supplier_CurrencyId,
                                Doc.Lines
                            };
                       }

                        // First serialize your anonymous object
                        var rawJson = JsonConvert.SerializeObject(jsonObject);

                        // Parse as JObject so we can modify it dynamically
                        var jObjT = JObject.Parse(rawJson);

                        // Remove unwanted properties from each line
                        foreach (var line in jObjT["Lines"])
                        {
                            //line["ItemType"]?.Parent.Remove();
                            line["CurrencyId"]?.Parent.Remove();
                            line["ExchRate"]?.Parent.Remove();
                            line["localCurrLineVal"]?.Parent.Remove();
                        }
                        jsonBody = jObjT.ToString(Formatting.Indented);
                        //jsonBody = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

                        SupInv = await SendSupplierInvoice(jsonBody);

                        if (long.TryParse(SupInv.Split('|')[0], out long parsedValue))
                        {
                            SuppInvNum = SupInv.Split('|')[1].ToString();

                            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                            if (RBpoStatus.SelectedValue == "0")
                            {
                                // PO is fully received -> flip it to Invoiced (StatusId 4) WITHOUT altering its lines. We cannot reuse jsonBody (the supplier-invoice payload): it only carries THIS
                                // batch's lines, so posting it to PurchaseOrder/Save would truncate the PO; and stripping Lines out makes Sage reject the save (500). Instead we fetch the live PO from
                                // Sage and post it back with only the status changed - so the payload always holds the PO's own complete lines.
                                ApiUrlCall Api = new ApiUrlCall();
                                string poGetUrl = ApiUrlCall.sageurl + "PurchaseOrder/GET/" + docid +
                                    "?includeDetail={True}&includeSupplierDetails={True}&apikey={" +
                                    ApiUrlCall.APIKey + "}&CompanyID=" + CurrentUser.CoID;
                                JObject livePO = await Api.ApiCallAsync(poGetUrl, CurrentUser);

                                if (livePO != null && livePO["error"] == null && livePO["Lines"] != null)
                                {
                                    livePO["StatusId"] = 4;
                                    string updatedJsonBody = livePO.ToString(Formatting.Indented);
                                    JObject parsedJSON = await Api.APIUpdatePurchaseOrderAsync("PurchaseOrder", updatedJsonBody, CurrentUser);
                                }
                            }
                            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        }
                        else
                        {
                            string message = $"Error sending Supplier Invoice to Sage, Err: {SupInv} ";
                            AlertHelper.ShowSweetAlert(this, message, "error");
                            return;
                        }
                    }
                    else
                    {
                        SupInv = "000000";
                        SuppInvNum = "SINV0000 (Sample)";
                    }
                    #endregion

                    #region Creating Supplier Adjustments For Additional Costs Lines  
                    // get add costs
                    if (CurrentUser.UATMode == false)
                    {
                        if (GridAddCosts.Rows.Count > 0 && RBpoStatus.SelectedValue.ToString() == "0")
                        {
                            var AddCosts = _db.ReceivingAddCosts.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == docid && x.Imported != true).ToList();
                            foreach (var Adcost in AddCosts)
                            {
                                SupplierAdjustment SuppAdj = new SupplierAdjustment();
                                var jsonObject = new
                                {
                                    Date = Adcost.DocDate,
                                    SupplierId = Adcost.SupplierID,
                                    DocumentNumber = Adcost.FromDocument,
                                    Reference = $"{txtDocNum.Text}  estimated additional costs",
                                    Description = $"{txtDocNum.Text} {Adcost.Message.ToString()}",
                                    TaxTypeId = Adcost.TaxType?.ToString(),
                                    Adcost.Exclusive,
                                    Tax = Adcost.Vat,
                                    Total = Adcost.Exclusive + Adcost.Vat,
                                    ContraAccountId = Adcost.SelectionID?.ToString()
                                };
                                jsonBody = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
                                SupInv = await SendSupplierAdjustment(jsonBody);
                            }
                        }
                    }
                    #endregion

                    #region AllocatingAdditionalCosts
                    decimal ThisLineVal = 0;
                    decimal AddCostPerc = 0;
                    decimal AddCostPropValue = 0;
                    decimal ThisItemNettCost = 0;
                    decimal ThisItemUnitNett = 0;
                    long itemtransnum = 0;

                    int LCount = 0;
                    // check if there are line type 2 in the lines
                    // if yes, then remove stock and replace at the new value
                    var LnChk = FLines.Where(x => x.ItemType > 0).FirstOrDefault();
                    if (LnChk != null)
                    {
                        foreach (var dl in FLines)
                        {
                            // update internal lines and create relevantr records
                            if (dl.ToReceive == true)
                            {
                                decimal dlRecQty = dl.ReceiveQty ?? 0;
                                // W1 guard: only auto-allocate the in-PO service cost on a COMPLETE receive.
                                // On a partial receive, drop to base cost - the user handles add-costs manually.
                                if (RBAllocateCosts.SelectedValue.ToString() == "1" && RBpoStatus.SelectedValue.ToString() == "0")
                                {
                                    decimal totalPriceExclusive = (decimal)FLines.Where(x => x.ItemType == 0).Sum(x => x.Exclusive);
                                    decimal AddCost = (decimal)FLines.Where(x => x.ItemType > 0).Sum(x => x.UnitPriceExclusive * x.ReceiveQty);
                                    // get total value of docLines
                                    ThisLineVal = (decimal)dl.Exclusive;
                                    AddCostPerc = totalPriceExclusive != 0 ? ThisLineVal / totalPriceExclusive : 0;
                                    AddCostPropValue = AddCostPerc * AddCost;
                                    ThisItemNettCost = (decimal)dl.Exclusive + AddCostPropValue;
                                    ThisItemUnitNett = dlRecQty != 0 ? ThisItemNettCost / dlRecQty : 0;
                                }
                                else
                                {
                                    ThisLineVal = (decimal)dl.Exclusive;
                                    AddCostPerc = 0;
                                    AddCostPropValue = 0;
                                    ThisItemNettCost = (decimal)dl.Exclusive;
                                    ThisItemUnitNett = dlRecQty != 0 ? ThisItemNettCost / dlRecQty : 0;
                                }

                                if (dl.ItemType == 0)
                                {
                                    var tempLine = _db.DocLines.Where(x => x.SBCALineID == dl.SBCALineID).FirstOrDefault();
                                    tempLine.ItemDescription = dl.ItemDescription;
                                    tempLine.Quantity = dl.Quantity;
                                    tempLine.UnitPriceExclusive = dl.UnitPriceExclusive;
                                    tempLine.TaxPercentage = dl.TaxPercentage;
                                    tempLine.DiscountPercentage = dl.DiscountPercentage;
                                    tempLine.Exclusive = dl.Exclusive;
                                    tempLine.Discount = dl.Discount;
                                    tempLine.Tax = dl.Tax;
                                    tempLine.Total = dl.Total;
                                    tempLine.Comments = dl.Comments;
                                    tempLine.QtyLeft = dl.QtyLeft;
                                    tempLine.ReceiveQty = 0;   // reset on submit so the line reopens clean (matches no-add-costs branch)
                                    tempLine.RejectQty = 0;
                                    tempLine.ToReceive = dl.ToReceive;
                                    tempLine.ReceiveComplete = true;
                                    tempLine.StoreCode = dl.StoreCode;
                                    tempLine.LotNumber = dl.LotNumber;
                                    tempLine.LineTaxTypeID = dl.LineTaxTypeID;
                                    tempLine.AddCostsAmount = 0;
                                    tempLine.AddCostsAmount = AddCostPropValue;
                                    tempLine.AddCostsReason = txtAddCostsReason.Text.ToString();
                                    tempLine.ExchRate = dl.ExchRate;

                                    var ItmCon = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == dl.SelectionId).FirstOrDefault();
                                    decimal ConvRate = (decimal)ItmCon.UOMConvert;

                                    // 1) Create transaction to move items from supplier into the selected warehouse.
                                    ItemTransaction ItemTrans = new ItemTransaction();
                                    ItemTrans.CompanyID = CurrentUser.CoID;
                                    ItemTrans.DocumentID = dl.DocID;
                                    ItemTrans.TransactionType = "GRN";
                                    ItemTrans.ItemID = dl.SelectionId;
                                    ItemTrans.ItemCode = dl.ItemCode;
                                    ItemTrans.ItemDescription = dl.ItemDescription;
                                    ItemTrans.LotNumber = dl.LotNumber;
                                    ItemTrans.Unit = dl.Unit;
                                    ItemTrans.FromID = getstoreid("CoR");
                                    ItemTrans.ToID = getstoreid(dl.StoreCode);
                                    ItemTrans.Qty = dl.ReceiveQty * ConvRate;
                                    ItemTrans.PriceExclusive = (dl.UnitPriceExclusive / exchRate) / ConvRate;
                                    ItemTrans.AdditionalCosts = (ThisItemUnitNett / exchRate) - (dl.UnitPriceExclusive / exchRate);
                                    ItemTrans.TotalUnitPriceExclInclAdd = (ThisItemUnitNett / exchRate) / ConvRate;
                                    ItemTrans.TotalLineValExcl = (ThisItemUnitNett * dl.ReceiveQty) / exchRate;
                                    ItemTrans.DocumentType = 2;
                                    ItemTrans.TransactionReference = SuppInvNum;
                                    // Additional costs ????
                                    ItemTrans.TransactionDate = DateTime.Now;
                                    ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid
                                    ItemTrans.ExchRate = dl.ExchRate;
                                    _db.ItemTransactions.Add(ItemTrans);

                                    if (dl.LotNumber != null)
                                    {
                                        var LotNumUpdate = _db.LotTrackingMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.LotNumber == dl.LotNumber).FirstOrDefault();
                                        LotNumUpdate.LotTotUnitPrice = ThisItemUnitNett / exchRate;
                                    }

                                    // check for itemstore link
                                    var ItS = _db.ItemStoreLinkMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreID == ItemTrans.ToID && x.ItemID == dl.SelectionId).FirstOrDefault();
                                    if (ItS == null)
                                    {
                                        // create item/store link
                                        ItemStoreLinkMaster isL = new ItemStoreLinkMaster
                                        {
                                            CompanyID = CurrentUser.CoID,
                                            ItemID = Convert.ToInt64(dl.SelectionId),
                                            StoreID = (int?)ItemTrans.ToID,
                                            Active = true,
                                        };
                                        _db.ItemStoreLinkMasters.Add(isL);
                                    }
                                    _db.SaveChanges();
                                    itemtransnum = ItemTrans.TrnID;
                                }
                                LCount++;
                                #endregion

                                if (dl.ItemType == 0)
                                {
                                    await api.LoadOneItem(dl.SelectionId, CurrentUser);
                                    var Itm = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == dl.SelectionId).FirstOrDefault();
                                    if (Itm.AverageCost != ThisItemUnitNett / exchRate)
                                    {
                                        #region AdjustItemOut
                                        ItemAdjustment iAdj = new ItemAdjustment();
                                        iAdj.Date = DateTime.Now;
                                        iAdj.ItemID = dl.SelectionId;
                                        // update master record of item before adjustments
                                        iAdj.AverageCost = (decimal)dl.UnitPriceExclusive / exchRate;
                                        iAdj.Quantity = (decimal)dl.ReceiveQty * -1;
                                        iAdj.Reason = "ADJ Out Trans ID: " + itemtransnum + " - " + txtAddCostsReason.Text.ToString();
                                        iAdj.Created = DateTime.Now;
                                        jsonBody = JsonConvert.SerializeObject(iAdj, Formatting.Indented);
                                        if (CurrentUser.UATMode == false)
                                        {
                                            await SendItemAdjustment(jsonBody);
                                        }
                                        #endregion
                                        // --------------------------------------------------
                                        #region AdjustItemIn
                                        // adjust items back in at new price including additional costs
                                        decimal origqty = 0; decimal origavcost = 0; decimal origvalue = 0;
                                        try
                                        {
                                            origqty = avcostlist.Where(x => x.ID == dl.SelectionId).FirstOrDefault().QuantityOnHand;
                                            origavcost = avcostlist.Where(x => x.ID == dl.SelectionId).FirstOrDefault().AverageCost;
                                            origvalue = origqty * origavcost;
                                        }
                                        catch { }

                                        decimal ThisUnitVal = ThisItemUnitNett;
                                        decimal NewQty = (decimal)(origqty + dl.ReceiveQty);
                                        // Sage must receive the SAME uplifted value written to the lot/ledger
                                        // (ThisItemUnitNett incl. allocated add-cost), not the base price -
                                        // otherwise the SBCA average understates by the additional cost.
                                        decimal NewTotVal = (decimal)(origvalue + (ThisItemUnitNett * dl.ReceiveQty));
                                        decimal NewAvCost = (NewTotVal / NewQty) / exchRate;

                                        iAdj = new ItemAdjustment();
                                        iAdj.Date = DateTime.Now;
                                        iAdj.ItemID = dl.SelectionId;
                                        iAdj.AverageCost = NewAvCost;
                                        iAdj.Quantity = (decimal)dl.ReceiveQty;
                                        iAdj.Reason = "ADJ IN Trans ID: " + itemtransnum + " - " + txtAddCostsReason.Text.ToString();
                                        iAdj.Created = DateTime.Now;
                                        jsonBody = JsonConvert.SerializeObject(iAdj, Formatting.Indented);
                                        if (CurrentUser.UATMode == false)
                                        {
                                            await SendItemAdjustment(jsonBody);
                                        }
                                        #endregion
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // W1: estimated additional costs only apply on a COMPLETE receive, and only
                        // across lines fully received in this action (no outstanding balance). Partial
                        // receives defer the estimate (rows left un-imported) so nothing under-allocates.
                        bool receiveComplete = RBpoStatus.SelectedValue.ToString() == "0";
                        var AddC = _db.ReceivingAddCosts.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == docid && x.Imported != true).ToList();
                        decimal totAddCosts = 0;
                        decimal DocValue = (decimal)Head.Exclusive;
                        // denominator for the add-cost split = value of the fully-received stock lines only
                        decimal fullLinesValue = FLines
                            .Where(x => x.ToReceive == true && x.ItemType == 0
                                     && ((x.QtyLeft ?? 0) - (x.ReceiveQty ?? 0) - (x.RejectQty ?? 0)) <= 0)
                            .Sum(x => x.ReceiveTotalExcl ?? 0);
                        decimal totval = DocValue;
                        if (AddC.Count > 0 && receiveComplete)
                        {
                            totAddCosts = AddC.Sum(x => (decimal?)x.Exclusive ?? 0);
                            totval = DocValue + totAddCosts;
                        }
                        foreach (var dl in FLines)
                        {
                            // update internal lines and create relevant records
                            if (dl.ToReceive == true)
                            {
                                // this line value / total value = % of which this line value is of total.
                                // apply perc to additional costs
                                // reverse calculate unit price based on line quantity.
                                decimal linevalue = dl.ReceiveTotalExcl ?? 0;
                                decimal qty = dl.ReceiveQty ?? 0;
                                // a line only carries add-costs when it is fully received in this action
                                bool lineFull = ((dl.QtyLeft ?? 0) - (dl.ReceiveQty ?? 0) - (dl.RejectQty ?? 0)) <= 0;
                                decimal linevalueperc = 0;
                                if (lineFull && linevalue != 0 && fullLinesValue != 0)
                                {
                                    linevalueperc = linevalue / fullLinesValue;
                                }

                                decimal linevalAddCosts = 0;
                                decimal unitAddCosts = 0;
                                decimal newunitcost = 0;
                                if (linevalue != 0 && DocValue != 0 && qty != 0)
                                {
                                    newunitcost = linevalue / qty;
                                }
                                if (totAddCosts > 0 && qty != 0 && lineFull)
                                {
                                    linevalAddCosts = linevalueperc * totAddCosts;
                                    unitAddCosts = linevalAddCosts / qty;
                                    newunitcost = unitAddCosts + (linevalue / qty);
                                }
                                #region data fusion stock transactions
                                var tempLine = _db.DocLines.Where(x => x.SBCALineID == dl.SBCALineID).FirstOrDefault();
                                tempLine.ItemDescription = dl.ItemDescription;
                                tempLine.Quantity = dl.Quantity;
                                tempLine.UnitPriceExclusive = dl.UnitPriceExclusive;
                                tempLine.UnitPriceInclusive = dl.UnitPriceInclusive;
                                tempLine.TaxPercentage = dl.TaxPercentage;
                                tempLine.DiscountPercentage = dl.DiscountPercentage;
                                tempLine.Exclusive = dl.Exclusive;
                                tempLine.Discount = dl.Discount;
                                tempLine.Tax = dl.Tax;
                                tempLine.Total = dl.Total;
                                tempLine.Comments = dl.Comments;
                                tempLine.QtyLeft = dl.QtyLeft;
                                //tempLine.ReceiveQty = dl.ReceiveQty;
                                tempLine.ReceiveQty = 0;
                                tempLine.RejectQty = 0;
                                tempLine.ToReceive = dl.ToReceive;
                                tempLine.ReceiveComplete = true;
                                tempLine.StoreCode = dl.StoreCode;
                                tempLine.LotNumber = dl.LotNumber;
                                tempLine.LineTaxTypeID = dl.LineTaxTypeID;
                                tempLine.AddCostsAmount = linevalAddCosts;
                                tempLine.AddCostsReason = dl.AddCostsReason;
                                tempLine.ReceiveTotalExcl =dl.Exclusive - dl.Discount + linevalAddCosts;
                                tempLine.ExchRate = (decimal)exchRate;
                                tempLine.localCurrLineVal = (decimal)tempLine.ReceiveTotalExcl / (decimal)exchRate;
                                if (dl.ItemType == 0)
                                {
                                    var ItmCon = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == dl.SelectionId).FirstOrDefault();
                                    decimal ConvRate = (decimal)ItmCon.UOMConvert;

                                    // 1) Create transaction to move items from supplier into the selected warehouse.
                                    ItemTransaction ItemTrans = new ItemTransaction();
                                    ItemTrans.CompanyID = CurrentUser.CoID;
                                    ItemTrans.DocumentID = dl.DocID;
                                    ItemTrans.TransactionType = "GRN";
                                    ItemTrans.ItemID = dl.SelectionId;
                                    ItemTrans.ItemCode = dl.ItemCode;
                                    ItemTrans.ItemDescription = dl.ItemDescription;
                                    ItemTrans.LotNumber = dl.LotNumber;
                                    ItemTrans.Unit = dl.Unit;
                                    ItemTrans.FromID = getstoreid("CoR");
                                    ItemTrans.ToID = getstoreid(dl.StoreCode);
                                    ItemTrans.Qty = dl.ReceiveQty * ConvRate;
                                    ItemTrans.PriceExclusive = (dl.UnitPriceExclusive / exchRate) / ConvRate;
                                    ItemTrans.TotalLineValExcl = tempLine.ReceiveTotalExcl / exchRate;
                                    ItemTrans.AdditionalCosts = linevalAddCosts;
                                    ItemTrans.TotalUnitPriceExclInclAdd = (newunitcost / exchRate)/ConvRate;
                                    ItemTrans.DocumentType = 2;
                                    ItemTrans.TransactionReference = SuppInvNum;
                                    // Additional costs ????
                                    ItemTrans.TransactionDate = DateTime.Now;
                                    ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid
                                    ItemTrans.ExchRate = (decimal)exchRate;       
                                    _db.ItemTransactions.Add(ItemTrans);

                                    if (dl.LotNumber != null)
                                    {
                                        var LotNumUpdate = _db.LotTrackingMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.LotNumber == dl.LotNumber).FirstOrDefault();
                                        // Cardinal rule: lot carries the SAME uplifted unit cost as the ledger
                                        // (TotalUnitPriceExclInclAdd) and the Sage adjust-in (newunitcost) — never base.
                                        LotNumUpdate.LotTotUnitPrice = newunitcost / exchRate;
                                    }
                                    // check for itemstore link
                                    var ItS = _db.ItemStoreLinkMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreID == ItemTrans.ToID && x.ItemID == dl.SelectionId).FirstOrDefault();
                                    if (ItS == null)
                                    {
                                        // create item/store link
                                        ItemStoreLinkMaster isL = new ItemStoreLinkMaster
                                        {
                                            CompanyID = CurrentUser.CoID,
                                            ItemID = Convert.ToInt64(dl.SelectionId),
                                            StoreID = (int?)ItemTrans.ToID,
                                            Active = true,
                                        };
                                        _db.ItemStoreLinkMasters.Add(isL);
                                    }
                                    _db.SaveChanges();
                                    itemtransnum = ItemTrans.TrnID;
                                }
                                #endregion

                                #region sage adjustments for additional costs 
                                if (CurrentUser.UATMode == false)
                                {
                                    if (AddC.Count > 0)
                                    {
                                        await api.LoadOneItem(dl.SelectionId, CurrentUser);
                                        var Itm = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == dl.SelectionId).FirstOrDefault();
                                        if (Itm.AverageCost != newunitcost)
                                        {
                                            #region AdjustItemOut
                                            ItemAdjustment iAdj = new ItemAdjustment();
                                            iAdj.Date = DateTime.Now;
                                            iAdj.ItemID = dl.SelectionId;
                                            // undate master record of item before adjustments

                                            decimal QOH = (decimal)Itm.QuantityOnHand;
                                            decimal AvCost = (decimal)Itm.AverageCost;

                                            iAdj.AverageCost = (decimal)dl.UnitPriceExclusive;
                                            iAdj.Quantity = (decimal)qty * -1;
                                            iAdj.Reason = "ADJ Out Trans ID: " + itemtransnum + " - " + txtAddCostsReason.Text.ToString();
                                            iAdj.Created = DateTime.Now;
                                            jsonBody = JsonConvert.SerializeObject(iAdj, Formatting.Indented);
                                            await SendItemAdjustment(jsonBody);

                                            #endregion
                                            // --------------------------------------------------
                                            #region AdjustItemIn
                                            // adjust items back in at new price including revised additional costs
                                            iAdj = new ItemAdjustment();
                                            iAdj.Date = DateTime.Now;
                                            iAdj.ItemID = dl.SelectionId;
                                            decimal origqty = avcostlist.Where(x => x.ID == dl.SelectionId).FirstOrDefault().QuantityOnHand;
                                            decimal origavcost = avcostlist.Where(x => x.ID == dl.SelectionId).FirstOrDefault().AverageCost;
                                            decimal origvalue = origqty * origavcost;

                                            decimal ThisUnitVal = ThisItemUnitNett;
                                            decimal NewQty = (decimal)(origqty + qty);
                                            decimal NewTotVal = (decimal)(origvalue + dl.Exclusive - dl.Discount + linevalAddCosts);
                                            decimal NewAvCost = NewTotVal / NewQty;

                                            // Audit trail: record the Sage average-cost change driven by add-costs.
                                            _db.AvCostChangeLogs.Add(new AvCostChangeLog
                                            {
                                                CompanyID = (int)CurrentUser.CoID,
                                                ItemID = dl.ItemCode,
                                                CostChangeDate = DateTime.Now,
                                                CostChangeBy = CurrentUser.RoleID,
                                                SageQOHBefore = origqty,
                                                SageAvUnitCostBefore = origavcost,
                                                SageTotValueBefore = origvalue,
                                                QtyImported = qty,
                                                QtyUnitPrice = (decimal)dl.UnitPriceExclusive,
                                                QtyUnitAddCosts = unitAddCosts,
                                                QtyImportTotalValue = (decimal)(dl.Exclusive - dl.Discount + linevalAddCosts),
                                                SageQOHAfter = NewQty,
                                                SageTotValueAfter = NewTotVal,
                                            });

                                            iAdj = new ItemAdjustment();
                                            iAdj.Date = DateTime.Now;
                                            iAdj.ItemID = dl.SelectionId;
                                            iAdj.AverageCost = NewAvCost;
                                            iAdj.Quantity = (decimal)dl.ReceiveQty;
                                            iAdj.Reason = "ADJ IN Trans ID: " + itemtransnum + " - " + txtAddCostsReason.Text.ToString();
                                            iAdj.Created = DateTime.Now;
                                            jsonBody = JsonConvert.SerializeObject(iAdj, Formatting.Indented);
                                            if (CurrentUser.UATMode == false)
                                            {
                                                await SendItemAdjustment(jsonBody);
                                            }
                                        }
                                    }
                                    #endregion
                                }
                            }
                            #endregion
                            LCount++;
                        }
                    }

                    // === Split each line's rejected portion into the reject store ===
                    // Reject was billed on the supplier invoice as part of the total, so local
                    // stock matches Sage. Booked CoR -> IsRejectStore (no add-cost uplift on the
                    // reject portion; flag if add-costs + rejects need exact averaging).
                    var rejStore = _db.Stores.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID
                                       && x.IsRejectStore == true && x.StoreActive == true);
                    if (rejStore != null)
                    {
                        long rejStoreId = rejStore.StoreID;
                        long corStoreId = getstoreid("CoR");
                        foreach (var dl in FLines)
                        {
                            decimal rejQty = dl.RejectQty ?? 0;
                            if (rejQty <= 0 || dl.ItemType != 0) continue;

                            var ItmConR = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == dl.SelectionId).FirstOrDefault();
                            decimal ConvR = (decimal)(ItmConR != null ? ItmConR.UOMConvert : 1);
                            if (ConvR == 0) ConvR = 1;
                            decimal unitExcl = ((decimal)(dl.UnitPriceExclusive ?? 0) / (decimal)exchRate) / ConvR;

                            ItemTransaction rTrans = new ItemTransaction();
                            rTrans.CompanyID = CurrentUser.CoID;
                            rTrans.DocumentID = dl.DocID;
                            rTrans.TransactionType = "GRN";
                            rTrans.ItemID = dl.SelectionId;
                            rTrans.ItemCode = dl.ItemCode;
                            rTrans.ItemDescription = dl.ItemDescription;
                            rTrans.LotNumber = dl.LotNumber;
                            rTrans.Unit = dl.Unit;
                            rTrans.FromID = corStoreId;
                            rTrans.ToID = rejStoreId;
                            rTrans.Qty = rejQty * ConvR;
                            rTrans.PriceExclusive = unitExcl;
                            rTrans.AdditionalCosts = 0;
                            rTrans.TotalUnitPriceExclInclAdd = unitExcl;
                            rTrans.TotalLineValExcl = ((decimal)(dl.UnitPriceExclusive ?? 0) * rejQty) / (decimal)exchRate;
                            rTrans.DocumentType = 2;
                            rTrans.TransactionReference = SuppInvNum + " (Reject)";
                            rTrans.TransactionDate = DateTime.Now;
                            rTrans.ByRoleID = CurrentUser.RoleID;
                            rTrans.ExchRate = (decimal)exchRate;
                            _db.ItemTransactions.Add(rTrans);

                            var ItSR = _db.ItemStoreLinkMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreID == (int?)rejStoreId && x.ItemID == dl.SelectionId).FirstOrDefault();
                            if (ItSR == null)
                            {
                                _db.ItemStoreLinkMasters.Add(new ItemStoreLinkMaster
                                {
                                    CompanyID = CurrentUser.CoID,
                                    ItemID = Convert.ToInt64(dl.SelectionId),
                                    StoreID = (int?)rejStoreId,
                                    Active = true,
                                });
                            }
                        }
                        _db.SaveChanges();
                    }

                    //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

                    // SET DOC HEADER VALUES
                    if (LCount == FLines.Count)
                    {
                        Head.Complete = true;
                        Head.RecStatus = 2;   // Submitted (supplier invoice posted to Sage)
                        Head.CompBy = 0;
                        Head.CompleteDate = DateTime.Now;
                        if (Head.SupplierInvNum != null)
                        {
                            Head.SupplierInvNum = Head.SupplierInvNum + "-" + SuppInvNum.ToString();
                            if (Head.SupplierInvNum.Length > 50)
                            {
                                Head.SupplierInvNum = Head.SupplierInvNum.Substring(0, 50);
                            }
                        }
                        else
                        {
                            Head.SupplierInvNum = SuppInvNum.ToString();
                        }
                        Head.InvNum = txtInvNum.Text.ToString().Replace("'", "'')");
                        Head.DNNum = txtDNNum.Text.ToString().Replace("'", "'')");
                    }
                    if (RBpoStatus.SelectedValue.ToString() == "1") { Head.Complete = false; Head.RecStatus = 0; }   // partial -> still in progress (Started)

                    // Stamp this GRN's receivings with a single BatchID so the receiving-note PDF
                    // can print just this delivery ("This Receiving Only") vs the running total.
                    // Every row received since the last finish (BatchID still null) belongs to it.
                    Guid batchId = Guid.NewGuid();
                    var batchRows = _db.ReceivingOutstandings
                        .Where(x => x.PODocID == docid && x.Archive == false && x.BatchID == null)
                        .ToList();
                    foreach (var br in batchRows) br.BatchID = batchId;

                    //%%%%%%%%%%%%%%%%%%%%%%%%%%%%

                    // get lines from TempDocLines
                    var Lines = _db.TempDocLines.Where(x => x.DocID == docid && x.ReceiveComplete == false).ToList();
                    _db.TempDocLines.RemoveRange(Lines);

                    //var DocLD = _db.DocLines.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == docid).ToList();
                    //if (DocLD != null)
                    //{
                    //    _db.DocLines.RemoveRange(DocLD);
                    //}

                    // W1: estimates have now been capitalised + posted; mark them consumed so a
                    // later receive on this PO can never re-apply them.
                    if (RBpoStatus.SelectedValue.ToString() == "0")
                    {
                        var doneAdd = _db.ReceivingAddCosts.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == docid && x.Imported != true).ToList();
                        foreach (var a in doneAdd) { a.Imported = true; a.ImportDate = DateTime.Now.ToString(); }
                    }

                    // 2)Save changes to the database
                    _db.SaveChanges();

                    chkReceiveComplete.Checked = (Boolean)Head.Complete;
                    //lbtnReset.Style.Add("display", "none");
                    lbtnReceiveFinish.Style.Add("display", "none");
                    LbtnAddCosts.Style.Add("display", "none");
                    lbtnRecAll.Style.Add("display", "none");
                    txtDNNum.ReadOnly = true;
                    txtInvNum.ReadOnly = true;
                    txtRecDate.ReadOnly = true;
                    lblSaveStatus.Text = SuppInvNum + ": Successfully Generated In SBCA";
                    //PopMessage(SuppInvNum + ": Successfully Generated In SBCA");
                }
            }
            catch (Exception ex)
            {
                new ApiUrlCall().LogErrorToFile(ex.ToString());
                AlertHelper.ShowSweetAlert(this, "Receive Finish failed: " + ex.Message, "error");
            }
            finally
            {
                // Re-enable buttons when process completes
                ScriptManager.RegisterStartupScript(this, this.GetType(), "EnableReceiveButtons",  "enableReceiveButtons();", true);
            }
        }

        public async Task <string> SendSupplierInvoice(string Doc)
        {
            string doctype = "";
            doctype = "SupplierInvoice";
            ApiUrlCall Api = new ApiUrlCall();
            JObject parsedJSON = await Api.APIPostDocumentAsync(doctype, Doc, CurrentUser);     
            if (parsedJSON.ContainsKey("ID"))
            {
                return parsedJSON["ID"].ToString() + "|" + parsedJSON["DocumentNumber"].ToString();
            }
            else
            {
                return parsedJSON.ToString();
            }        
        }

        public async Task<string> SendSupplierAdjustment(string Doc)
        {
            string doctype = "";
            doctype = "SupplierAdjustment";
            ApiUrlCall Api = new ApiUrlCall();
            JObject parsedJSON = await Api.APIPostDocumentAsync(doctype, Doc, CurrentUser);
            if (parsedJSON.ContainsKey("ID"))
            {
                return parsedJSON["ID"].ToString() + "|" + parsedJSON["DocumentNumber"].ToString();
            }
            else
            {
                return parsedJSON.ToString();
            }
        }
        public async Task<string> SendItemAdjustment(string Item)
        {
            string doctype = "";
            doctype = "ItemAdjustment";
            ApiUrlCall Api = new ApiUrlCall();
            JObject parsedJSON = await Api.APIPostDocumentAsync(doctype, Item, CurrentUser);
            return "";
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

        protected long getstoreid(string stcode)
        {
            long storeid = 0;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var store = _db.Stores.Where(x => x.StoreCode == stcode && x.CompanyID == CurrentUser.CoID).FirstOrDefault();
                if (store != null)
                {
                    storeid = store.StoreID;
                }
              }
            return storeid;
        }
        // Re-open a completed PO for further receiving. Only acts when the PO is ALREADY
        // complete AND still has an outstanding balance: clears the two header status fields
        // (Complete / RecStatus) so the user can carry on receiving, then reloads. Nothing else
        // is touched - the already received history (ReceivingOutstandings) stays intact. When
        // the PO is not complete, the radio just changes selection as normal.
        // RBpoStatus is a full PostBackTrigger, so Response.Redirect works here.
        protected void RBpoStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            long docid = Convert.ToInt64(lblDocID.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Head = _db.DocHeaders.Where(x => x.DocID == docid).FirstOrDefault();
                if (Head == null || Head.Complete != true) return;   // not complete -> normal selection change
                if (!HasOutstandingReceiving(_db, docid)) return;     // nothing outstanding -> nothing to re-open

                Head.Complete = false;
                Head.RecStatus = 0;
                _db.SaveChanges();
            }
            Response.Redirect("~/Receiving.aspx?docid=" + Request.QueryString["docid"], false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // True when any PO line still has qty left to receive: ordered (DocLine.Quantity) minus
        // the received-to-date (sum of non-archived ReceivingOutstandings.RecQty for the line,
        // matched by SBCALineID, ItemCode fallback for legacy rows). Same match the receive
        // outstanding calc uses, so it agrees with what the screen shows as remaining.
        private bool HasOutstandingReceiving(SBMSEntities _db, long docid)
        {
            var docLines = _db.DocLines.Where(x => x.DocID == docid).ToList();
            foreach (var dl in docLines)
            {
                decimal ordered = dl.Quantity ?? 0;
                decimal received = _db.ReceivingOutstandings
                    .Where(x => x.PODocID == docid && x.Archive == false
                             && (x.SBCALineID == dl.SBCALineID
                                 || (x.SBCALineID == null && x.ItemCode == dl.ItemCode)))
                    .Sum(x => (decimal?)x.RecQty) ?? 0;
                if (ordered - received > 0.0001m) return true;
            }
            return false;
        }

         protected async void lbtnReset_Click(object sender, EventArgs e)
        {
            ApiUrlCall api = new ApiUrlCall();
            docid = Convert.ToInt64(lblDocID.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var DocHeaderDelete = _db.DocHeaders.Where(x => x.DocID == docid).ToList();
                DocHeaderDelete.ForEach(x => x.Active = false);
                DocHeaderDelete.ForEach(x => x.Started = false);
                DocHeaderDelete.ForEach(x => x.Complete = false);
                DocHeaderDelete.ForEach(x => x.Status = "Deleted");

                // Retrieve the records to be deleted
                var tempLinesToDelete = _db.TempDocLines.Where(x => x.DocID == docid).ToList();
                _db.TempDocLines.RemoveRange(tempLinesToDelete);

                var LinesToDelete = _db.DocLines.Where(x => x.DocID == docid).ToList();
                _db.DocLines.RemoveRange(LinesToDelete);

                var PreRec = _db.ReceivingOutstandings.Where(x => x.PODocID == docid);
                _db.ReceivingOutstandings.RemoveRange(PreRec);

                    _db.SaveChanges();
                await api.LoadPOLines(docid, CurrentUser);
                LoadLines();
                BindGrid();
            }
        }

        protected void lbtnItmC_Click(object sender, EventArgs e)
        {
            if (DDStoreEdit.Items.Count == 0)
            {
                btnApprovYes.Attributes.Add("style", "display:none");
                lbtnReceive.Attributes.Add("style", "display:none");
                string message = "No stores available for receiving, please go to settings and allow at least 1 store to receive goods";
                AlertHelper.ShowSweetAlert(this, message, "error");
                return;
            }
            
            chkAddLotNum.Checked = false;
            LinkButton lbtnItmC = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnItmC.NamingContainer;
            long lineid = Convert.ToInt64(lbtnItmC.CommandArgument);
            lblLineID.Text = lineid.ToString();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Docline = _db.TempDocLines.Where(x => x.LineID == lineid).FirstOrDefault();
                if (Docline == null)
                {
                    AlertHelper.ShowSweetAlert(this, "Receiving line not found, please refresh and try again.", "error");
                    return;
                }
                txtordqty.Text = ApiUrlCall.NumberToDecimal((Docline.QtyLeft ?? Docline.Quantity ?? 0).ToString(), CurrentUser.CompanyDecPlaces).ToString();
                txtQtyReceive.Text = ApiUrlCall.NumberToDecimal((Docline.ReceiveQty ?? 0).ToString(), CurrentUser.CompanyDecPlaces).ToString();
                lblItemdescr.Text = Docline.ItemDescription ?? string.Empty;
                txtNumPieces.Text = txtQtyReceive.Text;
                chkEdit.Checked = (Boolean)Docline.ReceiveComplete;
                if (Docline.StoreCode != null)
                {
                    string storecode = Docline.StoreCode;
                    DropDownList ddl = DDStoreEdit;
                    if (ddl != null)
                    {
                        // Check if the DropDownList contains the storecode and set the SelectedValue
                        if (ddl.Items.FindByValue(storecode) != null)
                        {
                            ddl.SelectedValue = storecode;
                        }
                    }
                }

                if (Docline.LotNumber != null && Docline.LotNumber.ToString().Length > 3)
                {
                    lblLotNum.Text = Docline.LotNumber.ToString();
                    hfOriginalLotNumber.Value = Docline.LotNumber.ToString();
                }
                else
                {
                    hfOriginalLotNumber.Value = string.Empty;
                    lblLotNum.Text = "N/A";
                }
                PnlLotTracking.Style.Add("display", "none");
                PnlLotAdditions.Style.Add("display", "none");
               
                if (CurrentUser.CompanyUseLotNumbers == true)
                {
                    if (Docline.ItemType != 1)
                    {
                        PnlLotTracking.Style.Add("display", "inline-block");
                        PnlLotTracking.Style.Add("width", "100%");
                    }
                    try
                    {
                        bool Itm = (bool)_db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == Docline.SelectionId).IsLotTracked;
                        if (Itm == true)
                        {
                            PnlLotTracking.Style.Add("display", "inline-block");
                            PnlLotTracking.Style.Add("width", "100%");
                        }

                        if (CurrentUser.CompanyUseLotAddDetails == true)
                        {
                            PnlLotAdditions.Style.Add("display", "inline-block");
                            PnlLotAdditions.Style.Add("width", "100%");
                        }
                    }
                    catch { DDStore.Enabled=false; }
                }
                
            DDStoreEdit.SelectedIndex = 0;
            if (chkReceiveComplete.Checked == true)
            {
                string message = "Receiving Complete, details not editable";
                AlertHelper.ShowSweetAlert(this, message, "error");
                return;
            }
            
                
                ModalPopupExtender1.Show();
                txtQtyReceive.Text = null;
                txtQtyReceive.Focus();
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
                    LotNumCheck = "Lot number already exists. Record not updated";
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
                        LotNumCheck = "Lot number already exists. Record not updated";
                    }
                }
            }     
        }

        protected void lbtnAdCSave_Click(object sender, EventArgs e)
        {
            docid = Convert.ToInt64(lblDocID.Text);
            decimal AddCost = 0;
           if (DDSupplier.SelectedIndex == 0 || DDAcctList.SelectedIndex == 0)
            {
                string message = "Please select valid Supplier and GL Accounts";
                AlertHelper.ShowSweetAlert(this, message, "error");
                return;
            }
            
            try
            {
                AddCost = Convert.ToDecimal(txtAddCosts.Text);
            } catch
            {
                string message = "Invalid amount captured, unable to continue";
                AlertHelper.ShowSweetAlert(this, message, "error");
                return;
            }
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string AddCReason = "Estimated Additional Costs";
                if (txtAddCostsReason.Text.ToString().Trim().Length > 0)
                {
                    AddCReason = txtAddCostsReason.Text.ToString();
                };
                ReceivingAddCost ACDocline = new ReceivingAddCost();
                ACDocline.DocID = docid;
                ACDocline.SupplierID = Convert.ToInt64(DDSupplier.SelectedValue);
                ACDocline.SupplierName = DDSupplier.SelectedItem.Text;
                ACDocline.Message = txtAddCostsReason.Text.Replace("'", "''").Trim();
                ACDocline.FromDocument = txtDocNum.Text;
                ACDocline.SelectionID = Convert.ToInt64(DDAcctList.SelectedValue);
                ACDocline.SupplierInvNum = txtInvNum.Text;
                ACDocline.DocDate = Convert.ToDateTime(txtRecDate.Text);
                ACDocline.CompanyID = CurrentUser.CoID;
                long txttypeid = Convert.ToInt64(DDVat.SelectedValue.ToString().Split('|')[0]);
                decimal txtperc = 0;
                try
                {
                    txtperc = Convert.ToDecimal(DDVat.SelectedValue.ToString().Split('|')[1]);
                }
                catch { }      
                ACDocline.TaxType = txttypeid;
               
                if (string.IsNullOrEmpty(txtAddCosts.Text))
                {
                    ACDocline.Exclusive = 0; // Insert 0 if the text is empty or null
                    ACDocline.Vat = 0;
                }
                else
                {
                    ACDocline.Exclusive = Convert.ToDecimal(txtAddCosts.Text); // Convert and assign the value
                    ACDocline.Vat = txtperc * ACDocline.Exclusive;
                }
                _db.ReceivingAddCosts.Add(ACDocline);
                _db.SaveChanges();
                LoadAddCosts();
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
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
            }
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

       protected void lbtnPrintRN_Click(object sender, EventArgs e)
        {
            long docid = Convert.ToInt64(lblDocID.Text);

            // If the PO is not fully received, ask whether the note should show the running
            // total received to date ("All Goods Received") or only this delivery's quantities
            // ("This Receiving Only"). A fully-complete PO prints the total with no prompt.
            bool notComplete;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                notComplete = _db.DocHeaders.Where(x => x.DocID == docid).Select(x => x.Complete).FirstOrDefault() != true;
            }

            string scope = hfPrintScope.Value;
            hfPrintScope.Value = "";
            if (notComplete && string.IsNullOrEmpty(scope))
            {
                string pb = Page.ClientScript.GetPostBackEventReference(lbtnPrintRN, "");
                string hf = hfPrintScope.ClientID;
                string js = "Swal.fire({title:'Receiving Note',text:'Which quantities should this receiving note show?',icon:'question',showDenyButton:true,showCancelButton:true,confirmButtonText:'All Goods Received',denyButtonText:'This Receiving Only'})"
                    + ".then(function(r){var hf=document.getElementById('" + hf + "');"
                    + "if(r.isConfirmed){hf.value='all';" + pb + ";}"
                    + "else if(r.isDenied){hf.value='batch';" + pb + ";}});";
                ScriptManager.RegisterStartupScript(this, GetType(), "PrintScopeAsk", js, true);
                return;
            }

            CreatePDF(scope == "batch");
            Response.Redirect($"~/ViewPDF.aspx?doc=" + CurrentUser.UserGuiD.ToString() + "\\GRN_" + txtDocNum.Text, false);
        }

        protected void DDStoreEdit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDStoreEdit.SelectedIndex > 0)
            {
               if (CurrentUser.CompanyUseLotNumbers == true)
                {
                    int recnum = GetLotNum(CurrentUser.CoID);
                    lblLotNum.Text = DateTime.Today.ToString("ddMMyyyy") + DDStoreEdit.SelectedValue.ToString() + recnum.ToString();
                }
                //chkAccept.Style.Add("display", "none");
                decimal OrdQty =0;
                decimal RecQty =0;
                try
                {
                    OrdQty = Convert.ToDecimal(txtordqty.Text);
                    RecQty = Convert.ToDecimal(txtQtyReceive.Text);
                }
                catch { }
                decimal BalQty = OrdQty - RecQty;
                //if (RecQty >= (OrdQty * 1.05m) || RecQty <= (OrdQty * 0.95m))
                //{
                //    errpop.InnerText = "Warning - Quantity variation of 5% or more being received";
                //    chkAccept.Style.Add("display", "inline-block");
                //}  
                ModalPopupExtender1.Show();
            }
        }

        protected void lbtnAtt_Click(object sender, EventArgs e)
        {
           
            ModalPopupExtender3.Show();
        }

        protected async void lbtnDownload_Click(object sender, EventArgs e)
        {
            LinkButton lbtnDownload = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnDownload.NamingContainer;
            string docid = lbtnDownload.CommandArgument;
            string DocName = lbtnDownload.CommandName;
               UserDetails userDets = Session["UserDetails"] as UserDetails;
            string requestUrl = ApiUrlCall.sageurl + $"PurchaseOrderAttachment/Download/" + docid + "?apikey=" + ApiUrlCall.APIKey + "&CompanyID=" + CurrentUser.CoID;
            ApiUrlCall Api = new ApiUrlCall();
            byte[] pdfContent = await Api.DownloadPdfAsync(requestUrl, userDets);
            if (pdfContent != null)
            {
                // Serve the PDF to the user
                Response.ContentType = "application/pdf";
                Response.AddHeader("content-disposition", "attachment;filename=" + DocName + "");
                Response.BinaryWrite(pdfContent);
                Response.End();
            }
            else
            {
                // Handle error, e.g., display a message to the user
                Response.Write("Failed to download the PDF.");
            }
        }

       
        protected void LoadAttachments(long Thisdocid)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Attchs = _db.DocHeaderAttachments.Where(x => x.DocID == Thisdocid && x.CompanyID == CurrentUser.CoID).ToList();
                if (Attchs.Count > 0)
                {
                    lbtnAtt.ForeColor = System.Drawing.Color.Orange;
                    GridAtts.DataSource = Attchs;
                    GridAtts.DataBind();
                }
            }
        }
        // thisBatchOnly = show only the most recent receiving batch's quantities on the note
        // instead of the running total received to date.
        private void CreatePDF(bool thisBatchOnly = false)
        {
            string filepath = string.Empty, fname = string.Empty;
            var regfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 10, BaseColor.BLACK);
            var regfontB = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 10, Font.BOLD, BaseColor.BLACK);
            var regfontS = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 8, BaseColor.BLACK);
            var medfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 11, BaseColor.BLACK);
            var headfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 18, BaseColor.BLACK);
            
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
             var DH = _db.DocHeaders.Where(x => x.DocGUID == docguid).FirstOrDefault();
                if (DH != null)
                {
                    iTextSharp.text.Document doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 40, 40);
                    PdfWriter writer = null;

                    try
                    {
                        if (!Directory.Exists(Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString())))
                        {
                            Directory.CreateDirectory(Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString()));
                        }
                        //filepath = Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString() + "\\GRN_" + docguid + ".PDF");
                        filepath = Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString() + "\\GRN_" + txtDocNum.Text + ".PDF");
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
                    cell.Rowspan = 3;
                    table.AddCell(cell);

                    Phrase rsHead = new Phrase();
                    if (CurrentUser.UseBarcodes)
                    {
                        Barcode128 bc = new Barcode128();
                        bc.Code = DH.DocumentNumber;
                        bc.Font = null;
                        iTextSharp.text.Image bcImg = bc.CreateImageWithBarcode(writer.DirectContent, BaseColor.BLACK, BaseColor.BLACK);
                        rsHead.Add(new Chunk(bcImg, 0, -8, true));
                        rsHead.Add(new Chunk("   ", headfont));
                    }
                    rsHead.Add(new Chunk("Receiving Slip #", headfont));
                    cell = new PdfPCell(rsHead);
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(DH.DocumentNumber, headfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Supplier:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase((DH.CustSupName ?? "").ToString(), medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                   cell = new PdfPCell(new Phrase("Due Date:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(Convert.ToDateTime(DH.DueDelDate).ToString("dd MMM yyyy"), medfont));
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

                    cellM = new PdfPCell(new Phrase("Delivery Note #:- " + Environment.NewLine + (DH.DNNum ?? "").ToString(), regfont));
                    cellM.HorizontalAlignment = 0;
                    cellM.FixedHeight = 25f;
                    tableM.AddCell(cellM);

                    cellM = new PdfPCell(new Phrase("Supplier Invoice #:- " + Environment.NewLine + (DH.SupplierInvNum ?? "").ToString(), regfont));
                    cellM.HorizontalAlignment = 0;
                    tableM.AddCell(cellM);

                    cellM = new PdfPCell(new Phrase("Receive Date:- " , regfont));
                    if (DH.CompleteDate != null)
                    {
                        cellM = new PdfPCell(new Phrase("Receive Date:- " + Environment.NewLine + Convert.ToDateTime(DH.CompleteDate).ToString("dd MMM yyyy"), regfont));
                    } 
                    cellM.HorizontalAlignment = 0;
                    tableM.AddCell(cellM);

                    doc.Add(tableM);
                    #endregion

                    #region HeaderRow
                    PdfPTable table4 = new PdfPTable(7);
                    PdfPCell cell4;
                    table4.SpacingBefore = 15f;
                    table4.SetWidths(new int[] { 50, 150, 90, 30, 70, 70, 80 });
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

                    cell4 = new PdfPCell(new Phrase("Order Qty", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Receive Qty", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("By", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    #endregion

                    var DocLines = _db.DocLines.Where(x => x.DocID == DH.DocID).OrderBy(x => x.LineID).ToList();
                    // Received-to-date per line comes from the receiving history: DocLine.ReceiveQty
                    // is reset to 0 when a receipt is submitted, so it cannot be used on the slip.
                    var RecHist = _db.ReceivingOutstandings.Where(x => x.PODocID == DH.DocID && x.Archive == false).ToList();
                    // "This Receiving Only": restrict to the most recent batch (newest row's BatchID).
                    // Falls back to null-batch (an in-progress, not-yet-finalised receipt).
                    Guid? printBatch = null;
                    if (thisBatchOnly)
                    {
                        printBatch = RecHist.OrderByDescending(x => x.CreatedDate).Select(x => x.BatchID).FirstOrDefault();
                    }
                    foreach (var DL in DocLines)
                    {
                        cell4 = new PdfPCell(new Phrase(DL.ItemCode ?? "", regfont));
                        cell4.HorizontalAlignment = 0;
                        cell4.FixedHeight = 20f;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211); 
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(DL.ItemDescription ?? "", regfont));
                        cell4.HorizontalAlignment = 0;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  
                        table4.AddCell(cell4);

                        string Barcode = _db.ItemsMasters.Where(x => x.ID == DL.SelectionId).Select(x => x.BarCode).FirstOrDefault() ?? "";
                        cell4 = new PdfPCell(new Phrase(Barcode, regfont));
                        cell4.HorizontalAlignment = 0;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(DL.Unit ?? "", regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(Convert.ToDecimal(ApiUrlCall.NumberToDecimal(DL.Quantity.ToString(), CurrentUser.CompanyDecPlaces)).ToString(), regfont));
                        //cell4 = new PdfPCell(new Phrase((DL.Quantity ?? 0).ToString("N2"), regfont));
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  
                        table4.AddCell(cell4);

                        // Sum receivings against this line by SBCALineID; fall back to ItemCode
                        // for legacy rows that pre-date the SBCALineID column. When printing a single
                        // delivery, restrict to that batch only.
                        decimal recqty = RecHist
                            .Where(x => (x.SBCALineID == DL.SBCALineID || (x.SBCALineID == null && x.ItemCode == DL.ItemCode))
                                     && (!thisBatchOnly || x.BatchID == printBatch))
                            .Sum(x => (decimal?)x.RecQty) ?? 0;
                        if (recqty > 0)
                        {
                            cell4 = new PdfPCell(new Phrase(Convert.ToDecimal(ApiUrlCall.NumberToDecimal(recqty.ToString(), CurrentUser.CompanyDecPlaces)).ToString(), regfont));
                        }
                        else
                        {
                            cell4 = new PdfPCell(new Phrase("", regfont));
                        }
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase("", regfont));
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                    }
                    doc.Add(table4);
                    doc.AddTitle("Receiving Slip: ");
                    //doc.AddSubject("Classroom Review Instrument");
                    doc.AddAuthor("Data Fusion");
                    doc.Close();

                }
            }
         }

        protected async void lbtnUpload_Click(object sender, EventArgs e)
        {
            if (fuAttachment.HasFile)
            {        
                // Get the file name
                string fileName = Path.GetFileName(fuAttachment.PostedFile.FileName);
                // Get the file content as a byte array
                byte[] fileContent = fuAttachment.FileBytes;
                // Save the file to the server (optional)
                string savePath = Server.MapPath($"~\\PDFs\\"+ CurrentUser.UserGuiD + "\\" + fileName);
                fuAttachment.SaveAs(savePath);

                JArray parsedJSON = await UploadPurchaseOrderAttachmentAsync(lblDocID.Text.ToString(), savePath, CurrentUser);
                if (parsedJSON.Count > 0)
                {
                    JObject firstObject = (JObject)parsedJSON[0];
                    string Dname = firstObject["Name"].ToString();
                    long size = (long)firstObject["Size"];
                    Guid attachmentUID = Guid.Parse(firstObject["AttachmentUID"].ToString());
                    // insert link record
                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        DocHeaderAttachment dha = new DocHeaderAttachment
                        {
                            CompanyID = CurrentUser.CoID,
                            AttGUID = attachmentUID,
                            DocID = Convert.ToInt64(lblDocID.Text),
                            AttName = Dname.ToString() ?? "",
                        };
                        _db.DocHeaderAttachments.Add(dha);
                        _db.SaveChanges();
                        LoadAttachments(Convert.ToInt64(lblDocID.Text));
                    }
                    string message = "Successfully Uploaded";
                    AlertHelper.ShowSweetAlert(this, message, "success");
                }
                else
                {
                    string message = "Error uploading file: ";
                    AlertHelper.ShowSweetAlert(this, message, "error");
                }
            }
            else
            {
                lblMessage.Text = "Please select a file to upload.";
            }
        }

        protected void GridAddCosts_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                try
                {
                    addcosts += Convert.ToDecimal(e.Row.Cells[2].Text.ToString());
                }
                catch { }
            }
            else if (e.Row.RowType == DataControlRowType.Footer)
            {
                e.Row.Cells[0].Text = "Total";
                e.Row.Cells[2].Text = addcosts.ToString("N2");
                e.Row.Cells[2].HorizontalAlign = HorizontalAlign.Right;
            }
        }

        protected void LoadOtherDetails()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var AddCostSuppliers = _db.DocHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.DocType == 1).Select(x => new { x.CustSupName, x.CustSuppID }).Distinct().ToList();
                if (AddCostSuppliers != null)
                {
                    DDSupplier.DataSource = AddCostSuppliers;
                    DDSupplier.DataTextField = "CustSupName";
                    DDSupplier.DataValueField = "CustSuppID";
                    DDSupplier.DataBind();
                    DDSupplier.Items.Insert(0, "- Select -");

                }

                var GLAccts = _db.AccountsMasters.Where(x=>x.CompanyID == CurrentUser.CoID && x.AccountAddCosts == true).ToList();
                if (GLAccts != null)
                {
                    DDAcctList.DataSource = GLAccts;
                    DDAcctList.DataTextField = "AccountName";
                    DDAcctList.DataValueField = "AccountID";
                    DDAcctList.DataBind();
                    DDAcctList.Items.Insert(0, "- Select -");
                }

              List<VatType> VatL = new List<VatType>();
               var VatTypes = _db.TaxTypesMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.TaxTypeID <1).ToList();
                if (VatTypes != null)
                {
                    foreach(var vatT in VatTypes)
                    {
                        VatType vt = new VatType();
                        vt.ID = vatT.TaxTypeID + "|" + vatT.TaxPerc;
                        vt.TaxType = vatT.TaxTypeName + " - " + vatT.TaxPerc;
                        VatL.Add(vt);
                    }  
                    DDVat.DataSource = VatL;
                    DDVat.DataTextField = "TaxType";
                    DDVat.DataValueField = "ID";
                    DDVat.DataBind();
                    DDVat.Items.Insert(0, "- Select -");
                }
            }
        }

        private void LoadAddCosts()
        {
            docid = Convert.ToInt64(lblDocID.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var AddCosts = _db.ReceivingAddCosts.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == docid).ToList();
                if (AddCosts != null)
                {
                    foreach (var gr in AddCosts)
                    {
                        gr.Exclusive = gr.Exclusive.HasValue ? Math.Round(gr.Exclusive.Value, 2) : (decimal?)null;
                    }
                    GridAddCosts.DataSource = AddCosts;
                    GridAddCosts.DataBind();
                }
            }
        }

        protected void lntnDeleteLine_Click(object sender, EventArgs e)
        {
            LinkButton lbtnDeleteLine = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnDeleteLine.NamingContainer;

            long Lnid = Convert.ToInt64(lbtnDeleteLine.CommandArgument);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Ln = _db.ReceivingAddCosts.Where(x => x.ACID == Lnid);
                _db.ReceivingAddCosts.RemoveRange(Ln);
                _db.SaveChanges();
                LoadAddCosts();
            }
        }

         public static async Task<JArray> UploadPurchaseOrderAttachmentAsync(string transactionId, string filePath, UserDetails userDetails)
        {
            JArray parsedJSON = new JArray();

            string requestUrl = $"{ApiUrlCall.sageurl}PurchaseOrderAttachment/Save?apikey={ApiUrlCall.APIKey}&CompanyID={userDetails.CoID}";

            // Initialize RestClient with options
            var options = new RestClientOptions(requestUrl)
            {
                ThrowOnAnyError = true,  // Ensures errors are thrown on failure
                ThrowOnDeserializationError = true, // Catches JSON parsing errors
            };

            var client = new RestClient(options);
            var request = new RestRequest();
            request.Method = Method.Post;

            // Add Basic Authentication Header
            string combined = $"{userDetails.LoginName}:{userDetails.LoginPwd}";
            string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
            request.AddHeader("Authorization", "Basic " + base64Encoded);

            // Add file (PDF) and parameters
            request.AddFile("file", filePath);
            request.AddParameter("TransactionId", transactionId);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                RestResponse response = await client.ExecuteAsync(request);

                if (response.StatusCode == HttpStatusCode.Created && !string.IsNullOrWhiteSpace(response.Content))
                {
                    parsedJSON = JArray.Parse(response.Content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return parsedJSON;
        }

        protected void lbtnCancelP_Click(object sender, EventArgs e)
        {
            lblLineID.Text = "";
        }

        private class VatType
        {
            public string ID { get; set; }
            public string TaxType { get; set; }
        }

        protected void lbtnLotNumAdd_Click(object sender, EventArgs e)
        {
            LinkButton lbtn = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtn.NamingContainer;
            LinkButton lbtnItmC = (LinkButton)row.FindControl("lbtnItmC");
            string code = lbtnItmC.Text;
            OpenFirstMatchingItemCode(code);
            //ProcessLotNumAdd(row);
        }

        protected void GridPOLines_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (GridPOLines.SelectedRow != null)
            {
                LinkButton lbtnItmC = GridPOLines.SelectedRow.FindControl("lbtnItmC") as LinkButton;
                if (lbtnItmC != null)
                {
                    lbtnItmC_Click(lbtnItmC, EventArgs.Empty);
                }
            }
        }

        private void ProcessLotNumAdd(GridViewRow row)
        {
            docid = Convert.ToInt64(lblDocID.Text);

            decimal QtyLeft = 0, QtyOrd = 0, QtyRec = 0;

            if (row.Cells[10].Text.Trim().Replace("&nbsp;", "") != "")
            {
                chkAddLotNum.Checked = true;

                // Get ItemCode link button
                LinkButton lbtnItmC = (LinkButton)row.FindControl("lbtnItmC");
                long lineid = Convert.ToInt64(lbtnItmC.CommandArgument);
                lblLineID.Text = lineid.ToString();

                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    // Resolve the TempDocLine for this grid row so we have its stable
                    // SBCALineID (the autonumber LineID rotates each time TempDocLines
                    // is rebuilt, so we can't trust it across page loads).
                    var tempL = _db.TempDocLines.FirstOrDefault(x => x.LineID == lineid);

                    if (tempL != null)
                    {
                        long sbcaLineId = tempL.SBCALineID;

                        // Sum by SBCALineID with the usual legacy ItemCode fallback.
                        decimal Prerecqty = _db.ReceivingOutstandings
                            .Where(x => x.PODocID == docid
                                     && x.Archive == false
                                     && (x.SBCALineID == sbcaLineId
                                         || (x.SBCALineID == null && x.ItemCode == tempL.ItemCode)))
                            .Sum(x => (decimal?)x.RecQty) ?? 0;

                        if (Prerecqty > 0)
                        {
                            QtyLeft = QtyOrd - (Prerecqty + QtyRec);
                        }
                    }
                }

                lblLotNum.Text = "";
                txtQtyReceive.Text = "1";
            }
            if (DDStoreEdit.Items.Count == 2 && DDStoreEdit.SelectedIndex == 1)
            {
                int recnum = GetLotNum(CurrentUser.CoID);
                lblLotNum.Text = DateTime.Today.ToString("ddMMyyyy") + DDStoreEdit.SelectedValue.ToString() + recnum.ToString();
            }
            ModalPopupExtender1.Show();
        }

        protected void OpenFirstMatchingItemCode(string itemCode)
        {
            foreach (GridViewRow row in GridPOLines.Rows)
            {
                LinkButton lbtnItmC = row.FindControl("lbtnItmC") as LinkButton;
                if (lbtnItmC == null) continue;

                if (lbtnItmC.Text.Trim().Equals(itemCode.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    // RUN EXACT SAME LOGIC AS A CLICK
                    ProcessLotNumAdd(row);
                    return;    // found the FIRST matching row
                }
            }
        }

    }
}