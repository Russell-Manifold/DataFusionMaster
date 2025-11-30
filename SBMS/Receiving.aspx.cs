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
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }

            docguid = Guid.Parse(Request.QueryString["docid"]);
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
                        if (thispo.CompleteDate != null) txtRecDate.Text = Convert.ToDateTime(thispo.CompleteDate).ToString("dd MMM yyyy");
                        if (thispo.Complete != null)
                        {
                            if (thispo.Complete == true)
                            {
                                chkReceiveComplete.Checked = (Boolean)thispo.Complete;
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
                        if (islines > 0)
                        {
                            if (Convert.ToBoolean(Request.QueryString["updt"]) == true)
                            {
                                if (thispo.Complete != true)
                                {
                                    await api.LoadPOLines(docid, CurrentUser);
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
                                await api.LoadPOLines(docid, CurrentUser);
                            }
                            LoadLines();
                            BindGrid();
                        }
                        LoadStores();
                        LoadAttachments(docid);
                    }
                }
            }
        }

        protected void lbtnRecAll_Click(object sender, EventArgs e)
        {
            docid = Convert.ToInt64(lblDocID.Text);
            int recnum = GetLotNum(CurrentUser.CoID);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Lines = _db.DocLines.Where(x => x.DocID == docid).OrderBy(x => x.LineID).ToList();
                foreach (DocLine dl in Lines)
                {
                    if (dl.ItemType == 0)
                    {
                        dl.ReceiveQty = dl.Quantity;
                        dl.QtyLeft = 0;
                        dl.ToReceive = true;
                        dl.StoreCode = DDStore.SelectedValue.ToString();
                        bool Itm = (bool)_db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == dl.SelectionId).IsLotTracked;
                        if (Itm == true) 
                        { 
                            dl.LotNumber = DateTime.Today.ToString("ddMMyyyy") + DDStore.SelectedValue.ToString() + recnum.ToString();
                            // save new Lot Number to db
                            LotTrackingMaster LtNew = new LotTrackingMaster();
                            LtNew.LotNumber = dl.LotNumber;
                            LtNew.CreatedDate = DateTime.Now;
                            LtNew.CompanyID = CurrentUser.CoID;
                            LtNew.ItemCode = dl.ItemCode;
                            LtNew.ItemId = dl.SelectionId;
                            LtNew.LotActive = true;
                            LtNew.LotQuantity = dl.Quantity ?? 0m;
                        _db.LotTrackingMasters.Add(LtNew);
                        }
                    }
                    else
                    {
                        dl.ReceiveQty = dl.Quantity;
                        dl.QtyLeft = 0;
                        dl.ToReceive = true;
                    }
                    recnum++;
                }
                try
                {
                    _db.SaveChanges();
                }
                catch (Exception ex)
                {
                    string str = ex.Message;
                }
            }
            LoadLines();
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

                // get lines from DocLines
                var Lines = _db.DocLines.Where(x => x.DocID == docid).OrderBy(x=>x.LineID).ToList();
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
                    QtyLeft = line.QtyLeft,
                    ReceiveQty = line.ReceiveQty,
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
                foreach (var TL in TempLines)
                {
                    if (containsServ == false)
                    {
                        if (TL.ItemType == 1 || TL.ItemType ==2)
                        {
                            RBAllocateCosts.Enabled = true;
                            PnlServices.Style.Add("display", "inline-block");
                            ViewState["pnlServicesDisplay"] = "inline-block";
                            PnlServices.Style.Add("max-width", "50%");
                            containsServ = true;
                            if (TL.ItemType == 2) RBAllocateCosts.Enabled = false;
                            break;
                        }
                    }
                    if (TL.Quantity != null)
                    {
                        TL.Quantity = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(TL.Quantity.ToString(), CurrentUser.CompanyDecPlaces));
                    }
                    if (TL.Quantity != null)
                    {
                        TL.ReceiveQty = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(TL.ReceiveQty.ToString(), CurrentUser.CompanyDecPlaces));
                    }
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
                decimal OrdQty = 0, recqty = 0, LineVal = 0, LineTax = 0, LineExTax = 0; ;
                foreach (TempDocLine tl in TempLines)
                {
                    OrdQty = (decimal)tl.Quantity;
                    if (tl.ReceiveQty != null) recqty = (decimal)tl.ReceiveQty;
                    if (tl.Total != null) LineVal = (decimal)tl.Total;
                    if (tl.Tax != null) LineTax = (decimal)tl.Tax;
                    if (tl.Exclusive != null && tl.ItemType != 2) LineExTax = (decimal)tl.Exclusive;
                    if (OrdQty > 0) tl.ReceiveTotal = (LineVal / OrdQty) * recqty;
                    if (OrdQty > 0) tl.ReceiveTotalTax = (LineTax / OrdQty) * recqty;
                    if (OrdQty > 0) tl.ReceiveTotalExcl = (LineExTax / OrdQty) * recqty;
                    if (recqty > 0)
                    {
                        tl.QtyVar = Math.Round(OrdQty / recqty, 2);
                    }
                    else
                    {
                        tl.QtyVar = 0;
                    }
                    if (OrdQty > 0) RecValue += (decimal)tl.ReceiveTotal;
                    if (OrdQty > 0) RecTax += (decimal)tl.ReceiveTotalTax;
                    if (OrdQty > 0) RecEx += (decimal)tl.ReceiveTotalExcl;
                }
                _db.SaveChanges();
            }
        }
        protected void LoadStores()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true && x.AllowReceiving == true && x.StoreCode != "CoR" && x.StoreCode != "CoD").ToList(); // && x.StoreCode != "Co"
                DDStore.DataSource = stores;
                DDStore.DataTextField = "StoreDescript";
                DDStore.DataValueField = "StoreCode";
                DDStore.DataBind(); 
                
                DDStoreEdit.DataSource = stores;
                DDStoreEdit.DataTextField = "StoreDescript";
                DDStoreEdit.DataValueField = "StoreCode";
                DDStoreEdit.DataBind();
                
                if (stores.Count > 1)
                {
                    DDStore.Items.Insert(0, "-Select-");
                    DDStoreEdit.Items.Insert(0, "-Select-");
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
                if (e.Row.Cells[4].Text != e.Row.Cells[11].Text)
                {
                    e.Row.Cells[11].BackColor = System.Drawing.Color.AntiqueWhite;
                }
                if (Convert.ToDecimal(e.Row.Cells[15].Text.ToString()) > (decimal)1.05 || Convert.ToDecimal(e.Row.Cells[15].Text.ToString()) < (decimal)0.95) e.Row.Cells[9].Style.Add("border", "1px solid red");
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
            }
            e.Row.Cells[15].Visible = false;
            e.Row.Cells[16].Visible = false;
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
                QtyOrd = decimal.TryParse(txtordqty.Text.Replace(" ", "").Replace("\u00A0", "").Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal resultOrd) ? resultOrd : 0;
                QtyLeft = QtyOrd - QtyRec;
            } catch (Exception ex)
            {
                string message = "Receiving Qty Error - Unable to continue: " + ex.Message;
                AlertHelper.ShowSweetAlert(this, message, "error");
                return;
            }
           
            long lineid = Convert.ToInt64(lblLineID.Text);
            if (QtyLeft != 0)
            {
               if (!chkAccept.Checked)
                {
                    string message = "Please accept the variation to continue";
                    AlertHelper.ShowSweetAlert(this, message, "warning");
                    ModalPopupExtender1.Show();
                    return;
                }
                // insert row into outstandingReceiving table.
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var Docline = _db.TempDocLines.Where(x => x.LineID == lineid).FirstOrDefault();
                    long docid = Convert.ToInt64(lblDocID.Text);
                    // check for part received quantities and calculate balance
                    decimal Prerecqty = _db.ReceivingOutstandings
                             .Where(x => x.ItemCode == Docline.ItemCode && x.PODocID == docid && x.Archive==false)
                             .Sum(x => (decimal?)x.RecQty) ?? 0;
                    if (Prerecqty > 0)
                    {
                        QtyLeft = QtyOrd - (Prerecqty + QtyRec);
                    } 
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
                        CreatedBy = CurrentUser.RoleID,
                        CreatedDate = DateTime.Now,
                        Archive = false
                    };
                _db.ReceivingOutstandings.Add(or);
                    if (chkAddLotNum.Checked == true)
                    {
                        // add new row to the document. 
                        TempDocLine Tdl = new TempDocLine();
                        Tdl = Docline;
                        Tdl.LotNumber = lblLotNum.Text;
                        Tdl.ReceiveQty = QtyRec;
                        _db.TempDocLines.Add(Tdl);
                        _db.SaveChanges();
                        lineid = Tdl.LineID;
                    }
                    _db.SaveChanges();
                }
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
                if(DDStore.Enabled == true) Docline.StoreCode = DDStoreEdit.SelectedValue.ToString();
                Docline.ReceiveQty = QtyRec;
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
                        DateTime ubDate = Convert.ToDateTime(txtUseBy.Text);
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
            string SuppInvN = "";
            if (txtDNNum.Text.Trim().ToString().Length < 3)
            {
                string warnMsg = "Please capture a Delivery Note # longer that 3 characters.";
                AlertHelper.ShowSweetAlert(this, warnMsg, "warning");
                return;
            } 
            if (txtDNNum.Text.Trim().ToString().Length < 3 && txtInvNum.Text.Trim().ToString().Length < 3)
            {
                string warnMsg = "Please capture an Invoice # longer that 3 characters.";
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

            try
            {
                ApiUrlCall api = new ApiUrlCall();
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {

                    // get DocHeader
                    var Head = _db.DocHeaders.Where(x => x.DocID == docid).FirstOrDefault();

                    // check if Supplier Inv Num has already been received
                    if (txtInvNum.ToString().Trim().Length < 1)
                    {
                        SuppInvN = txtInvNum.Text.Trim().ToString();
                    }
                    else
                    {
                        SuppInvN = txtDNNum.Text.Trim().ToString();
                    }

                    int SuppInvNumb = _db.DocHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.SupplierInvNum == SuppInvN).Count();
                    if (SuppInvNumb > 0)
                    {
                        string warnMsg = "Supplier Invoice number already used, unable to duplicate.";
                        AlertHelper.ShowSweetAlert(this, warnMsg, "warning");
                    }

                    var FLines = _db.TempDocLines.Where(x => x.DocID == docid && x.ToReceive == true && x.ReceiveComplete == false).ToList();
                    if (FLines.Count == 0)
                    {
                        string message = "Please receive line items before continuing.";
                        AlertHelper.ShowSweetAlert(this, message, "warning");
                    }

                    #region create new documents preparing for Sage
                    // 3) Save supplier Invoice in SBCA.
                    Document Doc = new Document();
                    // add document header
                    SupplierInvoiceHeader DocH = new SupplierInvoiceHeader();

                    //ID = Head.DocID,
                    DocH.DueDate = DateTime.Now;
                    DocH.SupplierId = (long)Head.CustSuppID;
                    DocH.SupplierName = Head.CustSupName.ToString();
                    DocH.StatusId = 1;
                    DocH.Date = DateTime.Now;
                    DocH.Inclusive = (bool)Head.Inclusive;
                    DocH.DiscountPercentage = (decimal)Head.DiscountPercentage;
                    DocH.TaxReference = Head.TaxReference.ToString();
                    DocH.Reference = SuppInvN.ToString();
                    DocH.Message = Head.Message.ToString();
                    DocH.FromDocument = Head.DocumentNumber.ToString();
                    if (Head.Supplier_ExchangeRate != 1) DocH.Supplier_ExchangeRate = (decimal)Head.Supplier_ExchangeRate;
                    if (Head.Supplier_CurrencyId != null) DocH.Supplier_CurrencyId = (long)Head.Supplier_CurrencyId;

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
                            DL.Quantity = (decimal)dl.ReceiveQty;
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
                    }
                    ;
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

                        if (Doc.Header.Supplier_ExchangeRate != 1)
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
                                var jObj = JObject.Parse(jsonBody);
                                jObj["ID"] = docid;
                                jObj["StatusId"] = "4";
                                jObj["DueDate"]?.Parent.Remove();
                                jObj["DeliveryDate"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                                // Re-serialize back to string
                                string updatedJsonBody = jObj.ToString(Formatting.Indented);
                                // change status of PO to Invoiced., 
                                string doctype = "";
                                doctype = "PurchaseOrder";
                                ApiUrlCall Api = new ApiUrlCall();
                                JObject parsedJSON = await Api.APIUpdatePurchaseOrderAsync(doctype, updatedJsonBody, CurrentUser);

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
                        if (GridAddCosts.Rows.Count > 0)
                        {
                            var AddCosts = _db.ReceivingAddCosts.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == docid).ToList();
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
                                    TaxTypeId = DDVat.SelectedValue.ToString().Split('|')[0],
                                    Adcost.Exclusive,
                                    Tax = Adcost.Vat,
                                    Total = Adcost.Exclusive + Adcost.Vat,
                                    ContraAccountId = DDAcctList.SelectedValue
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
                                if (RBAllocateCosts.SelectedValue.ToString() == "1")
                                {
                                    decimal totalPriceExclusive = (decimal)FLines.Where(x => x.ItemType == 0).Sum(x => x.Exclusive);
                                    decimal AddCost = (decimal)FLines.Where(x => x.ItemType > 0).Sum(x => x.UnitPriceExclusive * x.ReceiveQty);
                                    // get total value of docLines                            
                                    ThisLineVal = (decimal)dl.Exclusive;
                                    AddCostPerc = ThisLineVal / totalPriceExclusive;
                                    AddCostPropValue = AddCostPerc * AddCost;
                                    ThisItemNettCost = (decimal)dl.Exclusive + AddCostPropValue;
                                    ThisItemUnitNett = ThisItemNettCost / (decimal)dl.ReceiveQty;
                                }
                                else
                                {
                                    ThisLineVal = (decimal)dl.Exclusive;
                                    AddCostPerc = 0;
                                    AddCostPropValue = 0;
                                    ThisItemNettCost = (decimal)dl.Exclusive;
                                    ThisItemUnitNett = ThisItemNettCost / (decimal)dl.ReceiveQty;
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
                                    tempLine.ReceiveQty = dl.ReceiveQty;
                                    tempLine.ToReceive = dl.ToReceive;
                                    tempLine.ReceiveComplete = true;
                                    tempLine.StoreCode = dl.StoreCode;
                                    tempLine.LotNumber = dl.LotNumber;
                                    tempLine.LineTaxTypeID = dl.LineTaxTypeID;
                                    tempLine.AddCostsAmount = AddCostPropValue;
                                    tempLine.AddCostsReason = txtAddCostsReason.Text.ToString();
                                    tempLine.ExchRate = dl.ExchRate;

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
                                    ItemTrans.Qty = dl.ReceiveQty;
                                    ItemTrans.PriceExclusive = dl.UnitPriceExclusive;
                                    ItemTrans.AdditionalCosts = ThisItemUnitNett - dl.UnitPriceExclusive;
                                    ItemTrans.TotalUnitPriceExclInclAdd = ThisItemUnitNett;
                                    ItemTrans.TotalLineValExcl = ThisItemUnitNett * dl.ReceiveQty;
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
                                        LotNumUpdate.LotTotUnitPrice = ThisItemUnitNett;
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
                                    if (Itm.AverageCost != ThisItemUnitNett)
                                    {
                                        #region AdjustItemOut
                                        ItemAdjustment iAdj = new ItemAdjustment();
                                        iAdj.Date = DateTime.Now;
                                        iAdj.ItemID = dl.SelectionId;
                                        // update master record of item before adjustments
                                        iAdj.AverageCost = (decimal)dl.UnitPriceExclusive;
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
                                        decimal NewTotVal = (decimal)(origvalue + (dl.UnitPriceExclusive * dl.ReceiveQty));
                                        decimal NewAvCost = NewTotVal / NewQty;

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
                        var AddC = _db.ReceivingAddCosts.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == docid).ToList();
                        decimal totAddCosts = 0;
                        decimal DocValue = (decimal)Head.Exclusive;
                        decimal totval = DocValue;
                        if (AddC.Count > 0)
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
                                decimal linevalue = (decimal)dl.ReceiveTotalExcl;
                                decimal qty = (decimal)dl.ReceiveQty;
                                decimal linevalueperc = 0;
                                if (linevalue != 0 && DocValue != 0)
                                {
                                    linevalueperc = linevalue / DocValue;
                                }

                                decimal linevalAddCosts = 0;
                                decimal unitAddCosts = 0;
                                decimal newunitcost = 0;
                                if (linevalue != 0 && DocValue != 0)
                                {
                                    newunitcost = (decimal)(linevalue / qty);
                                }
                                if (totAddCosts > 0)
                                {
                                    linevalAddCosts = linevalueperc * totAddCosts;
                                    unitAddCosts = linevalAddCosts / qty;
                                    newunitcost = (decimal)(unitAddCosts + (linevalue / qty));
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
                                tempLine.ReceiveQty = dl.ReceiveQty;
                                tempLine.ToReceive = dl.ToReceive;
                                tempLine.ReceiveComplete = true;
                                tempLine.StoreCode = dl.StoreCode;
                                tempLine.LotNumber = dl.LotNumber;
                                tempLine.LineTaxTypeID = dl.LineTaxTypeID;
                                tempLine.AddCostsAmount = linevalAddCosts;
                                tempLine.AddCostsReason = dl.AddCostsReason;
                                tempLine.ReceiveTotalExcl = dl.Exclusive - dl.Discount + linevalAddCosts;
                                tempLine.ExchRate = dl.ExchRate;
                                if (dl.ItemType == 0)
                                {
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
                                    ItemTrans.Qty = dl.ReceiveQty;
                                    ItemTrans.PriceExclusive = dl.UnitPriceExclusive;
                                    ItemTrans.TotalLineValExcl = tempLine.ReceiveTotalExcl;
                                    ItemTrans.AdditionalCosts = linevalAddCosts;
                                    ItemTrans.TotalUnitPriceExclInclAdd = newunitcost;
                                    ItemTrans.DocumentType = 2;
                                    ItemTrans.TransactionReference = SuppInvNum;
                                    // Additional costs ????
                                    ItemTrans.TransactionDate = DateTime.Now;
                                    ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid
                                    ItemTrans.ExchRate = tempLine.ExchRate;
                                    _db.ItemTransactions.Add(ItemTrans);

                                    if (dl.LotNumber != null)
                                    {
                                        var LotNumUpdate = _db.LotTrackingMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.LotNumber == dl.LotNumber).FirstOrDefault();
                                        LotNumUpdate.LotTotUnitPrice = (decimal)dl.UnitPriceExclusive;
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


                    //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

                    // SET DOC HEADER VALUES
                    if (LCount == FLines.Count)
                    {
                        Head.Complete = true;
                        Head.CompBy = 0;
                        Head.CompleteDate = DateTime.Now;
                        Head.SupplierInvNum = SuppInvNum.ToString();
                        Head.InvNum = txtInvNum.Text.ToString().Replace("'", "'')");
                        Head.DNNum = txtDNNum.Text.ToString().Replace("'", "'')");
                    }
                    if (RBpoStatus.SelectedValue.ToString() == "1") Head.Complete = false;

                    //%%%%%%%%%%%%%%%%%%%%%%%%%%%%

                    // get lines from TempDocLines
                    var Lines = _db.TempDocLines.Where(x => x.DocID == docid && x.ReceiveComplete == false).ToList();
                    _db.TempDocLines.RemoveRange(Lines);

                    //var DocLD = _db.DocLines.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == docid).ToList();
                    //if (DocLD != null)
                    //{
                    //    _db.DocLines.RemoveRange(DocLD);
                    //}

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
                // Handle errors
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
            chkAddLotNum.Checked = false;
            LinkButton lbtnItmC = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnItmC.NamingContainer;
            long lineid = Convert.ToInt64(lbtnItmC.CommandArgument);
            lblLineID.Text = lineid.ToString();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Docline = _db.TempDocLines.Where(x => x.LineID == lineid).FirstOrDefault();
                txtordqty.Text = ApiUrlCall.NumberToDecimal(Docline.Quantity.ToString(), CurrentUser.CompanyDecPlaces).ToString();
                txtQtyReceive.Text = ApiUrlCall.NumberToDecimal(Docline.ReceiveQty.ToString(), CurrentUser.CompanyDecPlaces).ToString(); 
                lblItemdescr.Text = Docline.ItemDescription.ToString();
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
            }
            DDStoreEdit.SelectedIndex = 0;
            if (chkReceiveComplete.Checked == true)
            {
                string message = "Receiving Complete, details not editable";
                AlertHelper.ShowSweetAlert(this, message, "error");
                return;
            }
            else
            {
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
            CreatePDF();
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
                chkAccept.Style.Add("display", "none");
                decimal OrdQty = Convert.ToDecimal(txtordqty.Text);
                decimal RecQty = Convert.ToDecimal(txtQtyReceive.Text);
                decimal BalQty = OrdQty - RecQty;
                if (RecQty >= (OrdQty * 1.05m) || RecQty <= (OrdQty * 0.95m))
                {
                    errpop.InnerText = "Warning - Quantity variation of 5% or more being received";
                    chkAccept.Style.Add("display", "inline-block");
                }  
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
        private void CreatePDF()
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
                    cell.Rowspan = 3;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Receiving Slip #", headfont));
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

                        if (DL.ReceiveQty != null)
                        {
                            if (!DL.ReceiveQty.ToString().StartsWith("0.00"))
                            {
                                cell4 = new PdfPCell(new Phrase(Convert.ToDecimal(ApiUrlCall.NumberToDecimal(DL.Quantity.ToString(), CurrentUser.CompanyDecPlaces)).ToString(), regfont));
                                //cell4 = new PdfPCell(new Phrase((DL.ReceiveQty ?? 0).ToString("N2"), regfont));
                            }
                            else
                            {
                                cell4 = new PdfPCell(new Phrase("", regfont));
                            }         
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
            LinkButton lbtnLotNumAdd = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnLotNumAdd.NamingContainer;
            if (row.Cells[11].Text.Trim().Replace("&nbsp;", "") != "")
            {
                chkAddLotNum.Checked = true;
                LinkButton lbtnItmC = new LinkButton();
                lbtnItmC = (LinkButton)row.FindControl("lbtnItmC");
                long lineid = Convert.ToInt64(lbtnItmC.CommandArgument);
                lblLineID.Text = lineid.ToString();
                
                //using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                //{
                //    decimal Prerecqty = _db.ReceivingOutstandings
                //             .Where(x => x.ItemCode == Docline.ItemCode && x.PODocID == docid && x.Archive == false)
                //             .Sum(x => (decimal?)x.RecQty) ?? 0;
                //    if (Prerecqty > 0)
                //    {
                //        QtyLeft = QtyOrd - (Prerecqty + QtyRec);
                //    }
                //}


                lblLotNum.Text = "";
                txtQtyReceive.Text = "0";
                DDStoreEdit.SelectedIndex = 0;
            }

            ModalPopupExtender1.Show();
        }
    }
}