using Org.BouncyCastle.Utilities.Collections;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ItemEdit : BasePage
    {
        long itmid;
        long CoID;
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            MaintainScrollPositionOnPostBack = true;
            itmid = Convert.ToInt64(Request.QueryString["itm"]);
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
            if (!IsPostBack)
            {
                if (CurrentUser.CompanyUseLotNumbers == true)
                {
                    chkIsTracked.Enabled = true;
                }
                else
                {
                    chkIsTracked.Style.Add("visibility", "hidden");
                    LbLIsTracked.Visible = false;
                    lbtnBOM.Visible = false;
                    LbtnKit.Visible = false;
                }
                if (CurrentUser.UseModule2 == false)
                {
                    chkisKit.Style.Add("visibility", "hidden");
                    chkIsFromKit.Style.Add("visibility", "hidden");
                    LbtnKit.Visible = false;
                }
                if (CurrentUser.UseModule3 == false)
                {
                    chkisBom.Style.Add("visibility", "hidden");
                    chkIsFromBom.Style.Add("visibility", "hidden");
                    lbtnBOM.Visible = false;
                }

                LoadAllStores();
                LoadItems();
                LoadHistory();
            }
            else
            {
                string eventTarget = Request["__EVENTTARGET"];
                string eventArgument = Request["__EVENTARGUMENT"];

                if (!string.IsNullOrEmpty(eventTarget))
                {
                    if (eventTarget == chkIsFromKit.UniqueID && eventArgument == "UnlinkKit")
                    {
                        UnlinkKit();
                    }
                    else if (eventTarget == chkIsFromBom.UniqueID && eventArgument == "UnlinkBom")
                    {
                        UnlinkBom();
                    }
                }
            }
        }

        protected void LoadItems()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Item = _db.ItemsMasters.Where(x => x.CompanyID == CoID && x.ID == itmid).FirstOrDefault();
                lblItemCode.Text = Item.Code ?? "";
                txtDescription.Text = Item.Description ?? "";
                if ((bool)Item.IsLotTracked) chkIsTracked.Checked = true;
                if (Convert.ToDecimal(Item.ReorderLevel) < 0) txtReOrdQty.Text = (Convert.ToDecimal(Item.ReorderLevel * -1)).ToString();
                if (Item.Physical != null)
                {
                    chkisFinished.Checked = (bool)Item.IsFinishedGoods;
                    chkPhysical.Checked = (bool)Item.Physical;
                    if ((bool)Item.Physical == false)
                    {
                        chkisFinished.Checked = true;
                        chkisFinished.Enabled = false;
                        chkIsFromBom.Checked = false;
                        chkIsFromBom.Enabled = false;
                        chkIsFromKit.Checked = false;
                        chkIsFromKit.Enabled = false;
                        lbtnBOM.Visible = false;
                        LbtnKit.Visible = false;
                        txtReOrdQty.Enabled = false;
                        Accordion1.Panes.RemoveAt(3);
                        Accordion1.Panes.RemoveAt(2);
                    }
                    else
                    {
                        if (Item.IsLotTracked == true) chkIsTracked.Checked = true;
                        if (CurrentUser.UseModule2 == true)
                        {
                            if (Item.IsFromKit != null)
                            {
                                if (Item.IsFromKit == true) LbtnKit.Visible = true;
                                chkIsFromKit.Checked = (bool)Item.IsFromKit;
                            }
                            if (Item.IsKitComponent != null) chkisKit.Checked = (bool)Item.IsKitComponent;
                        }
                        else
                        {
                            chkIsFromKit.Checked = false;
                            chkIsFromKit.Enabled = false;
                            chkisKit.Checked = false;
                            chkisKit.Enabled = false;
                        }

                        if (CurrentUser.UseModule3 == true)
                        {
                            if ((bool)Item.Physical == true)
                            {
                                if (Item.IsFinishedGoods != null) chkisFinished.Checked = (bool)Item.IsFinishedGoods;

                                if (Item.IsFromBOM != null)
                                {
                                    if (Item.IsFromBOM == true) lbtnBOM.Visible = true;
                                    chkIsFromBom.Checked = (bool)Item.IsFromBOM;
                                }
                                if (Item.IsBOMComponent != null) chkisBom.Checked = (bool)Item.IsBOMComponent;
                            }
                        }
                        else
                        {
                            chkisBom.Checked = false;
                            chkisBom.Enabled = false;
                            chkIsFromBom.Checked = false;
                            chkIsFromBom.Enabled = false;
                        }
                    }
                }
                else
                {
                    chkisFinished.Style.Add("visibility", "hidden");
                    chkisBom.Style.Add("visibility", "hidden");
                    chkisKit.Style.Add("visibility", "hidden");
                    chkIsFromBom.Style.Add("visibility", "hidden");
                    chkIsFromKit.Style.Add("visibility", "hidden");
                }

                if ((bool)Item.Physical == true)
                {
                    #region QtyOnHandByStore
                    var StoreList = _db.ItemStoreLinkMasters.Where(x => x.CompanyID == CoID && x.ItemID == itmid).ToList();
                    List<QOHPerStore> QOHList = new List<QOHPerStore>();
                    decimal totqoh = 0;
                    foreach (var ItmS in StoreList)
                    {
                        QOHPerStore ThisQOH = new QOHPerStore();
                        ThisQOH.StoreID = (int)ItmS.StoreID;
                        ThisQOH.StoreCode = _db.Stores.Where(x => x.CompanyID == CoID && x.StoreID == (int)ItmS.StoreID).Select(x => x.StoreDescript).FirstOrDefault();
                        ThisQOH.QOH = _db.ItemTransactions.Where(it => it.CompanyID == CurrentUser.CoID && it.ItemID == itmid && it.ToID == (int)ItmS.StoreID).Select(it => (decimal?)it.Qty).DefaultIfEmpty(0).Sum() ?? 0;
                        totqoh += ThisQOH.QOH;
                        if (ThisQOH.QOH != 0) QOHList.Add(ThisQOH);
                    }
                    GridQOHByStore.DataSource = QOHList;
                    GridQOHByStore.DataBind();
                    if (totqoh > 0) GridQOHByStore.FooterRow.Cells[1].Text = totqoh.ToString("N2");
                    #endregion

                    #region Barcodes
                    var BCode = _db.ItemBarCodeLinks.Where(x => x.CompanyID == CoID && x.ItemID == itmid).ToList();
                    if (BCode.Count > 0)
                    {
                        txtBarcode1.Text = BCode[0].BarCode.ToString();
                        txtBQty1.Text = BCode[0].QtyPerBarcode.ToString();
                    }
                    if (BCode.Count > 1)
                    {
                        txtBarcode2.Text = BCode[1].BarCode.ToString();
                        txtBQty2.Text = BCode[1].QtyPerBarcode.ToString();
                    }
                    if (BCode.Count > 2)
                    {
                        txtBarcode3.Text = BCode[2].BarCode.ToString();
                        txtBQty3.Text = BCode[2].QtyPerBarcode.ToString();
                    }
                    #endregion
                }
                #region LinkedStores
                var LStores = _db.ItemStoreLinkMasters.Where(x => x.CompanyID == CoID && x.ItemID == itmid).ToList();
                foreach (var itemStoreLink in LStores)
                {
                    foreach (ListItem item in chkStores.Items)
                    {
                        if (item.Value == itemStoreLink.StoreID.ToString())
                        {
                            item.Selected = true;
                            break; // Exit the inner loop once the match is found
                        }
                    }
                }
                #endregion

                #region UDFs
                TxtUDF1.Text = Item.TextUserField1 ?? "";
                TxtUDF2.Text = Item.TextUserField2 ?? "";
                TxtUDF3.Text = Item.TextUserField3 ?? "";

                txtUDFN1.Text = Item.NumericUserField1.ToString() ?? "";
                txtUDFN2.Text = Item.NumericUserField2.ToString() ?? "";    
                txtUDFN3.Text = Item.NumericUserField3.ToString() ?? "";
                #endregion
            }
        }

        private class QOHPerStore
        {
            public int StoreID { get; set; }
            public string StoreCode { get; set; }
            public decimal QOH { get; set; }


        }
        protected void lbtnSaveItem_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Item = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == itmid).FirstOrDefault();
                Item.IsBOMComponent = Convert.ToBoolean(chkisBom.Checked);
                Item.IsKitComponent = Convert.ToBoolean(chkisKit.Checked);
                Item.IsFinishedGoods = Convert.ToBoolean(chkisFinished.Checked);
                Item.IsFromBOM = Convert.ToBoolean(chkIsFromBom.Checked);
                Item.IsFromKit = Convert.ToBoolean(chkIsFromKit.Checked);
                decimal MinQty = 0;
                try
                {
                    MinQty = Convert.ToDecimal(txtReOrdQty.Text) * -1;
                }
                catch { }
                Item.ReorderLevel = MinQty;
                Item.IsLotTracked = chkIsTracked.Checked;
                
                var LStores = _db.ItemStoreLinkMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == itmid).ToList();
                _db.ItemStoreLinkMasters.RemoveRange(LStores);
               
                var BCode = _db.ItemBarCodeLinks.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == itmid).ToList();
                _db.ItemBarCodeLinks.RemoveRange(BCode);
                _db.SaveChanges();

                // re add new store links
                foreach (ListItem item in chkStores.Items)
                {
                   if (item.Selected == true)
                    {      
                        ItemStoreLinkMaster StLink = new ItemStoreLinkMaster();
                        StLink.CompanyID = CurrentUser.CoID;
                        StLink.ItemID = itmid;
                        StLink.StoreID = Convert.ToInt32(item.Value);
                        StLink.Active = true;
                        _db.ItemStoreLinkMasters.Add(StLink);
                    }
                }

                // re-add barcode links
                if (txtBarcode1.Text.ToString().Trim().Length > 0 && txtBQty1.Text != "")
                {
                    ItemBarCodeLink Blink = new ItemBarCodeLink();
                    Blink.BarCode = txtBarcode1.Text.ToString().Trim();
                    Blink.CompanyID = CurrentUser.CoID;
                    Blink.ItemID = itmid;
                    Blink.QtyPerBarcode = Convert.ToInt32(txtBQty1.Text.ToString().Trim());
                    _db.ItemBarCodeLinks.Add(Blink);
                }
                if (txtBarcode2.Text.ToString().Trim().Length > 0 && txtBQty2.Text != "")
                {
                    ItemBarCodeLink Blink2 = new ItemBarCodeLink();
                    Blink2.BarCode = txtBarcode2.Text.ToString().Trim();
                    Blink2.CompanyID = CurrentUser.CoID;
                    Blink2.ItemID = itmid;
                    Blink2.QtyPerBarcode = Convert.ToInt32(txtBQty2.Text.ToString().Trim());
                    _db.ItemBarCodeLinks.Add(Blink2);
                }
                if (txtBarcode3.Text.ToString().Trim().Length > 0 && txtBQty3.Text != "")
                {
                    ItemBarCodeLink Blink3 = new ItemBarCodeLink();
                    Blink3.BarCode = txtBarcode3.Text.ToString().Trim();
                    Blink3.CompanyID = CurrentUser.CoID;
                    Blink3.ItemID = itmid;
                    Blink3.QtyPerBarcode = Convert.ToInt32(txtBQty3.Text.ToString().Trim());
                    _db.ItemBarCodeLinks.Add(Blink3);
                }
                _db.SaveChanges();
                LoadItems();
                PopMessage("Succesfully Saved");
            }
        }

        protected void LoadAllStores()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Stores = _db.Stores.Where(x => x.CompanyID == CoID && x.StoreActive == true && x.StoreCode != "CoR" && x.StoreCode != "CoD") 
                    .Select(d => new StoreList
                    {
                        StoreID = d.StoreID,
                        StoreCode = d.StoreCode,
                        StoreDescript = d.StoreCode + " - " + d.StoreDescript,
                    }).ToList();
                chkStores.DataSource = Stores;
                chkStores.DataTextField = "StoreDescript";
                chkStores.DataValueField = "StoreID";
                chkStores.DataBind();

                ddStore.DataSource = Stores;
                ddStore.DataTextField = "StoreCode";
                ddStore.DataValueField = "StoreID";
                ddStore.DataBind();
                ddStore.Items.Insert(0, "-Select Store-");
            }
        }

        private class StoreList
        {
            public int StoreID { get; set; }
            public string StoreCode { get; set; }
            public string StoreDescript { get; set; }
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

        protected void LbtnKit_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var KitM = _db.KitHeaders.Where(x => x.CompanyID == CoID && x.FGID == itmid).FirstOrDefault();
                if (KitM == null)
                {
                    KitHeader NewKH = new KitHeader();
                    NewKH.CompanyID = (int)CoID;
                    NewKH.KitCode = "NEW";
                    NewKH.FGID = itmid;
                    NewKH.FGCode = lblItemCode.Text;
                    NewKH.FGDescript = txtDescription.Text;
                    NewKH.KitActive = true;
                    _db.KitHeaders.Add(NewKH);
                    _db.SaveChanges();
                    int newbhid = NewKH.KitHID;
                    Response.Redirect("~/KitDetailed.aspx?kitid=" + newbhid, false);
                }
                else
                {
                    int newbhid = KitM.KitHID;
                    Response.Redirect("~/KitDetailed.aspx?kitid=" + newbhid, false);
                }
            }
        }

        protected void lbtnBOM_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var BOM = _db.BOMHeaders.Where(x => x.CompanyID == CoID && x.FGID == itmid).FirstOrDefault();
                if (BOM == null)
                {
                    BOMHeader NewBH = new BOMHeader();
                    NewBH.CompanyID = (int)CoID;
                    NewBH.BOMCode = "NEW";
                    NewBH.FGID = itmid;
                    NewBH.FGCode = lblItemCode.Text;
                    NewBH.FGDescript = txtDescription.Text;
                    NewBH.BomActive = true;
                    _db.BOMHeaders.Add(NewBH);
                    _db.SaveChanges();
                    int newbhid = NewBH.BomHID;
                    Response.Redirect("~/BOMDetailed.aspx?bomid=" + newbhid, false);
                }
                else
                {
                    int newbhid = BOM.BomHID;
                    Response.Redirect("~/BOMDetailed.aspx?bomid=" + newbhid, false);
                }
            }
        }
        private void UnlinkKit()
        {
            // Your server-side logic to handle unlinking the item from the kit
            // For example:
            // int itemId = ...; // Retrieve item ID
            // UnlinkItemFromKit(itemId);
        }

        private void UnlinkBom()
        {
            // Your server-side logic to handle unlinking the item from the BOM
            // For example:
            // int itemId = ...; // Retrieve item ID
            // UnlinkItemFromBom(itemId);
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
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

        private void LoadHistory()
        {
            decimal TotQty = 0, totVal = 0;
            List<TransLine> TLL = GetTransactions();
            if (TLL.Count > 0)
            {
                if (ddStore.SelectedIndex != 0)
                {
                    string storecode = ddStore.SelectedItem.Text;
                    TLL = TLL.Where(x => x.Store == storecode).ToList();
                }
                foreach (var Trn in TLL)
                {
                    TotQty += (decimal)Trn.Qty;
                    totVal += (decimal)Trn.TotalLineValExcl;
                }    
                GridItemTrans.DataSource = TLL.ToList();
                GridItemTrans.DataBind();
                if (TLL.Count > 0)
                {
                    GridItemTrans.FooterRow.Cells[0].Text = "Totals";
                    GridItemTrans.FooterRow.Cells[3].Text = TotQty.ToString("N2");
                    GridItemTrans.FooterRow.Cells[4].Text = totVal.ToString("N2");
                }
            }
        }
        public List<TransLine> GetTransactions()
        {
            decimal TotQty = 0, totVal = 0;
            long itmID = Convert.ToInt64(itmid);
            List<TransLine> TLL = new List<TransLine>();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                int recordsToTake = 100; 
                var TransList = _db.ItemTransactions.Where(x => x.CompanyID == CurrentUser.CoID && x.ItemID == itmID).OrderBy(x => x.TransactionDate).Take(recordsToTake).ToList();
                foreach (var Trn in TransList)
                {
                    TransLine TL = new TransLine();
                    TL.Code = Trn.ItemCode;
                    TL.TransactionType = Trn.TransactionType;
                    if (Trn.DocumentID.ToString() != "0" && Trn.DocumentID != null)
                    {
                        long Docid = (long)Trn.DocumentID;
                        if (Trn.TransactionType.Contains("JC"))
                        {
                            try
                            {
                                TL.Document = _db.JobCardsMasters.FirstOrDefault(x => x.CustomerID == CurrentUser.CoID && x.JCID == Trn.DocumentID).JCNumber;
                            }
                            catch { }
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
                    if (Trn.PriceExclusive != null) TL.PriceExclusive = (decimal)Trn.PriceExclusive;
                    if (Trn.AdditionalCosts != null) TL.AdditionalCosts = (decimal)Trn.AdditionalCosts;
                    if (Trn.TotalLineValExcl != null) TL.TotalLineValExcl = (decimal)Trn.TotalLineValExcl;
                    if (Trn.ToID == 0)
                    {
                        TL.Store = "-";
                    }
                    else
                    {
                        TL.Store = _db.Stores
                        .FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StoreID == Trn.ToID)
                        ?.StoreCode ?? "";
                    }
                   if (Trn.TransactionDate != null) TL.TransactionDate = (DateTime)Trn.TransactionDate;
                    try
                    {
                        if (Trn.ByRoleID != 0 && Trn.ByRoleID != null) TL.ByRole = _db.RolesMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.RoleID == Trn.ByRoleID).RoleName;
                    }
                    catch { }
                    TL.TransactionReference = Trn.TransactionReference;
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
            public string Store { get; set; }
            public DateTime TransactionDate { get; set; }
            public string ByRole { get; set; }
            public string TransactionReference { get; set; }
        }

        protected void GridItemTrans_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (CurrentUser.CompanyUseLotNumbers == false)
            {
                e.Row.Cells[2].Visible = false;
            }
        }

        protected void ddStore_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadHistory();
        }
    }
}