using AjaxControlToolkit;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class WorksOrdersCloseOff : BasePage
    {
        long CoID;
        private List<ItemsMaster> _items;
        private List<BOMHeader> _boms;
        private List<KitHeader> _kits;
        private List<GetLinkedStoredFromItem_Result> _itemST;
        private List<GetActiveLotNumbersLinkedToStores_Result> _ActiveLotNums;

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

                LoadItems();
                LoadBoms();
                LoadKits();
                LoadActiveLotNums();
                LoadItemStores();
                LoadWOHeader();
                LoadWOLines();
                if (AccordionWOLines.Panes.Count == 2)
                {
                    AccordionWOLines.SelectedIndex = 1;
                }
            }
            else
            {
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
                    woheader.InnerText = "Allocate Items: Manufacture WO-" + WOHeader.WONum;
                    if (WOHeader.CustSupName != null) lblFCCustName.Text = WOHeader.CustSupName.ToString() ?? "";
                    if (WOHeader.Reference != null) lblFCRef.Text = WOHeader.Reference.ToString() ?? "";
                    lblcreatedDate.Text = Convert.ToDateTime(WOHeader.WOrderDate, CultureInfo.InvariantCulture).ToString("dd MMM yyyy");
                    if (WOHeader.DueDate != null) txtDueDate.Text = Convert.ToDateTime(WOHeader.DueDate, CultureInfo.InvariantCulture).ToString("dd MMM yyyy");
                    if (WOHeader.LinkedDocumentNum != null) txtLinkedDoc.Text = WOHeader.LinkedDocumentNum ?? "";
                    if (WOHeader.Message != null) txtwomsg.Text = WOHeader.Message.ToString() ?? "";
                    if (WOHeader.WOrderBy != null) lblCreatedBy.Text = WOHeader.WOrderBy.ToString() ?? "";
                    DDStatus.SelectedValue=WOHeader.Status ??"";
                }
            }
        }

        protected void LoadWOLines()
        {
            LoadAccordion(woid, CoID);
        }

        protected void LoadAccordion(long woid, long CoID)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var WOLines = _db.WorksOrderLines
                                 .Where(x => x.CompanyID == CoID && x.WOID == woid && x.Quantity > 0)
                                 .OrderBy(x => x.LineID)
                                 .ToList();

                AccordionWOLines.Panes.Clear();
                
                AccordionPane paneH = new AccordionPane();
                var HLiteral = new Literal
                {
                    Text = ""                   
                };
                paneH.HeaderContainer.Controls.Add(HLiteral);
                //paneH.HeaderCssClass = "accordionFooter";
                paneH.Attributes.Add("style", "text-align:center; color:#000");
                AccordionWOLines.Panes.Add(paneH);

                foreach (var line in WOLines)
                {
                    AccordionPane pane = new AccordionPane();

                       CheckBox chkC = new CheckBox {
                        ID = $"chk_{line.LineID}",
                        Text = "Complete",
                        AutoPostBack = true,
                        Checked = line.Complete.HasValue ? line.Complete.Value : false
                    };
                    chkC.CheckedChanged += chkC_CheckedChanged;
                    chkC.Attributes.Add("style", "float:right");
                    pane.HeaderContainer.Controls.Add(chkC);

                    if (line.IsLotTracked == true)
                    {
                        LinkButton btnLotNum = new LinkButton
                        {
                            ID = "btnAddLotNum",
                            Text = " Lot Number",
                            CssClass = "icon fa-plus buttonCancelZ",
                            CommandName = "AddLotNum",  // Command to identify the action  
                            ToolTip = "Allocate Lot Number Works Order Item",
                            CommandArgument = line.LineID.ToString() + "|" + line.SelectionId.ToString()
                        };
                        btnLotNum.Click += btnLotNum_Click;
                        pane.HeaderContainer.Controls.Add(btnLotNum);
                    }

                    // Add header text to the AccordionPane's HeaderContainer
                    var headerLiteral = new Literal
                    {
                        Text = line.ItemCode + " - " + line.ItemDescription + " Qty: " + line.Quantity + "  --> Lot Number:" +  line.LotNumber 
                    };
                    pane.HeaderContainer.Controls.Add(headerLiteral);
                    HiddenField hiddenFieldH = new HiddenField
                    {
                        ID = "HiddenLineIDH",
                        Value = line.LineID.ToString()
                    };
                    pane.HeaderContainer.Controls.Add(hiddenFieldH);

                    HiddenField hiddenField = new HiddenField
                    {
                        ID = "HiddenLineID",
                        Value = line.LineID.ToString() + "|" + line.WOID
                    };
                    pane.ContentContainer.Controls.Add(hiddenField);

                    // Create GridView for the content
                    GridView gridRMs = new GridView
                    {
                        ID = $"GridRMs_{line.LineID}",
                        AutoGenerateColumns = false,
                        CssClass = "gridview",
                        ToolTip = "Select Row",
                        HeaderStyle = { CssClass = "gridViewHeader" },
                        FooterStyle = { CssClass = "gridViewHeader" },
                        RowStyle = { CssClass = "gridViewRow" },
                        AlternatingRowStyle = { CssClass = "gridViewAltRow" },
                        ShowFooter=true
                    };

                    gridRMs.RowCommand += GridRMs_RowCommand;

                    // Define the same columns as your example
                    gridRMs.Columns.Add(new BoundField { DataField = "LineID" });
                    gridRMs.Columns.Add(new BoundField { DataField = "SelectionId" });
                    gridRMs.Columns.Add(new BoundField { DataField = "ItemCode", HeaderText = "Code", ItemStyle = { Width = new Unit("5em") } });
                    gridRMs.Columns.Add(new BoundField { DataField = "ItemDescription", HeaderText = "Description" });
                    gridRMs.Columns.Add(new BoundField { DataField = "Unit", HeaderText = "Unit", ItemStyle = { Width = new Unit("3em") } });
                    gridRMs.Columns.Add(new BoundField { DataField = "Quantity", HeaderText = "Quantity", ItemStyle = { Width = new Unit("5em") } });
                    
                   TemplateField useQtyField = new TemplateField
                    {
                       HeaderText = "Use_Qty",
                       ItemTemplate = new GridViewTemplate(ListItemType.Item, "UseQuantity")            
                   };
                    useQtyField.HeaderStyle.HorizontalAlign=HorizontalAlign.Center;
                    useQtyField.ItemStyle.Width = 80;     
                    gridRMs.Columns.Add(useQtyField);

                    TemplateField rejectQtyField = new TemplateField
                    {
                        HeaderText = "Reject_Qty",
                        ItemTemplate = new GridViewTemplate(ListItemType.Item, "RejectQuantity")
                    };
                    rejectQtyField.HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
                    rejectQtyField.ItemStyle.Width = 80;
                    gridRMs.Columns.Add(rejectQtyField);

                    TemplateField scrapQtyField = new TemplateField
                    {
                        HeaderText = "Scrap_Qty",
                        ItemTemplate = new GridViewTemplate(ListItemType.Item, "ScrapQuantity")
                    };
                    scrapQtyField.HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
                    scrapQtyField.ItemStyle.Width = 80;
                    gridRMs.Columns.Add(scrapQtyField);

                    gridRMs.RowDataBound += GridRMs_RowDataBound;
                    // Add a dropdown field
                    TemplateField DDStore = new TemplateField
                    {
                        HeaderText = "Store",
                        ItemTemplate = new GridViewTemplate(ListItemType.Item, "DDStore")   
                    };
                    DDStore.ItemStyle.HorizontalAlign = HorizontalAlign.Center;
                    DDStore.ItemStyle.Width = 60;
                    gridRMs.Columns.Add(DDStore);

                    // Add a dropdown field
                    TemplateField DDlotNum = new TemplateField
                    {
                        HeaderText = "Lot Number",
                        ItemTemplate = new GridViewTemplate(ListItemType.Item, "DDlotNum"),  
                    };
                    DDlotNum.ItemStyle.HorizontalAlign = HorizontalAlign.Left;
                    DDlotNum.ItemStyle.Width = 80;
                    gridRMs.Columns.Add(DDlotNum);

                    // Add a button field
                    TemplateField btnDelRow = new TemplateField
                    {
                        HeaderText = "",
                        ItemTemplate = new GridViewTemplate(ListItemType.Item, "btnDelRow"),
                    };
                    btnDelRow.ItemStyle.HorizontalAlign = HorizontalAlign.Left;
                    btnDelRow.ItemStyle.Width = 30;
                    gridRMs.Columns.Add(btnDelRow);

                    var MLines = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LinkedWOLineID == line.LineID).ToList();
                    gridRMs.DataSource = MLines;
                    gridRMs.DataBind();

                    // Add the GridView to the ContentContainer
                    pane.ContentContainer.Controls.Add(gridRMs);
                    AccordionWOLines.Panes.Add(pane);         
                }
            }
        }

        protected void btnLotNum_Click(object sender, EventArgs e)
        {
            LinkButton lbtnAddLot = (LinkButton)sender;
            lblRowid.Text = lbtnAddLot.CommandArgument.ToString().Split('|')[0];
            long ItemID = Convert.ToInt32(lbtnAddLot.CommandArgument.ToString().Split('|')[1]);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Item = _db.ItemsMasters.Where(x => x.CompanyID == CoID && x.ID == ItemID).FirstOrDefault();
                lblAddLotItem.Text = Item.Description.ToString();
                lblItemD.Text = $"{Item.ID.ToString()}|{Item.Code.ToString()}";
                // generate default Lot Number

                int recnum = GetLotNum(CurrentUser.CoID);
                txtLotNum.Text = DateTime.Today.ToString("ddMMyyyy") + "RM" + recnum.ToString();

                ModalPopupExtender3.Show();
            }
        }
        protected void chkC_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            bool isChecked = chk.Checked;

            Control current = chk;
            AccordionPane pane = null;

            while (current != null)
            {
                if (current is AccordionPane)
                {
                    pane = (AccordionPane)current;
                    break;
                }
                current = current.Parent;
            }

            if (pane != null)
            {
                HiddenField hiddenField = pane.FindControl("HiddenLineID") as HiddenField;
                string Lineid = hiddenField.Value.Split('|')[0];
                // Find the GridView inside the ContentContainer of the pane
                GridView gridRMs = pane.ContentContainer.FindControl($"GridRMs_{Lineid}") as GridView;

                if (gridRMs != null)
                {
                    // Iterate through the rows of the GridView
                    foreach (GridViewRow row in gridRMs.Rows)
                    {
                        TextBox txtUseQty = row.FindControl("txtUseQty") as TextBox;
                        decimal useqty = 0;
                        try
                        {
                            useqty = Convert.ToDecimal(txtUseQty.Text);
                        }
                        catch
                        {
                            chk.Checked = false;
                            ShowMessage(sender, EventArgs.Empty, "Invalid Quanitity captured.");
                            return;
                        }

                        if (useqty != 0)
                        {
                            DropDownList DDStore = row.FindControl("DDStore") as DropDownList;
                            if (DDStore.SelectedIndex == 0)
                            {
                                chk.Checked = false;
                                ShowMessage(sender, EventArgs.Empty, "Please select a valid store for each item");
                                return;
                            }
                            else
                            {
                                int LnID = Convert.ToInt32(row.Cells[0].Text);
                                // check if items are lot tracked
                                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                                {
                                    var WoL = _db.WorksOrderRMLines.Where(x => x.LineID == LnID).FirstOrDefault();
                                    if (WoL != null)
                                    {
                                        if (WoL.IsLotTracked == true)
                                        {
                                            DropDownList DDlotNum = row.FindControl("DDlotNum") as DropDownList;
                                            if (DDlotNum.SelectedIndex == 0)
                                            {
                                                chk.Checked = false;
                                                ShowMessage(sender, EventArgs.Empty, "Please select a valid Lot Number for each Lot Tracked item.");
                                                return;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Response.Write("GridView not found in the current AccordionPane.<br/>");
            }
        }

        protected void lbtnWO_Click(object sender, EventArgs e)
        {

        }

        protected void lbtnDeleteFC_Click(object sender, EventArgs e)
        {

        }

        protected void LbtnSaveWO_Click(object sender, EventArgs e)
        {
            try
            {
                DateTime DtD = Convert.ToDateTime(txtDueDate.Text);
            }
            catch
            {
                ShowMessage(sender, EventArgs.Empty, "Invalid Due Date, Unable to Save");
                return;
            }
                 
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                foreach (AccordionPane pane in AccordionWOLines.Panes)
                {
                    foreach (Control controlH in pane.HeaderContainer.Controls)
                    {
                        if (controlH is CheckBox chkC)
                        {
                            HiddenField HiddenLineIDH = controlH.FindControl("HiddenLineIDH") as HiddenField;
                            int lid = Convert.ToInt32(HiddenLineIDH.Value);
                            var WOL = _db.WorksOrderLines.Where(x => x.LineID == lid).FirstOrDefault();
                            if (chkC.Checked == true)
                            {
                                WOL.Complete = true;
                                WOL.CompleteBy = CurrentUser.RoleID;
                                WOL.CompleteDate = DateTime.Today;
                            }
                            else
                            {
                                WOL.Complete = false;
                                WOL.CompleteBy = null;
                                WOL.CompleteDate = null;
                            }
                         _db.SaveChanges();       
                        }

                        foreach (Control control in pane.ContentContainer.Controls)
                        {
                            if (control is GridView grid)
                            {
                                foreach (GridViewRow row in grid.Rows)
                                {
                                    int TLineID = Convert.ToInt32(row.Cells[0].Text);
                                    TextBox txtUseQty = row.FindControl("txtUseQty") as TextBox;
                                    TextBox txtRejectQty = row.FindControl("txtRejectQty") as TextBox;
                                    TextBox txtScrapQty = row.FindControl("txtScrapQty") as TextBox;
                                    DropDownList DDStore = row.FindControl("DDStore") as DropDownList;
                                    DropDownList DDlotNum = row.FindControl("DDlotNum") as DropDownList;

                                    var WRMLine = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LineID == TLineID).FirstOrDefault();
                                    if (WRMLine.IsLotTracked == true)
                                    {
                                        if (DDlotNum.SelectedIndex > 0)
                                        {
                                            WRMLine.LotNumber = DDlotNum.SelectedValue.ToString();
                                        } else
                                        {
                                            WRMLine.LotNumber = null;
                                        }
                                    }

                                    if (DDStore.SelectedIndex > 0)
                                    {
                                        WRMLine.StoreCodeFrom = DDStore.SelectedValue.ToString();
                                    }
                                    else
                                    {
                                        WRMLine.StoreCodeFrom = null;
                                    }

                                    decimal UseQty = 0;
                                    try { UseQty = Convert.ToDecimal(txtUseQty.Text); } catch { }
                                    WRMLine.UseQty = UseQty;

                                    decimal RejQty = 0;
                                    try { RejQty = Convert.ToDecimal(txtRejectQty.Text); } catch { }
                                    WRMLine.RejectQty = RejQty;

                                    decimal ScrQty = 0;
                                    try { ScrQty = Convert.ToDecimal(txtScrapQty.Text); } catch { }
                                    WRMLine.ScrapQty = ScrQty;

                                    _db.SaveChanges();

                                }
                            }
                        }
                    }
                }
                var FCHeader = _db.WorksOrderHeaders.Where(x => x.CompanyID == CoID && x.ID == woid).FirstOrDefault();
                if (FCHeader != null)
                {
                    FCHeader.Reference = lblFCRef.Text.ToString().Trim();
                    FCHeader.CustSupName = lblFCCustName.Text.ToString();
                    FCHeader.WOrderBy = lblCreatedBy.Text.ToString();
                    FCHeader.LinkedDocumentNum = txtLinkedDoc.Text.ToString();
                    FCHeader.Message = txtwomsg.Text.ToString().Trim().Replace("'", "''");
                    FCHeader.DueDate = Convert.ToDateTime(txtDueDate.Text);
                    FCHeader.Status = DDStatus.Text;
                    _db.SaveChanges();
                    ShowMessage(sender, EventArgs.Empty, "Successfully Saved");
                }
            }
        }
         protected void GridWOLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var item = (WorksOrderLine)e.Row.DataItem;
                var ddlItemCode = (DropDownList)e.Row.FindControl("DDItemCode");
                var ddlLineType = (DropDownList)e.Row.FindControl("DDBOMKIT");

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
                        ddlItemCode.DataSource = _items;
                        ddlItemCode.DataTextField = "Code";
                        ddlItemCode.DataValueField = "ID";
                        ddlItemCode.DataBind();
                        ddlItemCode.Items.Insert(0, new ListItem("Select", "0"));
                        ddlItemCode.SelectedValue = item.SelectionId.ToString();
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
                        ddlItemCode.DataSource = _boms;
                        ddlItemCode.DataTextField = "FGCode";
                        ddlItemCode.DataValueField = "FGID";
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
                        ddlItemCode.DataSource = _kits;
                        ddlItemCode.DataTextField = "FGCode";
                        ddlItemCode.DataValueField = "FGID";
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
            LinkButton lbtn = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtn.NamingContainer;
            DropDownList ddlt = (DropDownList)row.FindControl("DDBOMKIT");
            TextBox txtQty = (TextBox)row.FindControl("txtQty");
            DropDownList ddl = (DropDownList)row.FindControl("DDItemCode");
            TextBox txtDescription = (TextBox)row.FindControl("txtDescription");

            DateTime dt;
            try {dt = Convert.ToDateTime(txtDueDate.Text.ToString(), CultureInfo.InvariantCulture);}
            catch {ShowMessage(sender, EventArgs.Empty, "Invalid Due Date, Unable to continue."); return; }

            if (ddl.SelectedIndex ==0)
            {
                ShowMessage(sender, EventArgs.Empty, "Invalid Item Selected, Unable to continue."); return;
            }

            decimal qty;
            try
            {
                qty = Convert.ToDecimal(txtQty.Text);
            }
            catch { ShowMessage(sender, EventArgs.Empty, "Invalid Quantity captured. Unable to continue."); return; }

            long rowid = Convert.ToInt32(lbtn.CommandArgument);
            long ItemID = Convert.ToInt32(ddl.SelectedValue);

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var ThisFCLine = _db.WorksOrderLines.Where(x => x.CompanyID == CoID && x.LineID == rowid).FirstOrDefault();
                ThisFCLine.SelectionId = ItemID;
                ThisFCLine.LineType = Convert.ToInt16(ddlt.SelectedValue);
                ThisFCLine.CompanyID = CoID;
                ThisFCLine.Quantity = Convert.ToDecimal(txtQty.Text);
                ThisFCLine.ItemCode = ddl.SelectedItem.Text.ToString();
                ThisFCLine.ItemDescription = txtDescription.Text.ToString().Trim();
                ThisFCLine.DueDelDate = dt;
                ThisFCLine.Active = true;

                // For each line - save BOM/Kit/Item quantities to WorksOrderRMLines Table
                // delete old rows for this WOID

                
                var Rmd = _db.WorksOrderRMLines.Where(x =>x.CompanyID == CoID && x.LinkedWOLineID == rowid).ToList();
                _db.WorksOrderRMLines.RemoveRange(Rmd);
                ///// check for line type and get Item/BOM/Kits items and create new lines to match
               
                if (ThisFCLine.LineType == 1)
                {
                    WorksOrderRMLine RML = new WorksOrderRMLine();
                    RML.LinkedWOLineID = (int)rowid;
                    RML.WOID = woid;
                    RML.SelectionId = ThisFCLine.SelectionId;
                    RML.ItemCode = ThisFCLine.ItemCode;
                    RML.ItemDescription = ThisFCLine.ItemDescription;
                    RML.Quantity = Convert.ToDecimal(ThisFCLine.Quantity);
                    RML.CompanyID = CoID;
                    RML.LinkedFGSelectionID = ThisFCLine.SelectionId;
                    RML.LinkedFGCode = ThisFCLine.ItemCode;
                    RML.LinkedFGQty = ThisFCLine.Quantity;
                    RML.IsLotTracked = false;
                    RML.PickComplete = false;
                    _db.WorksOrderRMLines.Add(RML);
                }
                if (ThisFCLine.LineType == 2)
                {
                    int bmc = _db.BOMHeaders.Where(x=>x.CompanyID == CoID && x.FGCode == ThisFCLine.ItemCode).Select(x=>x.BomHID).FirstOrDefault();
                    var BomLines = _db.GetBOMLinesFromBomHeaderID(bmc, CoID);

                    foreach (var bl in BomLines)
                    {
                        WorksOrderRMLine RML = new WorksOrderRMLine();
                        RML.LinkedWOLineID = (int)rowid;
                        RML.WOID = woid;
                        RML.SelectionId = (long) bl.ItemID;
                        RML.ItemCode = bl.ItemCode;
                        RML.ItemDescription = bl.Description;
                        RML.Quantity = Convert.ToDecimal(ThisFCLine.Quantity) * Convert.ToDecimal(bl.RMQty);
                        RML.CompanyID = CoID;
                        RML.LinkedFGSelectionID = ThisFCLine.SelectionId;
                        RML.LinkedFGCode = ThisFCLine.ItemCode;
                        RML.LinkedFGQty = ThisFCLine.Quantity;
                        RML.IsLotTracked = false;
                        RML.PickComplete = false;
                        _db.WorksOrderRMLines.Add(RML);
                    }
                }
                if (ThisFCLine.LineType == 3)
                {
                    string kmc = _db.KitHeaders.Where(x => x.CompanyID == CoID && x.FGCode == ThisFCLine.ItemCode).Select(x => x.KitCode).FirstOrDefault();
                    var KitLines = _db.GetKitLinesFromKitCode(kmc, CoID);

                    foreach (var bl in KitLines)
                    {
                        WorksOrderRMLine RML = new WorksOrderRMLine();
                        RML.LinkedWOLineID = (int)rowid;
                        RML.WOID = woid;
                        RML.SelectionId = (long)bl.ItemID;
                        RML.ItemCode = bl.ItemCode;
                        RML.ItemDescription = bl.Description;
                        RML.Quantity = Convert.ToDecimal(ThisFCLine.Quantity) * Convert.ToDecimal(bl.FGQty);
                        RML.CompanyID = CoID;
                        RML.LinkedFGSelectionID = ThisFCLine.SelectionId;
                        RML.LinkedFGCode = ThisFCLine.ItemCode;
                        RML.LinkedFGQty = ThisFCLine.Quantity;
                        RML.IsLotTracked = false;
                        RML.PickComplete = false;
                        _db.WorksOrderRMLines.Add(RML);
                    }
                }

                _db.SaveChanges();
                LoadWOLines();
                ShowMessage(sender, EventArgs.Empty, "Successfully Saved.");
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
                ShowMessage(sender, EventArgs.Empty, "Line successfully Deleted");
            }
        }

        protected void ShowMessage(object sender, EventArgs e, string msg)
        {
            string message = "alert('" + msg + "')";
            ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
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
                ShowMessage(sender, EventArgs.Empty, "Invalid manufacturing quantity, Unable to calculate.");
                return;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var MLines = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LinkedWOLineID == lineid).ToList();
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

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
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
                        ddlItemCode.DataSource = _items;
                        ddlItemCode.DataTextField = "Code";
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
                        ddlItemCode.DataSource = _boms;
                        ddlItemCode.DataTextField = "FGCode";
                        ddlItemCode.DataValueField = "FGID";
                        ddlItemCode.DataBind();
                        ddlItemCode.Items.Insert(0, new ListItem("Select", "0"));
                    }
                }
                else if (ddl.SelectedValue == "3")
                {
                    if (ddlItemCode != null)
                    {
                        _kits = _db.KitHeaders.Where(i => i.KitActive == true && i.CompanyID == CoID).OrderBy(x => x.KitCode).ToList();
                        ddlItemCode.DataSource = _kits;
                        ddlItemCode.DataTextField = "FGCode";
                        ddlItemCode.DataValueField = "FGID";
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
                ShowMessage(sender, EventArgs.Empty, "Invalid Quantity, Unable to continue");
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
                GridUseBom.DataSource = MLines;
                GridUseBom.DataBind();
                lblItem.Visible = false;
                lblQty.Visible = false;
                ModalPopupExtender1.Show();
            }
        }

        protected void lbtnWOPrint_Click(object sender, EventArgs e)
        {
            Response.Redirect($"~/WorksOrderPDFCreate.aspx?woid={woid}", true);
        }

        protected void GridRMs_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
            e.Row.Cells[1].Visible = false;

           if (e.Row.RowType == DataControlRowType.DataRow)
            { 
                // Get the data item
                var item = (WorksOrderRMLine)e.Row.DataItem;
                long itemID = Convert.ToInt64(e.Row.Cells[1].Text.ToString());
                // Find and bind DDStore
                DropDownList ddlStore = e.Row.FindControl("DDStore") as DropDownList;
                if (ddlStore != null)
                {
                    LoadItemStores();
                    var storeList = _itemST.Where(x => x.ItemID == itemID).ToList();
                    ddlStore.DataSource = storeList;
                    ddlStore.DataTextField = "StoreCode";
                    ddlStore.DataBind();
                    ddlStore.Items.Insert(0, "-?-");
                    ddlStore.SelectedValue = item.StoreCodeFrom;
                    ddlStore.SelectedIndexChanged += DDStore_SelectedIndexChanged;
                }

                // Find and bind DDlotNum
                DropDownList ddlLotNum = e.Row.FindControl("DDlotNum") as DropDownList;
                if (ddlLotNum != null && item.IsLotTracked)
                {
                    LoadActiveLotNums();
                    var lotNums = _ActiveLotNums
                        .Where(x => x.StoreCode == ddlStore.SelectedValue && x.ItemId == itemID)
                        .Select(x => new
                        {
                            LotNum = x.LotNumber,
                            LotDisplay = $"{x.LotNumber} ({x.QtyHandToStore:N2})"
                        })
                        .ToList();

                    ddlLotNum.DataSource = lotNums;
                    ddlLotNum.DataTextField = "LotDisplay";
                    ddlLotNum.DataValueField = "LotNum";
                    ddlLotNum.DataBind();
                    ddlLotNum.Items.Insert(0, "- Lot Number -");
                    try
                    {
                        ddlLotNum.SelectedValue = item.LotNumber;
                    }
                    catch { }
                    ddlLotNum.SelectedIndexChanged += DDlotNum_SelectedIndexChanged;
                }
                else
                {
                    ddlLotNum.Items.Clear();
                    ddlLotNum.Attributes.Add("style", "display:none");
                }
                
                TextBox txtUseQty = e.Row.FindControl("txtUseQty") as TextBox;
                txtUseQty.Text = item.UseQty.ToString();
                TextBox txtRejectQty = e.Row.FindControl("txtRejectQty") as TextBox;
                txtRejectQty.Text = item.RejectQty.ToString();
                TextBox txtScrapQty = e.Row.FindControl("txtScrapQty") as TextBox;
                txtScrapQty.Text = item.ScrapQty.ToString();
  
                LinkButton lbtnDelRow = e.Row.FindControl("btnDelRow") as LinkButton;
                ConfirmButtonExtender confirmExtender = new ConfirmButtonExtender
                {
                    ID = $"ConfirmExtender_{e.Row.RowIndex}", // Ensure a unique ID
                    TargetControlID = lbtnDelRow.ID, // Link it to the LinkButton
                    ConfirmText = "Are you sure you want to delete this row?", // Confirmation message
                    Enabled = true // Enable the extender
                };

                // Add the ConfirmButtonExtender to the same container as the LinkButton
                e.Row.Cells[11].Controls.Add(confirmExtender);
                lbtnDelRow.Click += lbtnDelRow_click;
            }
            else if (e.Row.RowType == DataControlRowType.Footer)
                {
                    // Add Save Button to the footer row
                    LinkButton btnSave = new LinkButton
                    {
                        ID = "btnSaveFooter",
                        Text = " Add additional line",
                        CssClass = "icon fa-save buttonRed",
                        CommandName = "SaveFooter",  // Command to identify the action  
                        ToolTip = "Add line to works order details"
                    };
                    e.Row.Cells[3].Controls.Add(btnSave);
             }
         }

        protected void GridRMs_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "SaveFooter")
            {
                lblDescript.Text = string.Empty;
                txtAddQty.Text = null;
                GridView grid = (GridView)sender;
                AccordionPane pane = FindParentAccordionPane(grid);
                HiddenField Hf = pane.FindControl("HiddenLineID") as HiddenField;
                woLineID.Text = Hf.Value.Split('|')[0].ToString();
                WordID.Text = Hf.Value.Split('|')[1].ToString();
                LoadItems();
                ddlItemCode.DataSource = _items;
                ddlItemCode.DataTextField = "Code";
                ddlItemCode.DataValueField = "Code";
                ddlItemCode.DataBind();
                ddlItemCode.Items.Insert(0, "-Add Item-");
                ModalPopupExtender2.Show();
            }
        }

        // Utility method to find the AccordionPane in the hierarchy
        private AccordionPane FindParentAccordionPane(Control control)
        {
            while (control != null)
            {
                if (control is AccordionPane pane)
                {
                    return pane;
                }
                control = control.Parent;
            }
            return null;
        }
        private void SaveNewRowToDatabase(string itemCode, decimal reqty)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                LoadItems();
                var itm = _items.Where(x => x.Code == itemCode).FirstOrDefault();
                int woLineIDF = Convert.ToInt32(woLineID.Text);
                // get one line from existing WorksOrderLines, to use some of the data
                var ExistL = _db.WorksOrderLines.Where(x => x.CompanyID == CoID && x.LineID == woLineIDF).FirstOrDefault();
                ExistL.Complete = false;
                ExistL.CompleteBy = null;
                ExistL.CompleteDate = null;

                var newRow = new WorksOrderRMLine
                {
                    SelectionId = itm.ID,
                    ItemCode = itemCode,
                    ItemDescription = itm.Description,
                    Quantity = reqty,
                    WOID = Convert.ToInt64(WordID.Text),
                    Unit = itm.Unit,
                    IsLotTracked = (bool)itm.IsLotTracked,
                    PickComplete = false,
                    CompanyID = CoID,
                    LinkedFGSelectionID = ExistL.SelectionId,
                    LinkedFGCode = ExistL.ItemCode,
                    LinkedFGQty = ExistL.Quantity,
                    LinkedWOLineID = woLineIDF,
                    UseQty = 0,
                    ScrapQty = 0,
                    RejectQty = 0
                };
                // Save to the database
                _db.WorksOrderRMLines.Add(newRow);
                _db.SaveChanges();
                LoadWOLines();
            }
        }

        private class LotNumList
        {
            public string LotNum { get; set; }
            public string LotDisplay { get; set; }
        }

        protected void LoadItems()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (_items == null) _items = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CoID && i.Physical == true).OrderBy(x => x.Code).ToList();
            }
        }
        protected void LoadBoms()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
               if (_boms == null) _boms = _db.BOMHeaders.Where(i => i.BomActive == true && i.CompanyID == CoID).OrderBy(x => x.BOMCode).ToList();
            }
        }
        protected void LoadKits()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (_kits == null) _kits = _db.KitHeaders.Where(i => i.KitActive == true && i.CompanyID == CoID).OrderBy(x => x.KitCode).ToList();
            }
        }
        protected void LoadItemStores()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (_itemST == null) _itemST = _db.GetLinkedStoredFromItem(CurrentUser.CoID).OrderBy(x => x.StoreCode).ToList();
            }
        }
        protected void LoadActiveLotNums()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (_ActiveLotNums == null) _ActiveLotNums = _db.GetActiveLotNumbersLinkedToStores(CurrentUser.CoID).OrderBy(x => x.LotNumber).ToList();
            }
        }

        protected void DDStore_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            GridViewRow row = (GridViewRow)ddl.NamingContainer;
            DropDownList DDStore = (DropDownList)row.FindControl("DDStore");
            
            string StoreCode = DDStore.SelectedValue.ToString();
            long LineID = Convert.ToInt64(row.Cells[0].Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var WOLine = _db.WorksOrderRMLines.Where(x => x.LineID == LineID).FirstOrDefault();
                WOLine.StoreCodeFrom = DDStore.SelectedItem.Text;
                _db.SaveChanges();

                if (DDStore.SelectedIndex > 0)
                {
                    long ItemID = Convert.ToInt64(row.Cells[1].Text.ToString());
                    PopulateDDLotNumbers(row, StoreCode, ItemID);
                }
                else
                {
                    DropDownList DDlotNum = (DropDownList)row.FindControl("DDlotNum");
                    DDlotNum.Items.Clear();
                    WOLine.LotNumber = "";
                    _db.SaveChanges();
                }
            }

        }
        private void PopulateDDLotNumbers(GridViewRow row, string StCode, long ItemID)
        {
            DropDownList DDlotNum = (DropDownList)row.FindControl("DDlotNum");
            DDlotNum.Items.Clear();
            // Example LINQ query based on itemType
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                LoadActiveLotNums();
                var LotNums = _ActiveLotNums.Where(x => x.ItemId == ItemID && x.StoreCode == StCode).ToList();
                if (LotNums.Count > 0)
                {
                    var lotNumList = new List<LotNumList>();
                    foreach (var lot in LotNums)
                    {
                        lotNumList.Add(new LotNumList
                        {
                            LotNum = lot.LotNumber,
                            LotDisplay = lot.LotNumber + " (" + lot.QtyHandToStore.ToString("N2") + ")"
                        });
                    }
                    try
                    {
                        DDlotNum.DataSource = lotNumList;
                        DDlotNum.DataValueField = "LotNum";
                        DDlotNum.DataTextField = "LotDisplay";
                        DDlotNum.DataBind();
                    }
                    catch { }
                    if (lotNumList.Count > 1)
                    {
                        DDlotNum.Items.Insert(0, "- Lot Number - ");
                    }
                    else
                    {
                        string LotNum = DDlotNum.SelectedValue.ToString();
                        long LineID = Convert.ToInt64(row.Cells[0].Text);
                        var WOLine = _db.WorksOrderRMLines.Where(x => x.LineID == LineID).FirstOrDefault();
                        WOLine.LotNumber = LotNum;
                        _db.SaveChanges();
                    }
                }
                else
                {
                    DDlotNum.Items.Clear();
                    long LineID = Convert.ToInt64(row.Cells[0].Text);
                    var WOLine = _db.WorksOrderRMLines.Where(x => x.LineID == LineID).FirstOrDefault();
                    WOLine.LotNumber = "";
                    _db.SaveChanges();
                }
            }
        }

        protected void DDlotNum_SelectedIndexChanged(object sender, EventArgs e)
        {             
            DropDownList ddl = (DropDownList)sender;
            GridViewRow row = (GridViewRow)ddl.NamingContainer;
            DropDownList DDlotNum = (DropDownList)row.FindControl("DDlotNum");
            DropDownList DDStore = (DropDownList)row.FindControl("DDStore");
            TextBox txtuseQty = (TextBox)row.FindControl("txtUseQty");
            TextBox txtRejectQty = (TextBox)row.FindControl("txtRejectQty");
            TextBox txtScrapQty = (TextBox)row.FindControl("txtScrapQty");
            
            if (DDlotNum.SelectedIndex > 0)
            {
                long ItemID = Convert.ToInt64(row.Cells[1].Text);
                decimal ItemQty = Convert.ToDecimal(row.Cells[5].Text);
                LoadActiveLotNums();
                var LotNums = _ActiveLotNums.Where(x => x.StoreCode == DDStore.SelectedItem.ToString() && x.ItemId == ItemID);
                if (LotNums.Sum(x=>x.QtyHandToStore) < ItemQty)
                {
                    ShowMessage(sender, EventArgs.Empty, "Insufficient quantity available.");
                    return;
                }
                else
                {
                    string LotNum = DDlotNum.SelectedValue.ToString();
                    long LineID = Convert.ToInt64(row.Cells[0].Text);
                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var WOLine = _db.WorksOrderRMLines.Where(x => x.LineID == LineID).FirstOrDefault();
                        WOLine.UseQty = Convert.ToDecimal(txtuseQty.Text);
                        WOLine.RejectQty = Convert.ToDecimal(txtRejectQty.Text);
                        WOLine.ScrapQty = Convert.ToDecimal(txtScrapQty.Text);
                        WOLine.LotNumber = LotNum;
                        _db.SaveChanges();
                    }
                }
            }
            else
            {
                long LineID = Convert.ToInt64(row.Cells[0].Text);
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var WOLine = _db.WorksOrderRMLines.Where(x => x.LineID == LineID).FirstOrDefault();
                    WOLine.UseQty = Convert.ToDecimal(txtuseQty.Text);
                    WOLine.RejectQty = Convert.ToDecimal(txtRejectQty.Text);
                    WOLine.ScrapQty = Convert.ToDecimal(txtScrapQty.Text);
                    WOLine.LotNumber = "";
                    _db.SaveChanges();
                }
            }
        }

        public class GridViewTemplate : ITemplate
        {
            private readonly ListItemType itemType;
            private readonly string columnName;

            public GridViewTemplate(ListItemType itemType, string columnName)
            {
                this.itemType = itemType;
                this.columnName = columnName;
            }

            public void InstantiateIn(Control container)
            {
                Debug.WriteLine($"InstantiateIn called for {columnName}, itemType: {itemType}");
                if (itemType == ListItemType.Item || itemType == ListItemType.AlternatingItem)
                {
                    if (columnName == "DDStore")
                    {
                        DropDownList ddlStore = new DropDownList
                        {
                            ID = "DDStore",
                            AutoPostBack = true // AutoPostBack can be set here
                        };
                        container.Controls.Add(ddlStore);
                    }
                    else if (columnName == "DDlotNum")
                    {
                        DropDownList ddlLotNum = new DropDownList
                        {
                            ID = "DDlotNum",
                            AutoPostBack = true, // AutoPostBack can be set here
                             Width = new Unit("125")
                        };
                        container.Controls.Add(ddlLotNum);
                    }
                    else if (columnName == "UseQuantity")
                    {
                        TextBox txtUseQty = new TextBox
                        {
                            ID = "txtUseQty",
                            Width = new Unit("5em")
                        };
                        txtUseQty.Attributes.Add("onkeypress", "return validateQuantityInput(event, this)");
                        txtUseQty.Attributes.Add("style", "text-align:center");
                        container.Controls.Add(txtUseQty);
                    }
                    else if (columnName == "RejectQuantity")
                    {
                        TextBox txtRejectQty = new TextBox
                        {
                            ID = "txtRejectQty",
                            Width = new Unit("5em")
                        };
                        txtRejectQty.Attributes.Add("onkeypress", "return validateQuantityInput(event, this)");
                        txtRejectQty.Attributes.Add("style", "text-align:center");
                        container.Controls.Add(txtRejectQty);
                    }
                    else if (columnName == "ScrapQuantity")

                    {
                        TextBox txtScrapQty = new TextBox
                        {
                            ID = "txtScrapQty",
                            Width = new Unit("5em")
                        };
                        txtScrapQty.Attributes.Add("onkeypress", "return validateQuantityInput(event, this)");
                        txtScrapQty.Attributes.Add("style", "text-align:center");
                        container.Controls.Add(txtScrapQty);
                    }
              else if (columnName == "btnDelRow")
                    {
                        LinkButton btnDelRow = new LinkButton
                        {
                            ID = "btnDelRow",
                            Width = new Unit("2em")
                        };
                        btnDelRow.CssClass = "fa fa-ban";
                        btnDelRow.Attributes.Add("style", "color:red");
                        btnDelRow.ToolTip = "Delete Row";
                        container.Controls.Add(btnDelRow);
                    }
                }
            }
        }

        protected void LbtnUpdateWO_Click(object sender, EventArgs e)
        {

        }

        protected void DDFCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            // Find the GridViewRow containing the DropDownList
            GridViewRow footerRow = (GridViewRow)ddl.NamingContainer;
            string selectedItemCode = ddl.SelectedValue;
            LoadItems();
            footerRow.Cells[3].Text = _items.FirstOrDefault(x=>x.Code == selectedItemCode).Description;
            footerRow.Cells[3].BackColor = System.Drawing.Color.White;
            footerRow.Cells[4].BackColor = System.Drawing.Color.White;
            footerRow.Cells[5].BackColor = System.Drawing.Color.White;
            footerRow.Cells[6].BackColor = System.Drawing.Color.White;
            footerRow.Cells[2].BackColor = System.Drawing.Color.White;
            TextBox txtQty = footerRow.FindControl("txtQty") as TextBox;
            txtQty.Text = "1";
            Page.SetFocus(txtQty);
        }

        protected void lbtnDelRow_click(object sender, EventArgs e)
        {
            LinkButton lbtn = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtn.NamingContainer;
            long lineID = Convert.ToInt64(row.Cells[0].Text);
           using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var WOLine = _db.WorksOrderRMLines.Where(x => x.LineID == lineID).FirstOrDefault();
                _db.WorksOrderRMLines.Remove(WOLine);
                int woLid = (int)WOLine.LinkedWOLineID;

                var ExistL = _db.WorksOrderLines.Where(x => x.CompanyID == CoID && x.LineID == woLid).FirstOrDefault();
                ExistL.Complete = false;
                ExistL.CompleteBy = null;
                ExistL.CompleteDate = null;

                _db.SaveChanges();
                LoadWOLines();
                ShowMessage(sender, EventArgs.Empty, "Successfully Deleted");
            }
        }

        protected void lbtnAddYesM_Click(object sender, EventArgs e)
        {
            decimal itemQty = 0;
            try
            {
                itemQty = Convert.ToDecimal(txtAddQty.Text);
            }
            catch { ShowMessage(sender, EventArgs.Empty, "Invalid Quantity!"); }
            long woLid = Convert.ToInt64(woLineID.Text);
            if (itemQty != 0)
            {
                SaveNewRowToDatabase(ddlItemCode.SelectedValue, itemQty);
            }
            LoadWOLines() ;
        }

        protected void ddlItemCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedItemCode = ddlItemCode.SelectedValue;
            LoadItems();
            lblDescript.Text = _items.FirstOrDefault(x=>x.Code == selectedItemCode).Description;
            txtAddQty.Attributes.Add("onkeypress", "return validateQuantityInput(event, this)");
            ModalPopupExtender2.Show();
        }

        protected void btnLotAddL_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                //var itm =_db.ItemsMasters.Where(x=>x.CompanyID == CoID && )
                // save new Lot Number to db
                LotTrackingMaster LtNew = new LotTrackingMaster();
                LtNew.LotNumber = txtLotNum.Text.ToString();
                LtNew.CreatedDate = DateTime.Now;
                LtNew.CompanyID = CurrentUser.CoID;
                LtNew.ItemCode = lblItemD.Text.ToString().Split('|')[1].ToString();
                LtNew.ItemId = Convert.ToInt32(lblItemD.Text.ToString().Split('|')[0].ToString());
                LtNew.LotActive = true;
                // need to add lot item cost
                LtNew.LotTotUnitPrice = 0;
                LtNew.LotQuantity = 1;

                _db.LotTrackingMasters.Add(LtNew);

                long wolid = Convert.ToInt32(lblRowid.Text);
                var WORow = _db.WorksOrderLines.Where(x => x.CompanyID == CoID && x.LineID == wolid).FirstOrDefault();
                WORow.LotNumber = txtLotNum.Text.ToString();
                _db.SaveChanges();

                LoadWOLines();
            }
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
    }
}