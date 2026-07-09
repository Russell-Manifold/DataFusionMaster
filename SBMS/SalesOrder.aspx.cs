using DocumentFormat.OpenXml.Drawing;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography.Pkcs;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class SalesOrder : BasePage
    {
        long docid = 0;
        string docguid;

        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }

        protected async void Page_Load(object sender, EventArgs e)
        {
            // Always restore docguid from QueryString or ViewState
            if (Request.QueryString["docid"] != null)
            {
                docguid = Request.QueryString["docid"];
                ViewState["docguid"] = docguid;
            }
            else
            {
                docguid = ViewState["docguid"] as string;
            }

            // Always restore docid from label on postback
            if (IsPostBack && lblDocID.Text.Length > 0)
            {
                docid = Convert.ToInt64(lblDocID.Text);
            }

            bool AutoUpdate = false;
            try
            {
                if (Request.QueryString["autosave"] != null)
                    AutoUpdate = Convert.ToBoolean(Request.QueryString["autosave"]);
            }
            catch { }

            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                // Load company image
                string imgname = CurrentUser.CoID + ".png";
                string imgPath = $"~/images/CoImages/{imgname}";
                imgCoImg.ImageUrl = File.Exists(Server.MapPath(imgPath)) ? ResolveUrl(imgPath) : ResolveUrl("~/images/CoImages/0000.png");

                // Load the order
                LoadOrder();

                if (AutoUpdate)
                {
                    if (await PostOrder() == "OK")
                    {
                        lbtnPost.Style.Add("display", "none");
                        lblErr.Text = "Sales Order updated in Sage.";
                        lbtnTaxInv.Style.Add("display", "none");
                        if (CurrentUser.AutoGenTaxInvoice)
                        {
                            lbtnTaxInv.Style.Add("display", "inline-block");
                        }
                    }
                }
                else
                {
                    lbtnTaxInv.Style.Add("display", "none");
                }
            }
            else
            {
                string eventTarget = Request["__EVENTTARGET"];
                if (eventTarget == "GenerateInvoice")
                {
                    lbtnTaxInv_Click(sender, EventArgs.Empty);
                }
            }
        }

        //protected async void Page_Load(object sender, EventArgs e)
        //{
        //    docguid = Request.QueryString["docid"];
        //    bool AutoUpdate = false;
        //    try
        //    {
        //        if (Request.QueryString["autosave"] != null)
        //            AutoUpdate = Convert.ToBoolean(Request.QueryString["autosave"]);
        //    }
        //    catch { }

        //    if (CurrentUser == null)
        //    {
        //        Response.Redirect("~/Login.aspx", false);
        //        Context.ApplicationInstance.CompleteRequest();
        //        return;
        //    }

        //    if (!IsPostBack)
        //    {
        //        // Load company image
        //        string imgname = CurrentUser.CoID + ".png";
        //        string imgPath = $"~/images/CoImages/{imgname}";
        //        imgCoImg.ImageUrl = File.Exists(Server.MapPath(imgPath)) ? ResolveUrl(imgPath) : ResolveUrl("~/images/CoImages/0000.png");

        //        // Load the order
        //        LoadOrder();

        //        if (AutoUpdate)
        //        {
        //            // Auto-post order
        //            if (await PostOrder() == "OK")
        //            {
        //                lbtnPost.Style.Add("display", "none");
        //                lblErr.Text =  "Sales Order updated in Sage."; 
        //                lbtnTaxInv.Style.Add("display", "none");
        //                if (CurrentUser.AutoGenTaxInvoice)
        //                {
        //                    lbtnTaxInv.Style.Add("display", "inline-block");
        //                }
        //            }
        //        }
        //        else
        //        {
        //            lbtnTaxInv.Style.Add("display", "none");
        //        }
        //    }
        //    else
        //    {
        //        string eventTarget = Request["__EVENTTARGET"];
        //        if (eventTarget == "GenerateInvoice")
        //        {
        //            lbtnTaxInv_Click(sender, EventArgs.Empty);
        //        }
        //    }
        //}

        private void LoadOrder()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var thispo = _db.GetOneDocHeaderFromDocID(CurrentUser.CoID, docguid).FirstOrDefault();
                if (thispo != null)
                {
                    docid = thispo.DocID;
                    lblDocID.Text = thispo.DocID.ToString();
                    txtCustName.Text = thispo.CustSupName.ToString();
                    lblCustID.Text = thispo.CustSuppID.ToString();
                    lblDocNum.Text = (thispo.DocumentNumber ?? "").ToString();
                    lblSOStatus.Text = "(" + (thispo.Status ?? "").ToString() + ")";
                    lbtnTaxInv.Style.Add("display", "none"); 
                    if (CurrentUser.AutoGenTaxInvoice == true)
                    {
                        lbtnTaxInv.Style.Add("display", "inline-block");
                    }
                    if (thispo.Status == "Invoiced")
                    {
                        lblSOStatus.ForeColor = System.Drawing.Color.Red;
                        lbtnPost.Style.Add("display", "none");
                        lbtnUndo.Style.Add("display", "none");
                        lbtnTaxInv.Style.Add("display", "none");
                    }
                    else
                    if (thispo.Status == "Partially Invoiced")
                    {
                        // Back order: still open for further picking/invoicing of the outstanding balance.
                        lblSOStatus.ForeColor = System.Drawing.Color.DarkOrange;
                        lblSOStatus.Font.Bold = true;
                    }
                    else
                    if (thispo.Status == "Cancelled")
                    {
                        lblSOStatus.ForeColor = System.Drawing.Color.Orange;
                        lbtnTaxInv.Style.Add("display", "none");
                    }

                    txtRef.Text = (thispo.Reference ?? "").ToString();
                    txtPODate.Text = Convert.ToDateTime(thispo.DueDelDate).ToString("dd MMM yyyy");
                    txtCaptDate.Text = Convert.ToDateTime(thispo.DocDate).ToString("dd MMM yyyy");
                    txtMsg.Text = thispo.Message ?? "";
                    txtRep.Text = thispo.SalesRepName ?? "";
                    lblRepID.Text = thispo.SalesRepresentativeId.ToString();
                    lbtnViewPS.Style.Add("display", "inline-block");
                    lbtnViewJC.Style.Add("display", "inline-block");

                    txtAddress1.Text = thispo.DelAddress1 ?? "";
                    txtAddress2.Text = thispo.DelAddress2 ?? "";
                    txtAddress3.Text = thispo.DelAddress3 ?? "";
                    txtAddress4.Text = thispo.DelAddress4 ?? "";

                    lblDocCost.Text = (thispo.DocCost ?? 0m).ToString("N2");
                    lblDocGP.Text = (thispo.DocGP ?? 0m).ToString("P2");

                    DDOptions.Attributes.Add("style", "display:inline-block; color:#4282C1; font-size:1em; border: 1px #4282C1 solid; border-radius:.25em; margin-top:.5em");
                    //if (thispo.LinkedJCNum != null || thispo.LinkedPSNum != null) DDOptions.Attributes.Add("style", "display:none");

                    if (CurrentUser.UseModule2 == false)
                    {
                        // Remove by Value
                        var item = DDOptions.Items.FindByValue("0");
                        if (item != null) DDOptions.Items.Remove(item);
                    }
                    else if (thispo.LinkedJCNum != null)
                    {
                        var item = DDOptions.Items.FindByValue("0");
                        if (item != null) DDOptions.Items.Remove(item);
                    }


                    if (CurrentUser.UseModule3 == false)
                    {
                        // Remove by Value
                        var item = DDOptions.Items.FindByValue("2");
                        if (item != null)
                            DDOptions.Items.Remove(item);
                    }
                    

                    if (CurrentUser.CanViewJobCards != false)
                    {
                        if (thispo.LinkedJCNum != null)
                        {
                            txtJCNum.Text = thispo.LinkedJCNum.ToString();
                            lbtnViewPS.Style.Add("display", "none");
                            lbtnViewJC.Style.Add("display", "inline-block");
                            txtPSNum.Enabled = false;
                            lblStatus.Text = (thispo.JCStatus ?? "").ToString();

                            if (thispo.JCIssuedTo != null)
                            {
                                var IssTo  =  _db.RolesMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.RoleID == thispo.JCIssuedTo);
                                if (IssTo != null) txtIssuedTo.Text = IssTo.RoleName.ToString();
                            }
                            
                            if (thispo.JCStatus == "Complete")
                            {
                                lblStatus.BackColor = System.Drawing.Color.Orange;
                            }
                        } else
                        {
                            if (CurrentUser.UseModule2 == false || thispo.LinkedJCNum == null) lbtnViewJC.Style.Add("display", "none");
                        }
                    }
                    else
                    {
                        if (CurrentUser.UseModule2 == false || thispo.LinkedJCNum == null) lbtnViewJC.Style.Add("display", "none");
                    }
                    if (thispo.LinkedPSNum != null)
                    {
                        lbtnViewPS.Style.Add("display", "inline-block");
                        var item = DDOptions.Items.FindByValue("1");
                        if (item != null) DDOptions.Items.Remove(item);
                        txtPSNum.Text = thispo.LinkedPSNum.ToString();
                        if (CurrentUser.UseModule2 == false || thispo.LinkedJCNum == null) lbtnViewJC.Style.Add("display", "none");
                        txtJCNum.Enabled = false;
                        lblStatus.Text = (thispo.PSStatus ?? "").ToString();
                        try
                        {
                            if (thispo.PSIssuedTo != null) txtIssuedTo.Text = _db.RolesMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.RoleID == thispo.PSIssuedTo).RoleName ?? "";
                        }
                        catch { }
                        if (thispo.PSStatus == "Complete")
                        {
                            lblStatus.BackColor = System.Drawing.Color.Orange;
                        }
                    }
                    else
                    {
                        lbtnViewPS.Style.Add("display", "none");
                    }
                    if (Convert.ToBoolean(thispo.Complete))
                    {
                        PnlButtons.Attributes.Add("style", "display:inline-block");
                        lbtnPrintDN.Attributes.Add("style", "display:inline-block");
                        if (CurrentUser.UATMode == false)
                        {
                            if (thispo.Status == "Invoiced")
                            {
                                lbtnPost.Style.Add("display", "none");
                                lbtnTaxInv.Style.Add("display", "none");
                                lbtnUndo.Style.Add("display", "none");
                                lblspan.Style.Add("display", "none");
                            }
                            else
                            {
                                lbtnTaxInv.Style.Add("display", "inline-block");
                                lbtnUndo.Style.Add("display", "inline-block");
                                lblspan.Style.Add("display", "inline-block");
                                lblspan.Style.Add("float", "left");
                            }
                        }
                    }
                    ApiUrlCall api = new ApiUrlCall();
                    api.LoadSOLines(docid, CurrentUser);
                    LoadLines();
                }
            }
        }
        protected void LoadLines()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Retrieve the records to be deleted
                var tempLinesToDelete = _db.TempDocLines.Where(x => x.DocID == docid).ToList();
                _db.TempDocLines.RemoveRange(tempLinesToDelete);
                _db.SaveChanges();

                // get lines from DocLines
                //var Lines = _db.DocLines.Where(x => x.DocID == docid && x.ReceiveComplete == false).OrderBy(x=>x.LineID).ToList();
                try
                {
                    var Lines = _db.DocLines.Where(x => x.DocID == docid).OrderBy(x => x.LineID).ToList();
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
                        LineTaxTypeID = line.LineTaxTypeID, 
                        ExchRate = (decimal) line.ExchRate ,
                        RejectQty = line.RejectQty
                    }).ToList();

                    // Insert the new list of entities into the TempDocLines table
                    _db.TempDocLines.AddRange(tempLines);

                    // Save changes to the database
                    _db.SaveChanges();
                }
                catch { }
                BindGrid();
            }
        }

        protected void BindGrid()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                //CalcTotals();       
                try
                {
                    var TempLines = _db.TempDocLines.Where(x => x.DocID == docid).OrderBy(x => x.LineID).ToList();
                    var totalQuantity = TempLines.Sum(x => x.Exclusive);
                    foreach (var TL in TempLines)
                    {
                        if (TL.Exclusive == 0)
                        {
                            lblspan.Style.Add("display", "inline-block");
                            lblspan.Style.Add("float", "left");
                            break;
                        }
                        if (TL.Quantity != null)
                        {
                            TL.Quantity = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(TL.Quantity.ToString(), CurrentUser.CompanyDecPlaces));
                        }
                        if (TL.ReceiveQty != null)
                        {
                            TL.ReceiveQty = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(TL.ReceiveQty.ToString(), CurrentUser.CompanyDecPlaces));
                        }
                    }

                    GridPOLines.DataSource = TempLines;
                    GridPOLines.DataBind();
                    bool hasZeroPrice = TempLines.Any(x => x.UnitPriceExclusive == 0);
                    if (hasZeroPrice)
                    {
                        lblspan.Style.Add("display", "inline-block");
                    } 
                    else 
                    {
                        lblspan.Style.Add("display", "none");
                    }
                    lblSubTotal.Text = TempLines.Sum(x => x.Exclusive).Value.ToString("N2");
                    lblTotVat.Text = TempLines.Sum(x => x.Tax).Value.ToString("N2");
                    lblTotal.Text = TempLines.Sum(x => x.Total).Value.ToString("N2");
                }
                catch { }
            }
        }

        protected void lbtnJCNew_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/JobCard.aspx?docid=" + docguid.ToString());
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
                else
                {
                    lblTpe.Text = "Picking Slip";
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
                WCHead.DueDate = Convert.ToDateTime(txtPODate.Text, CultureInfo.InvariantCulture);
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
                        WoL.DueDelDate = Convert.ToDateTime(txtPODate.Text, CultureInfo.InvariantCulture);
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
                    PSN.PSDueDate = (DateTime)Convert.ToDateTime(txtPODate.Text);
                    PSN.LinkedSOrdID = docid;
                    PSN.LinkedWONumber = 0;
                    if (DDStoreH.SelectedIndex > 0)
                    {
                        PSN.FromStoreID = Convert.ToInt64(_db.Stores.Where(x=>x.CompanyID == CurrentUser.CoID && x.StoreCode == stor).Select(x=>x.StoreID).FirstOrDefault());
                    }
                    else { PSN.FromStoreID = 0; }

                    _db.PickingSlipMasters.Add(PSN);
                    try
                    {
                        _db.SaveChanges();
                    }
                    catch (Exception ex) { }


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
                        } else { PsL.StoreCodeFrom = ""; }  
                        
                        PsL.LineType = Ln.LineType;
                        PsL.CompanyID = CurrentUser.CoID;
                        PsL.IsLotTracked = false;
                        var ThisItem = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == Ln.SelectionId).FirstOrDefault();
                        if (ThisItem != null)
                        {
                            PsL.IsLotTracked = ThisItem.IsLotTracked;
                        }
                        _db.PickSlipLines.Add(PsL);
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
                    if(Ln.LineType == 1) Jcl.LineType = 2;

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
                        var KitLines = _db.GetKitLinesFromKitCode(Ln.ItemCode, CurrentUser.CoID).Where(x => x.ItemID != null && x.ItemID >0 && x.FGQty > 0).ToList();
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
                catch (Exception ex)
                {
                }
                Response.Redirect("~/JobCard.aspx?docid=" + docguid.ToString());
            }
        }

        protected void lbtnViewPS_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/PickingSlip.aspx?docid=" + docguid.ToString());
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
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

        protected async void lbtnPost_Click(object sender, EventArgs e)
        {
            string RetStr = await PostOrder();
            if (RetStr == "OK")
            {
                // Posted (and any back-order SO created) - hide the button so the
                // same process cannot be triggered twice.
                lbtnPost.Style.Add("display", "none");
                if (CurrentUser.UATMode == false)
                {
                        if (CurrentUser.AutoGenTaxInvoice)
                        {
                            string script = @"
                                    Swal.fire({
                                        title: 'Generate Tax Invoice?',
                                        text: 'Do you want to generate the Tax Invoice for this Sales Order?',
                                        icon: 'question',
                                        showCancelButton: true,
                                        confirmButtonText: 'Yes',
                                        cancelButtonText: 'No'
                                    }).then((result) => {
                                        var overlay = document.getElementById('divOverlay');
                                        if (result.isConfirmed) {
                                            if (overlay) {
                                                overlay.style.display = 'block'; // Show overlay only on Yes
                                            }
                                            __doPostBack('GenerateInvoice','');
                                        } else {
                                            if (overlay) {
                                                overlay.style.display = 'none';  // Ensure hidden if Cancel
                                            }
                                        }
                                    });
                                ";
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "SweetAlertInvoice", script, true);
                        }
                }
                AlertHelper.ShowSweetAlert(this, "Sales Order updated successfully.", "success");
                return;
            }
            else
            {
                AlertHelper.ShowSweetAlert(this, $"Error occurred while posting the Sales Order -  {RetStr}" , "error");
            }
        }

        protected async Task<string> PostOrder()
        {
            string retStr = "OK";long RepID = 0;
            ApiUrlCall api = new ApiUrlCall();
            docid = Convert.ToInt64(lblDocID.Text);
            string jsonBody = "";
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var SOLines = _db.DocLines.Where(x => x.DocID == docid).OrderBy(x=>x.LineID).ToList();
                List<DocumentLine> documentLines = new List<DocumentLine>();
                DateTime DueDt = Convert.ToDateTime(txtPODate.Text);
                DateTime DocDT = Convert.ToDateTime(txtCaptDate.Text);
                if (lblRepID.Text != null && lblRepID.Text != "")
                {
                    RepID = Convert.ToInt64(lblRepID.Text);
                }
                else
                {
                    RepID = 0;
                }
                SageSalesOrder SO = new SageSalesOrder();
                SalesOrderHeader SOH = new SalesOrderHeader();
                SOH.DeliveryDate = DueDt;
                SOH.CustomerId = Convert.ToInt64(lblCustID.Text);
                SOH.ID = docid;
                SOH.DocumentNumber = lblDocNum.Text.ToString();
                SOH.Date = DocDT;
                SOH.Message = txtMsg.Text.ToString().Trim();
                SOH.Reference = txtRef.Text.ToString().Trim() ?? "";
                if (RepID > 0) SOH.SalesRepresentativeId = RepID;

                // Back order: a stock line with nothing picked this cycle is left off the invoice and
                // kept on back order, rather than blocking the whole post. Only stop if nothing is picked.
                bool anyPicked = SOLines.Any(dl => (dl.ReceiveQty ?? 0) >= 0.1m || (dl.LineType ?? 0) != 0);
                if (!anyPicked)
                {
                    retStr = "Cannot post Sales Order with no picked lines. Please pick at least one line before posting.";
                    return retStr;
                }
                foreach (var dl in SOLines)
                {
                    // Back order: skip stock lines that were not picked this cycle (they stay outstanding).
                    if ((dl.LineType ?? 0) == 0 && (dl.ReceiveQty ?? 0) < 0.1m) continue;
                    decimal origqty = dl.ReceiveQty ?? 0;
                    DocumentLine DL = new DocumentLine();
                    if (dl.isKit != null && (bool)dl.isKit)
                    {
                        // 1) Adjust Kit Item IN  (Stock counted down via the invoice)
                        #region AdjustItemIn
                        ItemAdjustment iAdj = new ItemAdjustment();
                        iAdj.Date = DateTime.Now;
                        iAdj.ItemID = dl.SelectionId;
                        // get item av cost from Sage
                        await api.LoadOneItem(iAdj.ItemID, CurrentUser);
                        var Itm = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == iAdj.ItemID).FirstOrDefault();
                        decimal AvCost = (decimal)Itm.AverageCost;
                        iAdj.AverageCost = AvCost;
                        iAdj.Quantity = (decimal)dl.ReceiveQty; // ReceiveQty = PickedQty
                        iAdj.Reason = "" + lblDocNum.Text + " -  Adj in  for invoicing of " + dl.ItemDescription + " as Flexi-kit.";
                        iAdj.Created = DateTime.Now;
                        jsonBody = JsonConvert.SerializeObject(iAdj, Formatting.Indented);
                        if (CurrentUser.UATMode == false)
                        {
                            await SendItemAdjustment(jsonBody);
                        }

                        // remove from MDF stock
                        ItemTransaction ItemTrans = new ItemTransaction();
                        ItemTrans.CompanyID = CurrentUser.CoID;
                        ItemTrans.DocumentID = 0;
                        ItemTrans.TransactionType = "Jc-Mf";
                        ItemTrans.ItemID = dl.SelectionId;
                        ItemTrans.ItemCode = dl.ItemCode;
                        ItemTrans.ItemDescription = dl.ItemDescription;
                        ItemTrans.Unit = dl.Unit;
                        ItemTrans.FromID = 0;
                        ItemTrans.ToID = _db.Stores.Where(x => x.StoreCode == dl.StoreCode && x.CompanyID == CurrentUser.CoID).Select(x => x.StoreID).FirstOrDefault();
                        ItemTrans.Qty = Convert.ToDecimal(dl.ReceiveQty * -1);
                        ItemTrans.DocumentType = 1;
                        ItemTrans.TransactionDate = DateTime.Now;
                        ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid
                        ItemTrans.PriceExclusive = dl.Exclusive;
                        ItemTrans.AdditionalCosts = 0;
                        ItemTrans.TotalUnitPriceExclInclAdd = dl.Exclusive;
                        ItemTrans.TotalLineValExcl = ItemTrans.PriceExclusive * ItemTrans.Qty;
                        ItemTrans.TransactionReference = lblDocNum.Text + " Complete - Issued to Sage";
                        ItemTrans.LotNumber = dl.LotNumber;
                        ItemTrans.ExchRate = (decimal)dl.ExchRate;
                        // cost fields deliberately carry the selling price here; StoreAvgCost still tracks the true store cost
                        if (ItemTrans.Qty < 0)
                        {
                            ItemTrans.StoreAvgCost = StoreCosting.GetStoreAvgCost(_db, CurrentUser.CoID, (long)ItemTrans.ItemID, (long)ItemTrans.ToID);
                        }
                        else
                        {
                            ItemTrans.StoreAvgCost = StoreCosting.ComputeMovement(_db, CurrentUser.CoID, (long)ItemTrans.ItemID, (long)ItemTrans.ToID, ItemTrans.Qty ?? 0, ItemTrans.TotalLineValExcl ?? 0, out var _v);
                        }
                        _db.ItemTransactions.Add(ItemTrans);

                        Itm.QuantityOnHand = Itm.QuantityOnHand + ItemTrans.Qty;
                        #endregion
                    }
                   
                    if (dl.isBundle == null || !(bool)dl.isBundle)
                    {
                        // Adding docuiment lines based on selection of DDNCSelect - sending no charge lines to Sage.
                        if (Convert.ToInt16(DDNCSelect.SelectedValue) < 2)
                        {
                            DL.LineType = (int)dl.LineType;
                            DL.SelectionId = dl.SelectionId;
                            DL.Description = dl.ItemDescription;
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
                                DL.Comments = "Store: " + dl.StoreCode +  " - Lot # " + dl.LotNumber + " : " + dl.Comments;
                            }
                            else if (dl.StoreCode != null && dl.StoreCode.ToString() != "")
                            {
                                DL.Comments = "Store: " + dl.StoreCode+ " : " + dl.Comments;
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
                            // reset Qty to origiunal qty. SO Qty must remain as original, on item movement and tax invoice must reflect picked qty
                            DL.Quantity = origqty;
                            documentLines.Add(DL);
                        }
                        else if (Convert.ToInt16(DDNCSelect.SelectedValue) == 2)
                        {
                            // if Price > 0
                            if (dl.Exclusive > 0) 
                            {
                                DL.LineType = (int)dl.LineType;
                                DL.SelectionId = dl.SelectionId;
                                DL.Description = dl.ItemDescription;
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
                                DL.Comments = dl.Comments;
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
                                // reset Qty to original qty. SO Qty must remain as original, on item movement and tax invoice must reflect picked qty
                                DL.Quantity = origqty;
                                documentLines.Add(DL);
                            }
                            else
                            {
                                ItemAdjustment iAdj = new ItemAdjustment();
                                iAdj.ItemID = dl.SelectionId;
                                //await api.LoadOneItem(iAdj.ItemID, CurrentUser);
                                var itm = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == iAdj.ItemID).FirstOrDefault();
                                if ((bool)itm.Physical)
                                {
                                    // get item av cost from Sage
                                    decimal AvCost = (decimal)itm.AverageCost;
                                    // If NOT sending lines to Sage, all stock for no charge lines must be adjusted out. 
                                    iAdj.Date = DateTime.Now;
                                    iAdj.AverageCost = AvCost;
                                    iAdj.Quantity = (decimal)dl.ReceiveQty * -1;
                                    iAdj.Reason = "" + lblDocNum.Text + " -  Adj OUT for No-Charge Line: Invoicing of " + dl.ItemDescription;
                                    iAdj.Created = DateTime.Now;
                                    jsonBody = JsonConvert.SerializeObject(iAdj, Formatting.Indented);
                                    if (CurrentUser.UATMode == false)
                                    {
                                        await SendItemAdjustment(jsonBody);
                                    }
                                }
                            }
                        }
                    }
                };

                #region CollateDocForsending
                if (CurrentUser.UATMode == false)
                {
                    SO.Header = SOH;
                    SO.Lines = documentLines;
                    object jsonObject;
                    if (SO.Header.SalesRepresentativeId > 0)
                    {
                        jsonObject = new
                        {
                            SO.Header.DeliveryDate,
                            SO.Header.CustomerId,
                            SO.Header.ID,
                            SO.Header.DocumentNumber,
                            SO.Header.Date,
                            SO.Header.Message,
                            SO.Header.Reference,
                            SO.Header.SalesRepresentativeId,
                            SO.Lines
                        };
                    }
                    else
                    {
                        jsonObject = new
                        {
                            SO.Header.DeliveryDate,
                            SO.Header.CustomerId,
                            SO.Header.ID,
                            SO.Header.DocumentNumber,
                            SO.Header.Date,
                            SO.Header.Message,
                            SO.Header.Reference,
                            SO.Lines
                        };
                    }
                    jsonBody = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

                    // Back order: fetch the FULL original SO from Sage BEFORE the update below,
                    // because the update only sends the picked lines. The original line set is
                    // needed to build the new balance SO (same pattern as GenerateTaxInvoiceAsync).
                    bool balanceOwing = SOLines.Any(x => (x.QtyLeft ?? 0) > 0.0001m);
                    JObject origSoJson = null;
                    if (balanceOwing)
                    {
                        origSoJson = await api.GetOneSOFull(CurrentUser, docid);
                    }

                    string SupInv = await SendSalesOrder(jsonBody);
                    string SuppInvNum = "";
                    long SuppDocID = 0;
                    if (long.TryParse(SupInv.Split('|')[0], out long parsedValue))
                    {
                        SuppInvNum = SupInv.Split('|')[1].ToString();
                        SuppDocID = Convert.ToInt64(SupInv.Split('|')[0]);
                        DocNote Dnt = new DocNote
                        {
                            ID = 0,
                            CompanyId = CurrentUser.CoID,
                            DocumentHeaderId = SO.Header.ID,
                            NoteTypeId = 0,
                            Username = CurrentUser.LoginName,
                            Created = DateTime.Now,
                            Note = "Ready To Invoice: " + DateTime.Now,
                            Completed = false
                        };
                        string jsonBodyN = JsonConvert.SerializeObject(Dnt, Formatting.Indented);
                        string DocN = await SendDocHeaderNote(jsonBodyN);

                        // Back order: create a NEW Sales Order in Sage for the outstanding balance
                        // (QtyLeft > 0). It syncs back into SBMS on the next Sales Order load and then
                        // follows the normal pick -> invoice workflow. Reference links it to this SO.
                        if (balanceOwing && origSoJson != null && origSoJson.Count > 0)
                        {
                            JObject boJson = ConvertSOtoBackOrderSO(origSoJson);
                            string BOResult = await SendBackOrderSO(boJson.ToString());
                            if (long.TryParse(BOResult.Split('|')[0], out long boDocId))
                            {
                                // Balance transferred to the new SO - clear it off this order's lines so
                                // the invoice step can finalise this SO as "Invoiced".
                                foreach (var bl in SOLines.Where(x => (x.QtyLeft ?? 0) > 0)) bl.QtyLeft = 0;
                                lblErr.Text = "Back order Sales Order " + BOResult.Split('|')[1] + " created for the outstanding balance.";
                            }
                            else
                            {
                                // QtyLeft stays owing, so this SO keeps "Partially Invoiced" as a visible
                                // flag that the balance SO was NOT created (update to Sage again to retry).
                                lblErr.Text = "Warning: the back order Sales Order could not be created in Sage.";
                            }
                        }
                    }
                    else
                    {
                        return SupInv;
                    }
                }
                    var DH = _db.DocHeaders.Where(x => x.DocID == docid).FirstOrDefault();
                    if (DH != null)
                    {
                        DH.Active = false;
                        // update Job Car or picking slip header to Active=false
                        if (DH.LinkedJCID != null)
                        {
                            var jc = _db.JobCardsMasters.Where(x => x.JCID == DH.LinkedJCID && x.CustomerID == CurrentUser.CoID).FirstOrDefault();
                            if (jc != null)
                            {
                                jc.JCActive = false;
                            // set to "complete"
                                try
                                {
                                jc.JCWSID = _db.WorkStations.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.WSName.ToLower() == "complete").WSID;
                                }
                                catch { }    
                            }
                        }
                        else
                            if (DH.LinkedPSID != null)
                        {
                            var ps = _db.PickingSlipMasters.Where(x => x.PSID == DH.LinkedPSID && x.CustomerID == CurrentUser.CoID).FirstOrDefault();
                            if (ps != null)
                            {
                                ps.PSActive = false;
                            // set to "complete"
                                try
                                {
                                    ps.PSStationID = _db.PickSlipProcesses.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.PSName.ToLower() == "complete").PSPID;
                                }
                                catch { }
                            }
                        }
                        var DocLD = _db.DocLines.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == docid && x.SBCALineID == 0).ToList();
                        if (DocLD != null)
                        {
                            _db.DocLines.RemoveRange(DocLD);
                        }
                        _db.SaveChanges(); 
                }
                else
                {
                    retStr = "Error sending Sales Order to Sage, Please try again";
                }
                #endregion
                return retStr;
            }
           
        }

        private class DocNote
        {
            public int ID { get; set; }
            public long CompanyId { get; set; }
            public long DocumentHeaderId { get; set; }
            public long NoteTypeId { get; set; }
            public string Username { get; set; }
            public DateTime Created { get; set; }
            public string Note { get; set; }
            public bool Completed { get; set; }
        }

        public async Task <string> SendItemAdjustment(string Item)
        {
            string doctype = "";
            doctype = "ItemAdjustment";
            ApiUrlCall Api = new ApiUrlCall();
            JObject parsedJSON = await Api.APIPostDocumentAsync(doctype, Item, CurrentUser);
            return "";
        }

        public async Task <string> SendSalesOrder(string Doc)
        {
            string doctype = "";
            doctype = "SalesOrder";
            ApiUrlCall Api = new ApiUrlCall();
           var RetJson = await Api.APIUpdateSalesOrderAsync(doctype, Doc, CurrentUser);
            JObject parsedJSON = await Api.APIUpdateSalesOrderAsync(doctype, Doc, CurrentUser);
            if (parsedJSON.HasValues)
            {
                return parsedJSON["ID"].ToString() + "|" + parsedJSON["DocumentNumber"].ToString();
            } else
            {
                return "Error sending Sales Order.";
            }
        }

        public async Task <string> SendDocHeaderNote(string Doc)
        {
            string doctype = "";
            doctype = "DocumentHeaderNote";
            ApiUrlCall Api = new ApiUrlCall();
            JObject parsedJSON = await Api.APIPostDocumentAsync(doctype, Doc, CurrentUser);
            return parsedJSON["ID"].ToString() + "|" + parsedJSON["DocumentHeaderId"].ToString();
        }
        protected void lbtnUndo_Click(object sender, EventArgs e)
        {
            Guid guid;
            bool isValidGuid = Guid.TryParse(docguid, out guid);
            int Procid = 0; string ProcName = ""; int OldStat = 0;
            if (isValidGuid)
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var DocH = _db.DocHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.DocGUID == guid).FirstOrDefault();
                    DocH.Complete = false;
                    DocH.Active = true;
                    if (DocH.LinkedPSID > 0)
                    {
                        var Proc = _db.PickSlipProcesses.Where(x => x.CompanyID == CurrentUser.CoID && x.PSActive == true).OrderByDescending(x => x.Seq).ToList();
                        if (Proc.Count > 0)
                        {
                            Procid = Proc[1].PSPID;
                            ProcName = Proc[1].PSName ?? "";
                        }
                        var PSH = _db.PickingSlipMasters.Where(x => x.PSID == DocH.LinkedPSID).FirstOrDefault();
                        if (PSH != null)
                        {
                            OldStat = (int)PSH.PSStationID;
                            PSH.PSActive = true;
                            PSH.PSComplete = false;
                            PSH.PSStatus = ProcName;
                            PSH.PSStationID = Procid;

                            // insert Picking Slip Transaction record
                            PickSlipTransaction PSract = new PickSlipTransaction
                            {
                                CompanyID = CurrentUser.CoID,
                                PSID = (int?)DocH.LinkedPSID,
                                FromStationID = OldStat,
                                ToStationID = Procid,
                                MoveDate = DateTime.Now,
                                MoveQty = 1,
                                MoveBy = CurrentUser.RoleID,
                                RejectQty = 0
                            };
                            _db.PickSlipTransactions.Add(PSract);
                            // re-add the items back to ItemsMaster stock on hand
                            var PSLines = _db.PickSlipLines.Where(x => x.CompanyID == CurrentUser.CoID && x.PSID == DocH.LinkedPSID).OrderBy(x => x.LineID).ToList();
                            if (PSLines != null)
                            {
                                foreach (var pl in PSLines)
                                {
                                    decimal Qty = (decimal)pl.Quantity;
                                    var Itm = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == pl.SelectionId).FirstOrDefault();
                                    if (Itm != null)
                                    {
                                        if (Itm.QuantityOnHand != null)
                                        {
                                            Itm.QuantityOnHand = Itm.QuantityOnHand + Qty;
                                        }
                                        else
                                        {
                                            Itm.QuantityOnHand = Qty;
                                        }
                                    }
                                }
                                _db.SaveChanges();
                            }
                        }

                    }
                    if (DocH.LinkedJCID > 0)
                    {
                        var JCWstat = _db.WorkStations.Where(x => x.CompanyID == CurrentUser.CoID && x.WSActive == true).OrderByDescending(x => x.Seq).ToList();
                        if (JCWstat.Count > 0)
                        {
                            Procid = JCWstat[1].WSID;
                            ProcName = JCWstat[1].WSName ?? "";
                        }
                        var PJC = _db.JobCardsMasters.Where(x => x.JCID == DocH.LinkedJCID).FirstOrDefault();
                        if (PJC != null)
                        {
                            OldStat = (int)PJC.JCWSID;
                            PJC.JCActive = true;
                            PJC.JCComplete = false;
                            PJC.JCStatus = ProcName;
                            PJC.JCWSID = Procid;

                            // insert jobcardtransaction record
                            JobTransaction JTract = new JobTransaction
                            {
                                CompanyID = CurrentUser.CoID,
                                JCID = (int?)DocH.LinkedJCID,
                                FromStationID = OldStat,
                                ToStationID = Procid,
                                MoveDate = DateTime.Now,
                                MoveQty = PJC.JCQtyOfItems,
                                MoveBy = CurrentUser.RoleID,
                                RejectQty = 0
                            };
                            _db.JobTransactions.Add(JTract);
                           
                            // re-add the items back to ItemsMaster stock on hand
                            var JCLines = _db.JobCardLines.Where(x => x.CompanyID == CurrentUser.CoID && x.JCID == DocH.LinkedJCID && x.ItemCode != null).OrderBy(x => x.LineID).ToList();
                            if (JCLines != null)
                            {
                                foreach (var pl in JCLines)
                                {
                                    decimal Qty = (decimal)pl.Quantity;
                                    var Itm = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == pl.SelectionId).FirstOrDefault();
                                    Itm.QuantityOnHand = Itm.QuantityOnHand + Qty;
                                }
                                _db.SaveChanges();
                            }

                        }
                    }
                    _db.SaveChanges();
                    lbtnPrintDN.Attributes.Add("style", "display:none");
                    lbtnPost.Attributes.Add("style", "display:none");
                    lbtnUndo.Attributes.Add("style", "display:none");
                    string message = "Sales Order successfully re-opened for editing";
                    AlertHelper.ShowSweetAlert(this, message, "success");
                }
            }
        }

        protected void lbtnPrintDN_Click(object sender, EventArgs e)
        {
            CreatePDF();
            Response.Redirect($"~/ViewPDF.aspx?doc=" + CurrentUser.UserGuiD.ToString() + "\\DN_" + lblDocNum.Text, false);
        }

        private void CreatePDF()
        {
            string filepath = string.Empty, fname = string.Empty;
            var regfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 10, BaseColor.BLACK);
            var regfontS = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 8, BaseColor.BLACK);
            var medfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 11, BaseColor.BLACK);
            var headfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 18, BaseColor.BLACK);
            Guid DocGuid = Guid.Parse(docguid);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var DH = _db.DocHeaders.Where(x => x.DocGUID == DocGuid).FirstOrDefault();
                if (DH != null)
                {
                    iTextSharp.text.Document doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 40, 40);

                    try
                    {
                        if (!Directory.Exists(Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString())))
                        {
                            Directory.CreateDirectory(Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString()));
                        }
                        filepath = Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString() + "\\DN_" + lblDocNum.Text + ".PDF");
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
                    cell.Rowspan = 6;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("Delivery Note #", headfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(DH.DocumentNumber.Replace("SO", ""), headfont));
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

                    cell = new PdfPCell(new Phrase("Address:", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase((DH.DelAddress1 ?? "").ToString(), regfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase((DH.DelAddress2 ?? "").ToString(), regfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase((DH.DelAddress3 ?? "").ToString(), regfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("", medfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase((DH.DelAddress4 ?? "").ToString(), regfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);
                    doc.Add(table);
                    #endregion

                    #region messages
                    PdfPTable tableM = new PdfPTable(2);
                    PdfPCell cellM;
                    tableM.SpacingBefore = 15f;
                    tableM.TotalWidth = doc.PageSize.Width - 80;
                    tableM.LockedWidth = true;

                    // get delivery notes from Picking slip or Job card
                    string DelMsg = string.Empty;
                    if (DH.LinkedJCID != null)
                    {
                        DelMsg = _db.JobCardsMasters.Where(x => x.CustomerID == CurrentUser.CoID && x.JCID == DH.LinkedJCID).Select(x => x.JCDeliveryMessage).FirstOrDefault();
                    }
                    else if (DH.LinkedPSID != null)
                    {
                        DelMsg = _db.PickingSlipMasters.Where(x => x.CustomerID == CurrentUser.CoID && x.PSID == DH.LinkedPSID).Select(x => x.PSDeliverMessage).FirstOrDefault();
                    }
                    cellM = new PdfPCell(new Phrase("Delivery Notes:- " + Environment.NewLine + (DelMsg ?? "").ToString(), regfont));
                    cellM.HorizontalAlignment = 0;
                    cellM.FixedHeight = 80f; ;
                    tableM.AddCell(cellM);

                    cellM = new PdfPCell(new Phrase("Message:- " + Environment.NewLine + (DH.Message ?? "").ToString(), regfont));
                    cellM.HorizontalAlignment = 0;
                    cellM.FixedHeight = 60f; ;
                    tableM.AddCell(cellM);

                    doc.Add(tableM);
                    #endregion

                    #region HeaderRow
                    PdfPTable table4 = new PdfPTable(7);
                    PdfPCell cell4;
                    table4.SpacingBefore = 15f;
                    table4.SetWidths(new int[] { 50, 150, 90, 30, 90, 50, 80 });
                    table4.TotalWidth = doc.PageSize.Width - 80;
                    table4.LockedWidth = true;

                    cell4 = new PdfPCell(new Phrase("Item Code", regfont));
                    cell4.HorizontalAlignment = 0;
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

                    cell4 = new PdfPCell(new Phrase("Qty", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Accept", regfont));
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

                        string Barcode = _db.ItemBarCodeLinks.Where(x => x.ItemID == DL.SelectionId && x.QtyPerBarcode == 1).Select(x => x.BarCode).FirstOrDefault() ?? "";
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

                        cell4 = new PdfPCell(new Phrase((DL.LotNumber ?? "").ToString(), regfont));
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

                        cell4 = new PdfPCell(new Phrase("", regfont));
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                    }
                    doc.Add(table4);
                    doc.AddTitle("Delivery Note: ");
                    //doc.AddSubject("Classroom Review Instrument");
                    doc.AddAuthor("Data Fusion");
                    doc.Close();

                }
            }
        }

        protected void GridPOLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (CurrentUser.CompanyUseLotNumbers == false)
            {
                e.Row.Cells[6].Visible = false;
            }
            e.Row.Cells[0].Visible = false;
        }

        protected async void lbtnReload_Click(object sender, EventArgs e)
        {
            long docid = Convert.ToInt64(lblDocID.Text); int psid = 0;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var DocH = _db.DocHeaders.Where(x => x.DocID == docid && x.CompanyID == CurrentUser.CoID).FirstOrDefault();
                if (DocH != null)
                {
                    // remove the DocHeader, DocLines, PickingSlipMaster and PickSlipLines
                    if (DocH.LinkedPSID != null && DocH.LinkedPSID > 0)
                        psid = (int)DocH.LinkedPSID;

                    _db.DocHeaders.Remove(DocH);
                }

                var DocLD = _db.DocLines.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == docid).ToList();
                if (DocLD != null)
                {
                    _db.DocLines.RemoveRange(DocLD);
                }

                if (psid > 0)
                {
                    var psh = _db.PickingSlipMasters.Where(x => x.PSID == psid && x.CustomerID == CurrentUser.CoID).FirstOrDefault();
                    if (psh != null)_db.PickingSlipMasters.Remove(psh);

                    var psl = _db.PickSlipLines.Where(x => x.PSID == psid && x.CompanyID == CurrentUser.CoID).ToList();
                    if (psl != null) _db.PickSlipLines.RemoveRange(psl);
                }
                
                _db.SaveChanges();
            }
            await ApiUrlCall.GetOneSalesOrder(CurrentUser,  docid);
            LoadOrder();
            string message = "Sales Order reloaded successfully.";
            AlertHelper.ShowSweetAlert(this, message, "success");
        }
        protected void lbtnDelSO_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var thispo = _db.GetOneDocHeaderFromDocID(CurrentUser.CoID, docguid).FirstOrDefault();
                if (thispo != null)
                {
                    if (thispo.LinkedJCNum != null)
                    {
                        string message = "Cannot delete Sales Order, it is linked to Job Card: " + thispo.LinkedJCNum + " Please delete the Job Card first.";
                        AlertHelper.ShowSweetAlert(this, message, "warning");
                        return;
                    } else if (thispo.LinkedPSNum != null)
                    {
                        string message = "Cannot delete Sales Order, it is linked to Picking Slip: " + thispo.LinkedPSNum + " Please delete the Picking Slip first.";
                        AlertHelper.ShowSweetAlert(this, message, "warning");
                        return;
                    }

                  var docid = thispo.DocID; 
                  var dh = _db.DocHeaders.Where(x => x.DocID == docid && x.CompanyID == CurrentUser.CoID).FirstOrDefault();
                    _db.DocHeaders.Remove(dh);
                    var dl = _db.DocLines.Where(x=>x.CompanyID == CurrentUser.CoID && x.DocID == docid).ToList();
                    _db.DocLines.RemoveRange(dl);
                    _db.SaveChanges();
                    Response.Redirect("~/OSSalesOrders.aspx", true);
                }
            }
        }
        public async Task<string> SendTaxInvoice(string Doc)
        {
            string doctype = "";
            doctype = "TaxInvoice";
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

        // Back order: same mechanics as SendTaxInvoice, but the payload is posted as a
        // SalesOrder (SalesOrder/Save) to create the new SO for the outstanding balance.
        public async Task<string> SendBackOrderSO(string Doc)
        {
            string doctype = "";
            doctype = "SalesOrder";
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

        // Back order: build the new balance SO from the FULL original SO JSON (fetched via
        // GetOneSOFull before the original was updated). Mirrors ConvertSOtoTaxInvoice, except
        // the result stays a Sales Order: quantities become the outstanding balance (QtyLeft)
        // and fully-picked lines are dropped.
        private JObject ConvertSOtoBackOrderSO(JObject soJson)
        {
            if (soJson == null || soJson.Count == 0) return new JObject();

            // Clone original SO JSON so we don't mutate it
            JObject boJson = (JObject)soJson.DeepClone();
            // Remove identifiers so SalesOrder/Save creates a NEW document
            boJson.Remove("ID");
            boJson.Remove("DocumentNumber");
            boJson.Remove("StatusId");

            // Update header fields for the balance SO
            boJson["Date"] = DateTime.Now.ToString("yyyy-MM-dd");
            boJson["Reference"] = ("B/O " + (soJson["DocumentNumber"]?.ToString() ?? "")).Trim();

            // The original delivery/due date may be in the past; Sage rejects a due date
            // earlier than the new SO's posting date, so bump it to today when needed.
            if (!DateTime.TryParse(boJson["DeliveryDate"]?.ToString(), out DateTime delDate) || delDate.Date < DateTime.Now.Date)
            {
                boJson["DeliveryDate"] = DateTime.Now.ToString("yyyy-MM-dd");
            }
            if (boJson["DueDate"] != null)
            {
                if (!DateTime.TryParse(boJson["DueDate"]?.ToString(), out DateTime dueDate) || dueDate.Date < DateTime.Now.Date)
                {
                    boJson["DueDate"] = DateTime.Now.ToString("yyyy-MM-dd");
                }
            }

            boJson.Remove("Modified");
            boJson.Remove("Created");
            boJson.Remove("Printed");
            boJson.Remove("Editable");
            boJson.Remove("HasAttachments");
            boJson.Remove("HasNotes");
            boJson.Remove("HasSpecialCountryTax");
            boJson.Remove("Status");

            // Update Lines: only lines with an outstanding balance, at the balance quantity
            if (soJson["Lines"] is JArray soLines)
            {
                JArray boLines = new JArray();
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    foreach (JObject line in soLines)
                    {
                        var id = line["ID"]?.Value<long>() ?? 0;
                        decimal qtyLeft = _db.DocLines.Where(x => x.SBCALineID == id).Select(x => x.QtyLeft ?? 0).FirstOrDefault();
                        if (qtyLeft <= 0) continue;

                        JObject newLine = new JObject
                        {
                            ["SelectionId"] = line["SelectionId"],
                            ["TaxTypeId"] = line["TaxTypeId"],
                            ["Description"] = line["Description"],
                            ["LineType"] = line["LineType"],
                            ["Quantity"] = qtyLeft,
                            ["UnitPriceExclusive"] = line["UnitPriceExclusive"],
                            ["UnitPriceInclusive"] = line["UnitPriceInclusive"],
                            ["TaxPercentage"] = line["TaxPercentage"],
                            ["DiscountPercentage"] = line["DiscountPercentage"],
                            ["Exclusive"] = line["Exclusive"],
                            ["Discount"] = line["Discount"],
                            ["Tax"] = line["Tax"],
                            ["Total"] = line["Total"],
                            ["Unit"] = line["Unit"],
                            ["Comments"] = line["Comments"] ?? "",
                            ["CurrencyId"] = line["CurrencyId"],
                            ["UnitCost"] = line["UnitCost"],
                            ["ExchangeRate"] = line["ExchangeRate"] ?? soJson["Customer_ExchangeRate"] // use header as fallback
                        };
                        boLines.Add(newLine);
                    }
                }
                boJson["Lines"] = boLines;
            }

            return boJson;
        }
        private async Task GenerateTaxInvoiceAsync()
        {
            try
            {
                long docidS = Convert.ToInt64(lblDocID.Text);
                ApiUrlCall ApiC = new ApiUrlCall();

                // 1) Get full Sales Order JSON
                JObject soJson = await ApiC.GetOneSOFull(CurrentUser, docidS);

                if (soJson != null && soJson.Count > 0)
                {       
                    // 2) Post updated Sales Order back to Sage
                   string soResponse = await SendSalesOrder(soJson.ToString());

                    // 3) Convert to Tax Invoice JSON
                    JObject taxInvoiceJson = ConvertSOtoTaxInvoice(soJson);
                    // 4) Send Tax Invoice
                    string response = await SendTaxInvoice(taxInvoiceJson.ToString());

                    // 6) Show success alert
                    string docNumber = response.Contains("|") ? response.Split('|')[1] : response;
                    lbtnPost.Style.Add("display", "none");
                    lbtnUndo.Style.Add("display", "none");
                    lbtnTaxInv.Style.Add("display", "none");

                    lblErr.Text = docNumber + " Successfully generated";

                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var thispo = _db.DocHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == docidS).FirstOrDefault();
                        // Back order: QtyLeft > 0 means the balance SO has not been created in Sage yet
                        // (creation happens in PostOrder) - keep "Partially Invoiced" as a visible flag;
                        // otherwise finalise as "Invoiced".
                        bool stillOwing = _db.DocLines.Any(x => x.DocID == docidS && (x.QtyLeft ?? 0) > 0.0001m);
                        thispo.Status = stillOwing ? "Partially Invoiced" : "Invoiced";
                        _db.SaveChanges();
                     }

                    string message = $"Invoice created: {docNumber}";
                    AlertHelper.ShowSweetAlert(this, message, "success");
                }
                else
                {
                    string message = $"Sales Order not found";
                    AlertHelper.ShowSweetAlert(this, message, "error");
                }
            }
            catch (Exception ex)
            {
                string message = $"'Error','{ex.Message}";
                AlertHelper.ShowSweetAlert(this, message, "error");
            }
        }
        private JObject ConvertSOtoTaxInvoice(JObject soJson)
        {
            if (soJson == null || soJson.Count == 0) return new JObject();

            // Clone original SO JSON so we don't mutate it
            JObject invoiceJson = (JObject)soJson.DeepClone();   
            // Remove Sales Order specific identifiers
            invoiceJson.Remove("ID");
            invoiceJson.Remove("DocumentNumber");
            invoiceJson.Remove("StatusId");
            invoiceJson.Remove("DeliveryDate");

            // Update header fields for Tax Invoice
            invoiceJson["FromDocument"] = "SalesOrder";
            invoiceJson["FromDocumentId"] = soJson["ID"];
            invoiceJson["Date"] = DateTime.Now.ToString("yyyy-MM-dd");
            invoiceJson["DueDate"] = DateTime.Now.ToString("yyyy-MM-dd");

            invoiceJson.Remove("Modified");
            invoiceJson.Remove("Created");
            invoiceJson.Remove("Printed");
            invoiceJson.Remove("Editable");
            invoiceJson.Remove("HasAttachments");
            invoiceJson.Remove("HasNotes");
            invoiceJson.Remove("HasSpecialCountryTax");
            invoiceJson.Remove("Status");

            // Update Lines
            if (soJson["Lines"] is JArray soLines)
            {
                JArray invoiceLines = new JArray();
                foreach (JObject line in soLines)
                {
                    decimal pickQty = 0;
                    bool skipLine = false;
                    // get picked quantity from local db and send this quantity to the Invoice in Sage.
                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var id = line["ID"]?.Value<long>() ?? 0;
                        var dl = _db.DocLines.Where(x => x.SBCALineID == id).Select(x => new { x.ReceiveQty, x.LineType }).FirstOrDefault();
                        if (dl != null)
                        {
                            pickQty = dl.ReceiveQty ?? 0;
                            // Stock lines (LineType 0): invoice only the qty picked this cycle. If nothing was
                            // picked the line is fully back-ordered, so drop it from this invoice entirely.
                            if (dl.LineType == 0)
                            {
                                if (pickQty <= 0) skipLine = true;
                            }
                            else if (pickQty <= 0)
                            {
                                // Non-stock line (charge/comment/service): always invoice the full qty.
                                pickQty = line["Quantity"]?.Value<decimal>() ?? 0;
                            }
                        }
                        else if (pickQty <= 0)
                        {
                            pickQty = line["Quantity"]?.Value<decimal>() ?? 0;
                        }
                    }
                    if (skipLine) continue;

                    JObject newLine = new JObject
                    {
                        ["SelectionId"] = line["SelectionId"],
                        ["TaxTypeId"] = line["TaxTypeId"],
                        ["Description"] = line["Description"],
                        ["LineType"] = line["LineType"],
                        ["Quantity"] =pickQty,
                        ["UnitPriceExclusive"] = line["UnitPriceExclusive"],
                        ["UnitPriceInclusive"] = line["UnitPriceInclusive"],
                        ["TaxPercentage"] = line["TaxPercentage"],
                        ["DiscountPercentage"] = line["DiscountPercentage"],
                        ["Exclusive"] = line["Exclusive"],
                        ["Discount"] = line["Discount"],
                        ["Tax"] = line["Tax"],
                        ["Total"] = line["Total"],
                        ["Unit"] = line["Unit"],
                        ["Comments"] = line["Comments"] ?? "",
                        ["CurrencyId"] = line["CurrencyId"],
                        ["UnitCost"] = line["UnitCost"],
                        ["ExchangeRate"] = line["ExchangeRate"] ?? soJson["Customer_ExchangeRate"] // use header as fallback
                    };
                    invoiceLines.Add(newLine);
                }
                invoiceJson["Lines"] = invoiceLines;
            }

            return invoiceJson;
        }
        protected async void lbtnTaxInv_Click(object sender, EventArgs e)
        {
            await GenerateTaxInvoiceAsync();
        }
    }
}