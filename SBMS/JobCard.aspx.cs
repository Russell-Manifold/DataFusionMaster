using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Data.Entity;

namespace SBMS
{   
    public partial class JobCard : BasePage
    {
        long docid = 0;
        string docguid;
        long CoID;
        private List<ItemsMaster> _items;
        private List<GetLinkedStoredFromItem_Result> _itemST;
        private List<GetActiveLotNumbersLinkedToStores_Result> _ActiveLotNums;
        private List<AccountsMaster> _accounts;
        private List<BundlesHeader> _bundles;
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

            CoID = CurrentUser.CoID;
            docguid = Request.QueryString["docid"];
            ViewState["Bindgrid"] = "0";
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                _items = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CoID).OrderBy(x => x.Code).ToList();
                _itemST = _db.GetLinkedStoredFromItem(CoID).Where(X=>X.AllowPicking == true).OrderBy(x => x.StoreCode).ToList();
                _ActiveLotNums = _db.GetActiveLotNumbersLinkedToStores(CoID).OrderBy(x => x.LotNumber).ToList();
                _accounts = _db.AccountsMasters.Where(i => i.CompanyID == CoID && i.JCUse == true).OrderBy(x => x.AccountName).ToList();
                _bundles = _db.BundlesHeaders.Where(y => y.CompanyID == CoID && y.Active == true).OrderBy(y => y.BundCode).ToList();
            }
            if (!IsPostBack)
                {
                LoadDelivBy();
                LoadJCHeader();
                BindGrid();
                LoadHistory();
                }
         }

        protected void LoadJCHeader()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var thispo = _db.GetOneJobCardFromDocHeaderID(CoID, docguid).FirstOrDefault();
                if (thispo != null)
                {
                    docid = thispo.DocID;
                    lblDocID.Text = thispo.DocID.ToString();
                    txtCustName.Text = thispo.CustSupName.ToString();
                    lblJCid.Text = thispo.JCID.ToString();
                    lblDocNum.Text = (thispo.JCNumber ?? "").ToString();
                    txtRef.Text = (thispo.Reference ?? "").ToString();
                    txtPODate.Text = Convert.ToDateTime(thispo.DueDelDate).ToString("dd MMM yyyy");
                    txtPSNum.Text = thispo.DocumentNumber ?? "";
                    txtAddress1.Text = thispo.DelAddress1 ?? "";
                    txtAddress2.Text = thispo.DelAddress2 ?? "";
                    txtAddress3.Text = thispo.DelAddress3 ?? "";
                    if (thispo.DeliveryBy != null) { DDeliveryBy.Text = thispo.DeliveryBy.ToString(); }
                    txtWMsg.Text = thispo.JCWorkMessage ?? "";
                    txtDMsg.Text = thispo.JCDeliveryMessage ?? "";
                    txtPMsg.Text = thispo.JCPackMessage ?? "";
                    txtRep.Text = thispo.SalesRepName ?? "";
                    txtIssuedTo.Text = thispo.IssuedTo ?? "";
                    lblstatus.Text = thispo.JCStatus ?? "";
                    txtJobCardSummary.Text = thispo.JCSummary ?? "";
                    if (thispo.JCQtyOfItems != null) txtJCQuantity.Text = Convert.ToInt64(thispo.JCQtyOfItems).ToString();
                    if (thispo.JCStartDate != null)
                    {
                        try
                        {
                            DateTime dt = Convert.ToDateTime(thispo.JCStartDate.ToString(), CultureInfo.InvariantCulture);
                            txtIssueDate.Text = dt.ToString("dd MMM yyyy");
                        }
                        catch { }
                        
                    }
                    if (thispo.Complete == true)
                    {
                        LbtnJCSave.Attributes.Add("style", "display:none");
                        LbtnSaveEdits.Attributes.Add("style", "display:none");
                        lbtnDelJC.Attributes.Add("style", "display:none");
                    }
                }
            } 
        }
        protected void BindGrid ()
        {
            long jcid = Convert.ToInt64(lblJCid.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var TempLines = _db.JobCardLines.Where(x => x.JCID == jcid).OrderBy(x=>x.LineID).AsQueryable();
                if (LbtnJCSave.Attributes["style"].Contains("display:none"))
                {
                    // remove empty lines
                    var delLines = TempLines.Where(x => x.ItemDescription == null).ToList();
                    _db.JobCardLines.RemoveRange(delLines);
                    _db.SaveChanges();
                }
                else
                {
                    if (!TempLines.ToList().Any()) // If there are no lines
                    {
                        JobCardLine NewJCLine = new JobCardLine();
                        NewJCLine.JCID = jcid;
                        NewJCLine.LinePickDate = Convert.ToDateTime(txtPODate.Text);
                        NewJCLine.CompanyID = CurrentUser.CoID;
                        NewJCLine.IsLotTracked = false;
                        _db.JobCardLines.Add(NewJCLine);
                        _db.SaveChanges();
                    }
                    else
                    {
                        var lastLine = TempLines.ToList().Last();
                        if (lastLine != null && lastLine.ItemDescription != null) // Check if NewJCLine and JCID are not null
                        {
                            JobCardLine NewJCLine = new JobCardLine();
                            NewJCLine.JCID = jcid;
                            NewJCLine.LinePickDate = Convert.ToDateTime(txtPODate.Text);
                            NewJCLine.CompanyID = CurrentUser.CoID;
                            NewJCLine.IsLotTracked = false;
                            _db.JobCardLines.Add(NewJCLine);
                            _db.SaveChanges();
                        }
                    }
                }

               var DispLines = _db.JobCardLines.Where(x => x.JCID == jcid).OrderBy(x => x.LineID).ToList();
                foreach (var itm in DispLines)
                {
                    if (itm.Quantity != null)
                    {
                        itm.Quantity = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(itm.Quantity.ToString(), CurrentUser.CompanyDecPlaces));
                    }
                }
                GridJCLines.DataSource = DispLines.OrderBy(x=>x.LineID);
                GridJCLines.DataBind();

                if (txtJobCardSummary.Text.Length < 1)
                {
                    txtJobCardSummary.Text = DispLines[0].ItemDescription.ToString();
                    txtJCQuantity.Text = DispLines[0].Quantity.ToString();  
                }
            }
        }
   
        protected void GridJCLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            CheckBox chkCompl = (CheckBox)e.Row.FindControl("chkCompl");
            DropDownList ddLotNum = (DropDownList)e.Row.FindControl("DDlotNum");
            DropDownList DDStore = (DropDownList)e.Row.FindControl("DDStore");
            TextBox txtBarC = (TextBox)e.Row.FindControl("txtBarcode");
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var item = (JobCardLine)e.Row.DataItem;
                if (lblstatus.Text.ToLower().Contains("complete"))
                {
                    chkCompl.Enabled = false;
                }

                // Find the DropDownList in the current row
                var ddlItemType = (DropDownList)e.Row.FindControl("DDItemType");
                var ddlItemCode = (DropDownList)e.Row.FindControl("DDItemCode");
                if (item.isKitLine != null || item.isBundleLine != null)
                {
                    if ((bool)item.isKit || (bool)item.isBundle)
                    {
                        ddLotNum.Visible = false;
                        //DDStore.Visible = false; 
                        txtBarC.Visible = false;
                        chkCompl.Visible = false;
                    }
                }
                if (ddlItemType != null)
                {
                    // Set the selected value of the DropDownList to the value from the data item
                    
                    ddlItemType.SelectedValue = item.LineType.ToString();
                    if (ddlItemType.SelectedValue == "0" || ddlItemType.SelectedValue == "3")
                    {
                        if (item.Physical == false)
                        {
                            item.LineType = 1;
                            ddlItemCode.DataSource = _items.Where(x => x.Physical == false).ToList();
                            ddlItemType.SelectedValue = "1";
                            txtBarC.Visible = false;
                            ddLotNum.Visible = false;
                            DDStore.Visible = false;
                        }
                        else
                        {
                            ddlItemCode.DataSource = _items.Where(x => x.Physical == true).ToList();                        
                        }
                        ddlItemCode.DataTextField = "Code";
                        ddlItemCode.DataValueField = "ID";
                        ddlItemCode.DataBind();
                        ddlItemCode.Items.Insert(0, new System.Web.UI.WebControls.ListItem("Select", "0"));
                        ddlItemCode.SelectedValue = item.SelectionId.ToString();        
                    }
                    else
                    if (ddlItemType.SelectedValue == "1" || ddlItemType.SelectedValue == "2")
                    {
                        ddlItemType.SelectedValue = "2";
                        ddlItemCode.DataSource = _accounts;
                        ddlItemCode.DataTextField = "AccountName";
                        ddlItemCode.DataValueField = "AccountID";
                        ddlItemCode.DataBind();
                        ddlItemCode.Items.Insert(0, new System.Web.UI.WebControls.ListItem("Select", "0"));
                        ddlItemCode.SelectedValue = item.SelectionId.ToString();

                        ddLotNum.Visible = false;
                        DDStore.Visible = false;
                        txtBarC.Visible = false;
                        TextBox txtQty = (TextBox)e.Row.FindControl("txtQty");
                        txtQty.Enabled = false;
                    }
                    else
                    if (ddlItemType.SelectedValue == "6")
                    {
                        ddlItemCode.DataSource = _bundles;
                        ddlItemCode.DataTextField = "BundCode";
                        ddlItemCode.DataValueField = "BundCode";
                        ddlItemCode.DataBind();
                        ddlItemCode.Items.Insert(0, new System.Web.UI.WebControls.ListItem("Select", "0"));
                        ddlItemCode.SelectedValue = item.ItemCode.ToString();
                    }
                }
                if (ddlItemCode.SelectedIndex > 0 && (ddlItemType.SelectedValue == "0" || ddlItemType.SelectedValue == "3"))
                {
                    var StoreList = _itemST.Where(x => x.ItemID == item.SelectionId).ToList();
                    if (StoreList != null && StoreList.Count > 0)
                    {
                        DDStore.DataSource = StoreList;
                        DDStore.DataTextField = "StoreCode";
                        DDStore.DataBind();
                        DDStore.Items.Insert(0, "-?-");
                    }
                    if (item.StoreCodeFrom != null)
                    {
                        DDStore.SelectedValue = item.StoreCodeFrom.ToString();
                    }
                    //else
                    //{
                    //    if ((bool)item.isKit)
                    //    {
                    //        using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    //        {
                    //            var Store = _db.GetItemLinkedStores(CurrentUser.CoID, item.SelectionId).ToList().FirstOrDefault();
                    //            if (Store != null)
                    //            {
                    //                DDStore.SelectedValue = Store[0].ToString();
                    //            }
                    //            else
                    //            {
                    //                DDStore.BorderColor = System.Drawing.Color.Red;
                    //            }
                    //        }
                    //    }
                    //}            
                }
                if (item.IsLotTracked == false)
                {
                    ddLotNum.Visible = false;
                }
                else if (item.LotNumber != null)
                {
                    PopulateDDLotNumbers(e.Row, item.StoreCodeFrom, item.SelectionId);
                    ddLotNum.SelectedValue = item.LotNumber.ToString();
                } 

                if (LbtnJCSave.Attributes["style"].Contains("display:none"))
                {
                    ddlItemType.Enabled = false;
                    ddlItemCode.Enabled = false;
                    ddLotNum.Enabled = false;
                    DDStore.Enabled = false;
                    var txtQty = (TextBox)e.Row.FindControl("txtQty");
                    txtQty.ReadOnly = true;
                    txtBarC.ReadOnly = true;
                    var txtDescription = (TextBox)e.Row.FindControl("txtDescription");
                    txtDescription.ReadOnly = true;
                    var lbtnLineSave = (LinkButton)e.Row.FindControl("lbtnLineSave");
                    lbtnLineSave.Visible = false;
                    var lbtnComment = (LinkButton)e.Row.FindControl("lbtnComment");
                    lbtnComment.Visible = false;
                    LinkButton lbtnDeleteLine = (LinkButton)e.Row.FindControl("lbtnDeleteLine");
                    lbtnDeleteLine.Visible = false;
                }
            }
        }

        protected void DDItemType_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            GridViewRow row = (GridViewRow)ddl.NamingContainer;
            DropDownList ddlItemCode = (DropDownList)row.FindControl("DDItemCode");
            TextBox txtDesc = (TextBox)row.FindControl("txtDescription");
            int itemType = Convert.ToInt32(ddl.SelectedValue);
            PopulateDDItemCode(ddlItemCode, itemType);
            txtDesc.Text = string.Empty;

            DropDownList DDlotNum = (DropDownList)row.FindControl("DDlotNum");
            TextBox txtBarcode = (TextBox)row.FindControl("txtBarcode");
            DropDownList DDStore = (DropDownList)row.FindControl("DDStore");

            if (itemType == 1 || itemType == 2)
            { 
                DDlotNum.Items.Clear();
                DDlotNum.Visible = false;
                txtBarcode.Visible = false;  
                DDStore.Items.Clear();
                DDStore.Visible = false;
            }
            else
            {
                //DDlotNum.Items.Add("-Lot Number -");
                DDlotNum.Visible = true;
                txtBarcode.Visible = true;
                //DDStore.Visible = true;
            }
        }

        private void PopulateDDItemCode(DropDownList ddlItemCode, int itemType)
        {
            // Example LINQ query based on itemType
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var items = new List<ItemsMaster>(); // Initialize a list of items based on your context
                var servs = new List<ItemsMaster>(); // Initialize a list of items based on your context  
                var Accts = new List<AccountsMaster>(); // Initialize a list of items based on your context
                var Bundles = new List<BundlesHeader>(); // Initialize a list of items based on your context
                var Kits = new List<KitHeader>();

                switch (itemType)
                {
                    case 0: // Item
                        items = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CoID && i.Physical ==true).OrderBy(x => x.Code).ToList();
                        var ThisItemList = new List<ItemsList>();
                        foreach (var Itm in items)
                        {
                            ThisItemList.Add(new ItemsList
                            {
                                ItemID = Itm.ID,
                                ItemDescr = Itm.Code + " -> " + Itm.Description.ToString()
                            });
                        }
                        ddlItemCode.DataSource = ThisItemList;
                        ddlItemCode.DataTextField = "ItemDescr";
                        ddlItemCode.DataValueField = "ItemID";
                        ddlItemCode.DataBind();
                        break;
                    case 1: // Service
                        servs = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CoID && i.Physical == false).OrderBy(x=>x.Code).ToList();
                        var SItemList = new List<ItemsList>();
                        foreach (var Itm in servs)
                        {
                            SItemList.Add(new ItemsList
                            {
                                ItemID = Itm.ID,
                                ItemDescr = Itm.Code + " -> " + Itm.Description.ToString()
                            });
                        }
                        ddlItemCode.DataSource = SItemList;
                        ddlItemCode.DataTextField = "ItemDescr";
                        ddlItemCode.DataValueField = "ItemID";
                        ddlItemCode.DataBind();
                        break;
                    case 2: // Account
                        Accts = _db.AccountsMasters.Where(i => i.CompanyID == CoID && i.JCUse == true).OrderBy(x=>x.AccountName).ToList();
                        var AItemList = new List<ItemsList>();
                        foreach (var Itm in Accts)
                        {
                            AItemList.Add(new ItemsList
                            {
                                ItemID = (long)Itm.AccountID,
                                ItemDescr = Itm.AccountID.ToString() + " -> " + Itm.AccountName.ToString()
                            });
                        }
                        ddlItemCode.DataSource = AItemList;
                        ddlItemCode.DataTextField = "ItemDescr";
                        ddlItemCode.DataValueField = "ItemID";
                        ddlItemCode.DataBind();
                        break;
                    case 3: // Kit
                        Kits = _db.KitHeaders.Where(x=>x.CompanyID == CoID && x.KitActive == true).OrderBy(x=>x.KitCode).ToList();
                        var KItemList = new List<ItemsList>();
                        foreach (var Itm in Kits)
                        {
                            KItemList.Add(new ItemsList
                            {
                                ItemID = Itm.KitHID,
                                ItemDescr = Itm.KitCode.ToString() + " -> " + Itm.kitDescript.ToString()
                            });
                        }
                        ddlItemCode.DataSource = KItemList;
                        ddlItemCode.DataTextField = "ItemDescr";
                        ddlItemCode.DataValueField = "ItemID";
                        ddlItemCode.DataBind();
                        break;
                    case 6: // Bundle
                        Bundles = _db.BundlesHeaders.Where(x =>x.CompanyID == CoID &&  x.Active == true).OrderBy(x=>x.BundCode).ToList();
                        var BItemList = new List<ItemsList>();
                        foreach (var Itm in Bundles)
                        {
                            BItemList.Add(new ItemsList
                            {
                                ItemID = (long)Itm.SBCAID,
                                ItemDescr = Itm.BundCode.ToString() + " -> " + Itm.BundDescription.ToString()
                            });
                        }
                        ddlItemCode.DataSource = BItemList;
                        ddlItemCode.DataTextField = "ItemDescr";
                        ddlItemCode.DataValueField = "ItemID";
                        ddlItemCode.DataBind();
                        break;
                }

                ddlItemCode.Items.Insert(0, new System.Web.UI.WebControls.ListItem("Select", "0"));
            }
        }
        
        protected void DDItemCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            GridViewRow row = (GridViewRow)ddl.NamingContainer;
            DropDownList DDItemType = (DropDownList)row.FindControl("DDItemType");
            DropDownList DDStore = (DropDownList)row.FindControl("DDStore");
            DDStore.Enabled = true;
            TextBox txtBarcode = (TextBox)row.FindControl("txtBarcode");
            txtBarcode.Enabled = true;
            DropDownList DDlotNum = (DropDownList)row.FindControl("DDlotNum");
            DDlotNum.Enabled = true;

            var jcl = (JobCardLine)row.DataItem;

            if (DDItemType.SelectedValue.ToString() == "0" || DDItemType.SelectedValue.ToString() == "1")
            {
                long selectedItemid = Convert.ToInt64(ddl.SelectedValue);
                if (ddl.SelectedIndex > 0)
                {
                    PopulateItemDetails(row, selectedItemid);
                }
            }
            else if (DDItemType.SelectedValue.ToString() == "2")
            {
                DDStore = (DropDownList)row.FindControl("DDStore");
                DDStore.Enabled = false;
                txtBarcode = (TextBox)row.FindControl("txtBarcode");
                txtBarcode.Enabled = false;
                DDlotNum = (DropDownList)row.FindControl("DDlotNum");
                DDlotNum.Enabled = false;
                var txtQty = (TextBox)row.FindControl("txtQty");
                txtQty.Text = "1";
                txtQty.ReadOnly = true;
            }
            else if (DDItemType.SelectedValue.ToString() == "3")
            {
                lblKitCode.Text = ddl.SelectedItem.Text.ToString().Split('-')[0].Trim();
                ModalPopupExtender1.Show();
            }
            else if (DDItemType.SelectedValue.ToString() == "6")
            {
                lblBundleCode.Text = ddl.SelectedItem.Text.ToString().Split('-')[0].Trim();
                ModalPopupExtender2.Show();
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
                    TextBox txtBarcode = (TextBox)row.FindControl("txtBarcode");
                    DropDownList DDlotNum = (DropDownList)row.FindControl("DDlotNum");
                    Label txtUnit = (Label)row.FindControl("txtUnit");
                    TextBox txtQty = (TextBox)row.FindControl("txtQty");
                    DropDownList DDStore = (DropDownList)row.FindControl("DDStore");

                    txtDescription.Text = item.Description;
                    txtBarcode.Text = item.BarCode;
                    txtUnit.Text = item.Unit;
                    txtQty.Text = "1"; // Adjust based on your item properties
                    // load Lot number available from Finished Good stores only 
                    var StoreList = _itemST.Where(x => x.ItemID == itemid).ToList();
                    if (StoreList != null && StoreList.Count > 0)
                    {
                        DDStore.DataSource = StoreList;
                        DDStore.DataTextField = "StoreCode";
                        DDStore.DataBind();
                        DDStore.Items.Insert(0, "-?-");
                    }
                    if (item.IsLotTracked == false)
                    {
                        DDlotNum.Enabled = false;
                    }
                }
            }
        }

        protected void txtBarcode_TextChanged(object sender, EventArgs e)
        {
            TextBox txtBarcode = (TextBox)sender;
            GridViewRow row = (GridViewRow)txtBarcode.NamingContainer;
            TextBox txtQty = (TextBox)row.FindControl("txtQty");
            string scannedBarcode = txtBarcode.Text;

            if (!string.IsNullOrEmpty(scannedBarcode))
            {
                PopulateDescriptionFromBarcode(row, scannedBarcode);
            }
            ScriptManager.RegisterStartupScript(this, this.GetType(), "SetFocus", $"document.getElementById('{txtQty.ClientID}').focus();", true);
        }

        private void PopulateDescriptionFromBarcode(GridViewRow row, string barcode)
        {
            // Example LINQ query based on barcode
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var item = _db.GetOneItemFromBarcode(CoID, barcode).FirstOrDefault();

                if (item != null)
                {
                    DropDownList itemdl = (DropDownList)row.FindControl("DDItemCode");
                    TextBox txtDescription = (TextBox)row.FindControl("txtDescription");
                    //DropDownList DDlotNum = (DropDownList)row.FindControl("DDlotNum");
                    TextBox txtUnit = (TextBox)row.FindControl("txtUnit");
                    TextBox txtQty = (TextBox)row.FindControl("txtQty");
                    itemdl.SelectedValue = item.ID.ToString();
                    txtDescription.Text = item.Description;
                    //txtLotNumber.Text = item.LotNumber;
                    txtUnit.Text = item.Unit;
                    txtQty.Text = "1"; // Adjust based on your item properties
                }
            }
        }

        protected void lbtnLineSave_Click(object sender, EventArgs e)
        {
            long jcid = Convert.ToInt64(lblJCid.Text);
            long DocID = Convert.ToInt64(lblDocID.Text);
            decimal qoh = 0;

            LinkButton lbtnLineSave = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnLineSave.NamingContainer;

            DropDownList DDItemType = (DropDownList)row.FindControl("DDItemType");
            DropDownList DDItemCode = (DropDownList)row.FindControl("DDItemCode");
            TextBox txtDescription = (TextBox)row.FindControl("txtDescription");
            TextBox txtBarcode = (TextBox)row.FindControl("txtBarcode");
            DropDownList DDlotNum = (DropDownList)row.FindControl("DDlotNum");
            Label txtUnit = (Label)row.FindControl("txtUnit");
            TextBox txtQty = (TextBox)row.FindControl("txtQty");
            TextBox txtUseQty = (TextBox)row.FindControl("txtUseQty");
            CheckBox chkCompl = (CheckBox)row.FindControl("chkCompl");
            DropDownList DDStore = (DropDownList)row.FindControl("DDStore");
            Label lblLotNum = (Label)row.FindControl("lblLotNum");

            if (DDItemCode.SelectedIndex == 0)
            {
                chkCompl.Checked = false;
                ShowMessage(sender, EventArgs.Empty, "Invalid Item selected, unable to continue");
                return;
            }

            long ThisLineID = Convert.ToInt64(lbtnLineSave.CommandArgument);
            // get line type
            int LType = 0;
            if  (Convert.ToInt32(DDItemType.SelectedValue) == 0 || Convert.ToInt32(DDItemType.SelectedValue) ==1 || Convert.ToInt32(DDItemType.SelectedValue) == 3)
            {
                LType = 0;
            } else if (Convert.ToInt32(DDItemType.SelectedValue) == 2)
            {
                LType = 1;
            }
            else if (Convert.ToInt32(DDItemType.SelectedValue) == 6)
            {
                LType = 6;
            }

            if (Convert.ToInt32(DDItemType.SelectedValue) == 0)
            {
                if (DDStore.SelectedIndex == 0)
                {
                    chkCompl.Checked = false;
                    ShowMessage(sender, EventArgs.Empty, "Invalid Store selected, unable to continue");
                    return;
                }
            }
            
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // update existing line
                var NewJCLine = _db.JobCardLines.Where(x => x.LineID == ThisLineID).FirstOrDefault();
                if (NewJCLine.IsLotTracked == true)
                {
                    if (DDlotNum.SelectedItem == null ||  DDlotNum.SelectedItem.Value.ToLower().Contains("number") || DDlotNum.Items.Count ==0)
                    {
                        chkCompl.Checked = false;
                        ShowMessage(sender, EventArgs.Empty, DDItemCode.SelectedItem.Text + " is Lot Tracked, select a valid lot number before continuing.");
                        return;
                    }
                }

                if (NewJCLine.isBundle == null || NewJCLine.isBundle == false)
                {
                    NewJCLine.LineType = LType;
                    NewJCLine.JCID = jcid;
                    NewJCLine.SelectionId = Convert.ToInt64(DDItemCode.SelectedItem.Value.ToString());
                    NewJCLine.ItemCode = DDItemCode.SelectedItem.Text.Split(new string[] { "->" }, StringSplitOptions.None)[0].ToString().Trim();
                    NewJCLine.ItemDescription = txtDescription.Text.ToString();
                    NewJCLine.BarCode = txtBarcode.Text.ToString() ?? "";
                    NewJCLine.StoreCodeFrom = DDStore.SelectedValue.ToString();
                    NewJCLine.LotNumber = string.Empty;
                    if (NewJCLine.IsLotTracked == true)
                    {
                        NewJCLine.LotNumber = DDlotNum.SelectedValue.ToString();
                    }
                    NewJCLine.LinePickDate = Convert.ToDateTime(txtPODate.Text);
                    if (DDlotNum.SelectedIndex > 0) NewJCLine.LotNumber = DDlotNum.SelectedValue.ToString();

                    if (txtQty.Text != string.Empty) NewJCLine.Quantity = Convert.ToDecimal(txtQty.Text.ToString());          
                    if (txtUseQty.Text != string.Empty) NewJCLine.LinePickQty = Convert.ToDecimal(txtUseQty.Text.ToString());
                    if ((txtQty.Text == string.Empty || txtQty.Text == "") && (txtUseQty.Text != "" && txtUseQty.Text != string.Empty)) NewJCLine.Quantity = Convert.ToDecimal(txtUseQty.Text.ToString());

                    NewJCLine.PickComplete = chkCompl.Checked;
                    var Itm = _db.ItemsMasters.Where(x => x.CompanyID == CoID && x.Code == NewJCLine.ItemCode).FirstOrDefault();
                    if (Itm != null)
                    {
                        NewJCLine.TaxPercentage = Convert.ToDecimal(Itm.TaxTypeSalesPerc != null ? (decimal)Itm.TaxTypeSalesPerc : 0m);
                        NewJCLine.UnitPriceExclusive = Convert.ToDecimal(Itm.PriceExclusive != null ? (decimal)Itm.PriceExclusive : 0m);
                        NewJCLine.UnitPriceInclusive = Convert.ToDecimal(Itm.PriceInclusive != null ? (decimal)Itm.PriceInclusive : 0m);
                        NewJCLine.TaxPercentage = Itm.TaxTypeSalesPerc != null ? (decimal)Itm.TaxTypeSalesPerc : 0m;
                        NewJCLine.Exclusive = NewJCLine.Quantity * NewJCLine.UnitPriceExclusive;
                        NewJCLine.Tax = NewJCLine.Exclusive * NewJCLine.TaxPercentage;
                        NewJCLine.Total = NewJCLine.Exclusive + NewJCLine.Tax;
                        NewJCLine.Unit = (Itm.Unit ?? "").ToString();
                        NewJCLine.Physical = Itm.Physical;
                        NewJCLine.LineTaxTypeID = Itm.TaxTypeIdSales;
                    }
                    else
                    {
                        NewJCLine.TaxPercentage = 0;
                        NewJCLine.UnitPriceExclusive = 0;
                        NewJCLine.UnitPriceInclusive = 0;
                        NewJCLine.TaxPercentage = 0;
                        NewJCLine.DiscountPercentage = 0;
                        NewJCLine.Discount = 0;
                        NewJCLine.Exclusive = 0;
                        NewJCLine.Tax = 0;
                        NewJCLine.Total = 0;
                        NewJCLine.Unit = "ea";
                        NewJCLine.Physical = false;
                        NewJCLine.LineTaxTypeID = 0;
                    }
                    NewJCLine.DiscountPercentage = 0;
                    NewJCLine.Discount = 0;
                    NewJCLine.PickComplete = chkCompl.Checked;
                    int FrmStorid = _db.Stores.Where(x => x.StoreCode == NewJCLine.StoreCodeFrom && x.CompanyID == CurrentUser.CoID).Select(x => x.StoreID).FirstOrDefault();

                    if (DDItemType.SelectedValue != "1" && DDItemType.SelectedValue != "2")
                    {
                        #region UpdateItemTransactions - reverse transaction if there is a previous one only
                        // NEED TO REVERSE TRANSACTIONS - NOT DELETE
                        if (NewJCLine.ItemTransLineID != null)
                        {
                            long TrnLineid = 0;
                            try
                            {
                                TrnLineid = Convert.ToInt64(NewJCLine.ItemTransLineID);
                            }
                            catch { }
                            if (TrnLineid > 0)
                            {
                                var ItmR = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.TrnID == TrnLineid).FirstOrDefault();
                                ItemTransaction ItemTrans = new ItemTransaction();
                                ItemTrans.CompanyID = CurrentUser.CoID;
                                ItemTrans.DocumentID = ItmR.DocumentID;
                                ItemTrans.TransactionType = "JC";
                                ItemTrans.ItemID = ItmR.ItemID;
                                ItemTrans.ItemCode = ItmR.ItemCode ?? "";
                                ItemTrans.ItemDescription = ItmR.ItemDescription ?? "";
                                ItemTrans.Unit = ItmR.Unit;
                                ItemTrans.FromID = 0;
                                ItemTrans.ToID = FrmStorid;
                                ItemTrans.Qty = Convert.ToDecimal(ItmR.Qty) * -1;
                                ItemTrans.DocumentType = 10;
                                ItemTrans.PriceExclusive = ItmR.PriceExclusive;
                                ItemTrans.TotalUnitPriceExclInclAdd = ItmR.TotalUnitPriceExclInclAdd;
                                ItemTrans.ExchRate = 1;
                                if (NewJCLine.IsLotTracked == true)
                                {
                                    var ItmT = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == NewJCLine.SelectionId && x.ToID == FrmStorid && x.LotNumber == DDlotNum.SelectedValue).OrderByDescending(x => x.TrnID);
                                    if (ItmT != null)
                                    {
                                        qoh = (decimal)ItmT.Sum(x=>x.Qty);
                                    }
                                }
                                else
                                {
                                    var ItmT = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == NewJCLine.SelectionId && x.ToID == FrmStorid).OrderByDescending(x => x.TrnID);
                                    if (ItmT != null)
                                    {
                                        qoh = (decimal)ItmT.Sum(x => x.Qty);
                                    }
                                }
                                ItemTrans.TransactionDate = DateTime.Now;
                                ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid     
                                ItemTrans.AdditionalCosts = 0;
                                ItemTrans.TotalLineValExcl = ItemTrans.TotalUnitPriceExclInclAdd * ItemTrans.Qty;
                                ItemTrans.TransactionReference = lblDocNum.Text + " Reversal";
                                ItemTrans.LotNumber = ItmR.LotNumber;
                                _db.ItemTransactions.Add(ItemTrans);
                                NewJCLine.ItemTransLineID = null;
                                _db.SaveChanges();
                            }
                        }
                        #endregion
                      
                        #region If Row is complete
                        if (chkCompl.Checked)
                        {
                            // add record to item movement table to update on hand balances   
                            ItemTransaction ItemTrans = new ItemTransaction();
                            ItemTrans.CompanyID = CurrentUser.CoID;
                            ItemTrans.DocumentID = Convert.ToInt64(lblJCid.Text);
                            if (NewJCLine.isKitLine == true)
                            {
                                ItemTrans.TransactionType = "JC-Dr";
                            }
                            else
                            {
                                ItemTrans.TransactionType = "JC";
                            }
                            ItemTrans.ItemID = Convert.ToInt64(NewJCLine.SelectionId);
                            ItemTrans.ItemCode = NewJCLine.ItemCode ?? "";
                            ItemTrans.ItemDescription = NewJCLine.ItemDescription ?? "";
                            ItemTrans.Unit = NewJCLine.Unit;
                            ItemTrans.FromID = 0;
                            ItemTrans.ToID = FrmStorid;
                            ItemTrans.Qty = Convert.ToDecimal(NewJCLine.Quantity) * -1;
                            ItemTrans.DocumentType = 7;
                            ItemTrans.ExchRate = 1;
                            // get latest ItemTransaction Line with Unit costs
                            if (!DDlotNum.SelectedValue.ToLower().Contains("number") && DDlotNum.SelectedValue.ToString() != "")
                            {
                                var ItmT = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == NewJCLine.SelectionId && x.ToID == FrmStorid && x.LotNumber == DDlotNum.SelectedValue).OrderByDescending(x => x.TrnID);
                                if (ItmT != null)
                                {
                                    qoh = (decimal)ItmT.Sum(x=>x.Qty);
                                    var lastTrn = ItmT.OrderByDescending(x => x.TrnID).FirstOrDefault();
                                    ItemTrans.PriceExclusive = lastTrn.PriceExclusive;
                                    ItemTrans.TotalUnitPriceExclInclAdd = lastTrn.TotalUnitPriceExclInclAdd;
                                }
                            }
                            else
                            {
                                var ItmT = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == NewJCLine.SelectionId && x.ToID == FrmStorid).OrderByDescending(x => x.TrnID);
                                if (ItmT != null)
                                {
                                    try
                                    {
                                        qoh = (decimal)ItmT.Sum(x => x.Qty);
                                        var lastTrn = ItmT.OrderByDescending(x => x.TrnID).FirstOrDefault();
                                        ItemTrans.PriceExclusive = lastTrn.PriceExclusive;
                                        ItemTrans.TotalUnitPriceExclInclAdd = lastTrn.TotalUnitPriceExclInclAdd;
                                    }
                                    catch 
                                    {
                                        qoh = 0;
                                        ItemTrans.PriceExclusive = Itm.PriceExclusive ;
                                        ItemTrans.TotalUnitPriceExclInclAdd = Itm.PriceExclusive;
                                    }
                                }
                            }
                            ItemTrans.TransactionDate = DateTime.Now;
                            ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid     
                            ItemTrans.AdditionalCosts = 0;
                            ItemTrans.TotalLineValExcl = ItemTrans.TotalUnitPriceExclInclAdd * ItemTrans.Qty;
                            ItemTrans.TransactionReference = lblDocNum.Text;
                            ItemTrans.LotNumber = string.Empty;
                            //ItemTrans.LotNumber = string.Empty;
                            if (NewJCLine.IsLotTracked == true)
                            {
                                ItemTrans.LotNumber = NewJCLine.LotNumber;
                            }     
                            _db.ItemTransactions.Add(ItemTrans);
                            _db.SaveChanges();
                            NewJCLine.ItemTransLineID = ItemTrans.TrnID;    
                        }
                        #endregion
                    }
                    _db.SaveChanges();
                }
                if (ViewState["Bindgrid"].ToString() == "0") BindGrid();
            }
        }

        protected void lntnDeleteLine_Click(object sender, EventArgs e)
        {
            LinkButton lbtnDeleteLine = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnDeleteLine.NamingContainer;
            
            long Lnid = Convert.ToInt64(lbtnDeleteLine.CommandArgument);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Ln = _db.JobCardLines.Where(x => x.LineID == Lnid);
                _db.JobCardLines.RemoveRange(Ln);
                _db.SaveChanges();
                BindGrid();
            }
        }

        protected void ShowMessage(object sender, EventArgs e, string msg)
        {
            string message = "alert('" + msg + "')";
            ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
        }

      protected void PopMessage(string retmsg)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("<script type = 'text/javascript'>");
            sb.Append("window.onload=function(){");
            sb.Append("alert('");
            sb.Append(retmsg);
            sb.Append("')};");
            sb.Append("</script>");
            ClientScript.RegisterClientScriptBlock(this.GetType(), "alert", sb.ToString());
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
                    LoadJCHeader();
                }
                else
                if (lblSender.Text == "lbtnPackMsg")
                {
                    var jc = _db.JobCardsMasters.Where(x => x.JCID == docid).FirstOrDefault();
                    jc.JCPackMessage = txtMsgBody.Text.ToString() ?? "";
                    _db.SaveChanges();
                    LoadJCHeader();
                }
                else
                if (lblSender.Text == "lbtnDelMsg")
                {
                    var jc = _db.JobCardsMasters.Where(x => x.JCID == docid).FirstOrDefault();
                    jc.JCDeliveryMessage = txtMsgBody.Text.ToString() ?? "";
                    _db.SaveChanges();
                    LoadJCHeader();
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

        protected void lbtnOKKit_Click(object sender, EventArgs e)
        {
            int kitqty = Convert.ToInt16(txtKitCount.Text);
            string kitcode = lblKitCode.Text;
            
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {    
                long Jcid = Convert.ToInt64(lblJCid.Text); 
                var Items = _db.ItemsMasters.AsQueryable();
                var itmD = Items.Where(x => x.Code == kitcode && x.CompanyID == CoID).FirstOrDefault();

                decimal useqty = (decimal)kitqty;
                JobCardLine jcl = new JobCardLine();
                jcl.JCID = Jcid;
                jcl.SelectionId = (long)itmD.ID;
                jcl.ItemCode = kitcode ?? "";
                jcl.Quantity = kitqty;
                jcl.LineType = 0;
                jcl.isKit = true;
                jcl.isKitLine = false;
                jcl.isBundle = false;
                jcl.isBundleLine = false;
                if (itmD.Description != null) jcl.ItemDescription = itmD.Description.ToString() ?? "";
                if (itmD.BarCode != null) jcl.BarCode = itmD.BarCode.ToString() ?? "";
                jcl.UnitPriceExclusive = Convert.ToDecimal(itmD.PriceExclusive);
                jcl.UnitPriceInclusive = Convert.ToDecimal(itmD.PriceInclusive);
                jcl.TaxPercentage = itmD.TaxTypeSalesPerc;
                jcl.LineTaxTypeID = itmD.TaxTypeIdSales;
                jcl.DiscountPercentage = 0;
                jcl.Exclusive = Convert.ToDecimal(itmD.PriceExclusive) * kitqty;
                jcl.Discount = 0;
                jcl.Tax = jcl.Exclusive * jcl.TaxPercentage;
                jcl.Total = jcl.Exclusive + jcl.Tax;
                jcl.Unit = (itmD.Unit ?? "").ToString();
                jcl.UnitCost = itmD.AverageCost;
                jcl.LinePickDate = Convert.ToDateTime(txtPODate.Text);
                jcl.Physical = itmD.Physical;
                jcl.CompanyID = CurrentUser.CoID;
                jcl.IsLotTracked = itmD.IsLotTracked;
                _db.JobCardLines.Add(jcl);

                useqty = 0;
                var KitLines = _db.KitLines.Where(x => x.KitCode == kitcode && x.ItemID != null && x.CompanyID == CoID).ToList();
                // create JC Lines
                foreach (var KL in KitLines)
                {
                    if (KL.ItemID > 0 && KL.FGQty >0)
                    {
                        useqty = (decimal)KL.FGQty;
                        jcl = new JobCardLine();
                        jcl.JCID = Jcid;
                        jcl.SelectionId = (long)KL.ItemID;
                        jcl.ItemCode = KL.ItemCode ?? "";
                        jcl.Quantity = (kitqty * useqty);
                        jcl.LineType = 0;
                        jcl.isKit = false;
                        jcl.isKitLine = true;
                        jcl.isBundle = false;
                        jcl.isBundleLine = false;
                        itmD = Items.Where(x => x.ID == jcl.SelectionId && x.CompanyID == CoID).FirstOrDefault();
                        if (itmD.Description != null) jcl.ItemDescription = itmD.Description.ToString() + " (Flexi-Kit " + kitcode + ")";
                        if (itmD.BarCode != null) jcl.BarCode = itmD.BarCode.ToString() ?? "";
                        jcl.IsLotTracked = itmD.IsLotTracked;
                        jcl.UnitPriceExclusive = 0;
                        jcl.UnitPriceExclusive = 0;
                        jcl.UnitPriceInclusive = 0;
                        jcl.TaxPercentage = 0;
                        jcl.LineTaxTypeID = 0;
                        jcl.DiscountPercentage = 0;
                        jcl.Exclusive = 0;
                        jcl.Discount = 0;
                        jcl.Tax = 0;
                        jcl.Total = 0;
                        jcl.Unit = (itmD.Unit ?? "").ToString();
                        jcl.UnitCost = 0;
                        jcl.LinePickDate = Convert.ToDateTime(txtPODate.Text);
                        jcl.Physical = itmD.Physical;
                        jcl.CompanyID = CurrentUser.CoID;
                        jcl.LinePickQty = 0;
                        _db.JobCardLines.Add(jcl);
                    }
                }
                _db.SaveChanges();
                //int jcNLen = x.ItemDescription.ToString().Length;
                var BlLines = _db.JobCardLines.Where(x => x.JCID == Jcid && ((int)x.ItemDescription.Length < 1 || x.ItemDescription == null)).OrderBy(x => x.LineID).ToList();
                _db.JobCardLines.RemoveRange(BlLines);
                _db.SaveChanges();
            }
            BindGrid();
        }

        protected void DDOptions_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        protected void LbtnJCSave_Click(object sender, EventArgs e)
        {
            long jcid = Convert.ToInt64(lblJCid.Text);
            foreach (GridViewRow Grv in GridJCLines.Rows)
            {
                CheckBox chkComplete = (CheckBox)Grv.FindControl("chkCompl");
                TextBox txtDescription = (TextBox)Grv.FindControl("txtDescription");
                DropDownList DDItemCode = (DropDownList)Grv.FindControl("DDItemCode");
               
                if (chkComplete.Visible == true && DDItemCode.SelectedIndex >0)
                {  
                    if (chkComplete.Checked == false)
                    {
                        ShowMessage(sender, EventArgs.Empty, txtDescription.Text + ": has not been maked as picked. Unable to continue");
                        return;
                    }
                }
            }
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // get current jobcard lines
                var JCL = _db.JobCardLines.Where(x => x.JCID == jcid).OrderBy(x => x.LineID).ToList();
                // for each jobcard line - ensure there is an item transaction for that item, moving stock between stores.
                foreach (var JCLn in JCL)
                {
                   if (JCLn.Quantity > 0 && JCLn.SelectionId != 0 && JCLn.isKit == true) 
                    { 
                    var ItemT = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.DocumentID == jcid && x.ItemID == JCLn.SelectionId && x.LotNumber == JCLn.LotNumber && x.Qty == JCLn.Quantity * -1).FirstOrDefault();
                        if (ItemT == null)
                        {
                            // add a new item transaction - primarilly used for primary Kit Items
                            ItemTransaction ItemTrans = new ItemTransaction();
                            ItemTrans.CompanyID = CurrentUser.CoID;
                            ItemTrans.DocumentID = Convert.ToInt64(lblJCid.Text);
                            ItemTrans.TransactionType = "JC-Mf";
                            ItemTrans.ItemID = Convert.ToInt64(JCLn.SelectionId);
                            ItemTrans.ItemCode = JCLn.ItemCode ?? "";
                            ItemTrans.ItemDescription = JCLn.ItemDescription ?? "";
                            ItemTrans.Unit = JCLn.Unit;
                            ItemTrans.FromID = 0;
                            ItemTrans.ToID = _db.Stores.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == JCLn.StoreCodeFrom).StoreID;
                            ItemTrans.Qty = Convert.ToDecimal(JCLn.LinePickQty);
                            //ItemTrans.Qty = Convert.ToDecimal(JCLn.Quantity);
                            ItemTrans.DocumentType = 7;
                            ItemTrans.ExchRate = 1;
                            ItemTrans.PriceExclusive = JCLn.UnitPriceExclusive;
                            ItemTrans.TotalUnitPriceExclInclAdd = JCLn.UnitPriceExclusive;

                            decimal qoh;
                            if (JCLn.LotNumber != null)
                            {
                                if (!JCLn.LotNumber.ToLower().Contains("number") && JCLn.LotNumber.ToString() != "")
                                {
                                    var ItmT = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == JCLn.SelectionId && x.ToID == ItemTrans.ToID && x.LotNumber == JCLn.LotNumber).OrderByDescending(x => x.TrnID);
                                    if (ItmT != null)
                                    {
                                        qoh = (decimal)ItmT.Sum(x => x.Qty);
                                        var lastTrn = ItmT.OrderByDescending(x => x.TrnID).FirstOrDefault();
                                        ItemTrans.PriceExclusive = lastTrn.PriceExclusive;
                                        ItemTrans.TotalUnitPriceExclInclAdd = lastTrn.TotalUnitPriceExclInclAdd;
                                    }
                                    else
                                    {
                                        qoh = (decimal)ItmT.Sum(x => x.Qty);
                                        var lastTrn = ItmT.OrderByDescending(x => x.TrnID).FirstOrDefault();
                                        ItemTrans.PriceExclusive = lastTrn.PriceExclusive;
                                        ItemTrans.TotalUnitPriceExclInclAdd = lastTrn.TotalUnitPriceExclInclAdd;
                                    }
                                }
                                else
                                {
                                    var ItmT = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == JCLn.SelectionId && x.ToID == ItemTrans.ToID).OrderByDescending(x => x.TrnID);
                                    if (ItmT != null)
                                    {
                                        qoh = (decimal)ItmT.Sum(x => x.Qty);
                                        var lastTrn = ItmT.OrderByDescending(x => x.TrnID).FirstOrDefault();
                                        ItemTrans.PriceExclusive = lastTrn.PriceExclusive;
                                        ItemTrans.TotalUnitPriceExclInclAdd = lastTrn.TotalUnitPriceExclInclAdd;
                                    }
                                    else
                                    {
                                        qoh = (decimal)ItmT.Sum(x => x.Qty);
                                        var lastTrn = ItmT.OrderByDescending(x => x.TrnID).FirstOrDefault();
                                        ItemTrans.PriceExclusive = lastTrn.PriceExclusive;
                                        ItemTrans.TotalUnitPriceExclInclAdd = lastTrn.TotalUnitPriceExclInclAdd;
                                    }
                                }

                            }
                            else
                            {
                                var ItmT = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == JCLn.SelectionId && x.ToID == ItemTrans.ToID).OrderByDescending(x => x.TrnID);
                                if (ItmT != null)
                                {
                                    qoh = (decimal)ItmT.Sum(x => x.Qty);
                                    var lastTrn = ItmT.OrderByDescending(x => x.TrnID).FirstOrDefault();
                                    ItemTrans.PriceExclusive = lastTrn.PriceExclusive;
                                    ItemTrans.TotalUnitPriceExclInclAdd = lastTrn.TotalUnitPriceExclInclAdd;
                                }
                                else
                                {
                                    qoh = (decimal)ItmT.Sum(x => x.Qty);
                                    var lastTrn = ItmT.OrderByDescending(x => x.TrnID).FirstOrDefault();
                                    ItemTrans.PriceExclusive = lastTrn.PriceExclusive;
                                    ItemTrans.TotalUnitPriceExclInclAdd = lastTrn.TotalUnitPriceExclInclAdd;
                                }
                            }
                            ItemTrans.TransactionDate = DateTime.Now;
                            ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid     
                            ItemTrans.AdditionalCosts = 0;
                            ItemTrans.TotalLineValExcl = ItemTrans.TotalUnitPriceExclInclAdd * ItemTrans.Qty;
                            ItemTrans.TransactionReference = lblDocNum.Text;
                            ItemTrans.LotNumber = JCLn.LotNumber ?? "";
                            _db.ItemTransactions.Add(ItemTrans);
                            _db.SaveChanges();
                        }
                    }
                }
               

                ///////////////////

                long Docid = Convert.ToInt64(lblDocID.Text);
                // delete add lines not in SB
                var DelLines = _db.DocLines.Where(x => x.DocID == Docid && x.SBCALineID == 0).ToList();
                _db.DocLines.RemoveRange(DelLines);
                _db.SaveChanges();
              
                 var SODocLines = _db.DocLines.Where(x => x.DocID == Docid).AsQueryable();
                var docLineIDs = SODocLines.ToList().Select(d => d.SBCALineID).ToHashSet();
                var JCLInsert = JCL.Where(j=>j.ItemDescription != null && (j.SBCALineID == null || !docLineIDs.Contains((long)j.SBCALineID))).ToList();
                var JCLUpdate = JCL.Where(j => j.SBCALineID != null && docLineIDs.Contains((long)j.SBCALineID)).ToList();
                foreach (var JCLine in JCLUpdate)
                {
                    var thisdocline = SODocLines.Where(x => x.SBCALineID == (long)JCLine.SBCALineID).FirstOrDefault();
                    thisdocline.LotNumber = (JCLine.LotNumber ?? "").ToString();
                    thisdocline.StoreCode = (JCLine.StoreCodeFrom ?? "").ToString();
                    decimal SOQty = (decimal)thisdocline.Quantity;
                    thisdocline.Quantity = JCLine.Quantity;
                    thisdocline.ReceiveQty = JCLine.LinePickQty;
                    thisdocline.QtyLeft = SOQty - JCLine.Quantity;
                    thisdocline.isKit = JCLine.isKit ?? false;
                    thisdocline.isKitLine = JCLine.isKitLine ?? false;
                    thisdocline.isBundle = JCLine.isBundle ?? false;
                    thisdocline.isBundleLine =JCLine.isBundleLine ?? false;
                }
                _db.SaveChanges();
                foreach (var JCNLine in JCLInsert)
                {
                    DocLine newdocline = new DocLine();
                    newdocline.LotNumber = (JCNLine.LotNumber ?? "").ToString();
                    newdocline.StoreCode = (JCNLine.StoreCodeFrom ?? "").ToString();
                    newdocline.Quantity = JCNLine.Quantity;
                    newdocline.ReceiveQty = JCNLine.LinePickQty;
                    newdocline.ReceiveQty = JCNLine.Quantity;
                    newdocline.ReceiveComplete = false;
                    newdocline.QtyLeft = 0;
                    newdocline.DocID = Docid;
                    newdocline.SelectionId = JCNLine.SelectionId;
                    newdocline.ItemCode = JCNLine.ItemCode;
                    newdocline.ItemDescription = JCNLine.ItemDescription;
                    newdocline.isKit = JCNLine.isKit ?? false;
                    newdocline.isKitLine = JCNLine.isKitLine ?? false;
                    newdocline.isBundle = JCNLine.isBundle ?? false;
                    newdocline.isBundleLine = JCNLine.isBundleLine ?? false;
                    newdocline.CompanyID = CurrentUser.CoID;
                    newdocline.Unit = JCNLine.Unit;
                    newdocline.LineTaxTypeID = JCNLine.LineTaxTypeID;
                    newdocline.LineType = JCNLine.LineType;

                    if (JCNLine.isKitLine != null && (bool)JCNLine.isKitLine)
                    {
                        newdocline.UnitPriceExclusive = 0;
                        newdocline.UnitPriceInclusive = 0;
                        newdocline.Total = 0;
                        newdocline.Tax = 0;
                        newdocline.Exclusive = 0;
                        newdocline.TaxPercentage = 0;
                        newdocline.DiscountPercentage = 0;
                        newdocline.Discount = 0;
                        newdocline.UnitCost = 0;
                        newdocline.ExchRate = 1;
                    }
                    else
                    {
                        newdocline.UnitPriceExclusive = JCNLine.UnitPriceExclusive;
                        newdocline.UnitPriceInclusive = JCNLine.UnitPriceInclusive;
                        newdocline.Total = JCNLine.Total;
                        newdocline.Tax = JCNLine.Tax;
                        newdocline.LineTaxTypeID = JCNLine.LineTaxTypeID;
                        newdocline.Exclusive = JCNLine.Exclusive;
                        newdocline.LineType = JCNLine.LineType;
                        newdocline.TaxPercentage = JCNLine.TaxPercentage;
                        newdocline.DiscountPercentage = JCNLine.DiscountPercentage;
                        newdocline.Discount = JCNLine.Discount;
                        newdocline.UnitCost = JCNLine.UnitCost;
                        newdocline.ExchRate = 1;
                    }
                    _db.DocLines.Add(newdocline);   
                }
                var Stat = _db.WorkStations.Where(ws => ws.CompanyID == CurrentUser.CoID).OrderByDescending(ws => ws.Seq).Select(ws => new { ws.Seq, ws.WSName, ws.WSID }).FirstOrDefault();
                var JCH = _db.JobCardsMasters.Where(x => x.CustomerID == CurrentUser.CoID && x.JCID == jcid).FirstOrDefault();
                JCH.JCWSID = Stat.WSID;
                JCH.JCStatus = Stat.WSName.ToString();
                var DocH = _db.DocHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == Docid).FirstOrDefault();
                DocH.Complete = true;
                DocH.CompBy = CurrentUser.RoleID;
                DocH.CompleteDate = DateTime.Today;
                
                decimal JCQty = 1;
                if (decimal.TryParse(txtJCQuantity.Text, out JCQty)) { }
                    var jobTransaction = new JobTransaction
                    { 
                    JCID = (int?)jcid,
                    MoveQty = JCQty,
                    RejectQty = 0,
                    MoveDate = DateTime.Now,
                    FromStationID = JCH.JCWSID,
                    ToStationID = Stat.WSID,
                    CompanyID = CurrentUser.CoID,
                    MoveBy = CurrentUser.RoleID
                    // Set other properties as needed
                };
                _db.JobTransactions.Add(jobTransaction);

                // get users linked to PS notifications
                var PSUsers = _db.RolesMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.NotifyJCMove == true).ToList();
                if (PSUsers.Count > 0)
                {
                    foreach (var usr in PSUsers)
                    {
                        var Notif = new Notification
                        {
                            Message = JCH.JCNumber + " Moved to Process " + JCH.JCStatus.ToString(),
                            IsRead = false,
                            CreatedAt = DateTime.Now,
                            CompanyID = CurrentUser.CoID,
                            UserRoleID = usr.RoleID
                        };
                        _db.Notifications.Add(Notif);
                    }
                }
                _db.SaveChanges();
                Response.Redirect("~/SalesOrder.aspx?docid=" + docguid.ToString() + "&autosave=" + CurrentUser.AutoUpdateSageSOs);
                //LbtnJCSave.Attributes.Add("style", "display:none");
                //LbtnSaveEdits.Attributes.Add("style", "display:none");
                //lbtnDelJC.Attributes.Add("style", "display:none");
                //BindGrid();
                //LoadHistory();
                //ShowMessage(sender, EventArgs.Empty,"Successfully closed off");
            }
        }

        protected void LbtnSaveEdits_Click(object sender, EventArgs e)
        {
            long Docid = Convert.ToInt64(lblDocID.Text);
            int thisJcID = Convert.ToInt32(lblJCid.Text);

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {     
                if (DDeliveryBy.SelectedIndex > 0)
                {
                    var DocH = _db.DocHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == Docid).FirstOrDefault();
                    DocH.DeliveryBy = DDeliveryBy.Text.ToString().Trim().Replace("'", "''");
                }
                var thisJC = _db.JobCardsMasters.Where(x => x.JCID == thisJcID).FirstOrDefault();
                if (thisJC != null)
                {
                    thisJC.JCSummary = (txtJobCardSummary.Text ?? "").ToString().Trim().Replace("'", "''");
                    thisJC.JCQtyOfItems = Convert.ToDecimal(txtJCQuantity.Text);
                    thisJC.JCWorkMessage = (txtWMsg.Text ?? "").ToString().Trim().Replace("'", "''");
                    thisJC.JCPackMessage = (txtPMsg.Text ?? "").ToString().Trim().Replace("'", "''");
                    thisJC.JCDeliveryMessage = (txtDMsg.Text ?? "").ToString().Trim().Replace("'", "''");
                    _db.SaveChanges();
                }
            }
            ShowMessage(sender, EventArgs.Empty, "Successfully Saved");
        }

        protected void DDStore_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            GridViewRow row = (GridViewRow)ddl.NamingContainer;
            DropDownList DDStore = (DropDownList)row.FindControl("DDStore");
            DropDownList DDItemCode = (DropDownList)row.FindControl("DDItemCode");
            DropDownList DDlotNum = (DropDownList)row.FindControl("DDlotNum");
            LinkButton lbtnLineSave = (LinkButton)row.FindControl("lbtnLineSave");
            DropDownList DDItemType = (DropDownList)row.FindControl("DDItemType");

            string StoreCode = DDStore.SelectedValue.ToString();
            long ItemID = Convert.ToInt64(DDItemCode.SelectedValue.ToString());
            long ThisLineID = Convert.ToInt64(lbtnLineSave.CommandArgument);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // check if item is lot tracked
                bool istracked = (bool)_db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == ItemID).IsLotTracked;
                if (istracked == true)
                {
                    PopulateDDLotNumbers(row, StoreCode, ItemID);
                    DDlotNum.Enabled = true;
                }
                else
                {
                    DDlotNum.Items.Clear();
                }

                // update existing line
                var NewJCLine = _db.JobCardLines.Where(x => x.LineID == ThisLineID).FirstOrDefault();
                NewJCLine.ItemCode = DDItemCode.SelectedItem.Text.Split(new string[] { "->" }, StringSplitOptions.None)[0].ToString().Trim();
                NewJCLine.SelectionId = Convert.ToInt64(DDItemCode.SelectedItem.Value.ToString());
                NewJCLine.StoreCodeFrom = DDStore.SelectedValue.ToString();
                NewJCLine.IsLotTracked = istracked;
               
                int LType = 0;
                if (Convert.ToInt32(DDItemType.SelectedValue) == 0 || Convert.ToInt32(DDItemType.SelectedValue) == 1 || Convert.ToInt32(DDItemType.SelectedValue) == 3)
                {
                    LType = 0;
                }
                else if (Convert.ToInt32(DDItemType.SelectedValue) == 2)
                {
                    LType = 1;
                }
                else if (Convert.ToInt32(DDItemType.SelectedValue) == 6)
                {
                    LType = 6;
                }
                NewJCLine.LineType = LType;

                if (DDlotNum.Items.Count >1 && !DDlotNum.SelectedItem.Value.ToLower().Contains("number"))
                {
                    NewJCLine.LotNumber = DDlotNum.SelectedValue.ToString();
                }
                _db.SaveChanges();
            }
        }

        private void PopulateDDLotNumbers(GridViewRow row, string StCode, long ItemID )
        {
            if (StCode != "0")
            { 
            DropDownList DDlotNum = (DropDownList)row.FindControl("DDlotNum");
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
                    if (lotNumList.Count > 0) DDlotNum.Items.Insert(0, "- Lot Number - ");
                }
            }
        }

        private class LotNumList
        {
           public string LotNum { get; set; }
            public string LotDisplay { get; set; }
        }

        private class ItemsList
        {
            public long ItemID { get; set; }
            public string ItemCode { get; set; }
            public string ItemDescr { get; set; }
        }
        protected void lbtnHist_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/JobTrackingHistory.aspx?jobid=" + lblJCid.Text);
        }

        private void LoadDelivBy()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var DelBy = _db.DelivMethods.Where(x => x.CompanyID == CoID && x.DelActive == true).OrderBy(x=>x.DelivMethod1).ToList();
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

        protected void lbtnPrintJC_Click(object sender, EventArgs e)
        {
            // create .pdf
            CreatePDF();
            Response.Redirect($"~/ViewPDF.aspx?doc=" + CurrentUser.UserGuiD.ToString() + "\\JC_" + lblDocNum.Text, false);
        }

        private void CreatePDF()
        {
            string filepath = string.Empty, fname = string.Empty;
            var regfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 10, BaseColor.BLACK);
            var regfontB = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 10, Font.BOLD, BaseColor.BLACK);
            var regfontS = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 8, BaseColor.BLACK);
            var medfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 11, BaseColor.BLACK);
            var headfont = FontFactory.GetFont(Server.MapPath("~/fonts/Roboto-Regular.ttf"), 18, BaseColor.BLACK);
            docid = Convert.ToInt64(lblJCid.Text);
            Guid DocGuid = Guid.Parse(docguid);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var JC = _db.JobCardsMasters.Where(x => x.CustomerID == CurrentUser.CoID && x.JCID == docid).FirstOrDefault();
                var DH = _db.DocHeaders.Where(x => x.DocGUID == DocGuid).FirstOrDefault();
                if (JC != null)
                {
                    iTextSharp.text.Document doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 40, 40);

                    try
                    {
                        if (!Directory.Exists(Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString())))
                        {
                            Directory.CreateDirectory(Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString()));
                        }
                        filepath = Server.MapPath($"~\\PDFs\\" + CurrentUser.UserGuiD.ToString() + "\\JC_" + lblDocNum.Text + ".PDF");
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

                    cell = new PdfPCell(new Phrase("Job Card #", headfont));
                    cell.Border = 0;
                    cell.HorizontalAlignment = 2;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(JC.JCNumber, headfont));
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

                    cell = new PdfPCell(new Phrase(Convert.ToDateTime(DH.DueDelDate).ToString("dd MMM yyyy"), medfont));
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

                    cellM = new PdfPCell(new Phrase("Job Card Note:- " + Environment.NewLine + (JC.JCWorkMessage ?? "").ToString(), regfont));
                    cellM.HorizontalAlignment = 0;
                    cellM.FixedHeight = 80f; ;
                    tableM.AddCell(cellM);

                    cellM = new PdfPCell(new Phrase("Packing Notes:- " + Environment.NewLine + (JC.JCPackMessage ?? "").ToString(), regfont));
                    cellM.HorizontalAlignment = 0;
                    cellM.FixedHeight = 60f; ;
                    tableM.AddCell(cellM);

                    cellM = new PdfPCell(new Phrase("Delivery Notes:- " + Environment.NewLine  + (JC.JCDeliveryMessage ?? "").ToString(), regfont));
                    cellM.HorizontalAlignment = 0;
                    cellM.FixedHeight = 60f; ;
                    tableM.AddCell(cellM);

                    doc.Add(tableM);
                    #endregion


                    #region HeaderRow
                    PdfPTable table4 = new PdfPTable(9);
                    PdfPCell cell4;
                    table4.SpacingBefore = 15f;
                    table4.SetWidths(new int[] { 50, 130, 70, 25, 30, 70, 40, 40, 50 });
                    table4.TotalWidth = doc.PageSize.Width - 80;
                    table4.LockedWidth = true;

                    cell4 = new PdfPCell(new Phrase("Code", regfont));
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

                    cell4 = new PdfPCell(new Phrase("Use Qty", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    cell4 = new PdfPCell(new Phrase("Complete", regfont));
                    cell4.HorizontalAlignment = 1;
                    cell4.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell4.BorderColor = new BaseColor(211, 211, 211);
                    table4.AddCell(cell4);

                    #endregion

                    var DocLines = _db.JobCardLines.Where(x => x.JCID == JC.JCID).OrderBy(x => x.LineID).ToList();
                    int DLCount = DocLines.Count();
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

                        cell4 = new PdfPCell(new Phrase(DL.BarCode ?? "", regfont));
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

                        string dnp = string.Empty;
                        decimal jcqty = Convert.ToDecimal(DL.Quantity);
                        if (jcqty > 0)
                        {
                            cell4 = new PdfPCell(new Phrase(Convert.ToDecimal(ApiUrlCall.NumberToDecimal(jcqty.ToString(), CurrentUser.CompanyDecPlaces)).ToString(), regfont));
                        }
                        else
                        {
                            cell4 = new PdfPCell(new Phrase("", regfont));
                        }
                        
                        if (DL.isBundle != null)
                        {
                            if ((bool)DL.isBundle)
                            {
                                Chunk chunk = new Chunk(DL.Quantity.ToString(), regfontS);
                                // Set the Chunk to have a strikethrough
                                chunk.SetUnderline(0.5f, 2f);  // The first parameter is the thickness, the second is the position
                                // Add the Chunk to a Phrase
                                Phrase phrase = new Phrase(chunk);
                                // Create a PdfPCell with the Phrase
                                cell4 = new PdfPCell(phrase);
                                dnp = "Bundle";
                            }
                        }
                        if (DL.isKit != null)
                        {
                            if ((bool)DL.isKit)
                            {
                                Chunk chunk = new Chunk(DL.Quantity.ToString(), regfontS);
                                // Set the Chunk to have a strikethrough
                                chunk.SetUnderline(0.5f, 2f);  // The first parameter is the thickness, the second is the position
                                                               // Add the Chunk to a Phrase
                                Phrase phrase = new Phrase(chunk);
                                // Create a PdfPCell with the Phrase
                                cell4 = new PdfPCell(phrase);
                                dnp = "Flexi- Kit";
                            }
                        }
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);


                        decimal jcUseqty = Convert.ToDecimal(DL.LinePickQty);
                        if (jcUseqty > 0)
                        {
                            cell4 = new PdfPCell(new Phrase(Convert.ToDecimal(ApiUrlCall.NumberToDecimal(jcUseqty.ToString(), CurrentUser.CompanyDecPlaces)).ToString(), regfont));
                        }
                        else
                        {
                            cell4 = new PdfPCell(new Phrase("", regfont));
                        }
                        cell4.HorizontalAlignment = 1;
                        cell4.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                        cell4 = new PdfPCell(new Phrase(dnp, regfont));
                        cell4.BorderColor = new BaseColor(211, 211, 211);  // RGB values for light gray
                        table4.AddCell(cell4);

                    }

                    if (DLCount < 20)
                    {
                        do
                        {
                            cell4 = new PdfPCell(new Phrase("", regfont));
                            cell4.FixedHeight = 25f;
                            cell4.BorderColor = BaseColor.LIGHT_GRAY;
                            table4.AddCell(cell4);

                            cell4 = new PdfPCell(new Phrase("", regfont));
                            cell4.BorderColor = BaseColor.LIGHT_GRAY;
                            table4.AddCell(cell4);

                            cell4 = new PdfPCell(new Phrase("", regfont));
                            cell4.BorderColor = BaseColor.LIGHT_GRAY;
                            table4.AddCell(cell4);

                            cell4 = new PdfPCell(new Phrase("", regfont));
                            cell4.BorderColor = BaseColor.LIGHT_GRAY;
                            table4.AddCell(cell4);

                            cell4 = new PdfPCell(new Phrase("", regfont));
                            cell4.BorderColor = BaseColor.LIGHT_GRAY;
                            table4.AddCell(cell4);

                            cell4 = new PdfPCell(new Phrase("", regfont));
                            cell4.BorderColor = BaseColor.LIGHT_GRAY;
                            table4.AddCell(cell4);

                            cell4 = new PdfPCell(new Phrase("", regfont));
                            cell4.BorderColor = BaseColor.LIGHT_GRAY;
                            table4.AddCell(cell4);

                            cell4 = new PdfPCell(new Phrase("", regfont));
                            cell4.BorderColor = BaseColor.LIGHT_GRAY; ;
                            table4.AddCell(cell4);

                            cell4 = new PdfPCell(new Phrase("", regfont));
                            cell4.BorderColor = BaseColor.LIGHT_GRAY;
                            table4.AddCell(cell4);
                            DLCount++;
                        } while (DLCount < 20);
                    }
                    doc.Add(table4);

                    doc.AddTitle("Job Card: ");
                    //doc.AddSubject("Classroom Review Instrument");
                    doc.AddAuthor("Data Fusion");
                    doc.Close();

                }
            }
        }

        protected void lbtnOKBundle_Click(object sender, EventArgs e)
        {
            int bundqty = Convert.ToInt16(txtBundleCount.Text);
            string bundcode = lblBundleCode.Text;

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                long Jcid = Convert.ToInt64(lblJCid.Text);

                var Bundles = _db.BundlesHeaders.AsQueryable();
                var BundD = Bundles.Where(x => x.BundCode == bundcode && x.CompanyID == CoID).FirstOrDefault();
                var Items = _db.ItemsMasters.AsQueryable();
                
                decimal useqty = (decimal)bundqty;
                JobCardLine jcl = new JobCardLine();
                jcl.JCID = Jcid;
                jcl.SelectionId = (long)BundD.SBCAID;
                jcl.ItemCode = bundcode ?? "";
                jcl.Quantity = bundqty;
                jcl.LineType = 6;
                jcl.isKit = false;
                jcl.isKitLine = false;
                jcl.isBundle = true;
                jcl.isBundleLine = false;
                if (BundD.BundDescription != null) jcl.ItemDescription = BundD.BundDescription.ToString() ?? "";
                jcl.UnitPriceExclusive = 0;
                jcl.UnitPriceExclusive = 0;
                jcl.UnitPriceInclusive = 0;
                jcl.TaxPercentage = 0;
                jcl.LineTaxTypeID = 0;
                jcl.DiscountPercentage = 0;
                jcl.Exclusive =    0;
                jcl.Discount = 0;
                jcl.Tax = jcl.Exclusive * jcl.TaxPercentage;
                jcl.Total = jcl.Exclusive + jcl.Tax;
                jcl.UnitCost = 0;
                jcl.LinePickDate = Convert.ToDateTime(txtPODate.Text);
                jcl.Physical = true;
                jcl.CompanyID = CurrentUser.CoID;
                jcl.IsLotTracked = false;
                jcl.LinePickQty = 0;
                _db.JobCardLines.Add(jcl);

                useqty = 0;
                var BundLines = _db.BundlesLines.Where(x => x.BundCode == bundcode).ToList();
                // create JC Lines
                foreach (var KL in BundLines)
                {
                    useqty = (decimal)KL.BLQuantity;
                    jcl = new JobCardLine();
                    jcl.JCID = Jcid;
                    jcl.SelectionId = (long)KL.SBCAID;
                    jcl.ItemCode = KL.BLCode ?? "";
                    jcl.Quantity = (bundqty * useqty);
                    jcl.LineType = 0;
                    jcl.isKit = false;
                    jcl.isKitLine = false;
                    jcl.isBundle = false;
                    jcl.isBundleLine = true;
                    var itmD = Items.Where(x => x.ID == jcl.SelectionId && x.CompanyID == CoID).FirstOrDefault();
                    if (itmD.Description != null) jcl.ItemDescription = itmD.Description.ToString() + " (Bundle " + bundcode + ")";
                    if (itmD.BarCode != null) jcl.BarCode = itmD.BarCode.ToString() ?? "";
                    jcl.IsLotTracked = itmD.IsLotTracked;
                    jcl.UnitPriceExclusive = Convert.ToDecimal(itmD.PriceExclusive);
                    jcl.UnitPriceInclusive = Convert.ToDecimal(itmD.PriceInclusive);
                    jcl.TaxPercentage = itmD.TaxTypeSalesPerc;
                    jcl.LineTaxTypeID = itmD.TaxTypeIdSales;
                    jcl.DiscountPercentage = 0;
                    jcl.Exclusive = Convert.ToDecimal(itmD.PriceExclusive) * jcl.Quantity;
                    jcl.Discount = 0;
                    jcl.Tax = jcl.Exclusive * jcl.TaxPercentage;
                    jcl.Total = jcl.Exclusive + jcl.Tax;
                    jcl.Unit = (itmD.Unit ?? "").ToString();
                    jcl.UnitCost = itmD.AverageCost;
                    jcl.LinePickDate = Convert.ToDateTime(txtPODate.Text);
                    jcl.Physical = itmD.Physical;
                    jcl.CompanyID = CurrentUser.CoID;
                    jcl.LinePickQty = 0;
                    _db.JobCardLines.Add(jcl);
                }
                _db.SaveChanges();
                //int jcNLen = x.ItemDescription.ToString().Length;
                var BlLines = _db.JobCardLines.Where(x => x.JCID == Jcid && ((int)x.ItemDescription.Length < 1 || x.ItemDescription == null)).OrderBy(x => x.LineID).ToList();
                _db.JobCardLines.RemoveRange(BlLines);
                _db.SaveChanges();
            }
            BindGrid();
        }

        protected void DDlotNum_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            GridViewRow row = (GridViewRow)ddl.NamingContainer;
            LinkButton lbtnLineSave = (LinkButton)row.FindControl("lbtnLineSave");
            DropDownList DDlotNum = (DropDownList)row.FindControl("DDlotNum");
            long ThisLineID = Convert.ToInt64(lbtnLineSave.CommandArgument);

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // update existing line
                var NewJCLine = _db.JobCardLines.Where(x => x.LineID == ThisLineID).FirstOrDefault();
                NewJCLine.LotNumber = DDlotNum.SelectedValue.ToString();
                _db.SaveChanges();
            }
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

        protected void chkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            ViewState["Bindgrid"] = "1";
            CheckBox chkSelectAll = (CheckBox)sender;
            Boolean ischeck = chkSelectAll.Checked;
            // Iterate through each row in the GridView
            foreach (GridViewRow row in GridJCLines.Rows)
            {
                // Find the checkbox in each row
                CheckBox chkComplete = (CheckBox)row.FindControl("chkCompl");
                DropDownList DDItemCode = (DropDownList)row.FindControl("DDItemCode");
                // Set the checkbox's checked state to match the "Select All" checkbox
                if (chkComplete.Visible == true && DDItemCode.SelectedIndex > 0)
                {
                    if (chkComplete != null)
                    {
                        chkComplete.Checked = chkSelectAll.Checked;

                        // Optionally trigger the lbtnLineSave_Click if needed
                        LinkButton lbtnLineSave = (LinkButton)row.FindControl("lbtnLineSave");
                        if (lbtnLineSave != null)
                        {
                            lbtnLineSave_Click(lbtnLineSave, e);
                        }
                    }
                }
                else
                {
                    chkComplete.Checked = false;
                }
            }
            //BindGrid();
            // Ensure the "Select All" checkbox retains its checked state
            chkSelectAll.Checked = ischeck;  // This ensures no change to chkSelectAll
        }

        protected void chkComplete_CheckedChanged(object sender, EventArgs e)
        {
            ViewState["Bindgrid"] = "1";
                // Handle individual row checkbox change
            CheckBox chkComplete = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chkComplete.NamingContainer;

            // Perform your logic for handling individual checkbox state change
            LinkButton lbtnLineSave = (LinkButton)row.FindControl("lbtnLineSave");
            if (lbtnLineSave != null)
            {
                lbtnLineSave_Click(lbtnLineSave, e);
            }
        }

        private void LoadHistory()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                int jobid = Convert.ToInt32(lblJCid.Text);
                var HistLines = _db.GetJobMovementTransactions(CoID, jobid).ToList();
                if (HistLines.Count > 0)
                {
                    GridHistLines.DataSource = HistLines;
                    GridHistLines.DataBind();
                }
            }
        }

        protected void lbtnDelJC_Click(object sender, EventArgs e)
        {
            long jcid = Convert.ToInt64(lblJCid.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var JCH = _db.JobCardsMasters.Where(x=>x.CustomerID == CurrentUser.CoID && x.JCID == jcid).FirstOrDefault();
                _db.JobCardsMasters.Remove(JCH);

                var JCL = _db.JobCardLines.Where(x => x.JCID == jcid).OrderBy(x => x.LineID).ToList();
                // item transactions to replace stock - cannot delete lines as stock will then be out of balance
                foreach (var jcln in JCL)
                {
                    if (jcln.Physical == true && jcln.PickComplete == true)
                    {
                        if (jcln.Quantity != null && jcln.Quantity > 0 && jcln.ItemCode != null && jcln.ItemCode.Length > 0)
                        {
                            // add record to item movement table to update on hand balances   
                            ItemTransaction ItemTrans = new ItemTransaction();
                            ItemTrans.CompanyID = CurrentUser.CoID;
                            ItemTrans.DocumentID = Convert.ToInt64(lblJCid.Text);
                            ItemTrans.TransactionType = "JCC";
                            ItemTrans.ItemID = Convert.ToInt64(jcln.SelectionId);
                            ItemTrans.ItemCode = jcln.ItemCode ?? "";
                            ItemTrans.ItemDescription = jcln.ItemDescription ?? "";
                            ItemTrans.Unit = jcln.Unit;
                            ItemTrans.FromID = 0;
                            ItemTrans.ToID = _db.Stores.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == jcln.StoreCodeFrom).StoreID;
                            ItemTrans.Qty = Convert.ToDecimal(jcln.Quantity) * -1;
                            ItemTrans.DocumentType = 7;
                            ItemTrans.ExchRate = 1;
                            ItemTrans.PriceExclusive = jcln.UnitPriceExclusive;
                            ItemTrans.TotalUnitPriceExclInclAdd = jcln.UnitPriceExclusive;

                            decimal qoh;
                            if (!jcln.LotNumber.ToLower().Contains("number") && jcln.LotNumber != null)
                            {
                                var ItmT = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == jcln.SelectionId && x.ToID == ItemTrans.ToID && x.LotNumber == jcln.LotNumber).OrderByDescending(x => x.TrnID);
                                qoh = (decimal)ItmT.Sum(x => x.Qty);
                                var lastTrn = ItmT.OrderByDescending(x => x.TrnID).FirstOrDefault();
                                ItemTrans.PriceExclusive = lastTrn.PriceExclusive;
                                ItemTrans.TotalUnitPriceExclInclAdd = lastTrn.TotalUnitPriceExclInclAdd;
                            }
                            else
                            {
                                var ItmT = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == jcln.SelectionId && x.ToID == ItemTrans.ToID).OrderByDescending(x => x.TrnID);
                                qoh = (decimal)ItmT.Sum(x => x.Qty);
                                var lastTrn = ItmT.OrderByDescending(x => x.TrnID).FirstOrDefault();
                                ItemTrans.PriceExclusive = lastTrn.PriceExclusive;
                                ItemTrans.TotalUnitPriceExclInclAdd = lastTrn.TotalUnitPriceExclInclAdd;
                            }
                            ItemTrans.TransactionDate = DateTime.Now;
                            ItemTrans.ByRoleID = CurrentUser.RoleID; // roleid     
                            ItemTrans.AdditionalCosts = 0;
                            ItemTrans.TotalLineValExcl = ItemTrans.TotalUnitPriceExclInclAdd * ItemTrans.Qty;
                            ItemTrans.TransactionReference = lblDocNum.Text;
                            ItemTrans.LotNumber = jcln.LotNumber;
                            _db.ItemTransactions.Add(ItemTrans);
                        }
                    }
                }
                    _db.JobCardLines.RemoveRange(JCL);

                    var JCT = _db.JobTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.JCID == jcid).ToList();
                    _db.JobTransactions.RemoveRange(JCT);

                    var DH = _db.DocHeaders.Where(x => x.LinkedJCID == jcid).FirstOrDefault();
                    DH.LinkedJCID = null;
                    DH.Complete = false;

                    long Docid = Convert.ToInt64(lblDocID.Text);
                    // delete add lines not in SB
                    var DelLines = _db.DocLines.Where(x => x.DocID == Docid && x.SBCALineID == 0).ToList();
                    _db.DocLines.RemoveRange(DelLines);
                    _db.SaveChanges();

                GridJCLines.DataSource = null;
                GridJCLines.DataBind();
                PnlButtons.Attributes.Add("style", "display:none");
                lbtnIssue.Attributes.Add("style", "display:none");
                lblDocNum.Text = string.Empty;
                txtCustName.Text = string.Empty;
                txtAddress1.Text = string.Empty;
                txtPSNum.Text = string.Empty;
                txtPODate.Text = string.Empty;
                txtAddress2.Text = string.Empty;
                txtIssuedTo.Text = string.Empty;
                txtRef.Text = string.Empty;
                txtAddress3.Text = string.Empty;
                txtIssueDate.Text = string.Empty;
                lblstatus.Text = string.Empty;
                DDeliveryBy.SelectedIndex = 0;
                ShowMessage(sender, EventArgs.Empty, "Successfully Deleted, return to the Sales Order to start again.");
                return;

            }
         }

        protected void lbtnIssue_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var JCRoles = _db.RolesMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.IsJobCards == true).ToList();
                DDJCRoles.DataSource = JCRoles;
                DDJCRoles.DataTextField = "RoleName";
                DDJCRoles.DataValueField = "RoleID";
                DDJCRoles.DataBind();

                var JCWorksS = _db.WorkStations.Where(x => x.CompanyID == CurrentUser.CoID && x.WSActive == true).ToList();
                DDept.DataSource = JCWorksS;
                DDept.DataTextField = "WSName";
                DDept.DataValueField = "WSID";
                DDept.DataBind();

                ModalPopupExtender3.Show();
            }
        }


        protected void lbtnOKIssue_Click(object sender, EventArgs e)
        {
            long JCIDn = Convert.ToInt64(lblJCid.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var thisJC = _db.JobCardsMasters.Where(x => x.CustomerID == CoID && x.JCID == JCIDn).FirstOrDefault();
                if (thisJC != null)
                {
                    int frmStatid = (int)thisJC.JCWSID;
                    thisJC.JCStatus = DDept.SelectedItem.Text;
                    thisJC.JCWSID = Convert.ToInt32(DDept.SelectedItem.Value);
                    thisJC.JCStart = true;
                    thisJC.JCStartDate = DateTime.Now;
                    thisJC.JCIssuedTo = _db.RolesMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.IsJobCards == true).OrderBy(x => x.RoleID).Select(x => x.RoleID).FirstOrDefault();
                    var JCTransaction = new JobTransaction
                    {
                        JCID = (int?)JCIDn,
                        MoveQty = thisJC.JCQtyOfItems,
                        RejectQty = 0,
                        MoveDate = DateTime.Now,
                        FromStationID = frmStatid,
                        ToStationID = thisJC.JCWSID,
                        CompanyID = CurrentUser.CoID,
                        MoveBy = CurrentUser.RoleID
                        // Set other properties as needed
                    };
                    _db.JobTransactions.Add(JCTransaction);

                    var thisdoc = _db.DocHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.LinkedJCID == JCIDn).FirstOrDefault();
                    thisdoc.Started = true;
                    _db.SaveChanges();

                    LoadJCHeader();
                    LoadHistory();
                }
            }
        }
    }
}