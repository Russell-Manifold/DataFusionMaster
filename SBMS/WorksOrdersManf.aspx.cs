using AjaxControlToolkit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class WorksOrdersManf : BasePage
    {
        long CoID;
        private List<ItemsMaster> _items;
        private List<BOMHeader> _boms;
        private List<KitHeader> _kits;
        private List<GetLinkedStoredFromItem_Result> _itemST;
        private List<GetActiveLotNumbersLinkedToStores_Result> _ActiveLotNums;
        private readonly HashSet<string> sentKeys = new HashSet<string>();

        long woid = 0;

        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }
        private HashSet<string> SentKeys
        {
            get
            {
                if (Session["SentKeys"] == null)
                    Session["SentKeys"] = new HashSet<string>();
                return (HashSet<string>)Session["SentKeys"];
            }
            set
            {
                Session["SentKeys"] = value;
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
                Session["SentKeys"] = new HashSet<string>();
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
                    _items = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CoID && i.Physical == true && i.IsFinishedGoods == true).OrderBy(x => x.Code).ToList();
                    _boms = _db.BOMHeaders.Where(i => i.BomActive == true && i.CompanyID == CoID).OrderBy(x => x.BOMCode).ToList();
                    _kits = _db.KitHeaders.Where(i => i.KitActive == true && i.CompanyID == CoID).OrderBy(x => x.KitCode).ToList();
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
                    if (WOHeader.Active == false)
                    {
                        chkCompl.Checked = true;
                        LbtnSaveWO.Enabled = false;
                        LbtnSaveWO.Visible = false;
                        LbtnSaveWO.ToolTip = "This Works Order is Closed, no further changes allowed.";
                    }

                    woheader.InnerText = "Allocate Items: Manufacture WO-" + WOHeader.WONum;
                    lblwoid.Text = WOHeader.WONum.ToString();
                    if (WOHeader.CustSupName != null) lblFCCustName.Text = WOHeader.CustSupName.ToString() ?? "";
                    if (WOHeader.Reference != null)
                    {
                        lblFCRef.Text = WOHeader.Reference.ToString() ?? "";
                    }
                    else
                    {
                        AlertHelper.ShowSweetAlert(this, "Invalid Reference, please ammend the works order before continuing", "error");
                        return;
                    }

                    lblcreatedDate.Text = Convert.ToDateTime(WOHeader.WOrderDate, CultureInfo.InvariantCulture).ToString("dd MMM yyyy");
                    if (WOHeader.DueDate != null)
                    {
                        txtDueDate.Text = Convert.ToDateTime(WOHeader.DueDate, CultureInfo.InvariantCulture).ToString("dd MMM yyyy");
                    }
                    else
                    {
                        AlertHelper.ShowSweetAlert(this, "Invalid Due Date, please ammend the works order before continuing", "error");
                        return;
                    }

                    if (WOHeader.LinkedDocumentNum != null) txtLinkedDoc.Text = WOHeader.LinkedDocumentNum ?? "";
                    if (WOHeader.Message != null) txtwomsg.Text = WOHeader.Message.ToString() ?? "";
                    if (WOHeader.WOrderBy != null) lblCreatedBy.Text = WOHeader.WOrderBy.ToString() ?? "";
                    DDStatus.SelectedValue = WOHeader.Status ?? "";
                    if (WOHeader.Active == false)
                    {
                        LbtnUpdateWO.Style.Add("display", "none");
                        LbtnSaveWO.Style.Add("display", "none");
                    }
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
                bool iscomp = false;
                var WOHeader = _db.WorksOrderHeaders.Where(x => x.CompanyID == CoID && x.ID == woid).FirstOrDefault();
                if (WOHeader != null)
                {
                    if (WOHeader.Active == false) iscomp = true;
                }
                var WOLines = _db.WorksOrderLines
                         .Where(x => x.CompanyID == CoID && x.WOID == woid && x.Quantity > 0)
                         .OrderBy(x => x.LineID)
                         .ToList();

                AccordionWOLines.Panes.Clear();

                AccordionPane paneH = new AccordionPane();
                //paneH.HeaderCssClass = "accordionFooter";
                paneH.Attributes.Add("style", "text-align:center; color:#000");
                AccordionWOLines.Panes.Add(paneH);
                LoadItemStores();
                foreach (var line in WOLines)
                {
                    AccordionPane pane = new AccordionPane();
                    DropDownList DDHStore = new DropDownList();
                    if (DDHStore.Items.Count == 0)
                    {
                        var storeList = _itemST.Where(x => x.ItemID == line.SelectionId && x.StoreCode != "CoR" && x.StoreCode != "CoD").ToList();
                        DDHStore.ID = $"dd_{line.LineID}";
                        DDHStore.DataSource = storeList;
                        DDHStore.DataTextField = "StoreCode";
                        DDHStore.DataValueField = "StoreID";
                        DDHStore.DataBind();
                        DDHStore.Items.Insert(0, "-Store-");
                        DDHStore.Attributes.Add("style", "height:0.6em");
                    }
                    if (line.ToStoreID != null)
                    {
                        try
                        {
                            DDHStore.SelectedValue = line.ToStoreID.ToString();
                        }
                        catch { }
                    }

                    CheckBox chkC = new CheckBox
                    {
                        ID = $"chk_{line.LineID}",
                        Text = "Complete",
                        AutoPostBack = true,
                        Checked = line.Complete.HasValue ? line.Complete.Value : false
                    };
                    chkC.CheckedChanged += chkC_CheckedChanged;
                    chkC.Attributes.Add("style", "float:right");
                    pane.HeaderContainer.Controls.Add(chkC);
                    DDHStore.Attributes.Add("style", "float:right; padding:0.5em");
                    if (iscomp == true)
                    {
                        DDHStore.Enabled = false;
                        chkC.Enabled = false;
                    }
                    pane.HeaderContainer.Controls.Add(DDHStore);

                    if (CurrentUser.CompanyUseLotNumbers)
                    {
                        if (iscomp == false)
                        {
                            if (line.IsLotTracked == true)
                            {
                                LinkButton btnLotNum = new LinkButton
                                {
                                    ID = "btnAddLotNum",
                                    Text = " Lot Number",
                                    CssClass = "icon fa-plus buttonCancelZ",
                                    CommandName = "AddLotNum",  // Command to identify the action  
                                    ToolTip = "Allocate Lot Number Works Order Item",
                                    CommandArgument = line.LineID.ToString() + "|" + line.SelectionId.ToString(),

                                };
                                btnLotNum.Click += btnLotNum_Click;
                                pane.HeaderContainer.Controls.Add(btnLotNum);
                            }
                        }
                    }
                    // Add header text to the AccordionPane's HeaderContainer
                    var headerLiteral = new Literal { Text = line.ItemCode + " - " + line.ItemDescription + " Qty: " + line.Quantity };
                    if (CurrentUser.CompanyUseLotNumbers)
                    {
                        if (line.IsLotTracked == true)
                        {
                            headerLiteral = new Literal { Text = line.ItemCode + " - " + line.ItemDescription + " Qty: " + line.Quantity + "  --> Lot Number:" + line.LotNumber };
                        }
                    }
                    pane.HeaderContainer.Controls.Add(headerLiteral);
                    HiddenField hiddenFieldH = new HiddenField
                    {
                        ID = "HiddenLineIDH",
                        Value = line.LineID.ToString() + "|" + line.SelectionId + "|" + line.ItemCode + "|" + line.Quantity + "|" + line.LotNumber
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
                        ShowFooter = true
                    };

                    gridRMs.RowCommand += GridRMs_RowCommand;

                    // Define the same columns as your example
                    gridRMs.Columns.Add(new BoundField { DataField = "LineID" });
                    gridRMs.Columns.Add(new BoundField { DataField = "SelectionId" });
                    gridRMs.Columns.Add(new BoundField { DataField = "ItemCode", HeaderText = "Code", ItemStyle = { Width = new Unit("5em") } });
                    gridRMs.Columns.Add(new BoundField { DataField = "ItemDescription", HeaderText = "Description" });
                    gridRMs.Columns.Add(new BoundField { DataField = "Unit", HeaderText = "Unit", ItemStyle = { Width = new Unit("3em") } });
                    gridRMs.Columns.Add(new BoundField { DataField = "Quantity", HeaderText = "Quantity", ItemStyle = { Width = new Unit("5em") } });
                    if (iscomp == true) gridRMs.Enabled = false;

                    TemplateField useQtyField = new TemplateField
                    {
                        HeaderText = "Use_Qty",
                        ItemTemplate = new GridViewTemplate(ListItemType.Item, "UseQuantity")
                    };
                    useQtyField.HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
                    useQtyField.ItemStyle.Width = 80;
                    gridRMs.Columns.Add(useQtyField);

                    TemplateField scrapQtyField = new TemplateField
                    {
                        HeaderText = "Scrap_Qty",
                        ItemTemplate = new GridViewTemplate(ListItemType.Item, "ScrapQuantity")
                    };
                    scrapQtyField.HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
                    scrapQtyField.ItemStyle.Width = 80;
                    gridRMs.Columns.Add(scrapQtyField);

                    TemplateField costQtyField = new TemplateField
                    {
                        HeaderText = "Cost",
                        ItemTemplate = new GridViewTemplate(ListItemType.Item, "UnitCost")
                    };
                    costQtyField.HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
                    costQtyField.ItemStyle.Width = 20;
                    gridRMs.Columns.Add(costQtyField);

                    //TemplateField lineCostField = new TemplateField
                    //{
                    //    HeaderText = "Line_Cost",
                    //    ItemTemplate = new GridViewTemplate(ListItemType.Item, "LineCost")
                    //};
                    //lineCostField.HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
                    //lineCostField.ItemStyle.Width = 30;
                    //gridRMs.Columns.Add(lineCostField);

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

                    if (CurrentUser.CompanyUseLotNumbers == true)
                    {
                        // Add a dropdown field
                        TemplateField DDlotNum = new TemplateField
                        {
                            HeaderText = "Lot Number",
                            ItemTemplate = new GridViewTemplate(ListItemType.Item, "DDlotNum"),
                        };
                        DDlotNum.ItemStyle.HorizontalAlign = HorizontalAlign.Left;
                        DDlotNum.ItemStyle.Width = 80;

                        gridRMs.Columns.Add(DDlotNum);


                    }
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
                    foreach (var Ln in MLines)
                    {
                        if (Ln.Quantity != null)
                        {
                            Ln.Quantity = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(Ln.Quantity.ToString(), CurrentUser.CompanyDecPlaces));
                        }
                        Ln.UseQty = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(Ln.UseQty.ToString(), CurrentUser.CompanyDecPlaces));
                        Ln.ScrapQty = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(Ln.ScrapQty.ToString(), CurrentUser.CompanyDecPlaces));
                    }
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
                LoadItems();
                var Item = _items.Where(x => x.CompanyID == CoID && x.ID == ItemID).FirstOrDefault();
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
                            AlertHelper.ShowSweetAlert(this, "Invalid Quanitity captured.", "error");
                            return;
                        }

                        if (useqty != 0)
                        {
                            DropDownList DDStore = row.FindControl("DDStore") as DropDownList;
                            if (DDStore.SelectedIndex == 0)
                            {
                                chk.Checked = false;
                                AlertHelper.ShowSweetAlert(this, "Please select a valid store for each item", "error");
                                return;
                            }
                            else
                            {
                                if (CurrentUser.CompanyUseLotNumbers == true)
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
                                                if (WoL.LotNumber == "")
                                                {
                                                    DropDownList DDlotNum = row.FindControl("DDlotNum") as DropDownList;
                                                    if (DDlotNum.SelectedIndex == 0)
                                                    {
                                                        chk.Checked = false;
                                                        AlertHelper.ShowSweetAlert(this, "Please select a valid Lot Number for each Lot Tracked item.", "error");
                                                        return;
                                                    }
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
            string SuccStr = SaveWO();
            AlertHelper.ShowSweetAlert(this, SuccStr, "success");
        }

        protected string SaveWO()
        {
            List<string> errorMessages = new List<string>();
            bool cont = true;
            string retStr = "OK";
            try
            {
                DateTime DtD = Convert.ToDateTime(txtDueDate.Text);
            }
            catch
            {
                retStr = "Invalid Due Date, Unable to Save";
                return retStr;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                foreach (AccordionPane pane in AccordionWOLines.Panes)
                {
                    foreach (Control controlH in pane.HeaderContainer.Controls)
                    {
                        if (controlH is CheckBox chkC)
                        {
                            if (chkC.Checked == true)
                            {
                                HiddenField HiddenLineIDH = controlH.FindControl("HiddenLineIDH") as HiddenField;
                                int lid = Convert.ToInt32(HiddenLineIDH.Value.Split('|')[0]);

                                DropDownList ddStore = controlH.FindControl($"dd_{lid}") as DropDownList;
                                if (ddStore.SelectedIndex == 0)
                                {
                                    retStr = "You have marked the Works Order as Complete, but an invalid store is selected, Unable to Save";
                                    cont = false;
                                    return retStr;
                                }

                                var WOL = _db.WorksOrderLines.Where(x => x.LineID == lid).FirstOrDefault();
                                // Only save basic data, NOT completion status
                                WOL.ToStoreID = Convert.ToInt32(ddStore.SelectedValue);
                                WOL.LotNumber = HiddenLineIDH.Value.Split('|')[4];
                                // DO NOT set Complete, CompleteBy, CompleteDate here
                                _db.SaveChanges();
                            }
                        }
                        if (cont == false) break;
                        foreach (Control control in pane.ContentContainer.Controls)
                        {
                            if (control is GridView grid)
                            {
                                foreach (GridViewRow row in grid.Rows)
                                {
                                    int TLineID = Convert.ToInt32(row.Cells[0].Text);
                                    TextBox txtUseQty = row.FindControl("txtUseQty") as TextBox;
                                    TextBox txtScrapQty = row.FindControl("txtScrapQty") as TextBox;
                                    DropDownList DDStore = row.FindControl("DDStore") as DropDownList;
                                    DropDownList DDlotNum = row.FindControl("DDlotNum") as DropDownList;

                                    if (DDStore.SelectedIndex > 0)
                                    {
                                        var WRMLine = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LineID == TLineID).FirstOrDefault();
                                        if (WRMLine == null) continue;

                                        if (CurrentUser.CompanyUseLotNumbers && WRMLine.IsLotTracked)
                                        {
                                            WRMLine.LotNumber = null;

                                            if (DDlotNum.Items.Count > 0)
                                            {
                                                if (!DDlotNum.SelectedItem.Text.Contains("Number -"))
                                                {
                                                    WRMLine.LotNumber = DDlotNum.SelectedValue.ToString();
                                                }
                                                else
                                                {
                                                    errorMessages.Add($"Line {TLineID}: Invalid Lot Number, skipped.");
                                                    continue;
                                                }
                                            }
                                            else
                                            {
                                                errorMessages.Add($"Line {TLineID}: No Lot Number available, skipped.");
                                                continue;
                                            }
                                        }

                                        if (DDStore.SelectedItem != null)
                                        {
                                            WRMLine.StoreCodeFrom = DDStore.SelectedItem.Text.ToString();
                                        }
                                        else
                                        {
                                            WRMLine.StoreCodeFrom = null;
                                        }

                                        decimal UseQty = 0;
                                        try { UseQty = Convert.ToDecimal(txtUseQty.Text); } catch { }
                                        WRMLine.UseQty = UseQty;

                                        decimal ScrQty = 0;
                                        try { ScrQty = Convert.ToDecimal(txtScrapQty.Text); } catch { }
                                        WRMLine.ScrapQty = ScrQty;

                                        _db.SaveChanges();
                                    }
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
                    retStr = "Successfully Saved";
                    return retStr;
                }
            }
            if (errorMessages.Count > 0) retStr = string.Join(Environment.NewLine, errorMessages);
            return retStr;
        }

        protected void GridWOLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
            if (CurrentUser.CompanyUseLotNumbers == false)
            {
                e.Row.Cells[10].Visible = false;
            }
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var item = (WorksOrderLine)e.Row.DataItem;
                var ddlItemCode = (DropDownList)e.Row.FindControl("DDItemCode");
                var ddlLineType = (DropDownList)e.Row.FindControl("DDBOMKIT");

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

                else if (item.LineType == 2)
                {
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
                else if (item.LineType == 3)
                {
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
            try { dt = Convert.ToDateTime(txtDueDate.Text.ToString(), CultureInfo.InvariantCulture); }
            catch { AlertHelper.ShowSweetAlert(this, "Invalid Due Date, Unable to continue.", "error"); return; }

            if (ddl.SelectedIndex == 0)
            {
                AlertHelper.ShowSweetAlert(this, "Invalid Item Selected, Unable to continue.", "error");
            }

            decimal qty;
            try
            {
                qty = Convert.ToDecimal(txtQty.Text);
            }
            catch { AlertHelper.ShowSweetAlert(this, "Invalid Quantity captured. Unable to continue.", "error"); return; }

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


                var Rmd = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LinkedWOLineID == rowid).ToList();
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
                    int bmc = _db.BOMHeaders.Where(x => x.CompanyID == CoID && x.FGCode == ThisFCLine.ItemCode).Select(x => x.BomHID).FirstOrDefault();
                    var BomLines = _db.GetBOMLinesFromBomHeaderID(bmc, CoID);

                    foreach (var bl in BomLines)
                    {
                        WorksOrderRMLine RML = new WorksOrderRMLine();
                        RML.LinkedWOLineID = (int)rowid;
                        RML.WOID = woid;
                        RML.SelectionId = (long)bl.ItemID;
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
                AlertHelper.ShowSweetAlert(this, "Successfully Saved.", "success");
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
                AlertHelper.ShowSweetAlert(this, "Line successfully Deleted", "success");
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
            }
            catch
            {
                AlertHelper.ShowSweetAlert(this, "Invalid manufacturing quantity, Unable to calculate.", "error");
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
                        if (_items == null)
                        {
                            _items = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CoID && i.Physical == true && i.IsFinishedGoods == true).OrderBy(x => x.Code).ToList();
                        }
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
                        if (_boms == null)
                        {
                            _boms = _db.BOMHeaders.Where(i => i.BomActive == true && i.CompanyID == CoID).OrderBy(x => x.BOMCode).ToList();
                        }
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
                        if (_kits == null)
                        {
                            _kits = _db.KitHeaders.Where(i => i.KitActive == true && i.CompanyID == CoID).OrderBy(x => x.KitCode).ToList();
                        }
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
                AlertHelper.ShowSweetAlert(this, "Invalid Quantity, Unable to continue", "error");
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
                    var storeList = _itemST.Where(x => x.ItemID == itemID && x.StoreCode != "CoR" && x.StoreCode != "CoD").ToList();
                    ddlStore.DataSource = storeList;
                    ddlStore.DataTextField = "StoreCode";
                    ddlStore.DataValueField = "StoreID";
                    ddlStore.DataBind();
                    ddlStore.Items.Insert(0, "-?-");

                    // Set selected value only if it exists in the list
                    string storeValue = item.StoreCodeFrom;
                    ListItem foundByText = !string.IsNullOrEmpty(storeValue) ? ddlStore.Items.FindByText(storeValue) : null;
                    ListItem foundByValue = !string.IsNullOrEmpty(storeValue) ? ddlStore.Items.FindByValue(storeValue) : null;
                    if (foundByText != null)
                    {
                        ddlStore.SelectedValue = foundByText.Value;
                    }
                    else if (foundByValue != null)
                    {
                        ddlStore.SelectedValue = storeValue;
                    }
                    else
                    {
                        ddlStore.SelectedIndex = 0;
                    }
                    ddlStore.SelectedIndexChanged += DDStore_SelectedIndexChanged;
                }

                if (CurrentUser.CompanyUseLotNumbers == true)
                {
                    // Find and bind DDlotNum
                    DropDownList ddlLotNum = e.Row.FindControl("DDlotNum") as DropDownList;
                    if (ddlLotNum != null && item.IsLotTracked)
                    {
                        ddlLotNum.Attributes.Add("style", "display:inline-block");
                        LoadActiveLotNums();
                        var lotNums = _ActiveLotNums.Where(x => x.StoreCode == ddlStore.SelectedItem.Text && x.ItemId == itemID).Select(x => new
                            {
                                LotNum = x.LotNumber,
                                LotDisplay = $"{x.LotNumber} ({x.QtyHandToStore:N2})"
                            })
                            .ToList();

                        ddlLotNum.DataSource = lotNums;
                        ddlLotNum.DataTextField = "LotDisplay";
                        ddlLotNum.DataValueField = "LotNum";
                        ddlLotNum.DataBind();
                        if (lotNums.Count > 0)
                        {
                            ddlLotNum.Items.Insert(0, new ListItem("- Lot Number -", ""));
                        }
                        try
                        {
                            ddlLotNum.SelectedValue = item.LotNumber;
                        }
                        catch { }
                        ddlLotNum.SelectedIndexChanged += DDlotNum_SelectedIndexChanged;
                    }
                    else if (ddlLotNum != null)
                    {
                        ddlLotNum.Items.Clear();
                        ddlLotNum.Attributes.Add("style", "display:none");
                    }
                }
                TextBox txtUseQty = e.Row.FindControl("txtUseQty") as TextBox;
                txtUseQty.Text = item.UseQty.ToString();

                Label txtUnitCost = e.Row.FindControl("txtUnitCost") as Label;
                txtUnitCost.Text = string.IsNullOrEmpty(item.UnitCost?.ToString()) ? "0.00" : Convert.ToDecimal(item.UnitCost).ToString("N2");

                //Label txtLineCost = e.Row.FindControl("txtLineCost") as Label;
                //txtLineCost.Text = string.IsNullOrEmpty(item.LineCost?.ToString()) ? "0.00" : Convert.ToDecimal(item.UnitCost).ToString("N2");

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
                if (CurrentUser.CompanyUseLotNumbers == true)
                {
                    e.Row.Cells[10].Controls.Add(confirmExtender);
                }
                else
                {
                    e.Row.Cells[9].Controls.Add(confirmExtender);
                }
                lbtnDelRow.Click += lbtnDelRow_click;
            }
            else if (e.Row.RowType == DataControlRowType.Footer)
            {
                if (!chkCompl.Checked)
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
                    if (CurrentUser.UseAutoManf == true)
                    {
                        LinkButton btnAutoManf = new LinkButton
                        {
                            ID = "btnAutoManf",
                            Text = " Auto-fill Use Quantities",
                            CssClass = "icon fa-fill buttonRed",
                            CommandName = "SaveAutoManf",  // Command to identify the action  
                            ToolTip = "Auto full all Use-Qty fields with required quantities.",
                        };
                        e.Row.Cells[6].ColumnSpan = 3;
                        e.Row.Cells[6].Controls.Add(btnAutoManf);
                    }
                }
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
            else if (e.CommandName == "SaveAutoManf")
            {
                GridView grid = (GridView)sender;
                LoadActiveLotNums();
                foreach (GridViewRow gvr in grid.Rows)
                {
                    TextBox txtUseQty = gvr.FindControl("txtUseQty") as TextBox;
                    txtUseQty.Text = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(gvr.Cells[5].Text, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces).ToString();

                    DropDownList ddStore = gvr.FindControl("DDStore") as DropDownList;
                    if (ddStore.Items.Count > 2)
                    {
                        ddStore.SelectedIndex = 1; // Select the second item if only two items are present
                        long ItemID = Convert.ToInt64(gvr.Cells[1].Text);
                        string StoreCode = ddStore.SelectedItem.Text;
                        var LotNums = _ActiveLotNums.Where(x => x.ItemId == ItemID && x.StoreCode == StoreCode).ToList();

                        long LineID = Convert.ToInt64(gvr.Cells[0].Text);
                        using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                        {
                            var WOLine = _db.WorksOrderRMLines.Where(x => x.LineID == LineID).FirstOrDefault();
                            WOLine.StoreCodeFrom = ddStore.SelectedItem.Text;
                            WOLine.UseQty = Convert.ToDecimal(txtUseQty.Text, CultureInfo.InvariantCulture);

                            Label txtUnitCost = gvr.FindControl("txtUnitCost") as Label;
                            txtUnitCost.Text = Convert.ToDouble(LotNums[0].TotalUnitPriceExclInclAdd).ToString("N2");
                            WOLine.UnitCost = Convert.ToDecimal(txtUnitCost.Text, CultureInfo.InvariantCulture);

                            TextBox txtScrapQty = gvr.FindControl("txtScrapQty") as TextBox;
                            WOLine.ScrapQty = Convert.ToDecimal(txtScrapQty.Text, CultureInfo.InvariantCulture);
                            _db.SaveChanges();
                        }
                    }
                }
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
                    //RejectQty = 0
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
                if (_kits == null) _kits = _db.KitHeaders.Where(i => i.KitActive == true && i.CompanyID == CoID).ToList();
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
                if (_ActiveLotNums == null) _ActiveLotNums = _db.GetActiveLotNumbersLinkedToStores(CurrentUser.CoID).ToList();
            }
        }

        protected void DDStore_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            GridViewRow row = (GridViewRow)ddl.NamingContainer;
            DropDownList DDStore = (DropDownList)row.FindControl("DDStore");
            Label txtUnitCost = (Label)row.FindControl("txtUnitCost");
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string StoreCode = DDStore.SelectedItem.Text.ToString();
                long LineID = Convert.ToInt64(row.Cells[0].Text);
                long ItemID = Convert.ToInt64(row.Cells[1].Text.ToString());
                var WOLine = _db.WorksOrderRMLines.Where(x => x.LineID == LineID).FirstOrDefault();

                if (DDStore.SelectedIndex > 0)
                {
                    WOLine.StoreCodeFrom = DDStore.SelectedItem.Text;
                    if (CurrentUser.CompanyUseLotNumbers == true)
                    {
                        if (DDStore.SelectedIndex > 0)
                        {
                            PopulateDDLotNumbers(row, StoreCode, ItemID);
                        }
                        else
                        {
                            DropDownList DDlotNum = (DropDownList)row.FindControl("DDlotNum");
                            DDlotNum.Items.Clear();
                            WOLine.LotNumber = "";
                        }
                    }
                    else
                    {
                        LoadActiveLotNums();
                        var LotNums = _ActiveLotNums.Where(x => x.ItemId == ItemID && x.StoreCode == StoreCode).ToList();

                        txtUnitCost.Text = Convert.ToDouble(LotNums[0].TotalUnitPriceExclInclAdd).ToString("N2");
                        WOLine.UnitCost = Convert.ToDecimal(LotNums[0].TotalUnitPriceExclInclAdd.ToString());
                    }
                    _db.SaveChanges();
                }

                else
                {
                    WOLine.UnitCost = 0;
                    WOLine.StoreCodeFrom = null;
                    _db.SaveChanges();
                    txtUnitCost.Text = "0.00";
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
                    lotNumList.Add(new LotNumList
                    {
                        LotNum = "",
                        LotDisplay = "- Lot Number -"
                    });
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
                   
                    string LotNum = DDlotNum.SelectedValue.ToString();
                    long LineID = Convert.ToInt64(row.Cells[0].Text);
                    var WOLine = _db.WorksOrderRMLines.Where(x => x.LineID == LineID).FirstOrDefault();
                    WOLine.LotNumber = LotNum;
                    WOLine.UnitCost = LotNums[0].TotalUnitPriceExclInclAdd;
                    _db.SaveChanges();
                    Label txtUnitCost = (Label)row.FindControl("txtUnitCost");
                    txtUnitCost.Text = LotNums[0].TotalUnitPriceExclInclAdd.ToString();
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
            Label txtUnitCost = (Label)row.FindControl("txtUnitCost");
            TextBox txtScrapQty = (TextBox)row.FindControl("txtScrapQty");

            if (DDlotNum.SelectedIndex > 0)
            {
                string LotNum = DDlotNum.SelectedValue.ToString();
                long ItemID = Convert.ToInt64(row.Cells[1].Text);
                decimal ItemQty = Convert.ToDecimal(row.Cells[5].Text);
                LoadActiveLotNums();
                var LotNums = _ActiveLotNums.Where(x => x.StoreCode == DDStore.SelectedItem.ToString() && x.ItemId == ItemID && x.LotNumber == LotNum).ToList();
                txtUnitCost.Text = LotNums.Where(x => x.LotNumber == LotNum).Select(x=>x.TotalUnitPriceExclInclAdd).FirstOrDefault().ToString();
                if (LotNums.Sum(x=>x.QtyHandToStore) < ItemQty)
                {
                    AlertHelper.ShowSweetAlert(this, "Insufficient quantity available.", "error");
                    return;
                }
                else
                {
                    
                    long LineID = Convert.ToInt64(row.Cells[0].Text);
                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var WOLine = _db.WorksOrderRMLines.Where(x => x.LineID == LineID).FirstOrDefault();
                        WOLine.UseQty = Convert.ToDecimal(txtuseQty.Text);
                        var costText = txtUnitCost.Text?.Trim();
                        WOLine.UnitCost = string.IsNullOrEmpty(costText)
                            ? 0m
                            : Convert.ToDecimal(costText);
                        txtUnitCost.Text = Convert.ToDecimal(WOLine.UnitCost).ToString("N2");
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
                    var costText = txtUnitCost.Text?.Trim();
                    WOLine.UnitCost = string.IsNullOrEmpty(costText)
                        ? 0m
                        : Convert.ToDecimal(costText);
                    txtUnitCost.Text = Convert.ToDecimal(WOLine.UnitCost).ToString("N2");
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
                    else if (columnName == "UnitCost")
                    {
                        Label txtUnitCost = new Label
                        {
                            ID = "txtUnitCost",
                            Width = new Unit("2em")
                        };
                        container.Controls.Add(txtUnitCost);
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
            try
            {
                // Your existing long-running process
                SaveWO();
                ExtractAccordionHeaderDetails(sender);
            }
            catch (Exception ex)
            {
                // Show error in your existing label
                lblReccount.Text = "Error during transfer: " + ex.Message;
                lblReccount.ForeColor = System.Drawing.Color.Red;
            }
            finally
            {
                // Re-enable buttons via JavaScript
                ScriptManager.RegisterStartupScript(this, this.GetType(), "EnableButtons",
                    "enableButtonsAfterProcess();", true);
            }
        }

        protected void ExtractAccordionHeaderDetails(object sender)
        {
            bool cont = true;
            // Skip header pane, process all data panes
            for (int i = 1; i < AccordionWOLines.Panes.Count; i++)
            {
                AccordionPane pane = AccordionWOLines.Panes[i];
                HiddenField hiddenFieldH = (HiddenField)pane.HeaderContainer.FindControl("HiddenLineIDH");
                if (hiddenFieldH != null)
                {
                    string lineID = hiddenFieldH.Value.Split('|')[0];
                    string itemselectionid = hiddenFieldH.Value.Split('|')[1];
                    long ItemSelection = Convert.ToInt64(itemselectionid);
                    decimal itemqty = Convert.ToDecimal(hiddenFieldH.Value.Split('|')[3]);
                    string itemlotnum = string.Empty;

                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var WOline = _db.WorksOrderLines.Where(x => x.CompanyID == CurrentUser.CoID && x.WOID == woid && x.SelectionId == ItemSelection).FirstOrDefault();
                        if (CurrentUser.CompanyUseLotNumbers && WOline.IsLotTracked)
                        {
                            if (hiddenFieldH.Value.Split('|')[4].Length > 0)
                            {
                                itemlotnum = hiddenFieldH.Value.Split('|')[4];
                            }
                            if (itemlotnum == null || itemlotnum == "")
                            {
                                cont = false;
                                AlertHelper.ShowSweetAlert(this, "Lot Number required for " + WOline.ItemCode + ". Unable to continue.", "error");
                                return;
                            }
                        }
                    }

                    DropDownList ddStore = pane.HeaderContainer.Controls.OfType<DropDownList>().FirstOrDefault();
                    if (ddStore.SelectedIndex < 1)
                    {
                        cont = false;
                        AlertHelper.ShowSweetAlert(this, "Please select a destination store for the item.", "warning");
                        return;
                    }

                    CheckBox chkComplete = pane.HeaderContainer.Controls.OfType<CheckBox>().FirstOrDefault();
                    bool isComplete = chkComplete != null ? chkComplete.Checked : false;
                    if (isComplete == false)
                    {
                        cont = false;
                        AlertHelper.ShowSweetAlert(this, "Unable to update Sage, please mark the Works Order Header as complete.", "error");
                        return;
                    }

                    // PROCESS GRIDVIEW ROWS FOR VALIDATION
                    foreach (Control control in pane.ContentContainer.Controls)
                    {
                        if (control is GridView grid)
                        {
                            foreach (GridViewRow row in grid.Rows)
                            {
                                if (row.RowType == DataControlRowType.DataRow)
                                {
                                    TextBox txtUseQty = row.FindControl("txtUseQty") as TextBox;
                                    TextBox txtScrapQty = row.FindControl("txtScrapQty") as TextBox;
                                    DropDownList DDStore = row.FindControl("DDStore") as DropDownList;
                                    DropDownList DDlotNum = row.FindControl("DDlotNum") as DropDownList;

                                    if (DDStore.SelectedIndex < 1)
                                    {
                                        cont = false;
                                        AlertHelper.ShowSweetAlert(this, $"Invalid store selected in BOM items for {row.Cells[2].Text.ToString()} . Unable to continue.", "error");
                                        return;
                                    }

                                    decimal useQty = 0;
                                    decimal scrapQty = 0;
                                    try { useQty = Convert.ToDecimal(txtUseQty.Text); } catch { }
                                    try { scrapQty = Convert.ToDecimal(txtScrapQty.Text); } catch { }

                                    if (useQty == 0 && scrapQty == 0)
                                    {
                                        cont = false;
                                        AlertHelper.ShowSweetAlert(this, $"Invalid quantity for {row.Cells[2].Text.ToString()}. Unable to continue.", "error");
                                        return;
                                    }

                                    if (CurrentUser.CompanyUseLotNumbers)
                                    {
                                        var lineIDText = row.Cells[0].Text;
                                        int TLineID = Convert.ToInt32(lineIDText);
                                        using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                                        {
                                            var WRMLine = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LineID == TLineID).FirstOrDefault();
                                            if (WRMLine != null && WRMLine.IsLotTracked)
                                            {
                                                if (DDlotNum.SelectedItem == null ||DDlotNum.Items.Count == 0 ||string.IsNullOrEmpty(DDlotNum.SelectedItem.Text) || DDlotNum.SelectedItem.Text.Contains("Number"))
                                                {
                                                    cont = false;
                                                    AlertHelper.ShowSweetAlert(this, $"Lot Number required for {WRMLine.ItemCode}. Unable to continue.", "error");
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (cont == false) return;

            // FIRST: Process all API calls
            for (int i = 1; i < AccordionWOLines.Panes.Count; i++)
            {
                AccordionPane pane = AccordionWOLines.Panes[i];
                HiddenField hiddenFieldH = (HiddenField)pane.HeaderContainer.FindControl("HiddenLineIDH");

                if (hiddenFieldH != null)
                {
                    string lineID = hiddenFieldH.Value.Split('|')[0];
                    string itemselectionid = hiddenFieldH.Value.Split('|')[1];
                    decimal itemqty = Convert.ToDecimal(hiddenFieldH.Value.Split('|')[3]);
                    string itemlotnum = string.Empty;
                    if (hiddenFieldH.Value.Split('|')[4].Length > 0)
                    {
                        itemlotnum = hiddenFieldH.Value.Split('|')[4];
                    }

                    DropDownList ddStore = pane.HeaderContainer.Controls.OfType<DropDownList>().FirstOrDefault();

                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        string selectionId = itemselectionid;
                        string LotNumber = itemlotnum;
                        string store = ddStore.SelectedItem.Text;
                        string Quantity = itemqty.ToString();

                        string key = $"{selectionId}|{LotNumber}|{store}|{Quantity}";
                        if (!SentKeys.Contains(key))
                        {
                            int Lid = Convert.ToInt32(lineID);
                            long ItemID = Convert.ToInt64(selectionId);
                            var total =
                                        (from h in _db.WorksOrderHeaders
                                         join l in _db.WorksOrderLines on h.ID equals l.WOID
                                         join r in _db.WorksOrderRMLines on h.ID equals r.WOID
                                         where l.LineID == 11162
                                            && l.SelectionId == 42
                                            && h.CompanyID == 11961
                                         group new { r.Quantity, r.UnitCost } by l.SelectionId into g
                                         select g.Sum(x => x.Quantity * x.UnitCost)
                                        ).FirstOrDefault();
                            // add finished item to stock
                            string RetStr = DoItemAdjustment(Convert.ToInt64(selectionId), LotNumber, store, itemqty, 0, "H", (decimal)total/ itemqty);  // "H" denoted works order finished good
                            if (RetStr != "OK")
                            {
                                AlertHelper.ShowSweetAlert(this, "Error 1661 performing Item Adjustmnent in Data Fusion: " + RetStr, "error");
                                return;
                            }
                            SentKeys.Add(key);
                        }

                        // PROCESS GRIDVIEW ROWS FOR API CALLS
                        foreach (Control control in pane.ContentContainer.Controls)
                        {
                            if (control is GridView grid)
                            {
                                foreach (GridViewRow row in grid.Rows)
                                {
                                    if (row.RowType == DataControlRowType.DataRow)
                                    {
                                        TextBox txtUseQty = row.FindControl("txtUseQty") as TextBox;
                                        TextBox txtScrapQty = row.FindControl("txtScrapQty") as TextBox;
                                        DropDownList DDStore = row.FindControl("DDStore") as DropDownList;
                                        DropDownList DDlotNum = row.FindControl("DDlotNum") as DropDownList;

                                        var lineIDText = row.Cells[0].Text;
                                        int TLineID = Convert.ToInt32(lineIDText);

                                        decimal useQty = 0;
                                        decimal scrapQty = 0;
                                        try { useQty = Convert.ToDecimal(txtUseQty.Text); } catch { }
                                        try { scrapQty = Convert.ToDecimal(txtScrapQty.Text); } catch { }

                                        string gridSelectionId = row.Cells[1].Text; // SelectionId from second column
                                        string gridLotNumber = "";
                                        var WRMLine = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LineID == TLineID).FirstOrDefault();
                                        if (CurrentUser.CompanyUseLotNumbers)
                                        {     
                                                if (WRMLine != null && WRMLine.IsLotTracked)
                                                {
                                                    if (DDlotNum.SelectedItem != null || DDlotNum.Items.Count != 0 || !string.IsNullOrEmpty(DDlotNum.SelectedItem.Text) || !DDlotNum.SelectedItem.Text.Contains("Number"))
                                                    {
                                                        gridLotNumber = DDlotNum.SelectedValue.ToString();
                                                    }
                                                }

                                        }
                                        string gridStore = DDStore.SelectedItem.Text;

                                        // Process usage quantity
                                        if (useQty > 0)
                                        {
                                            string usageKey = $"{gridSelectionId}|{gridLotNumber}|{gridStore}|{useQty}";
                                            if (!SentKeys.Contains(usageKey))
                                            {
                                                string RetStr = DoItemAdjustment(Convert.ToInt64(gridSelectionId), gridLotNumber, gridStore, useQty * -1, 0, "L", (decimal)WRMLine.UnitCost); // "L" denoted works order component
                                                if (RetStr != "OK")
                                                {
                                                    AlertHelper.ShowSweetAlert(this, "Error performing Item Adjustment for usage: " + RetStr, "error");
                                                    return;
                                                }
                                                SentKeys.Add(usageKey);
                                            }
                                        }

                                        // Process scrap quantity
                                        if (scrapQty > 0)
                                        {
                                            string scrapKey = $"{gridSelectionId}|{gridLotNumber}|{gridStore}|{scrapQty}";
                                            if (!SentKeys.Contains(scrapKey))
                                            {
                                                string RetStr = DoItemAdjustment(Convert.ToInt64(gridSelectionId), gridLotNumber, gridStore, 0, scrapQty * -1, "L", (decimal)WRMLine.UnitCost);  // "L" denoted works order component
                                                if (RetStr != "OK")
                                                {
                                                    AlertHelper.ShowSweetAlert(this, "Error performing Item Adjustment for scrap: " + RetStr, "error");
                                                    return;
                                                }
                                                SentKeys.Add(scrapKey);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // ONLY AFTER ALL API CALLS SUCCEED: Update local DB completion status
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Update Works Order Lines completion status
                for (int i = 1; i < AccordionWOLines.Panes.Count; i++)
                {
                    AccordionPane pane = AccordionWOLines.Panes[i];
                    HiddenField hiddenFieldH = (HiddenField)pane.HeaderContainer.FindControl("HiddenLineIDH");
                    if (hiddenFieldH != null)
                    {
                        string lineID = hiddenFieldH.Value.Split('|')[0];
                        decimal itemqty = Convert.ToDecimal(hiddenFieldH.Value.Split('|')[3]);

                        long lined = Convert.ToInt32(lineID);
                        var wolP = _db.WorksOrderLines.Where(x => x.CompanyID == CurrentUser.CoID && x.LineID == lined).FirstOrDefault();
                        wolP.UseQty = itemqty;
                        wolP.Active = false;
                        wolP.Complete = true;
                        wolP.CompleteDate = DateTime.Now;
                        wolP.CompleteBy = CurrentUser.RoleID;
                    }
                }
                _db.SaveChanges();

                // Mark Works Order Header as complete
                long wonum = Convert.ToInt64(lblwoid.Text);
                var woh = _db.WorksOrderHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.WONum == wonum).FirstOrDefault();
                woh.Active = false;
                woh.WOrderCloseOffDate = DateTime.Now;
                woh.WOrderCloseBy = CurrentUser.RoleID.ToString();
                _db.SaveChanges();
            }

            LbtnUpdateWO.Style.Add("display", "none");
            LbtnSaveWO.Style.Add("display", "none");
            AlertHelper.ShowSweetAlert(this, "Successfully Saved", "success");
            return;
        }


        protected void DDFCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            // Find the GridViewRow containing the DropDownList
            GridViewRow footerRow = (GridViewRow)ddl.NamingContainer;
            string selectedItemCode = ddl.SelectedValue;
            LoadItems();
            footerRow.Cells[3].Text = _items.FirstOrDefault(x => x.Code == selectedItemCode).Description;
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
                AlertHelper.ShowSweetAlert(this, "Successfully Deleted", "success");
            }
        }

        protected void lbtnAddYesM_Click(object sender, EventArgs e)
        {
            decimal itemQty = 0;
            try
            {
                itemQty = Convert.ToDecimal(txtAddQty.Text);
            }
            catch { AlertHelper.ShowSweetAlert(this, "Invalid Quantity!", "error"); }
            long woLid = Convert.ToInt64(woLineID.Text);
            if (itemQty != 0)
            {
                SaveNewRowToDatabase(ddlItemCode.SelectedValue, itemQty);
            }
            LoadWOLines();
        }

        protected void ddlItemCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedItemCode = ddlItemCode.SelectedValue;
            LoadItems();
            lblDescript.Text = _items.FirstOrDefault(x => x.Code == selectedItemCode).Description;
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

        protected string DoItemAdjustment(long itmid, string LotNum, string stor, decimal useqty, decimal rejqty, string LineType, decimal unitcost)
        {
            string success = "OK";
            decimal QOH = 0;
            try
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {                 
                    long store1 = _db.Stores.Where(x => x.StoreCode == stor && x.CompanyID == CurrentUser.CoID).Select(x => x.StoreID).FirstOrDefault();

                    // Call API FIRST before any local DB saves
                    ApiUrlCall api = new ApiUrlCall();
                    api.LoadOneItemNA(itmid, CurrentUser);
                    var itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == itmid);

                    ItemAdjustment iAdj = new ItemAdjustment();
                    iAdj.Date = DateTime.Now;
                    iAdj.ItemID = itmid;
                    iAdj.AverageCost = (decimal)itm.AverageCost;
                    iAdj.Quantity = useqty;
                    iAdj.Reason = "Manf: of WO" + Convert.ToInt64(lblwoid.Text) + " - " + DateTime.Now.ToString() + " Lot:" + LotNum;
                    if (useqty < 0) iAdj.Reason = iAdj.Reason.Replace("Manf", "Draw");
                    iAdj.Created = DateTime.Now;
                    string jsonBody = JsonConvert.SerializeObject(iAdj, Formatting.Indented);

                    if (CurrentUser.UATMode == false)
                    {
                        string reslt = SendItemAdjustment(jsonBody);
                        if (reslt != "Success")
                        {
                            success = $"Error 1892:{reslt}";
                            return success;
                        }
                    }

                    // ONLY AFTER API SUCCESS - save to local DB
                    var result = _db.ItemTransactions.Where(item => item.ItemID == itmid && item.CompanyID == CurrentUser.CoID && item.ToID == store1);
                    decimal? sumResult = result.Sum(x => (decimal?)x.Qty);
                    QOH = sumResult ?? 0;

                    ItemTransaction ItemTrans = new ItemTransaction();
                    ItemTrans.CompanyID = CurrentUser.CoID;
                    ItemTrans.DocumentID = 0;
                    if (useqty > 0)
                    {
                        ItemTrans.TransactionType = "MANF";
                    }
                    else
                    {
                        ItemTrans.TransactionType = "DRAW";
                    }
                    ItemTrans.ItemID = itmid;
                    ItemTrans.ItemCode = itm.Code;
                    ItemTrans.ItemDescription = itm.Description;
                    ItemTrans.Unit = itm.Unit;
                    ItemTrans.FromID = 0;
                    ItemTrans.LotNumber = LotNum;
                    ItemTrans.ToID = store1;
                    ItemTrans.Qty = Convert.ToDecimal(useqty);
                    ItemTrans.DocumentType = 1;
                    ItemTrans.TransactionDate = DateTime.Now;
                    ItemTrans.ByRoleID = CurrentUser.RoleID;
                    ItemTrans.PriceExclusive = unitcost;
                    ItemTrans.AdditionalCosts = 0;
                    ItemTrans.TotalUnitPriceExclInclAdd = unitcost;
                    ItemTrans.TotalLineValExcl = ItemTrans.PriceExclusive * ItemTrans.Qty;
                    ItemTrans.TransactionReference = ItemTrans.TransactionType +": WO" + Convert.ToInt64(lblwoid.Text) + " - " + DateTime.Now.ToString() + " Lot:" + LotNum;
                    ItemTrans.ExchRate = 1;
                    _db.ItemTransactions.Add(ItemTrans);
                    _db.SaveChanges();

                    #region Record rejectes/scrap - ALSO REVERSED ORDER
                    if (rejqty > 0)
                    {
                        store1 = _db.Stores.Where(x => x.StoreDescript.ToLower().Contains("scrap") && x.CompanyID == CurrentUser.CoID).Select(x => x.StoreID).FirstOrDefault();
                        if (store1 == 0)
                        {
                            Store str = new Store();
                            str.CompanyID = CurrentUser.CoID;
                            str.StoreCode = "Scr";
                            str.StoreDescript = "Scrap";
                            str.AllowPicking = false;
                            str.AllowReceiving = false;
                            str.StoreActive = true;
                            _db.Stores.Add(str);
                            _db.SaveChanges();
                            store1 = str.StoreID;
                        }

                        // API CALL FIRST for scrap
                        iAdj = new ItemAdjustment();
                        iAdj.Date = DateTime.Now;
                        iAdj.ItemID = itmid;
                        iAdj.AverageCost = (decimal)itm.AverageCost;
                        iAdj.Quantity = rejqty * -1m;
                        iAdj.Reason = "Scrap WO" + Convert.ToInt64(lblwoid.Text) + " - " + DateTime.Now.ToString() + " Lot:" + LotNum;
                        iAdj.Created = DateTime.Now;
                        jsonBody = JsonConvert.SerializeObject(iAdj, Formatting.Indented);
                        if (CurrentUser.UATMode == false)
                        {
                            string scrapResult = SendItemAdjustment(jsonBody);
                            if (scrapResult != "Success")
                            {
                                success = $"Error 1960:{scrapResult}";
                                return success;
                            }
                        }

                        // ONLY AFTER API SUCCESS - save scrap to local DB
                        result = _db.ItemTransactions.Where(item => item.ItemID == itmid && item.CompanyID == CurrentUser.CoID && item.ToID == store1);
                        sumResult = result.Sum(x => (decimal?)x.Qty);
                        QOH = sumResult ?? 0;

                        ItemTrans = new ItemTransaction();
                        ItemTrans.CompanyID = CurrentUser.CoID;
                        ItemTrans.DocumentID = 0;
                        ItemTrans.TransactionType = "Scrap";
                        ItemTrans.ItemID = itmid;
                        ItemTrans.ItemCode = itm.Code;
                        ItemTrans.ItemDescription = itm.Description;
                        ItemTrans.Unit = itm.Unit;
                        ItemTrans.FromID = 0;
                        ItemTrans.LotNumber = LotNum;
                        ItemTrans.ToID = store1;
                        ItemTrans.Qty = Convert.ToDecimal(rejqty);
                        ItemTrans.DocumentType = 1;
                        ItemTrans.TransactionDate = DateTime.Now;
                        ItemTrans.ByRoleID = CurrentUser.RoleID;
                        ItemTrans.PriceExclusive = unitcost;
                        ItemTrans.AdditionalCosts = 0;
                        ItemTrans.TotalUnitPriceExclInclAdd = unitcost;
                        ItemTrans.TotalLineValExcl = ItemTrans.PriceExclusive * ItemTrans.Qty;
                        ItemTrans.TransactionReference = "Scrap WO" + Convert.ToInt64(lblwoid.Text) + " - " + DateTime.Now.ToString() + " Lot:" + LotNum;
                        ItemTrans.ExchRate = 1;
                        _db.ItemTransactions.Add(ItemTrans);
                        _db.SaveChanges();
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                success = "Err 1965: " + ex.Message;
            }
            return success;
        }
        public string SendItemAdjustment(string Item)
        {
            string doctype = "ItemAdjustment";

            try
            {
                ApiUrlCall Api = new ApiUrlCall();
                JObject parsedJSON = Api.APIPostDocumentNA(doctype, Item, CurrentUser);

                if (parsedJSON == null)
                    return "Null response from API";

                if (parsedJSON["error"] != null)
                {
                    JObject errorObj = (JObject)parsedJSON["error"];
                    string statusCode = errorObj["statusCode"]?.ToString();
                    string message = errorObj["message"]?.ToString();
                    string exception = errorObj["exception"]?.ToString();
                    string reason = errorObj["reason"]?.ToString();

                    string errorMessage = !string.IsNullOrEmpty(message) ? message :
                                        !string.IsNullOrEmpty(exception) ? exception :
                                        !string.IsNullOrEmpty(reason) ? reason : "Unknown API error";

                    if (!string.IsNullOrEmpty(statusCode))
                        errorMessage += $" (Status: {statusCode})";

                    return errorMessage;
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return $"SendItemAdjustment exception: {ex.Message}";
            }
        }

        protected void Page_Unload(object sender, EventArgs e)
        {
            // clear checklist when leaving the page
            Session.Remove("SentKeys");
        }

    }
}