using AjaxControlToolkit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Services.Description;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Windows.Interop;

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
                LoadDrawFromStores();
                if (AccordionWOLines.Panes.Count == 2)
                {
                    AccordionWOLines.SelectedIndex = 1;
                }
            }
            else
            {
                LoadWOLines();
                //AccordionWOLines.SelectedIndex = -1;      // All collapsed
                //AccordionWOLines.RequireOpenedPane = false; // Allow all closed
            }
        }

        protected void LoadWOHeader()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var WOHeader = _db.WorksOrderHeaders.Where(x => x.CompanyID == CoID && x.ID == woid).FirstOrDefault();
                if (WOHeader != null)
                {
                    if (WOHeader.Active == false || WOHeader.Status == "Complete")
                    {
                        chkCompl.Checked = true;
                        LbtnSaveWO.Enabled = false;
                        LbtnSaveWO.Visible = false;
                        LbtnSaveWO.ToolTip = "This Works Order is Closed, no further changes allowed.";
                        LbtnUpdateWO.Enabled = false;
                        LbtnUpdateWO.Visible = false;
                        LbtnUpdateWO.ToolTip = "This Works Order has already been manufactured.";
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
                }
            }
        }

        protected void LoadWOLines()
        {
            LoadAccordion(woid, CoID);
            SetPartManufactureButtons();
        }

        // Populates the single "Draw from" store selector used by no-lot Auto-fill.
        // Only shown when Auto Manufacture is enabled AND the company is not lot-tracked;
        // defaults to the company's WIP store when one exists.
        private void LoadDrawFromStores()
        {
            pnlDrawFrom.Visible = CurrentUser.UseAutoManf && !CurrentUser.CompanyUseLotNumbers;
            if (!pnlDrawFrom.Visible) return;

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var stores = _db.Stores
                    .Where(x => x.CompanyID == CoID && x.StoreActive == true && x.IsWip == true && x.StoreCode != "CoR" && x.StoreCode != "CoD")
                    .OrderBy(x => x.StoreCode).ToList();

                ddlDrawFrom.DataSource = stores;
                ddlDrawFrom.DataTextField = "StoreCode";
                ddlDrawFrom.DataValueField = "StoreID";
                ddlDrawFrom.DataBind();
                ddlDrawFrom.Items.Insert(0, new ListItem("-?-", "0"));

                var wip = stores.FirstOrDefault(x => x.IsWip);
                if (wip != null) ddlDrawFrom.SelectedValue = wip.StoreID.ToString();
            }
        }

        // When the "Draw from" store is changed, point every component row on every line
        // at the newly selected store and persist it. (Quantities/coverage are still applied
        // by the per-line Auto-fill button.)
        protected void ddlDrawFrom_SelectedIndexChanged(object sender, EventArgs e)
        {
            long drawStoreId = 0;
            long.TryParse(ddlDrawFrom.SelectedValue, out drawStoreId);
            if (drawStoreId == 0) return;
            string drawStoreCode = ddlDrawFrom.SelectedItem.Text;

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                foreach (AccordionPane pane in AccordionWOLines.Panes)
                {
                    foreach (Control ctl in pane.ContentContainer.Controls)
                    {
                        if (!(ctl is GridView grid)) continue;
                        foreach (GridViewRow gvr in grid.Rows)
                        {
                            if (gvr.RowType != DataControlRowType.DataRow) continue;
                            DropDownList ddStore = gvr.FindControl("DDStore") as DropDownList;
                            if (ddStore != null)
                            {
                                if (ddStore.Items.FindByValue(drawStoreId.ToString()) == null)
                                    ddStore.Items.Add(new ListItem(drawStoreCode, drawStoreId.ToString()));
                                ddStore.SelectedValue = drawStoreId.ToString();
                            }

                            long lineId = Convert.ToInt64(gvr.Cells[0].Text);
                            var WOLine = _db.WorksOrderRMLines.FirstOrDefault(x => x.LineID == lineId);
                            if (WOLine != null) WOLine.StoreCodeFrom = drawStoreCode;
                        }
                    }
                }
                _db.SaveChanges();
            }
        }

        // Decides whether the "Close Remaining" and "Reset Lines" header buttons are
        // available. (The Part Manufacture button lives on each open line's accordion
        // pane header, added in CreateAccordionPane.)
        //  - Close Remaining: only once something has been manufactured and a balance
        //    is still outstanding.
        //  - Reset Lines: an item with more than one incomplete line.
        protected void SetPartManufactureButtons()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var header = _db.WorksOrderHeaders.FirstOrDefault(x => x.CompanyID == CoID && x.ID == woid);
                bool headerActive = header != null && header.Active != false && header.Status != "Complete";

                var lines = _db.WorksOrderLines
                    .Where(x => x.CompanyID == CoID && x.WOID == woid && (x.Quantity ?? 0) > 0)
                    .ToList();

                var openLines = lines.Where(x => x.Active == true && x.Complete != true).ToList();
                int openCount = openLines.Count;
                int completedCount = lines.Count(x => x.Complete == true);

                lbtnCloseRemaining.Visible = headerActive && openCount >= 1 && completedCount >= 1;

                // Reset is offered only when an item has more than one incomplete line
                // (i.e. there are splits to consolidate).
                lbtnResetLines.Visible = headerActive
                    && openLines.GroupBy(x => new { x.SelectionId, x.LineType }).Any(grp => grp.Count() > 1);
            }
        }

        protected void LoadAccordion(long woid, long CoID)
        {
            try
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    bool iscomp = false;
                    var WOHeader = _db.WorksOrderHeaders
                        .FirstOrDefault(x => x.CompanyID == CoID && x.ID == woid);

                    if (WOHeader != null && WOHeader.Active == false)
                    {
                        iscomp = true;
                    }

                    var WOLines = _db.WorksOrderLines
                        .Where(x => x.CompanyID == CoID && x.WOID == woid && x.Quantity > 0)
                        .OrderBy(x => x.LineID)
                        .ToList();

                    AccordionWOLines.Panes.Clear();

                    LoadItemStores();

                    foreach (var line in WOLines)
                    {
                        AccordionPane pane = CreateAccordionPane(line, CoID, iscomp, _db);
                        AccordionWOLines.Panes.Add(pane);
                    }
                }
            }
            catch (Exception ex)
            {
                AlertHelper.ShowSweetAlert(this, $"Error loading accordion for WOID: { woid} - {ex}", "error");
                return;
            }
        }

        private AccordionPane CreateAccordionPane(WorksOrderLine line, long CoID, bool iscomp, SBMSEntities _db)
        {
            AccordionPane pane = new AccordionPane();
            pane.ID = $"AccordionPane_{line.LineID}"; // Add this line
            pane.Attributes.Add("class", "cost-calculation-pane");
            pane.Attributes.Add("data-line-id", line.LineID.ToString());

            // A line is locked if the whole WO is closed (iscomp) OR this individual
            // line has already been completed in an earlier (part-manufacture) batch.
            // This lets completed batches show read-only while the open balance stays editable.
            bool lineLocked = iscomp || (line.Complete == true);

            // Create and configure store dropdown
            DropDownList DDHStore = CreateStoreDropdown(line, lineLocked);

            // Create completion checkbox
            CheckBox chkC = CreateCompletionCheckbox(line, lineLocked);
            // Add header controls
            pane.HeaderContainer.Controls.Add(chkC);
            DDHStore.Attributes.Add("style", "float:right; width:5em; text-align:center");
            pane.HeaderContainer.Controls.Add(DDHStore);

            // Part Manufacture button on every open (incomplete) line. Added after the
            // Store/Complete controls and floated right so the header cluster reads
            // [Part Manufacture] [Store] [Complete] without overflowing the pane.
            if (!lineLocked)
            {
                AddPartManufactureButton(pane, line);
                AddAutoManufactureButton(pane, line);
            }

            // Add lot number button if applicable
            if (CurrentUser.CompanyUseLotNumbers && !lineLocked && line.IsLotTracked == true)
            {
                AddLotNumberButton(pane, line);
            }

            // Add header text
            AddHeaderText(pane, line);

            // Add hidden fields
            AddHiddenFields(pane, line);

            // Create and configure grid view
            GridView gridRMs = CreateGridView(line, CoID, lineLocked, _db);
            pane.ContentContainer.Controls.Add(gridRMs);

            Table tblCosts = CreateCostTable(line);
            pane.Attributes.Add("data-line-quantity", line.Quantity.ToString());
            pane.ContentContainer.Controls.Add(tblCosts);

            // Add cost calculation table
            if (CurrentUser.ShowManfCosts)
            {
                tblCosts.Attributes.Add("style", "display:inline-block; font-size:0.8em");
            }
            else
            {
                tblCosts.Attributes.Add("style", "display:none");
            }
            return pane;
        }

        private DropDownList CreateStoreDropdown(WorksOrderLine line, bool iscomp)
        {
            DropDownList DDHStore = new DropDownList();
            var storeList = _itemST.Where(x => x.ItemID == line.SelectionId &&
                                               x.StoreCode != "CoR" &&
                                               x.StoreCode != "CoD" &&
                                               x.StoreCode.ToLower() != "scr").ToList();

            DDHStore.ID = $"dd_{line.LineID}";
            DDHStore.DataSource = storeList;
            DDHStore.DataTextField = "StoreCode";
            DDHStore.DataValueField = "StoreID";
            DDHStore.DataBind();
            //DDHStore.Attributes.Add("style", "height:2em");

            if (storeList.Count > 1)
            {
                DDHStore.Items.Insert(0, "-Store-");
            }

            if (line.ToStoreID != null)
            {
                try
                {
                    DDHStore.SelectedValue = line.ToStoreID.ToString();
                }
                catch { return DDHStore; }
            }

            if (iscomp)
            {
                DDHStore.Enabled = false;
            }

            return DDHStore;
        }

        private CheckBox CreateCompletionCheckbox(WorksOrderLine line, bool iscomp)
        {
            CheckBox chkC = new CheckBox
            {
                ID = $"chk_{line.LineID}",
                Text = "Complete",
                AutoPostBack = true,
                Checked = line.Complete.HasValue ? line.Complete.Value : false
            };

            chkC.CheckedChanged += chkC_CheckedChanged;
            chkC.Attributes.Add("style", "float:right");

            if (iscomp)
            {
                chkC.Enabled = false;
            }

            return chkC;
        }

        private void AddLotNumberButton(AccordionPane pane, WorksOrderLine line)
        {
            LinkButton btnLotNum = new LinkButton
            {
                ID = $"btnAddLotNum_{line.LineID}",
                Text = " Lot Number",
                CssClass = "icon fa-plus buttonCancel",
                CommandName = "AddLotNum",
                ToolTip = "Allocate Lot Number Works Order Item",
                CommandArgument = $"{line.LineID}|{line.SelectionId}"
            };
            btnLotNum.Attributes.Add("style", "margin-right:0.5em");
            btnLotNum.Click += btnLotNum_Click;
            pane.HeaderContainer.Controls.Add(btnLotNum);
        }

        private void AddHeaderText(AccordionPane pane, WorksOrderLine line)
        {
            string qtyDisplay = ApiUrlCall.NumberToDecimal((line.Quantity ?? 0).ToString(), CurrentUser.CompanyDecPlaces);
            string headerText = $"{line.ItemCode} - {line.ItemDescription} Qty: {qtyDisplay}";

            if (CurrentUser.CompanyUseLotNumbers && line.IsLotTracked == true && !string.IsNullOrEmpty(line.LotNumber))
            {
                headerText += $"  --> Lot Number: {line.LotNumber}";
            }
            Label headerLabel = new Label
            {
                Text = headerText,
                Font = { Bold = true },
                ForeColor = System.Drawing.Color.FromArgb(0x42, 0x82, 0xC1) // #4282C1
            };
            pane.HeaderContainer.Controls.Add(headerLabel);
        }

        private void AddHiddenFields(AccordionPane pane, WorksOrderLine line)
        {
            HiddenField hiddenFieldH = new HiddenField
            {
                ID = $"HiddenLineIDH_{line.LineID}",
                Value = $"{line.LineID}|{line.SelectionId}|{line.ItemCode}|{line.Quantity}|{line.LotNumber}"
            };

            HiddenField hiddenQuantity = new HiddenField
            {
                ID = $"HiddenLineQty_{line.LineID}",
                Value = line.Quantity.ToString(),
                ClientIDMode = ClientIDMode.Static // Important for JavaScript access
            };

            HiddenField hiddenField = new HiddenField
            {
                ID = $"HiddenLineID_{line.LineID}",
                Value = $"{line.LineID}|{line.WOID}"
            };

            pane.HeaderContainer.Controls.Add(hiddenFieldH);
            pane.ContentContainer.Controls.Add(hiddenQuantity);
            pane.ContentContainer.Controls.Add(hiddenField);
        }

        private GridView CreateGridView(WorksOrderLine line, long CoID, bool iscomp, SBMSEntities _db)
        {
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
                ShowFooter = true,
                Enabled = !iscomp
            };
            gridRMs.RowCommand += GridRMs_RowCommand;
            gridRMs.RowDataBound += GridRMs_RowDataBound;

            // Add columns
            AddGridViewColumns(gridRMs);

            // Bind data
            var MLines = _db.WorksOrderRMLines
                .Where(x => x.CompanyID == CoID && x.LinkedWOLineID == line.LineID)
                .ToList();

            // Format decimal values
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

            return gridRMs;
        }

        private void AddGridViewColumns(GridView gridRMs)
        {
            // Hidden columns
            gridRMs.Columns.Add(new BoundField { DataField = "LineID" });
            gridRMs.Columns.Add(new BoundField { DataField = "SelectionId" });

            // Visible columns
            gridRMs.Columns.Add(new BoundField
            {
                DataField = "ItemCode",
                HeaderText = "Code",
                ItemStyle = { Width = Unit.Parse("5em") }
            });

            gridRMs.Columns.Add(new BoundField
            {
                DataField = "ItemDescription",
                HeaderText = "Description"
            });

            gridRMs.Columns.Add(new BoundField
            {
                DataField = "Unit",
                HeaderText = "Unit",
                ItemStyle = { Width = Unit.Parse("3em") }
            });

            gridRMs.Columns.Add(new BoundField
            {
                DataField = "Quantity",
                HeaderText = "Quantity",
                ItemStyle = { Width = Unit.Parse("5em") }
            });

            // On Hand (read-only) - total stock across all stores, set in BindDataRow.
            gridRMs.Columns.Add(new BoundField
            {
                HeaderText = "On Hand",
                HeaderStyle = { HorizontalAlign = HorizontalAlign.Center },
                ItemStyle = { Width = Unit.Parse("5em"), HorizontalAlign = HorizontalAlign.Center }
            });

            // Use Quantity template field
            TemplateField useQtyField = new TemplateField
            {
                HeaderText = "Use_Qty",
                ItemTemplate = new GridViewTemplate(ListItemType.Item, "UseQuantity"),
                HeaderStyle = { HorizontalAlign = HorizontalAlign.Center },
                ItemStyle = { Width = Unit.Parse("80px") }
            };
            gridRMs.Columns.Add(useQtyField);

            // Scrap Quantity template field
            TemplateField scrapQtyField = new TemplateField
            {
                HeaderText = "Scrap_Qty",
                ItemTemplate = new GridViewTemplate(ListItemType.Item, "ScrapQuantity"),
                HeaderStyle = { HorizontalAlign = HorizontalAlign.Center },
                ItemStyle = { Width = Unit.Parse("80px") }
            };
            gridRMs.Columns.Add(scrapQtyField);

            // Unit Cost template field
            TemplateField costQtyField = new TemplateField
            {
                HeaderText = "Cost",
                ItemTemplate = new GridViewTemplate(ListItemType.Item, "UnitCost"),
                HeaderStyle = { HorizontalAlign = HorizontalAlign.Center },
                ItemStyle = { Width = Unit.Parse("20px") }
            };
            gridRMs.Columns.Add(costQtyField);

            // Store dropdown template field
            TemplateField DDStore = new TemplateField
            {
                HeaderText = "Store",
                ItemTemplate = new GridViewTemplate(ListItemType.Item, "DDStore"),
                ItemStyle = { HorizontalAlign = HorizontalAlign.Center, Width = Unit.Parse("60px") }
            };
            gridRMs.Columns.Add(DDStore);

            // Lot number dropdown (if enabled)
            if (CurrentUser.CompanyUseLotNumbers)
            {
                TemplateField DDlotNum = new TemplateField
                {
                    HeaderText = "Lot Number",
                    ItemTemplate = new GridViewTemplate(ListItemType.Item, "DDlotNum"),
                    ItemStyle = { HorizontalAlign = HorizontalAlign.Left, Width = Unit.Parse("80px") }
                };
                gridRMs.Columns.Add(DDlotNum);
            }

            // Delete button template field
            TemplateField btnDelRow = new TemplateField
            {
                HeaderText = "",
                ItemTemplate = new GridViewTemplate(ListItemType.Item, "btnDelRow"),
                ItemStyle = { HorizontalAlign = HorizontalAlign.Left, Width = Unit.Parse("30px") }
            };
            gridRMs.Columns.Add(btnDelRow);
        }

        private Table CreateCostTable(WorksOrderLine line)
        {
            Table tblCosts = new Table();
            tblCosts.Style.Add("font-size", "0.8em");
            tblCosts.BorderColor = System.Drawing.Color.LightGray;
            tblCosts.BorderWidth = Unit.Pixel(1);
            tblCosts.GridLines = GridLines.Both;

            // Header row
            TableRow row0 = new TableRow();
            row0.Cells.Add(new TableCell
            {
                Text = "<h3>Manufacturing Costs</h3>",
                HorizontalAlign = HorizontalAlign.Left,
                Font = { Bold = true }
            });

            TableCell cell0_2 = new TableCell();
            Label lblItemdesc = new Label();
            lblItemdesc.Text = $"<h3>{line.ItemDescription}</h3>";
            lblItemdesc.Style.Add("text-align", "left");
            cell0_2.Controls.Add(lblItemdesc);
            cell0_2.ColumnSpan = 3;
            row0.Cells.Add(cell0_2);
            tblCosts.Rows.Add(row0);

            // Costs row - Sage Average Unit Cost and This Unit Cost
            TableRow row1 = new TableRow();
            TableCell cell1_1 = new TableCell();
            cell1_1.Text = "Sage Average Unit Cost:";
            cell1_1.Style.Add("padding-left", "1em");
            cell1_1.HorizontalAlign = HorizontalAlign.Left;
            row1.Cells.Add(cell1_1);

            LoadItems();
            var StItem = _items.FirstOrDefault(x => x.ID == line.SelectionId);
            TableCell cell1_2 = new TableCell();
            Label lblAvgCost = new Label();
            lblAvgCost.Text = "1.23";
            if (StItem != null) lblAvgCost.Text = Math.Round((decimal)StItem.AverageCost,2).ToString();
            lblAvgCost.Style.Add("width", "6em");
            lblAvgCost.Style.Add("text-align", "right");
            cell1_2.Style.Add("width", "6em");
            cell1_2.Style.Add("text-align", "right");
            cell1_2.Controls.Add(lblAvgCost);

            HiddenField hiddenTotalCost = new HiddenField
            {
                ID = $"HiddenTotalCost_{line.LineID}",
                Value = "0", // Will be updated by JavaScript
                ClientIDMode = ClientIDMode.Static
            };
            cell1_2.Controls.Add(hiddenTotalCost);
            row1.Cells.Add(cell1_2);

            // This Unit Cost
            TableCell cell1_3 = new TableCell();
            cell1_3.Text = "This Unit Cost:";
            cell1_3.HorizontalAlign = HorizontalAlign.Left;
            cell1_3.Style.Add("padding", "1em");
            row1.Cells.Add(cell1_3);

            TableCell cell1_4 = new TableCell();
            cell1_4.HorizontalAlign = HorizontalAlign.Right;

            Label lblThisCost = new Label();
            lblThisCost.Text = "0.00";
            lblThisCost.CssClass = "lblThisCost"; // Ensure this is set
            lblThisCost.ID = $"lblThisCost_{line.LineID}";
            lblThisCost.ClientIDMode = ClientIDMode.Static;

            cell1_4.Controls.Add(lblThisCost);
            row1.Cells.Add(cell1_4);

            // Update Sage radio buttons
            //TableCell cell1_5 = new TableCell();
            //cell1_5.Text = "Update Unit Cost In Sage?";
            //cell1_5.HorizontalAlign = HorizontalAlign.Left;
            //cell1_5.Style.Add("padding-left", "1em");
            //row1.Cells.Add(cell1_5);

            //TableCell cell1_6 = new TableCell();
            //RadioButtonList ddlYesNo = new RadioButtonList();
            //ddlYesNo.Items.Add(new ListItem("No", "No"));
            //ddlYesNo.Items.Add(new ListItem("Yes", "Yes"));
            //ddlYesNo.RepeatDirection = System.Web.UI.WebControls.RepeatDirection.Horizontal;
            //cell1_6.Style.Add("padding-top", "0.6em");
            //cell1_6.HorizontalAlign = HorizontalAlign.Left;
            //cell1_6.Controls.Add(ddlYesNo);
            //row1.Cells.Add(cell1_6);

            tblCosts.Rows.Add(row1);

           // Row 3: Sage Default Selling Price
            TableRow row3 = new TableRow();

            // Sage Default Selling Price
            TableCell cell3_1 = new TableCell { Text = "Sage Default Selling Price:" };
            cell3_1.HorizontalAlign = HorizontalAlign.Left;
            cell3_1.Style.Add("padding-left", "1em");

            TableCell cell3_2 = new TableCell();
            Label lblSellD = new Label();
            lblSellD.Text = "0.00";
            if (StItem != null) lblSellD.Text = Math.Round((decimal)StItem.PriceExclusive,2).ToString();
            cell3_2.Controls.Add(lblSellD);
            cell3_2.HorizontalAlign = HorizontalAlign.Right;
            row3.Cells.Add(cell3_1);
            row3.Cells.Add(cell3_2);

            // New Default Selling Price
            TableCell cell3_3 = new TableCell { Text = "New Default Selling Price:" };
            cell3_3.HorizontalAlign = HorizontalAlign.Left;
            cell3_3.Style.Add("padding-left", "1em");

            TableCell cell3_4 = new TableCell();
            TextBox txtNewSell = new TextBox();
            txtNewSell.Style.Add("text-align", "right");
            txtNewSell.Style.Add("width", "100%");
            txtNewSell.Style.Add("color", "#0275d8");
            txtNewSell.Style.Add("font-weight", "bold");
            txtNewSell.Style.Add("border", "2px solid #4282C1");
            txtNewSell.CssClass = "txtNewSell";
            txtNewSell.ID = $"txtNewSell_{line.LineID}";

            AjaxControlToolkit.FilteredTextBoxExtender ftbe = new AjaxControlToolkit.FilteredTextBoxExtender();
            ftbe.ID = $"ftbe_{line.LineID}";
            ftbe.TargetControlID = txtNewSell.ID;
            ftbe.FilterType = AjaxControlToolkit.FilterTypes.Custom | AjaxControlToolkit.FilterTypes.Numbers;
            ftbe.ValidChars = ".";

            // Add the extender to the cell controls
            cell3_4.Controls.Add(txtNewSell);
            cell3_4.Controls.Add(ftbe);

            cell3_4.Controls.Add(txtNewSell);
            row3.Cells.Add(cell3_3);
            row3.Cells.Add(cell3_4);

            TableRow row2 = new TableRow();
            TableCell cell2_1 = new TableCell { Text = " " };
            cell2_1.ColumnSpan = 2;
            row2.Cells.Add(cell2_1);

            // Update Default Selling Price In Sage?
            TableCell cell2_5 = new TableCell { Text = "Update Default Selling Price In Sage?" };
            cell2_5.HorizontalAlign = HorizontalAlign.Left;
            cell2_5.Style.Add("padding-left", "1em");
            row2.Cells.Add(cell2_5);

            TableCell cell2_6 = new TableCell();
            RadioButtonList ddlUpdate = new RadioButtonList();
            ddlUpdate.ID = $"ddlUpdate_{line.LineID}";
            ddlUpdate.Items.Add(new ListItem("No", "No"));
            ddlUpdate.Items.Add(new ListItem("Yes", "Yes"));
            ddlUpdate.RepeatDirection = System.Web.UI.WebControls.RepeatDirection.Horizontal;
            cell2_6.Style.Add("padding-top", "0.6em");
            cell2_6.HorizontalAlign = HorizontalAlign.Left;
            cell2_6.Controls.Add(ddlUpdate);
            row2.Cells.Add(cell2_6);

            tblCosts.Rows.Add(row3);
            tblCosts.Rows.Add(row2);

            // Set consistent styling for all cells
            foreach (TableRow row in tblCosts.Rows)
            {
                foreach (TableCell cell in row.Cells)
                {
                    cell.BorderColor = System.Drawing.Color.LightGray;
                    cell.BorderWidth = Unit.Pixel(1);
                }
            }
            tblCosts.Style.Add("margin-bottom", "20px");
            return tblCosts;
        }

        protected void GridRMs_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            try
            {
                e.Row.Cells[0].Visible = false;
                e.Row.Cells[1].Visible = false;

                if (e.Row.RowType == DataControlRowType.DataRow)
                {
                    BindDataRow(e);
                }
                else if (e.Row.RowType == DataControlRowType.Footer)
                {
                    BindFooterRow(e);
                }
            }
            catch (Exception ex)
            {
                // Log error
                throw new Exception("Error in GridRMs_RowDataBound", ex);
            }
        }

        private void BindDataRow(GridViewRowEventArgs e)
        {
            var item = e.Row.DataItem as WorksOrderRMLine;
            if (item == null) return;

            long itemID;
            if (!long.TryParse(e.Row.Cells[1].Text, out itemID))
            {
                // Handle parsing error
                return;
            }

            // On Hand column (index 6, right after Quantity): total stock across all stores.
            using (SBMSEntities _dbOH = new SBMSEntities(Config.GetConnectionString()))
            {
                decimal onHandT = _dbOH.ItemTransactions
                    .Where(x => x.CompanyID == CoID && x.ItemID == itemID)
                    .Select(x => (decimal?)x.Qty).DefaultIfEmpty(0).Sum() ?? 0;
                onHandT = ApiUrlCall.NumberToDecimal(onHandT, CurrentUser.CompanyDecPlaces);
                if (e.Row.Cells.Count > 6)
                {
                    e.Row.Cells[6].Text = onHandT.ToString("N2");
                    // Highlight when on-hand can't cover this line's required quantity.
                    if (onHandT < (item.Quantity ?? 0))
                    {
                        e.Row.Cells[6].BackColor = System.Drawing.Color.MistyRose;
                        e.Row.Cells[6].ForeColor = System.Drawing.Color.Firebrick;
                        e.Row.Cells[6].Font.Bold = true;
                    }
                }
            }

            // Find controls
            DropDownList ddlStore = e.Row.FindControl("DDStore") as DropDownList;
            TextBox txtUseQty = e.Row.FindControl("txtUseQty") as TextBox;
            TextBox txtScrapQty = e.Row.FindControl("txtScrapQty") as TextBox;
            Label txtUnitCost = e.Row.FindControl("txtUnitCost") as Label;

            // Configure controls
            if (ddlStore != null)
            {
                if (item.Physical != null && item.Physical != false)
                {
                    ConfigureStoreDropdown(ddlStore, item, itemID);
                }
                else
                {
                    ddlStore.Attributes.Add("style", "display:none");
                }
            }

            if (CurrentUser.CompanyUseLotNumbers)
            {
                ConfigureLotNumberDropdown(e.Row, ddlStore, item, itemID);
            }

            // Set quantity values
            if (txtUseQty != null)
            {
                txtUseQty.Text = item.UseQty.ToString();
                // Add attributes for JavaScript calculation
                txtUseQty.Attributes.Add("data-line-id", item.LinkedWOLineID.ToString());
                txtUseQty.Attributes.Add("class", "use-qty-input");
                txtUseQty.Attributes.Add("oninput", "if(typeof calculateLineCost !== 'undefined') calculateLineCost(this);");
            }

            if (txtScrapQty != null)
            {
                txtScrapQty.Text = item.ScrapQty.ToString();
            }

            // Set unit cost
            if (txtUnitCost != null)
            {
                decimal unitCost = item.UnitCost ?? 0m;
                txtUnitCost.Text = unitCost.ToString("N2");
                txtUnitCost.Attributes.Add("data-line-id", item.LineID.ToString());
                txtUnitCost.Attributes.Add("class", "unit-cost-label");
            }

            // Add delete button with confirmation
            LinkButton lbtnDelRow = e.Row.FindControl("btnDelRow") as LinkButton;
            if (lbtnDelRow != null)
            {
                AddDeleteConfirmation(e.Row, lbtnDelRow);
                lbtnDelRow.Click += lbtnDelRow_click;
            }
        }

        private void ConfigureStoreDropdown(DropDownList ddlStore, WorksOrderRMLine item, long itemID)
        {
            LoadItemStores();
            var storeList = _itemST.Where(x => x.ItemID == itemID &&
                                               x.StoreCode != "CoR" &&
                                               x.StoreCode != "CoD" &&
                                               x.StoreCode.ToLower() != "scr").ToList();

            ddlStore.DataSource = storeList;
            ddlStore.DataTextField = "StoreCode";
            ddlStore.DataValueField = "StoreID";
            ddlStore.DataBind();

            if (storeList.Count > 1)
            {
                ddlStore.Items.Insert(0, "-?-");
            }

            // Try to set unit cost from single store
            if (storeList.Count == 1)
            {
                Label txtUnitCost = (ddlStore.Parent.Parent as GridViewRow)?.FindControl("txtUnitCost") as Label;
                if (txtUnitCost != null && storeList[0].TotalUnitPriceExclInclAdd != null)
                {
                    decimal unitCost = Convert.ToDecimal(storeList[0].TotalUnitPriceExclInclAdd);
                    txtUnitCost.Text = unitCost.ToString("N2");
                }
            }

            // Set selected value
            if (!string.IsNullOrEmpty(item.StoreCodeFrom))
            {
                ListItem foundItem = ddlStore.Items.FindByText(item.StoreCodeFrom) ??
                                   ddlStore.Items.FindByValue(item.StoreCodeFrom);
                if (foundItem != null)
                {
                    ddlStore.SelectedValue = foundItem.Value;
                }
                else
                {
                    // Persisted store isn't in the item's linked-store list (e.g. a WIP
                    // "Draw from" store). Add it so the selection renders and the
                    // manufacture post (which reads the store code) draws from it.
                    ListItem added = new ListItem(item.StoreCodeFrom, item.StoreCodeFrom);
                    ddlStore.Items.Add(added);
                    ddlStore.SelectedValue = added.Value;
                }
            }
            else
            {
                ddlStore.SelectedIndex = 0;
            }

            ddlStore.SelectedIndexChanged += DDStore_SelectedIndexChanged;
            ddlStore.AutoPostBack = true;
        }

        private void ConfigureLotNumberDropdown(GridViewRow row, DropDownList ddlStore, WorksOrderRMLine item, long itemID)
        {
            DropDownList ddlLotNum = row.FindControl("DDlotNum") as DropDownList;
            if (ddlLotNum == null) return;

            if (item.IsLotTracked)
            {
                ddlLotNum.Attributes.Add("style", "display:inline-block");
                LoadActiveLotNums();

                string selectedStore = ddlStore?.SelectedItem?.Text;
                if (!string.IsNullOrEmpty(selectedStore))
                {
                    var lotNums = _ActiveLotNums
                        .Where(x => x.StoreCode == selectedStore && x.ItemId == itemID)
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

                    if (lotNums.Count > 0)
                    {
                        ddlLotNum.Items.Insert(0, new ListItem("- Lot Number -", ""));
                    }

                    if (!string.IsNullOrEmpty(item.LotNumber))
                    {
                        try
                        {
                            ddlLotNum.SelectedValue = item.LotNumber;
                        }
                        catch
                        {
                            // Handle invalid lot number selection
                        }
                    }

                    ddlLotNum.SelectedIndexChanged += DDlotNum_SelectedIndexChanged;
                    ddlLotNum.AutoPostBack = true;
                }
            }
            else
            {
                ddlLotNum.Items.Clear();
                ddlLotNum.Attributes.Add("style", "display:none");
            }
        }

        private void AddDeleteConfirmation(GridViewRow row, LinkButton lbtnDelRow)
        {
            ConfirmButtonExtender confirmExtender = new ConfirmButtonExtender
            {
                ID = $"ConfirmExtender_{row.RowIndex}_{Guid.NewGuid().ToString("N")}",
                TargetControlID = lbtnDelRow.ID,
                ConfirmText = "Are you sure you want to delete this row?",
                Enabled = true
            };

            int controlIndex = CurrentUser.CompanyUseLotNumbers ? 10 : 9;
            if (row.Cells.Count > controlIndex)
            {
                row.Cells[controlIndex].Controls.Add(confirmExtender);
            }
        }

        private void BindFooterRow(GridViewRowEventArgs e)
        {
            if (chkCompl.Checked) return;

            // Add Save button
            LinkButton btnSave = new LinkButton
            {
                ID = $"btnSaveFooter_{e.Row.RowIndex}",
                Text = " Add additional line",
                CssClass = "icon fa-save buttonRed",
                CommandName = "SaveFooter",
                ToolTip = "Add line to works order details"
            };
            e.Row.Cells[3].Controls.Add(btnSave);
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
        //protected void chkC_CheckedChanged(object sender, EventArgs e)
        //{
        //    CheckBox chk = (CheckBox)sender;
        //    bool isChecked = chk.Checked;

        //    Control current = chk;
        //    AccordionPane pane = null;

        //    while (current != null)
        //    {
        //        if (current is AccordionPane)
        //        {
        //            pane = (AccordionPane)current;
        //            break;
        //        }
        //        current = current.Parent;
        //    }

        //    if (pane != null)
        //    {
        //        HiddenField hiddenField = pane.FindControl("HiddenLineID") as HiddenField;
        //        string Lineid = hiddenField.Value.Split('|')[0];
        //        // Find the GridView inside the ContentContainer of the pane
        //        GridView gridRMs = pane.ContentContainer.FindControl($"GridRMs_{Lineid}") as GridView;

        //        if (gridRMs != null)
        //        {
        //            // Iterate through the rows of the GridView
        //            foreach (GridViewRow row in gridRMs.Rows)
        //            {
        //                TextBox txtUseQty = row.FindControl("txtUseQty") as TextBox;
        //                decimal useqty = 0;
        //                try
        //                {
        //                    useqty = Convert.ToDecimal(txtUseQty.Text);
        //                }
        //                catch
        //                {
        //                    chk.Checked = false;
        //                    AlertHelper.ShowSweetAlert(this, "Invalid Quanitity captured.", "error");
        //                    return;
        //                }

        //                if (useqty != 0)
        //                {
        //                    DropDownList DDStore = row.FindControl("DDStore") as DropDownList;
        //                    if (DDStore.SelectedItem.Text.ToString() == "-?-")
        //                    {
        //                        chk.Checked = false;
        //                        AlertHelper.ShowSweetAlert(this, "Please select a valid store for each item", "error");
        //                        return;
        //                    }
        //                    else
        //                    {
        //                        if (CurrentUser.CompanyUseLotNumbers == true)
        //                        {
        //                            int LnID = Convert.ToInt32(row.Cells[0].Text);
        //                            // check if items are lot tracked
        //                            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //                            {
        //                                var WoL = _db.WorksOrderRMLines.Where(x => x.LineID == LnID).FirstOrDefault();
        //                                if (WoL != null)
        //                                {
        //                                    if (WoL.IsLotTracked == true)
        //                                    {
        //                                        if (WoL.LotNumber == "")
        //                                        {
        //                                            DropDownList DDlotNum = row.FindControl("DDlotNum") as DropDownList;
        //                                            if (DDlotNum.SelectedIndex == 0)
        //                                            {
        //                                                chk.Checked = false;
        //                                                AlertHelper.ShowSweetAlert(this, "Please select a valid Lot Number for each Lot Tracked item.", "error");
        //                                                return;
        //                                            }
        //                                        }
        //                                    }
        //                                }
        //                                else
        //                                {
        //                                    return;
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        Response.Write("GridView not found in the current AccordionPane.<br/>");
        //    }
        //}

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
                // Find the HiddenLineID control by its pattern since it has dynamic ID
                HiddenField hiddenField = null;
                foreach (Control control in pane.ContentContainer.Controls)
                {
                    if (control is HiddenField && control.ID != null && control.ID.StartsWith("HiddenLineID_"))
                    {
                        hiddenField = control as HiddenField;
                        break;
                    }
                }

                if (hiddenField == null)
                {
                    // Try in HeaderContainer as fallback
                    foreach (Control control in pane.HeaderContainer.Controls)
                    {
                        if (control is HiddenField && control.ID != null && control.ID.StartsWith("HiddenLineID_"))
                        {
                            hiddenField = control as HiddenField;
                            break;
                        }
                    }
                }

                if (hiddenField != null)
                {
                    string Lineid = hiddenField.Value.Split('|')[0];

                    // Find the GridView inside the ContentContainer of the pane
                    GridView gridRMs = null;

                    // Try to find GridView with dynamic ID first
                    foreach (Control control in pane.ContentContainer.Controls)
                    {
                        if (control is GridView && control.ID != null && control.ID.StartsWith("GridRMs_"))
                        {
                            gridRMs = control as GridView;
                            break;
                        }
                    }

                    // If not found with dynamic ID, try to find any GridView
                    if (gridRMs == null)
                    {
                        gridRMs = pane.ContentContainer.FindControl("GridRMs") as GridView;
                    }

                    if (gridRMs != null)
                    {
                        // Iterate through the rows of the GridView
                        foreach (GridViewRow row in gridRMs.Rows)
                        {
                            if (row.RowType == DataControlRowType.DataRow)
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
                                    AlertHelper.ShowSweetAlert(this, "Invalid Quantity captured.", "error");
                                    return;
                                }

                                if (useqty != 0)
                                {
                                    DropDownList DDStore = row.FindControl("DDStore") as DropDownList;
                                    if (DDStore != null && DDStore.SelectedItem != null && DDStore.SelectedItem.Text.ToString() == "-?-")
                                    {
                                        chk.Checked = false;
                                        AlertHelper.ShowSweetAlert(this, "Please select a valid store for each item", "error");
                                        return;
                                    }
                                    else if (DDStore != null && DDStore.SelectedItem != null)
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
                                                            if (DDlotNum != null && DDlotNum.SelectedIndex == 0)
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
                }
                else
                {
                    chk.Checked = false;
                    AlertHelper.ShowSweetAlert(this, "Unable to find line information.", "error");
                    return;
                }
            }
            else
            {
                chk.Checked = false;
                AlertHelper.ShowSweetAlert(this, "Unable to find accordion pane.", "error");
                return;
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

        //protected string SaveWO()
        //{
        //    List<string> errorMessages = new List<string>();
        //    bool cont = true;
        //    string retStr = "OK";
        //    try
        //    {
        //        DateTime DtD = Convert.ToDateTime(txtDueDate.Text);
        //    }
        //    catch
        //    {
        //        retStr = "Invalid Due Date, Unable to Save";
        //        return retStr;
        //    }

        //    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //    {
        //        foreach (AccordionPane pane in AccordionWOLines.Panes)
        //        {
        //            foreach (Control controlH in pane.HeaderContainer.Controls)
        //            {
        //                if (controlH is CheckBox chkC)
        //                {
        //                    if (chkC.Checked == true)
        //                    {
        //                        HiddenField HiddenLineIDH = controlH.FindControl("HiddenLineIDH") as HiddenField;
        //                        int lid = Convert.ToInt32(HiddenLineIDH.Value.Split('|')[0]);

        //                        DropDownList ddStore = controlH.FindControl($"dd_{lid}") as DropDownList;
        //                        if (ddStore.Items.Count > 1 && ddStore.SelectedIndex == 0)
        //                        {
        //                            retStr = "You have marked the Works Order as Complete, but an invalid store is selected, Unable to Save";
        //                            cont = false;
        //                            return retStr;
        //                        }

        //                        var WOL = _db.WorksOrderLines.Where(x => x.LineID == lid).FirstOrDefault();
        //                        // Only save basic data, NOT completion status
        //                        WOL.ToStoreID = Convert.ToInt32(ddStore.SelectedValue);
        //                        WOL.LotNumber = HiddenLineIDH.Value.Split('|')[4];
        //                        // DO NOT set Complete, CompleteBy, CompleteDate here
        //                        _db.SaveChanges();
        //                    }
        //                }
        //                if (cont == false) break;
        //                foreach (Control control in pane.ContentContainer.Controls)
        //                {
        //                    if (control is GridView grid)
        //                    {
        //                        foreach (GridViewRow row in grid.Rows)
        //                        {
        //                            int TLineID = Convert.ToInt32(row.Cells[0].Text);
        //                            TextBox txtUseQty = row.FindControl("txtUseQty") as TextBox;
        //                            TextBox txtScrapQty = row.FindControl("txtScrapQty") as TextBox;
        //                            Label txtUnitCost = row.FindControl("txtUnitCost") as Label;
        //                            DropDownList DDStore = row.FindControl("DDStore") as DropDownList;
        //                            DropDownList DDlotNum = row.FindControl("DDlotNum") as DropDownList;

        //                            if (DDStore.SelectedItem.Text.ToLower() != "-store-")
        //                            {
        //                                var WRMLine = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LineID == TLineID).FirstOrDefault();
        //                                if (WRMLine == null) continue;

        //                                if (CurrentUser.CompanyUseLotNumbers && WRMLine.IsLotTracked)
        //                                {
        //                                    WRMLine.LotNumber = null;

        //                                    if (DDlotNum.Items.Count > 0)
        //                                    {
        //                                        if (!DDlotNum.SelectedItem.Text.Contains("Number -"))
        //                                        {
        //                                            WRMLine.LotNumber = DDlotNum.SelectedValue.ToString();
        //                                        }
        //                                        else
        //                                        {
        //                                            WRMLine.LotNumber = null;
        //                                            errorMessages.Add($"Line {TLineID}: Invalid Lot Number, skipped.");
        //                                            continue;
        //                                        }
        //                                    }
        //                                    else
        //                                    {
        //                                        WRMLine.LotNumber = null;
        //                                        errorMessages.Add($"Line {TLineID}: No Lot Number available, skipped.");
        //                                        continue;
        //                                    }
        //                                }

        //                                if (DDStore.SelectedItem != null)
        //                                {
        //                                    WRMLine.StoreCodeFrom = DDStore.SelectedItem.Text.ToString();
        //                                }
        //                                else
        //                                {
        //                                    WRMLine.StoreCodeFrom = null;
        //                                }

        //                                decimal UseQty = 0;
        //                                try { UseQty = Convert.ToDecimal(txtUseQty.Text); } catch { }
        //                                WRMLine.UseQty = UseQty;

        //                                decimal ScrQty = 0;
        //                                try { ScrQty = Convert.ToDecimal(txtScrapQty.Text); } catch { }
        //                                WRMLine.ScrapQty = ScrQty;

        //                                decimal LineUnitCost = 0;
        //                                try { LineUnitCost = Convert.ToDecimal(txtUnitCost.Text); } catch { }
        //                                WRMLine.UnitCost = LineUnitCost;
        //                                _db.SaveChanges();
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        var FCHeader = _db.WorksOrderHeaders.Where(x => x.CompanyID == CoID && x.ID == woid).FirstOrDefault();
        //        if (FCHeader != null)
        //        {
        //            FCHeader.Reference = lblFCRef.Text.ToString().Trim();
        //            FCHeader.CustSupName = lblFCCustName.Text.ToString();
        //            FCHeader.WOrderBy = lblCreatedBy.Text.ToString();
        //            FCHeader.LinkedDocumentNum = txtLinkedDoc.Text.ToString();
        //            FCHeader.Message = txtwomsg.Text.ToString().Trim().Replace("'", "''");
        //            FCHeader.DueDate = Convert.ToDateTime(txtDueDate.Text);
        //            FCHeader.Status = DDStatus.Text;
        //            _db.SaveChanges();
        //            retStr = "Successfully Saved";
        //            return retStr;
        //        }
        //    }
        //    if (errorMessages.Count > 0) retStr = string.Join(Environment.NewLine, errorMessages);
        //    return retStr;
        //}
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
                                // Find HiddenLineIDH by searching through controls since it has dynamic ID
                                HiddenField HiddenLineIDH = null;
                                foreach (Control c in pane.HeaderContainer.Controls)
                                {
                                    if (c is HiddenField && c.ID != null && c.ID.StartsWith("HiddenLineIDH_"))
                                    {
                                        HiddenLineIDH = c as HiddenField;
                                        break;
                                    }
                                }

                                if (HiddenLineIDH == null || string.IsNullOrEmpty(HiddenLineIDH.Value))
                                {
                                    retStr = "Unable to find line information for checked item.";
                                    cont = false;
                                    return retStr;
                                }

                                int lid = Convert.ToInt32(HiddenLineIDH.Value.Split('|')[0]);

                                // Find dropdown using dynamic ID pattern
                                DropDownList ddStore = null;
                                foreach (Control c in pane.HeaderContainer.Controls)
                                {
                                    if (c is DropDownList && c.ID != null && c.ID.StartsWith($"dd_{lid}"))
                                    {
                                        ddStore = c as DropDownList;
                                        break;
                                    }
                                }

                                if (ddStore == null)
                                {
                                    // Try alternative approach if dropdown not found by pattern
                                    ddStore = pane.HeaderContainer.Controls.OfType<DropDownList>().FirstOrDefault();
                                }

                                if (ddStore != null && ddStore.Items.Count > 1 && ddStore.SelectedIndex == 0)
                                {
                                    retStr = "You have marked the Works Order as Complete, but an invalid store is selected, Unable to Save";
                                    cont = false;
                                    return retStr;
                                }

                                var WOL = _db.WorksOrderLines.Where(x => x.LineID == lid).FirstOrDefault();
                                if (WOL != null && ddStore != null && ddStore.SelectedItem != null)
                                {
                                    // Only save basic data, NOT completion status
                                    WOL.ToStoreID = Convert.ToInt32(ddStore.SelectedValue);
                                    WOL.LotNumber = HiddenLineIDH.Value.Split('|')[4];
                                    // DO NOT set Complete, CompleteBy, CompleteDate here
                                    _db.SaveChanges();
                                }
                            }
                        }
                        if (cont == false) break;
                    }

                    if (cont == false) break;

                    foreach (Control control in pane.ContentContainer.Controls)
                    {
                        if (control is GridView grid)
                        {
                            foreach (GridViewRow row in grid.Rows)
                            {
                                if (row.RowType == DataControlRowType.DataRow)
                                {
                                    int TLineID = Convert.ToInt32(row.Cells[0].Text);
                                    TextBox txtUseQty = row.FindControl("txtUseQty") as TextBox;
                                    TextBox txtScrapQty = row.FindControl("txtScrapQty") as TextBox;
                                    Label txtUnitCost = row.FindControl("txtUnitCost") as Label;
                                    DropDownList DDStore = row.FindControl("DDStore") as DropDownList;
                                    DropDownList DDlotNum = row.FindControl("DDlotNum") as DropDownList;

                                    var WRMLine = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LineID == TLineID).FirstOrDefault();
                                    if (WRMLine == null) continue;

                                    if (DDStore.SelectedItem != null)
                                    {
                                        if (DDStore.SelectedItem.Text.ToLower() != "-store-")
                                        {
                                            if (CurrentUser.CompanyUseLotNumbers && WRMLine.IsLotTracked)
                                            {
                                                WRMLine.LotNumber = null;

                                                if (DDlotNum != null && DDlotNum.Items.Count > 0)
                                                {
                                                    if (!DDlotNum.SelectedItem.Text.Contains("Number -"))
                                                    {
                                                        WRMLine.LotNumber = DDlotNum.SelectedValue.ToString();
                                                    }
                                                    else
                                                    {
                                                        WRMLine.LotNumber = null;
                                                        errorMessages.Add($"Line {TLineID}: Invalid Lot Number, skipped.");
                                                        continue;
                                                    }
                                                }
                                                else
                                                {
                                                    WRMLine.LotNumber = null;
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
                                        }
                                    }
                                    else 
                                    {
                                        WRMLine.LotNumber = null;
                                        WRMLine.StoreCodeFrom = null;
                                    }
                                    decimal UseQty = 0;
                                    try { UseQty = Convert.ToDecimal(txtUseQty.Text); } catch { }
                                    WRMLine.UseQty = UseQty;

                                    decimal ScrQty = 0;
                                    try { ScrQty = Convert.ToDecimal(txtScrapQty.Text); } catch { }
                                    WRMLine.ScrapQty = ScrQty;

                                    decimal LineUnitCost = 0;
                                    try { LineUnitCost = Convert.ToDecimal(txtUnitCost.Text); } catch { }
                                    WRMLine.UnitCost = LineUnitCost;
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

                    // Keep a partially-manufactured WO flagged correctly regardless of the
                    // status dropdown, so a plain Save can't wrongly mark it Complete while
                    // a balance is still open.
                    bool anyComplete = _db.WorksOrderLines.Any(x => x.CompanyID == CoID && x.WOID == woid && x.Complete == true);
                    bool anyOpen = _db.WorksOrderLines.Any(x => x.CompanyID == CoID && x.WOID == woid && x.Active == true && x.Complete != true && (x.Quantity ?? 0) > 0);
                    if (anyComplete && anyOpen)
                        FCHeader.Status = "Partially Manufactured";
                    else
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
                    ddlItemCode.DataSource = _items.Select(i => new {i.ID, Display = i.Code + " - " + i.Description}).ToList();
                    ddlItemCode.DataTextField = "Display";
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
                // Persist the header (Reference/Notes/Due Date/Status) on every line save,
                // not only when "Save Works Order" is clicked.
                SaveWO();
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
                        ddlItemCode.DataSource = _items.Select(i => new { i.ID, Display = i.Code + " - " + i.Description }).ToList();
                        ddlItemCode.DataTextField = "Display";
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
            // A Works Order must carry a Reference before it can be printed.
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var wo = _db.WorksOrderHeaders.FirstOrDefault(x => x.CompanyID == CoID && x.ID == woid);
                if (wo == null || string.IsNullOrWhiteSpace(wo.Reference))
                {
                    AlertHelper.ShowSweetAlert(this, "Please capture a Reference before printing this Works Order.", "warning");
                    return;
                }
            }
            Response.Redirect($"~/WorksOrderPDFCreate.aspx?woid={woid}", true);
        }

        #region Part Manufacture

        // Adds the Auto-Manufacture button to an open line's pane header, next to Part
        // Manufacture. Auto-fills that line's component Use-Qty (and store, for no-lot
        // companies, from the Draw-from store). Enabled by the UseAutoManf company flag.
        private void AddAutoManufactureButton(AccordionPane pane, WorksOrderLine line)
        {
            if (!CurrentUser.UseAutoManf) return;

            LinkButton btnAuto = new LinkButton
            {
                ID = $"btnAutoManfH_{line.LineID}",
                Text = " Auto-Manufacture",
                CssClass = "icon fa-fill buttonSage",
                CommandArgument = line.LineID.ToString(),
                ToolTip = "Auto-fill all component Use-Qty fields for this line."
            };
            btnAuto.Attributes.Add("style", "float:right; margin:0 0.5em 0 0; padding:0.15em 0.6em; font-size:0.8em; line-height:1.6em");
            btnAuto.Click += lbtnAutoManfHeader_Click;
            pane.HeaderContainer.Controls.Add(btnAuto);
        }

        // Header Auto-Manufacture click: locate this line's component grid and auto-fill it.
        protected void lbtnAutoManfHeader_Click(object sender, EventArgs e)
        {
            LinkButton btn = sender as LinkButton;
            if (btn == null) return;
            AccordionPane pane = FindParentAccordionPane(btn);
            if (pane == null) return;
            foreach (Control ctl in pane.ContentContainer.Controls)
            {
                if (ctl is GridView grid) { RunAutoFill(grid); break; }
            }
        }

        // Adds the Part Manufacture button to an open line's accordion pane header.
        // CommandArgument carries the LineID so the click handler knows exactly which
        // line to split (no need to re-derive "the single open line").
        private void AddPartManufactureButton(AccordionPane pane, WorksOrderLine line)
        {
            LinkButton btnPart = new LinkButton
            {
                ID = $"btnPartManf_{line.LineID}",
                Text = " Part Manufacture",
                CssClass = "icon fa-cut buttonCancel",
                CommandName = "PartManf",
                ToolTip = "Manufacture part of this order now and keep the balance open.",
                CommandArgument = line.LineID.ToString()
            };
            // Compact + float right so it sits inside the 3em header next to Store/Complete
            // rather than overflowing the pane.
            btnPart.Attributes.Add("style", "float:right; margin:0 0.5em 0 0; padding:0.15em 0.6em; font-size:0.8em; line-height:1.6em");
            btnPart.Click += lbtnPartManf_Click;
            pane.HeaderContainer.Controls.Add(btnPart);
        }

        // Opens the Part Manufacture dialog for the line identified by the button's
        // CommandArgument.
        protected void lbtnPartManf_Click(object sender, EventArgs e)
        {
            LinkButton btn = sender as LinkButton;
            long lineId;
            if (btn == null || !long.TryParse(btn.CommandArgument, out lineId))
            {
                AlertHelper.ShowSweetAlert(this, "Unable to identify the line to part-manufacture.", "error");
                return;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var line = _db.WorksOrderLines.FirstOrDefault(x => x.CompanyID == CoID && x.LineID == (int)lineId);
                if (line == null || line.Active != true || line.Complete == true || (line.Quantity ?? 0) <= 0)
                {
                    AlertHelper.ShowSweetAlert(this, "This line is no longer open for Part Manufacture.", "warning");
                    return;
                }
                lblPMOpenLineId.Text = line.LineID.ToString();
                lblPMItem.Text = $"{line.ItemCode} - {line.ItemDescription}";
                if (line.Quantity != null) lblPMRemaining.Text = ApiUrlCall.NumberToDecimal((line.Quantity ?? 0).ToString() ?? "", CurrentUser.CompanyDecPlaces);
                txtPartQty.Text = "";
            }
            ModalPopupPartManf.Show();
        }

        // Logs the part quantity: splits the open line into a batch line (the quantity
        // to make now) and a new balance line for the remainder. The original ordered
        // quantity is preserved on every resulting line via OrderedQty. Entering the
        // full remaining (or more, for over-supply) leaves it as one line.
        protected void btnPartManfSave_Click(object sender, EventArgs e)
        {
            decimal batchQty;
            if (!decimal.TryParse(txtPartQty.Text, out batchQty) || batchQty <= 0)
            {
                AlertHelper.ShowSweetAlert(this, "Please enter a valid quantity greater than zero.", "warning");
                ModalPopupPartManf.Show();
                return;
            }

            long lineId;
            if (!long.TryParse(lblPMOpenLineId.Text, out lineId))
            {
                AlertHelper.ShowSweetAlert(this, "Unable to identify the line to split.", "error");
                return;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var line = _db.WorksOrderLines.FirstOrDefault(x => x.CompanyID == CoID && x.LineID == (int)lineId);
                if (line == null || line.Active != true || line.Complete == true)
                {
                    AlertHelper.ShowSweetAlert(this, "This line is no longer open for Part Manufacture.", "warning");
                    return;
                }

                decimal origQty = line.Quantity ?? 0;
                if (origQty <= 0)
                {
                    AlertHelper.ShowSweetAlert(this, "Invalid line quantity, unable to split.", "error");
                    return;
                }
                decimal ordered = line.OrderedQty ?? origQty;

                var rms = _db.WorksOrderRMLines
                    .Where(x => x.CompanyID == CoID && x.LinkedWOLineID == (int)lineId)
                    .ToList();

                if (batchQty >= origQty)
                {
                    // Full remaining or over-supply: keep it as one line, just rescale.
                    decimal factor = batchQty / origQty;
                    foreach (var rm in rms)
                    {
                        rm.Quantity = (rm.Quantity ?? 0) * factor;
                        rm.LinkedFGQty = batchQty;
                        rm.UseQty = 0; rm.ScrapQty = 0; rm.RejectQty = 0;
                        rm.PickComplete = false;
                    }
                    line.Quantity = batchQty;
                    line.OrderedQty = ordered;
                    _db.SaveChanges();

                    LoadWOHeader();
                    LoadWOLines();
                    // origQty is the remaining balance, so anything beyond it is over-supply.
                    string m = batchQty > origQty
                        ? $"Quantity set to {batchQty} (over-supply of {batchQty - origQty}). Allocate and Transfer to complete."
                        : "Full remaining quantity set. Allocate and Transfer to complete.";
                    AlertHelper.ShowSweetAlert(this, m, "success");
                    return;
                }

                decimal balance = origQty - batchQty;
                decimal factorBatch = batchQty / origQty;
                decimal factorBal = balance / origQty;

                // New balance line (clone of the open finished-good line).
                var balLine = new WorksOrderLine
                {
                    WOID = line.WOID,
                    CompanyID = CoID,
                    SelectionId = line.SelectionId,
                    LineType = line.LineType,
                    ItemCode = line.ItemCode,
                    ItemDescription = line.ItemDescription,
                    Quantity = balance,
                    OrderedQty = ordered,
                    DueDelDate = line.DueDelDate,
                    IsLotTracked = line.IsLotTracked,
                    Active = true,
                    Complete = false,
                    Comments = line.Comments
                };
                _db.WorksOrderLines.Add(balLine);
                _db.SaveChanges(); // assigns balLine.LineID

                // Split the raw-material requirements proportionally: a fresh set scaled
                // to the balance for the new line, and the originals scaled to the batch.
                foreach (var rm in rms)
                {
                    var rmBal = new WorksOrderRMLine
                    {
                        WOID = rm.WOID,
                        CompanyID = CoID,
                        SelectionId = rm.SelectionId,
                        ItemCode = rm.ItemCode,
                        ItemDescription = rm.ItemDescription,
                        Unit = rm.Unit,
                        LineType = rm.LineType,
                        UnitCost = rm.UnitCost,
                        Physical = rm.Physical,
                        IsLotTracked = rm.IsLotTracked,
                        LinkedFGSelectionID = rm.LinkedFGSelectionID,
                        LinkedFGCode = rm.LinkedFGCode,
                        LinkedFGQty = balance,
                        LinkedWOLineID = balLine.LineID,
                        Quantity = (rm.Quantity ?? 0) * factorBal,
                        UseQty = 0,
                        ScrapQty = 0,
                        RejectQty = 0,
                        PickComplete = false
                    };
                    _db.WorksOrderRMLines.Add(rmBal);

                    rm.Quantity = (rm.Quantity ?? 0) * factorBatch;
                    rm.LinkedFGQty = batchQty;
                    rm.UseQty = 0; rm.ScrapQty = 0; rm.RejectQty = 0;
                    rm.PickComplete = false;
                }

                line.Quantity = batchQty;
                line.OrderedQty = ordered;
                _db.SaveChanges();

                LoadWOHeader();
                LoadWOLines();
                AlertHelper.ShowSweetAlert(this,
                    $"Split into a {ApiUrlCall.NumberToDecimal((batchQty).ToString() ?? "", CurrentUser.CompanyDecPlaces)} batch and a {ApiUrlCall.NumberToDecimal((balance).ToString() ?? "", CurrentUser.CompanyDecPlaces)} balance. Allocate and Transfer the batch.",
                    "success");

            }
        }

        // Closes the outstanding balance and completes a partially-manufactured WO.
        // Keeps the already-completed batch lines intact (unlike Delete, which wipes them).
        protected void lbtnCloseRemaining_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var header = _db.WorksOrderHeaders.FirstOrDefault(x => x.CompanyID == CoID && x.ID == woid);
                if (header == null || header.Active == false || header.Status == "Complete")
                {
                    AlertHelper.ShowSweetAlert(this, "This Works Order is already closed.", "warning");
                    return;
                }

                bool anyCompleted = _db.WorksOrderLines.Any(x => x.CompanyID == CoID && x.WOID == woid && x.Complete == true);
                if (!anyCompleted)
                {
                    AlertHelper.ShowSweetAlert(this, "Nothing has been manufactured yet. Use Delete on the Works Orders list to cancel an unstarted order.", "warning");
                    return;
                }

                var openLines = _db.WorksOrderLines
                    .Where(x => x.CompanyID == CoID && x.WOID == woid && x.Active == true && x.Complete != true && (x.Quantity ?? 0) > 0)
                    .ToList();

                foreach (var ol in openLines)
                {
                    var rms = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LinkedWOLineID == ol.LineID).ToList();
                    if (rms.Any()) _db.WorksOrderRMLines.RemoveRange(rms);
                    _db.WorksOrderLines.Remove(ol);
                }

                header.Status = "Complete";
                header.Active = false;
                header.WOrderCloseOffDate = DateTime.Now;
                header.WOrderCloseBy = CurrentUser.RoleID.ToString();
                _db.SaveChanges();
            }

            LoadWOHeader();
            LoadWOLines();
            AlertHelper.ShowSweetAlert(this, "Remaining balance closed. Works Order complete.", "success");
        }

        // Undo accidental splits: consolidate all incomplete (un-manufactured) lines of
        // each item back into a single line whose quantity is their sum, re-scaling that
        // line's raw materials. Already-completed batch lines (posted to Sage) are left
        // untouched. Safe because no split has posted anything to stock.
        protected void lbtnResetLines_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var header = _db.WorksOrderHeaders.FirstOrDefault(x => x.CompanyID == CoID && x.ID == woid);
                if (header == null || header.Active == false || header.Status == "Complete")
                {
                    AlertHelper.ShowSweetAlert(this, "This Works Order is closed.", "warning");
                    return;
                }

                var openLines = _db.WorksOrderLines
                    .Where(x => x.CompanyID == CoID && x.WOID == woid && x.Active == true && x.Complete != true && (x.Quantity ?? 0) > 0)
                    .OrderBy(x => x.LineID)
                    .ToList();

                bool mergedAny = false;
                foreach (var grp in openLines.GroupBy(x => new { x.SelectionId, x.LineType }))
                {
                    var groupLines = grp.OrderBy(x => x.LineID).ToList();
                    if (groupLines.Count <= 1) continue;

                    var keep = groupLines.First();
                    decimal oldKeepQty = keep.Quantity ?? 0;
                    decimal newQty = groupLines.Sum(x => x.Quantity ?? 0);

                    // Re-scale the kept line's raw materials to the consolidated quantity.
                    var keepRms = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LinkedWOLineID == keep.LineID).ToList();
                    decimal factor = oldKeepQty > 0 ? newQty / oldKeepQty : 0;
                    foreach (var rm in keepRms)
                    {
                        rm.Quantity = (rm.Quantity ?? 0) * factor;
                        rm.LinkedFGQty = newQty;
                        rm.UseQty = 0; rm.ScrapQty = 0; rm.RejectQty = 0;
                        rm.PickComplete = false;
                    }
                    keep.Quantity = newQty;
                    // OrderedQty preserved on the kept line.

                    // Remove the surplus split lines and their raw materials.
                    foreach (var extra in groupLines.Skip(1))
                    {
                        var extraRms = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LinkedWOLineID == extra.LineID).ToList();
                        if (extraRms.Any()) _db.WorksOrderRMLines.RemoveRange(extraRms);
                        _db.WorksOrderLines.Remove(extra);
                    }
                    mergedAny = true;
                }

                if (!mergedAny)
                {
                    AlertHelper.ShowSweetAlert(this, "There are no split lines to reset.", "info");
                    return;
                }
                _db.SaveChanges();
            }

            LoadWOHeader();
            LoadWOLines();
            AlertHelper.ShowSweetAlert(this, "Incomplete lines reset and consolidated.", "success");
        }

        // True when the operator has selected this pane to manufacture now: its Complete
        // checkbox is both enabled (i.e. an open line, not a locked/already-done one) and ticked.
        private bool PaneSelectedForManufacture(AccordionPane pane)
        {
            CheckBox chk = pane.HeaderContainer.Controls.OfType<CheckBox>().FirstOrDefault();
            return chk != null && chk.Enabled && chk.Checked;
        }

        #endregion

        protected void GridRMs_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "SaveFooter")
            {
                lblDescript.Text = string.Empty;
                //txtAddQty.Text = null;
                // Find the GridView that triggered the event
                GridView grid = sender as GridView;
                if (grid == null)
                    return;

                // Find the parent AccordionPane
                AccordionPane pane = FindParentAccordionPane(grid);
                string hiddenLineIDHValue = null;
                if (pane != null)
                {
                    // Find the HiddenLineIDH hidden field in the header controls (ID pattern: HiddenLineIDH_*)
                    foreach (Control control in pane.HeaderContainer.Controls)
                    {
                        if (control is HiddenField hf && hf.ID != null && hf.ID.StartsWith("HiddenLineIDH_"))
                        {
                            hiddenLineIDHValue = hf.Value;
                            break;
                        }
                    }
                }
                woLineID.Text = hiddenLineIDHValue.Split('|')[0].ToString();
                WordID.Text = hiddenLineIDHValue.Split('|')[1].ToString();
                LoadItems();
                ddlItemCode.DataSource = _items.Select(i => new { i.ID, Display = i.Code + " - " + i.Description }).ToList();
                ddlItemCode.DataTextField = "Display";
                ddlItemCode.DataValueField = "ID";
                ddlItemCode.DataBind();
                ddlItemCode.Items.Insert(0, "-Add Item-");
                ModalPopupExtender2.Show();
            }
            else if (e.CommandName == "SaveAutoManf")
            {
                RunAutoFill((GridView)sender);
            }
        }

        // Auto-fill a finished-good line's components. No-lot companies fill from the single
        // "Draw from" store with an all-or-nothing on-hand check; lot-tracked companies keep
        // the original per-line behaviour.
        private void RunAutoFill(GridView grid)
        {
            if (grid == null) return;

            if (!CurrentUser.CompanyUseLotNumbers)
            {
                AutoFillFromDrawStore(grid);
                return;
            }

            LoadActiveLotNums();
            foreach (GridViewRow gvr in grid.Rows)
            {
                TextBox txtUseQty = gvr.FindControl("txtUseQty") as TextBox;
                txtUseQty.Text = ApiUrlCall.NumberToDecimal(CellParse.ToDecimal(gvr.Cells[5].Text), CurrentUser.CompanyDecPlaces).ToString();

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

        // No-lot Auto-fill: sets every component line to draw from the chosen "Draw from"
        // store at its required quantity, but only after confirming on-hand covers ALL
        // components at that store. If any are short, nothing is changed and the operator
        // is told exactly what is short (so they can manufacture the sub-assembly first).
        private void AutoFillFromDrawStore(GridView grid)
        {
            long drawStoreId = 0;
            long.TryParse(ddlDrawFrom.SelectedValue, out drawStoreId);
            if (drawStoreId == 0)
            {
                AlertHelper.ShowSweetAlert(this, "Select a 'Draw from' store before auto-filling.", "warning");
                return;
            }
            string drawStoreCode = ddlDrawFrom.SelectedItem.Text;

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Pass 1 - coverage check across every component row (all-or-nothing).
                var shortfalls = new List<string>();
                foreach (GridViewRow gvr in grid.Rows)
                {
                    if (gvr.RowType != DataControlRowType.DataRow) continue;
                    long itemId = Convert.ToInt64(gvr.Cells[1].Text);
                    decimal required = CellParse.ToDecimal(gvr.Cells[5].Text);
                    if (required <= 0) continue;

                    decimal onHand = _db.ItemTransactions
                        .Where(x => x.CompanyID == CoID && x.ItemID == itemId && x.ToID == drawStoreId)
                        .Select(x => (decimal?)x.Qty).DefaultIfEmpty(0).Sum() ?? 0;

                    if (onHand < required)
                    {
                        shortfalls.Add($"{gvr.Cells[2].Text}: need {required:N2}, only {onHand:N2} in {drawStoreCode}");
                    }
                }
                if (shortfalls.Count > 0)
                {
                    AlertHelper.ShowSweetAlert(this,
                        "Insufficient stock in " + drawStoreCode + " - nothing was filled:<br/>" + string.Join("<br/>", shortfalls),
                        "error");
                    return;
                }

                // Pass 2 - all covered: fill the grid controls and persist each line.
                foreach (GridViewRow gvr in grid.Rows)
                {
                    if (gvr.RowType != DataControlRowType.DataRow) continue;
                    long itemId = Convert.ToInt64(gvr.Cells[1].Text);
                    long lineId = Convert.ToInt64(gvr.Cells[0].Text);
                    decimal required = CellParse.ToDecimal(gvr.Cells[5].Text);

                    TextBox txtUseQty = gvr.FindControl("txtUseQty") as TextBox;
                    if (txtUseQty != null)
                        txtUseQty.Text = ApiUrlCall.NumberToDecimal(required, CurrentUser.CompanyDecPlaces).ToString();

                    // Force the row's store dropdown to the draw-from store (add it if the
                    // item wasn't otherwise linked to that store) so the Manufacture post
                    // draws from it.
                    DropDownList ddStore = gvr.FindControl("DDStore") as DropDownList;
                    if (ddStore != null)
                    {
                        if (ddStore.Items.FindByValue(drawStoreId.ToString()) == null)
                            ddStore.Items.Add(new ListItem(drawStoreCode, drawStoreId.ToString()));
                        ddStore.SelectedValue = drawStoreId.ToString();
                    }

                    // Unit cost: latest ledger cost at the draw store, else item average.
                    decimal unitCost = _db.ItemTransactions
                        .Where(x => x.CompanyID == CoID && x.ItemID == itemId && x.ToID == drawStoreId)
                        .OrderByDescending(x => x.TrnID)
                        .Select(x => x.TotalUnitPriceExclInclAdd ?? 0)
                        .FirstOrDefault();
                    if (unitCost == 0)
                        unitCost = _db.ItemsMasters.Where(x => x.CompanyID == CoID && x.ID == itemId)
                                       .Select(x => x.AverageCost ?? 0).FirstOrDefault();

                    Label txtUnitCost = gvr.FindControl("txtUnitCost") as Label;
                    if (txtUnitCost != null) txtUnitCost.Text = ((double)unitCost).ToString("N2");

                    decimal scrapQty = 0;
                    TextBox txtScrapQty = gvr.FindControl("txtScrapQty") as TextBox;
                    if (txtScrapQty != null) decimal.TryParse(txtScrapQty.Text, out scrapQty);

                    var WOLine = _db.WorksOrderRMLines.FirstOrDefault(x => x.LineID == lineId);
                    if (WOLine != null)
                    {
                        WOLine.StoreCodeFrom = drawStoreCode;
                        WOLine.UseQty = required;
                        WOLine.UnitCost = unitCost;
                        WOLine.ScrapQty = scrapQty;
                        _db.SaveChanges();
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
        private void SaveNewRowToDatabase(long itemID, decimal reqty, string storeCode )
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                LoadItems();
                var itm = _items.Where(x => x.ID == itemID).FirstOrDefault();
                int woLineIDF = Convert.ToInt32(woLineID.Text);
                // get one line from existing WorksOrderLines, to use some of the data
                var ExistL = _db.WorksOrderLines.Where(x => x.CompanyID == CoID && x.LineID == woLineIDF).FirstOrDefault();
                ExistL.Complete = false;
                ExistL.CompleteBy = null;
                ExistL.CompleteDate = null;

                var newRow = new WorksOrderRMLine
                {
                    SelectionId = itemID,
                    ItemCode = itm.Code,
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
                    UnitCost = itm.AverageCost / itm.UOMConvert ?? 0,
                    StoreCodeFrom = DDItemAddStore.SelectedItem.Text.ToString(),
                    Physical = itm.Physical        
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
                        var thisitem = _items.FirstOrDefault(x => x.ID == ItemID);
                        if (thisitem != null)
                        {
                            txtUnitCost.Text = thisitem.AverageCost?.ToString("N2") ?? "0.00";
                            WOLine.UnitCost = thisitem.AverageCost ?? 0m;
                        }
                        else
                        {
                            txtUnitCost.Text = "0.00";
                            WOLine.UnitCost = 0;
                        }

                        //if (CurrentUser.CompanyUseLotNumbers == true) 
                        //{ 


                        //LoadActiveLotNums();
                        //    var LotNums = _ActiveLotNums.Where(x => x.ItemId == ItemID && x.StoreCode == StoreCode).ToList();
                        //    if (LotNums.Count > 0)
                        //    {
                        //        txtUnitCost.Text = Convert.ToDouble(LotNums[0].TotalUnitPriceExclInclAdd).ToString("N2");
                        //        WOLine.UnitCost = Convert.ToDecimal(LotNums[0].TotalUnitPriceExclInclAdd.ToString());
                        //    }
                        //    else
                        //    {
                        //        txtUnitCost.Text = "0.00";
                        //        WOLine.UnitCost = 0;
                        //    }
                        //}
                        //else
                        //  {
                        // GET ITEMS
                        //    var thisitem = _items.FirstOrDefault(x => x.ID == ItemID);
                        //    txtUnitCost.Text =thisitem.AverageCost.ToString() ;
                        //    WOLine.UnitCost = (decimal)thisitem.AverageCost;
                        //}
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
                decimal ItemQty = CellParse.ToDecimal(row.Cells[5].Text);
                LoadActiveLotNums();
                var LotNums = _ActiveLotNums.Where(x => x.StoreCode == DDStore.SelectedItem.ToString() && x.ItemId == ItemID && x.LotNumber == LotNum).ToList();
                
                if (LotNums.Count > 0)
                {
                    txtUnitCost.Text = LotNums.Where(x => x.LotNumber == LotNum).Select(x => x.TotalUnitPriceExclInclAdd).FirstOrDefault().ToString();
                    if (LotNums.Sum(x => x.QtyHandToStore) < ItemQty)
                    {
                        txtuseQty.Text = LotNums.Sum(x => x.QtyHandToStore).ToString();
                        txtuseQty.BorderColor = System.Drawing.Color.Red;
                        if (Session["InsufficientQtyAlertShown"] == null)
                        {
                            AlertHelper.ShowSweetAlert(this, "Insufficient quantity available.", "warning");
                            Session["InsufficientQtyAlertShown"] = true;
                        }
                    }
                    else
                    {
                        txtuseQty.Text = ItemQty.ToString();
                        long LineID = Convert.ToInt64(row.Cells[0].Text);
                        using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                        {
                            try
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
                            catch { }                           
                        }
                    }
                }
                else
                {
                    long LineID = Convert.ToInt64(row.Cells[0].Text);
                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        try { 
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
                        catch { }
                    }
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

        protected async void LbtnUpdateWO_Click(object sender, EventArgs e)
        {
            try
            {
                // Hard guard: refuse to manufacture a WO that is already closed.
                // This catches double-click races, stale-page postbacks, and two
                // users hitting Manufacture concurrently from different sessions.
                using (SBMSEntities _dbGuard = new SBMSEntities(Config.GetConnectionString()))
                {
                    var headerCheck = _dbGuard.WorksOrderHeaders
                        .Where(x => x.CompanyID == CurrentUser.CoID && x.ID == woid)
                        .FirstOrDefault();
                    if (headerCheck == null)
                    {
                        AlertHelper.ShowSweetAlert(this, "Works Order not found, unable to continue.", "error");
                        return;
                    }
                    if (headerCheck.Active == false || headerCheck.Status == "Complete")
                    {
                        LbtnUpdateWO.Enabled = false;
                        LbtnUpdateWO.Visible = false;
                        LbtnSaveWO.Enabled = false;
                        LbtnSaveWO.Visible = false;
                        AlertHelper.ShowSweetAlert(this,
                            "This Works Order has already been manufactured and cannot be processed again.",
                            "warning");
                        return;
                    }
                }

                // Your existing long-running process
                SaveWO();
                string Retstr = await ExtractAccordionHeaderDetails(sender);
                if (Retstr == "OK")
                {
                    // Re-read the works order: it may now be fully Complete, or still
                    // open with an outstanding balance (a part manufacture).
                    bool fullyComplete;
                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var h = _db.WorksOrderHeaders.FirstOrDefault(x => x.CompanyID == CoID && x.ID == woid);
                        fullyComplete = h == null || h.Active == false || h.Status == "Complete";
                    }

                    // Rebuild the page from the new state (completed lines lock, balance stays editable).
                    LoadWOHeader();
                    LoadWOLines();

                    if (fullyComplete)
                    {
                        LbtnUpdateWO.Enabled = false;
                        LbtnUpdateWO.Visible = false;
                        LbtnSaveWO.Enabled = false;
                        LbtnSaveWO.Visible = false;
                        AlertHelper.ShowSweetAlert(this, "Works Order fully manufactured and completed.", "success");
                    }
                    else
                    {
                        AlertHelper.ShowSweetAlert(this, "Batch manufactured. The outstanding balance is still open for the next batch.", "success");
                    }
                    return;
                }
                else
                {
                    AlertHelper.ShowSweetAlert(this, "Error during transfer: " + Retstr, "error");
                    return;
                }
            }
            catch (Exception ex)
            {
                new ApiUrlCall().LogErrorToFile(ex.ToString());
                lblReccount.Text = "Error during transfer: " + ex.Message;
                lblReccount.ForeColor = System.Drawing.Color.Red;
                AlertHelper.ShowSweetAlert(this, "Error during transfer: " + ex.Message, "error");
            }
            finally
            {
                // Re-enable buttons via JavaScript
                ScriptManager.RegisterStartupScript(this, this.GetType(), "EnableButtons",
                    "enableButtonsAfterProcess();", true);
            }
        }

        //protected void ExtractAccordionHeaderDetails(object sender)
        //{
        //    bool cont = true;
        //    for (int i = 0; i < AccordionWOLines.Panes.Count; i++)
        //    {
        //        AccordionPane pane = AccordionWOLines.Panes[i];
        //        // ---------- header controls ----------
        //        HiddenField hiddenFieldH = pane.HeaderContainer.FindControl("HiddenLineIDH") as HiddenField;
        //        if (hiddenFieldH != null)
        //        {
        //            string lineID = hiddenFieldH.Value.Split('|')[0];
        //            string itemselectionid = hiddenFieldH.Value.Split('|')[1];
        //            long ItemSelection = Convert.ToInt64(itemselectionid);
        //            decimal itemqty = Convert.ToDecimal(hiddenFieldH.Value.Split('|')[3]);
        //            string itemlotnum = string.Empty;

        //            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //            {
        //                var WOline = _db.WorksOrderLines.Where(x => x.CompanyID == CurrentUser.CoID && x.WOID == woid && x.SelectionId == ItemSelection).FirstOrDefault();
        //                if (CurrentUser.CompanyUseLotNumbers && WOline.IsLotTracked)
        //                {
        //                    if (hiddenFieldH.Value.Split('|')[4].Length > 0)
        //                    {
        //                        itemlotnum = hiddenFieldH.Value.Split('|')[4];
        //                    }
        //                    if (itemlotnum == null || itemlotnum == "")
        //                    {
        //                        cont = false;
        //                        AlertHelper.ShowSweetAlert(this, "Lot Number required for " + WOline.ItemCode + ". Unable to continue.", "error");
        //                        return;
        //                    }
        //                }
        //            }

        //            DropDownList ddStore = pane.HeaderContainer.Controls.OfType<DropDownList>().FirstOrDefault();
        //            if (ddStore.Items.Count > 1 && ddStore.SelectedIndex < 1)
        //            {
        //                cont = false;
        //                AlertHelper.ShowSweetAlert(this, "Please select a destination store for the item.", "warning");
        //                return;
        //            }

        //            CheckBox chkComplete = pane.HeaderContainer.Controls.OfType<CheckBox>().FirstOrDefault();
        //            bool isComplete = chkComplete != null ? chkComplete.Checked : false;
        //            if (isComplete == false)
        //            {
        //                cont = false;
        //                AlertHelper.ShowSweetAlert(this, "Unable to update Sage, please mark the Works Order Header as complete.", "error");
        //                return;
        //            }

        //            // PROCESS GRIDVIEW ROWS FOR VALIDATION
        //            foreach (Control control in pane.ContentContainer.Controls)
        //            {
        //                if (control is GridView grid)
        //                {
        //                    foreach (GridViewRow row in grid.Rows)
        //                    {
        //                        if (row.RowType == DataControlRowType.DataRow)
        //                        {
        //                            TextBox txtUseQty = row.FindControl("txtUseQty") as TextBox;
        //                            TextBox txtScrapQty = row.FindControl("txtScrapQty") as TextBox;
        //                            DropDownList DDStore = row.FindControl("DDStore") as DropDownList;
        //                            DropDownList DDlotNum = row.FindControl("DDlotNum") as DropDownList;

        //                            if (DDStore.SelectedItem.ToString() == "-?-")
        //                            {
        //                                cont = false;
        //                                AlertHelper.ShowSweetAlert(this, $"Invalid store selected in BOM items for {row.Cells[2].Text.ToString()} . Unable to continue.", "error");
        //                                return;
        //                            }

        //                            decimal useQty = 0;
        //                            decimal scrapQty = 0;
        //                            try { useQty = Convert.ToDecimal(txtUseQty.Text); } catch { }
        //                            try { scrapQty = Convert.ToDecimal(txtScrapQty.Text); } catch { }

        //                            if (useQty == 0 && scrapQty == 0)
        //                            {
        //                                cont = false;
        //                                AlertHelper.ShowSweetAlert(this, $"Invalid quantity for {row.Cells[2].Text.ToString()}. Unable to continue.", "error");
        //                                return;
        //                            }

        //                            if (CurrentUser.CompanyUseLotNumbers)
        //                            {
        //                                var lineIDText = row.Cells[0].Text;
        //                                int TLineID = Convert.ToInt32(lineIDText);
        //                                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //                                {
        //                                    var WRMLine = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LineID == TLineID).FirstOrDefault();
        //                                    if (WRMLine != null && WRMLine.IsLotTracked)
        //                                    {
        //                                        if (DDlotNum.SelectedItem == null || DDlotNum.Items.Count == 0 || string.IsNullOrEmpty(DDlotNum.SelectedItem.Text) || DDlotNum.SelectedItem.Text.Contains("Number"))
        //                                        {
        //                                            cont = false;
        //                                            AlertHelper.ShowSweetAlert(this, $"Lot Number required for {WRMLine.ItemCode}. Unable to continue.", "error");
        //                                            return;
        //                                        }
        //                                    }
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //                else if (control is Table AddCostTbl)
        //                {
        //                    string str = "";
        //                }
        //            }
        //        }
        //    }

        //    if (cont == false) return;

        //    // FIRST: Process all API calls
        //    for (int i = 0; i < AccordionWOLines.Panes.Count; i++)
        //    {
        //        AccordionPane pane = AccordionWOLines.Panes[i];
        //        HiddenField hiddenFieldH = (HiddenField)pane.HeaderContainer.FindControl("HiddenLineIDH");
        //        if (hiddenFieldH != null)
        //        {
        //            string lineID = hiddenFieldH.Value.Split('|')[0];
        //            string itemselectionid = hiddenFieldH.Value.Split('|')[1];
        //            decimal itemqty = Convert.ToDecimal(hiddenFieldH.Value.Split('|')[3]);
        //            string itemlotnum = string.Empty;
        //            if (hiddenFieldH.Value.Split('|')[4].Length > 0)
        //            {
        //                itemlotnum = hiddenFieldH.Value.Split('|')[4];
        //            }

        //            DropDownList ddStore = pane.HeaderContainer.Controls.OfType<DropDownList>().FirstOrDefault();

        //            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //            {
        //                string selectionId = itemselectionid;
        //                string LotNumber = itemlotnum;
        //                string store = ddStore.SelectedItem.Text;
        //                string Quantity = itemqty.ToString();

        //                string key = $"{selectionId}|{LotNumber}|{store}|{Quantity}";
        //                if (!SentKeys.Contains(key))
        //                {
        //                    int Lid = Convert.ToInt32(lineID);
        //                    long ItemID = Convert.ToInt64(selectionId);
        //                    var total = (from h in _db.WorksOrderHeaders
        //                                           join l in _db.WorksOrderLines on h.ID equals l.WOID
        //                                           join r in _db.WorksOrderRMLines on h.ID equals r.WOID
        //                                           where h.CompanyID == CurrentUser.CoID
        //                                           && l.LineID == Lid && l.SelectionId == ItemID
        //                                            select r.UseQty + r.ScrapQty
        //                                            ).FirstOrDefault();
        //                    // add finished item to stock
        //                    string RetStr = DoItemAdjustment(Convert.ToInt64(selectionId), LotNumber, store, itemqty, 0, "H", (decimal)total/ itemqty);  // "H" denoted works order finished good


        //                    if (RetStr != "OK")
        //                    {
        //                        AlertHelper.ShowSweetAlert(this, "Error 1661 performing Item Adjustmnent in Data Fusion: " + RetStr, "error");
        //                        return;
        //                    }
        //                    SentKeys.Add(key);
        //                }

        //                // PROCESS GRIDVIEW ROWS FOR API CALLS
        //                foreach (Control control in pane.ContentContainer.Controls)
        //                {
        //                    if (control is GridView grid)
        //                    {
        //                        foreach (GridViewRow row in grid.Rows)
        //                        {
        //                            if (row.RowType == DataControlRowType.DataRow)
        //                            {
        //                                TextBox txtUseQty = row.FindControl("txtUseQty") as TextBox;
        //                                TextBox txtScrapQty = row.FindControl("txtScrapQty") as TextBox;
        //                                DropDownList DDStore = row.FindControl("DDStore") as DropDownList;
        //                                DropDownList DDlotNum = row.FindControl("DDlotNum") as DropDownList;

        //                                var lineIDText = row.Cells[0].Text;
        //                                int TLineID = Convert.ToInt32(lineIDText);

        //                                decimal useQty = 0;
        //                                decimal scrapQty = 0;
        //                                try { useQty = Convert.ToDecimal(txtUseQty.Text); } catch { }
        //                                try { scrapQty = Convert.ToDecimal(txtScrapQty.Text); } catch { }

        //                                string gridSelectionId = row.Cells[1].Text; // SelectionId from second column
        //                                string gridLotNumber = "";
        //                                var WRMLine = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LineID == TLineID).FirstOrDefault();
        //                                if (CurrentUser.CompanyUseLotNumbers)
        //                                {     
        //                                        if (WRMLine != null && WRMLine.IsLotTracked)
        //                                        {
        //                                            if (DDlotNum.SelectedItem != null || DDlotNum.Items.Count != 0 || !string.IsNullOrEmpty(DDlotNum.SelectedItem.Text) || !DDlotNum.SelectedItem.Text.Contains("Number"))
        //                                            {
        //                                                gridLotNumber = DDlotNum.SelectedValue.ToString();
        //                                            }
        //                                        }

        //                                }
        //                                string gridStore = DDStore.SelectedItem.Text;

        //                                // Process usage quantity
        //                                if (useQty > 0)
        //                                {
        //                                    string usageKey = $"{gridSelectionId}|{gridLotNumber}|{gridStore}|{useQty}";
        //                                    if (!SentKeys.Contains(usageKey))
        //                                    {
        //                                        string RetStr = DoItemAdjustment(Convert.ToInt64(gridSelectionId), gridLotNumber, gridStore, useQty * -1, 0, "L", (decimal)WRMLine.UnitCost); // "L" denoted works order component
        //                                        if (RetStr != "OK")
        //                                        {
        //                                            AlertHelper.ShowSweetAlert(this, "Error performing Item Adjustment for usage: " + RetStr, "error");
        //                                            return;
        //                                        }
        //                                        SentKeys.Add(usageKey);
        //                                    }
        //                                }

        //                                // Process scrap quantity
        //                                if (scrapQty > 0)
        //                                {
        //                                    string scrapKey = $"{gridSelectionId}|{gridLotNumber}|{gridStore}|{scrapQty}";
        //                                    if (!SentKeys.Contains(scrapKey))
        //                                    {
        //                                        string RetStr = DoItemAdjustment(Convert.ToInt64(gridSelectionId), gridLotNumber, gridStore, 0, scrapQty * -1, "L", (decimal)WRMLine.UnitCost);  // "L" denoted works order component
        //                                        if (RetStr != "OK")
        //                                        {
        //                                            AlertHelper.ShowSweetAlert(this, "Error performing Item Adjustment for scrap: " + RetStr, "error");
        //                                            return;
        //                                        }
        //                                        SentKeys.Add(scrapKey);
        //                                    }
        //                                }
        //                            }
        //                        }
        //                    }
        //                    else  if (control is Table AddCostTbl)
        //                    {
        //                        string str = "";
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    // ONLY AFTER ALL API CALLS SUCCEED: Update local DB completion status
        //    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //    {
        //        // Update Works Order Lines completion status
        //        for (int i = 1; i < AccordionWOLines.Panes.Count; i++)
        //        {
        //            AccordionPane pane = AccordionWOLines.Panes[i];
        //            HiddenField hiddenFieldH = (HiddenField)pane.HeaderContainer.FindControl("HiddenLineIDH");
        //            if (hiddenFieldH != null)
        //            {
        //                string lineID = hiddenFieldH.Value.Split('|')[0];
        //                decimal itemqty = Convert.ToDecimal(hiddenFieldH.Value.Split('|')[3]);

        //                long lined = Convert.ToInt32(lineID);
        //                var wolP = _db.WorksOrderLines.Where(x => x.CompanyID == CurrentUser.CoID && x.LineID == lined).FirstOrDefault();
        //                wolP.UseQty = itemqty;
        //                wolP.Active = false;
        //                wolP.Complete = true;
        //                wolP.CompleteDate = DateTime.Now;
        //                wolP.CompleteBy = CurrentUser.RoleID;
        //            }
        //        }
        //        _db.SaveChanges();

        //        // Mark Works Order Header as complete
        //        long wonum = Convert.ToInt64(lblwoid.Text);
        //        var woh = _db.WorksOrderHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.WONum == wonum).FirstOrDefault();
        //        woh.Active = false;
        //        woh.WOrderCloseOffDate = DateTime.Now;
        //        woh.WOrderCloseBy = CurrentUser.RoleID.ToString();
        //        _db.SaveChanges();
        //    }

        //    LbtnUpdateWO.Style.Add("display", "none");
        //    LbtnSaveWO.Style.Add("display", "none");
        //    AlertHelper.ShowSweetAlert(this, "Successfully Saved", "success");
        //    return;
        //}

        //protected async Task<string> ExtractAccordionHeaderDetails(object sender)
        //{
        //    bool cont = true;
        //    for (int i = 0; i < AccordionWOLines.Panes.Count; i++)
        //    {
        //        AccordionPane pane = AccordionWOLines.Panes[i];
        //        // ---------- header controls ----------``

        //        // Find HiddenLineIDH by searching through controls since it has dynamic ID
        //        HiddenField hiddenFieldH = null;
        //        foreach (Control control in pane.HeaderContainer.Controls)
        //        {
        //            if (control is HiddenField && control.ID != null && control.ID.StartsWith("HiddenLineIDH_"))
        //            {
        //                hiddenFieldH = control as HiddenField;
        //                break;
        //            }
        //        }

        //        if (hiddenFieldH != null)
        //        {
        //            string lineID = hiddenFieldH.Value.Split('|')[0];
        //            string itemselectionid = hiddenFieldH.Value.Split('|')[1];
        //            long ItemSelection = Convert.ToInt64(itemselectionid);
        //            decimal itemqty = Convert.ToDecimal(hiddenFieldH.Value.Split('|')[3]);
        //            string itemlotnum = string.Empty;

        //            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //            {
        //                var WOline = _db.WorksOrderLines.Where(x => x.CompanyID == CurrentUser.CoID && x.WOID == woid && x.SelectionId == ItemSelection).FirstOrDefault();
        //                if (CurrentUser.CompanyUseLotNumbers && WOline.IsLotTracked)
        //                {
        //                    if (hiddenFieldH.Value.Split('|')[4].Length > 0)
        //                    {
        //                        itemlotnum = hiddenFieldH.Value.Split('|')[4];
        //                    }
        //                    if (itemlotnum == null || itemlotnum == "")
        //                    {
        //                        cont = false;
        //                        string msg = $"Lot Number required for {WOline.ItemCode}";
        //                        return msg;
        //                    }
        //                }
        //            }

        //            DropDownList ddStore = pane.HeaderContainer.Controls.OfType<DropDownList>().FirstOrDefault();
        //            if (ddStore.Items.Count > 1 && ddStore.SelectedIndex < 1)
        //            {
        //                cont = false;
        //                string msg = "Please select a destination store for the item.";                     
        //                return msg;
        //            }

        //            CheckBox chkComplete = pane.HeaderContainer.Controls.OfType<CheckBox>().FirstOrDefault();
        //            bool isComplete = chkComplete != null ? chkComplete.Checked : false;
        //            if (isComplete == false)
        //            {
        //                cont = false;
        //                string msg = "Unable to update Sage, please mark the Works Order Header as complete.";
        //                //AlertHelper.ShowSweetAlert(this, "", "error");
        //                return msg;
        //            }

        //            // PROCESS GRIDVIEW ROWS FOR VALIDATION
        //            foreach (Control control in pane.ContentContainer.Controls)
        //            {
        //                if (control is GridView grid)
        //                {
        //                    foreach (GridViewRow row in grid.Rows)
        //                    {
        //                        if (row.RowType == DataControlRowType.DataRow)
        //                        {
        //                            TextBox txtUseQty = row.FindControl("txtUseQty") as TextBox;
        //                            TextBox txtScrapQty = row.FindControl("txtScrapQty") as TextBox;
        //                            DropDownList DDStore = row.FindControl("DDStore") as DropDownList;
        //                            DropDownList DDlotNum = row.FindControl("DDlotNum") as DropDownList;

        //                            if (DDStore.SelectedItem != null)
        //                            {
        //                                if (DDStore.SelectedItem.ToString() == "-?-")
        //                                {
        //                                    cont = false;
        //                                    string msg = $"Invalid store selected in BOM items for {row.Cells[2].Text.ToString()}";
        //                                    //AlertHelper.ShowSweetAlert(this, $"Invalid store selected in BOM items for {row.Cells[2].Text.ToString()} . Unable to continue.", "error");
        //                                    return msg;
        //                                }
        //                            }
        //                            decimal useQty = 0;
        //                            decimal scrapQty = 0;
        //                            try { useQty = Convert.ToDecimal(txtUseQty.Text); } catch { }
        //                            try { scrapQty = Convert.ToDecimal(txtScrapQty.Text); } catch { }

        //                            if (useQty == 0 && scrapQty == 0)
        //                            {
        //                                cont = false;
        //                                string msg = $"Invalid quantity for {row.Cells[2].Text.ToString()}";
        //                                //AlertHelper.ShowSweetAlert(this, $"Invalid quantity for {row.Cells[2].Text.ToString()}. Unable to continue.", "error");
        //                                return msg;
        //                            }

        //                            if (CurrentUser.CompanyUseLotNumbers)
        //                            {
        //                                var lineIDText = row.Cells[0].Text;
        //                                int TLineID = Convert.ToInt32(lineIDText);
        //                                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //                                {
        //                                    var WRMLine = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LineID == TLineID).FirstOrDefault();
        //                                    if (WRMLine != null && WRMLine.IsLotTracked)
        //                                    {
        //                                        if (DDlotNum.SelectedItem == null || DDlotNum.Items.Count == 0 || string.IsNullOrEmpty(DDlotNum.SelectedItem.Text) || DDlotNum.SelectedItem.Text.Contains("Number"))
        //                                        {
        //                                            cont = false;
        //                                            string msg = $"Lot Number required for {WRMLine.ItemCode}";
        //                                            //AlertHelper.ShowSweetAlert(this, $"Lot Number required for {WRMLine.ItemCode}. Unable to continue.", "error");
        //                                            return msg;
        //                                        }
        //                                    }
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //                else if (control is Table tbl)
        //                {
        //                    // check if update needed and also if 
        //                    if (CurrentUser.ShowManfCosts)
        //                    {
        //                        // Search through the table for txtNewSell with the specific lineID
        //                        TextBox txtNewSell = FindControlInTable<TextBox>(tbl, $"txtNewSell_{lineID}");
        //                        RadioButtonList ddlUpdate = FindControlInTable<RadioButtonList>(tbl, $"ddlUpdate_{lineID}");

        //                        if (ddlUpdate != null && ddlUpdate.SelectedItem != null)
        //                        {
        //                            string updateChoice = ddlUpdate.SelectedValue; // "Yes" or "No"
        //                            // Process the RadioButtonList selection
        //                            if (updateChoice == "Yes")
        //                            {
        //                                if (txtNewSell != null)
        //                                {
        //                                    string newSellingPrice = txtNewSell.Text; //

        //                                    // Do something with the value
        //                                    if (!string.IsNullOrEmpty(newSellingPrice))
        //                                    {
        //                                        decimal newPrice = 0m;
        //                                        if (decimal.TryParse(newSellingPrice, out newPrice))
        //                                        {
        //                                            if (newPrice == 0m)
        //                                            {
        //                                                string msg = "Invalid new Sage selling price -unable to continue.";
        //                                                //AlertHelper.ShowSweetAlert(this, "Invalid new Sage selling price - unable to continue.", "error");
        //                                                return msg;
        //                                            }
        //                                        }
        //                                    }
        //                                    else
        //                                    {
        //                                        string msg = "Invalid new Sage selling price -unable to continue.";
        //                                        //AlertHelper.ShowSweetAlert(this, "Invalid new Sage selling price - unable to continue.", "error");
        //                                        return msg;
        //                                    }
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    if (cont == false) return "OK";

        //    // FIRST: Process all API calls
        //    for (int i = 0; i < AccordionWOLines.Panes.Count; i++)
        //    {
        //        long ItemID = 0;
        //         AccordionPane pane = AccordionWOLines.Panes[i];
        //        // Find HiddenLineIDH by searching through controls since it has dynamic ID

        //        HiddenField hiddenFieldH = null;
        //        foreach (Control control in pane.HeaderContainer.Controls)
        //        {
        //            if (control is HiddenField && control.ID != null && control.ID.StartsWith("HiddenLineIDH_"))
        //            {
        //                hiddenFieldH = control as HiddenField;
        //                break;
        //            }
        //        }

        //        if (hiddenFieldH != null)
        //        {
        //            string lineID = hiddenFieldH.Value.Split('|')[0];
        //            string itemselectionid = hiddenFieldH.Value.Split('|')[1];
        //            decimal itemqty = Convert.ToDecimal(hiddenFieldH.Value.Split('|')[3]);
        //            string itemlotnum = string.Empty;
        //            if (hiddenFieldH.Value.Split('|')[4].Length > 0)
        //            {
        //                itemlotnum = hiddenFieldH.Value.Split('|')[4];
        //            }

        //            decimal thisunitcost = 0;
        //            string targetControlId = $"lblThisCost_{lineID}";

        //            // Search through the ContentContainer for Tables
        //            foreach (Control control in pane.ContentContainer.Controls)
        //            {
        //                if (control is Table tbl)
        //                {
        //                    // Iterate through all rows and cells in the table
        //                    foreach (TableRow row in tbl.Rows)
        //                    {
        //                        foreach (TableCell cell in row.Cells)
        //                        {
        //                            // Look for the label in the cell's controls
        //                            foreach (Control cellControl in cell.Controls)
        //                            {
        //                                if (cellControl is HiddenField hf && hf.ID != null && hf.ID.StartsWith("HiddenTotalCost"))
        //                                {
        //                                    thisunitcost = !string.IsNullOrEmpty(hf.Value) ? Convert.ToDecimal(hf.Value) : 0;
        //                                    break;
        //                                }
        //                            }

        //                            if (thisunitcost != 0) break;
        //                        }

        //                        if (thisunitcost != 0) break;
        //                    }

        //                    if (thisunitcost != 0) break;
        //                }
        //            }

        //            DropDownList ddStore = pane.HeaderContainer.Controls.OfType<DropDownList>().FirstOrDefault();
        //            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //            {
        //                string selectionId = itemselectionid;
        //                string LotNumber = itemlotnum;
        //                string store = ddStore.SelectedItem.Text;
        //                string Quantity = itemqty.ToString();

        //                string key = $"{selectionId}|{LotNumber}|{store}|{Quantity}";
        //                if (!SentKeys.Contains(key))
        //                {
        //                    int Lid = Convert.ToInt32(lineID);
        //                    ItemID = Convert.ToInt64(selectionId);


        //                    string RetStr = DoItemAdjustment(Convert.ToInt64(selectionId), LotNumber, store, itemqty, 0, "H", thisunitcost);  // "H" denoted works order finished good

        //                    if (RetStr != "OK")
        //                    {
        //                        string msg = $"Error 1661 performing Item Adjustmnent in Data Fusion: {RetStr}";
        //                        //AlertHelper.ShowSweetAlert(this, "Invalid new Sage selling price - unable to continue.", "error");
        //                        return msg;
        //                    }
        //                    SentKeys.Add(key);
        //                }

        //                // PROCESS GRIDVIEW ROWS FOR API CALLS
        //                foreach (Control control in pane.ContentContainer.Controls)
        //                {
        //                    if (control is GridView grid)
        //                    {
        //                        foreach (GridViewRow row in grid.Rows)
        //                        {
        //                            if (row.RowType == DataControlRowType.DataRow)
        //                            {
        //                                TextBox txtUseQty = row.FindControl("txtUseQty") as TextBox;
        //                                TextBox txtScrapQty = row.FindControl("txtScrapQty") as TextBox;
        //                                DropDownList DDStore = row.FindControl("DDStore") as DropDownList;
        //                                DropDownList DDlotNum = row.FindControl("DDlotNum") as DropDownList;

        //                                var lineIDText = row.Cells[0].Text;
        //                                int TLineID = Convert.ToInt32(lineIDText);

        //                                decimal useQty = 0;
        //                                decimal scrapQty = 0;
        //                                try { useQty = Convert.ToDecimal(txtUseQty.Text); } catch { }
        //                                try { scrapQty = Convert.ToDecimal(txtScrapQty.Text); } catch { }

        //                                string gridSelectionId = row.Cells[1].Text; // SelectionId from second column
        //                                string gridLotNumber = "";
        //                                var WRMLine = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LineID == TLineID).FirstOrDefault();
        //                                if (CurrentUser.CompanyUseLotNumbers)
        //                                {
        //                                    if (WRMLine != null && WRMLine.IsLotTracked)
        //                                    {
        //                                        if (DDlotNum.SelectedItem != null || DDlotNum.Items.Count != 0 || !string.IsNullOrEmpty(DDlotNum.SelectedItem.Text) || !DDlotNum.SelectedItem.Text.Contains("Number"))
        //                                        {
        //                                            gridLotNumber = DDlotNum.SelectedValue.ToString();
        //                                        }
        //                                    }
        //                                }
        //                                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                if (DDStore.SelectedItem != null)
        //                                {
        //                                    string gridStore = DDStore.SelectedItem.Text;

        //                                    // Process usage quantity
        //                                    if (useQty > 0)
        //                                    {
        //                                        string usageKey = $"{gridSelectionId}|{gridLotNumber}|{gridStore}|{useQty}";
        //                                        if (!SentKeys.Contains(usageKey))
        //                                        {
        //                                            string RetStr = DoItemAdjustment(Convert.ToInt64(gridSelectionId), gridLotNumber, gridStore, useQty * -1, 0, "L", (decimal)WRMLine.UnitCost); // "L" denoted works order component
        //                                            if (RetStr != "OK")
        //                                            {
        //                                                string msg = $"Error performing Item Adjustment for usage: {RetStr}";
        //                                                //AlertHelper.ShowSweetAlert(this, , "error");
        //                                                return msg;
        //                                            }
        //                                            SentKeys.Add(usageKey);
        //                                        }
        //                                    }

        //                                    // Process scrap quantity
        //                                    if (scrapQty > 0)
        //                                    {
        //                                        string scrapKey = $"{gridSelectionId}|{gridLotNumber}|{gridStore}|{scrapQty}";
        //                                        if (!SentKeys.Contains(scrapKey))
        //                                        {
        //                                            string RetStr = DoItemAdjustment(Convert.ToInt64(gridSelectionId), gridLotNumber, gridStore, 0, scrapQty * -1, "L", (decimal)WRMLine.UnitCost);  // "L" denoted works order component
        //                                            if (RetStr != "OK")
        //                                            {
        //                                                string msg = $"Error performing Item Adjustment for scrap: {RetStr}";
        //                                                //AlertHelper.ShowSweetAlert(this, "Error performing Item Adjustment for scrap: " + RetStr, "error");
        //                                                return msg;
        //                                            }
        //                                            SentKeys.Add(scrapKey);
        //                                        }
        //                                    }
        //                                }
        //                                //////////////////////////////////////////////////////////////////
        //                            }
        //                        }
        //                    }
        //                    else if (control is Table tbl)
        //                    {
        //                        // Search through the table for txtNewSell with the specific lineID
        //                        TextBox txtNewSell = FindControlInTable<TextBox>(tbl, $"txtNewSell_{lineID}");
        //                        RadioButtonList ddlUpdate = FindControlInTable<RadioButtonList>(tbl, $"ddlUpdate_{lineID}");

        //                        if (ddlUpdate != null && ddlUpdate.SelectedItem != null)
        //                        {
        //                            string updateChoice = ddlUpdate.SelectedValue;
        //                            // Process the RadioButtonList selection
        //                            if (updateChoice == "Yes")
        //                            {
        //                                if (txtNewSell != null)
        //                                {
        //                                    string newSellingPrice = txtNewSell.Text; // <-- HERE - extract the value
        //                                    if (!string.IsNullOrEmpty(newSellingPrice))
        //                                    {
        //                                        decimal newPrice = 0;
        //                                        if (decimal.TryParse(newSellingPrice, out newPrice))
        //                                        {
        //                                            ApiUrlCall Api = new ApiUrlCall();
        //                                            // NEED TO GET THE ITEM ID FROM THE WORKS ORDER LINE
        //                                            var updateTask = Api.UpdateSellingPriceOneItem(ItemID, newPrice, CurrentUser);
        //                                            string result = await updateTask.ConfigureAwait(false);
        //                                            if (result == "OK") {
        //                                                return result;
        //                                            }
        //                                            else { return result; }
        //                                        }
        //                                    }
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    // ONLY AFTER ALL API CALLS SUCCEED: Update local DB completion status
        //    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //    {
        //        // Update Works Order Lines completion status
        //        for (int i = 1; i < AccordionWOLines.Panes.Count; i++)
        //        {
        //            AccordionPane pane = AccordionWOLines.Panes[i];

        //            // Find HiddenLineIDH by searching through controls since it has dynamic ID
        //            HiddenField hiddenFieldH = null;
        //            foreach (Control control in pane.HeaderContainer.Controls)
        //            {
        //                if (control is HiddenField && control.ID != null && control.ID.StartsWith("HiddenLineIDH_"))
        //                {
        //                    hiddenFieldH = control as HiddenField;
        //                    break;
        //                }
        //            }

        //            if (hiddenFieldH != null)
        //            {
        //                string lineID = hiddenFieldH.Value.Split('|')[0];
        //                decimal itemqty = Convert.ToDecimal(hiddenFieldH.Value.Split('|')[3]);

        //                long lined = Convert.ToInt32(lineID);
        //                var wolP = _db.WorksOrderLines.Where(x => x.CompanyID == CurrentUser.CoID && x.LineID == lined).FirstOrDefault();
        //                wolP.UseQty = itemqty;
        //                wolP.Active = false;
        //                wolP.Complete = true;
        //                wolP.CompleteDate = DateTime.Now;
        //                wolP.CompleteBy = CurrentUser.RoleID;
        //            }
        //        }
        //        _db.SaveChanges();

        //        // Mark Works Order Header as complete
        //        long wonum = Convert.ToInt64(lblwoid.Text);
        //        var woh = _db.WorksOrderHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.WONum == wonum).FirstOrDefault();
        //        woh.Active = false;
        //        woh.WOrderCloseOffDate = DateTime.Now;
        //        woh.WOrderCloseBy = CurrentUser.RoleID.ToString();
        //        _db.SaveChanges();
        //    }

        //    LbtnUpdateWO.Style.Add("display", "none");
        //    LbtnSaveWO.Style.Add("display", "none");
        //    //AlertHelper.ShowSweetAlert(this, "Successfully Saved", "success");
        //    return "OK";
        //}

        protected async Task<string> ExtractAccordionHeaderDetails(object sender)
        {
            bool cont = true;

            // Only the lines the operator ticked Complete on this run are manufactured.
            // Already-completed (locked) lines and untouched balance lines are skipped,
            // which is what lets a part manufacture leave the outstanding balance open.
            int selectedCount = 0;
            for (int s = 0; s < AccordionWOLines.Panes.Count; s++)
            {
                if (PaneSelectedForManufacture(AccordionWOLines.Panes[s])) selectedCount++;
            }
            if (selectedCount == 0)
            {
                return "Please tick Complete on the line(s) you want to manufacture before transferring.";
            }

            for (int i = 0; i < AccordionWOLines.Panes.Count; i++)
            {
                AccordionPane pane = AccordionWOLines.Panes[i];
                if (!PaneSelectedForManufacture(pane)) continue;

                HiddenField hiddenFieldH = null;
                foreach (Control control in pane.HeaderContainer.Controls)
                {
                    if (control is HiddenField && control.ID != null && control.ID.StartsWith("HiddenLineIDH_"))
                    {
                        hiddenFieldH = control as HiddenField;
                        break;
                    }
                }

                if (hiddenFieldH != null)
                {
                    string lineID = hiddenFieldH.Value.Split('|')[0];
                    string itemselectionid = hiddenFieldH.Value.Split('|')[1];
                    int thisLineId = Convert.ToInt32(lineID);
                    decimal itemqty = Convert.ToDecimal(hiddenFieldH.Value.Split('|')[3]);
                    string itemlotnum = string.Empty;

                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        // Look up by LineID (not SelectionId): after a part-manufacture
                        // split the same item can appear on more than one line.
                        var WOline = _db.WorksOrderLines.Where(x => x.CompanyID == CurrentUser.CoID && x.LineID == thisLineId).FirstOrDefault();
                        if (CurrentUser.CompanyUseLotNumbers && WOline.IsLotTracked)
                        {
                            if (hiddenFieldH.Value.Split('|')[4].Length > 0)
                            {
                                itemlotnum = hiddenFieldH.Value.Split('|')[4];
                            }
                            if (itemlotnum == null || itemlotnum == "")
                            {
                                cont = false;
                                string msg = $"Lot Number required for {WOline.ItemCode}";
                                return msg;
                            }
                        }
                    }

                    DropDownList ddStore = pane.HeaderContainer.Controls.OfType<DropDownList>().FirstOrDefault();
                    if (ddStore.Items.Count > 1 && ddStore.SelectedIndex < 1)
                    {
                        cont = false;
                        string msg = "Please select a destination store for the item.";
                        return msg;
                    }
                    // Completion selection is handled by PaneSelectedForManufacture skip above.

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

                                    if (DDStore.SelectedItem != null)
                                    {
                                        if (DDStore.SelectedItem.ToString() == "-?-")
                                        {
                                            cont = false;
                                            string msg = $"Invalid store selected in BOM items for {row.Cells[2].Text.ToString()}";
                                            return msg;
                                        }
                                    }
                                    decimal useQty = 0;
                                    decimal scrapQty = 0;
                                    try { useQty = Convert.ToDecimal(txtUseQty.Text); } catch { }
                                    try { scrapQty = Convert.ToDecimal(txtScrapQty.Text); } catch { }

                                    if (useQty == 0 && scrapQty == 0)
                                    {
                                        cont = false;
                                        string msg = $"Invalid quantity for {row.Cells[2].Text.ToString()}";
                                        return msg;
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
                                                if (DDlotNum.SelectedItem == null || DDlotNum.Items.Count == 0 || string.IsNullOrEmpty(DDlotNum.SelectedItem.Text) || DDlotNum.SelectedItem.Text.Contains("Number"))
                                                {
                                                    cont = false;
                                                    string msg = $"Lot Number required for {WRMLine.ItemCode}";
                                                    return msg;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (control is Table tbl)
                        {
                            if (CurrentUser.ShowManfCosts)
                            {
                                TextBox txtNewSell = FindControlInTable<TextBox>(tbl, $"txtNewSell_{lineID}");
                                RadioButtonList ddlUpdate = FindControlInTable<RadioButtonList>(tbl, $"ddlUpdate_{lineID}");

                                if (ddlUpdate != null && ddlUpdate.SelectedItem != null)
                                {
                                    string updateChoice = ddlUpdate.SelectedValue;
                                    if (updateChoice == "Yes")
                                    {
                                        if (txtNewSell != null)
                                        {
                                            string newSellingPrice = txtNewSell.Text;
                                            if (!string.IsNullOrEmpty(newSellingPrice))
                                            {
                                                decimal newPrice = 0m;
                                                if (decimal.TryParse(newSellingPrice, out newPrice))
                                                {
                                                    if (newPrice == 0m)
                                                    {
                                                        string msg = "Invalid new Sage selling price -unable to continue.";
                                                        return msg;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                string msg = "Invalid new Sage selling price -unable to continue.";
                                                return msg;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (cont == false) return "OK";

            // FIRST: Process all API calls
            for (int i = 0; i < AccordionWOLines.Panes.Count; i++)
            {
                // FIX 1: Always assign ItemID here so it is set before UpdateSellingPriceOneItem is called
                long ItemID = 0;
                AccordionPane pane = AccordionWOLines.Panes[i];
                if (!PaneSelectedForManufacture(pane)) continue;

                HiddenField hiddenFieldH = null;
                foreach (Control control in pane.HeaderContainer.Controls)
                {
                    if (control is HiddenField && control.ID != null && control.ID.StartsWith("HiddenLineIDH_"))
                    {
                        hiddenFieldH = control as HiddenField;
                        break;
                    }
                }

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

                    // Guard: never re-post a line that has already been manufactured. SentKeys
                    // only dedupes within one page life, so a WO re-opened later could otherwise
                    // draw the same components to Sage again. A completed line is skipped entirely
                    // (its FG produce + RM draws); open balance lines are unaffected.
                    int lineGuardId = Convert.ToInt32(lineID);
                    using (SBMSEntities _dbLineGuard = new SBMSEntities(Config.GetConnectionString()))
                    {
                        var lineGuard = _dbLineGuard.WorksOrderLines
                            .FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.LineID == lineGuardId);
                        if (lineGuard == null || lineGuard.Complete == true || lineGuard.Active == false)
                            continue;
                    }

                    // FIX 2: Always assign ItemID outside the SentKeys check
                    ItemID = Convert.ToInt64(itemselectionid);

                    decimal thistotcost = 0;

                    // FIX 3: Read from HiddenTotalCost hidden field instead of lblThisCost label.
                    //        The label value is set by JavaScript and resets to "0.00" on every postback.
                    //        The hidden field survives postback via the POST form data.
                    foreach (Control control in pane.ContentContainer.Controls)
                    {
                        if (control is Table tbl)
                        {
                            foreach (TableRow trow in tbl.Rows)
                            {
                                foreach (TableCell cell in trow.Cells)
                                {
                                    foreach (Control cellControl in cell.Controls)
                                    {
                                        if (cellControl is HiddenField hf && hf.ID != null && hf.ID.StartsWith("HiddenTotalCost"))
                                        {
                                            thistotcost = !string.IsNullOrEmpty(hf.Value) ? Convert.ToDecimal(hf.Value) : 0;
                                            break;
                                        }
                                    }
                                    if (thistotcost != 0) break;
                                }
                                if (thistotcost != 0) break;
                            }
                            if (thistotcost != 0) break;
                        }
                    }

                    DropDownList ddStore = pane.HeaderContainer.Controls.OfType<DropDownList>().FirstOrDefault();
                    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        string selectionId = itemselectionid;
                        string LotNumber = itemlotnum;
                        string store = string.Empty;
                        if (ddStore.SelectedItem != null) store = ddStore.SelectedItem.Text;
                        string Quantity = itemqty.ToString();

                        // Compute the finished-good cost SERVER-SIDE from the actual value of the
                        // components about to be drawn - each at its draw store's weighted average,
                        // exactly as DoItemAdjustment posts the "L" draws below - so the value produced
                        // into the FG equals the value removed from raw materials. The client-JS
                        // HiddenTotalCost used a 2-dp displayed cost that could diverge from the store
                        // average, and was 0 whenever the script had not run (posting the FG at zero cost).
                        // Draws are outbound and never move a store's average, so these read-only lookups
                        // match the values the draws will actually use.
                        decimal serverTotCost = 0;
                        foreach (Control ctrlPre in pane.ContentContainer.Controls)
                        {
                            if (ctrlPre is GridView gridPre)
                            {
                                foreach (GridViewRow rowPre in gridPre.Rows)
                                {
                                    if (rowPre.RowType != DataControlRowType.DataRow) continue;
                                    TextBox txtUseQtyPre = rowPre.FindControl("txtUseQty") as TextBox;
                                    DropDownList ddStorePre = rowPre.FindControl("DDStore") as DropDownList;
                                    decimal useQtyPre = 0;
                                    try { useQtyPre = Convert.ToDecimal(txtUseQtyPre.Text); } catch { }
                                    if (useQtyPre <= 0 || ddStorePre?.SelectedItem == null) continue;
                                    long rmItemIdPre = Convert.ToInt64(rowPre.Cells[1].Text);
                                    int rmLineIdPre = Convert.ToInt32(rowPre.Cells[0].Text);
                                    long storeIdPre = _db.Stores.Where(x => x.CompanyID == CoID && x.StoreCode == ddStorePre.SelectedItem.Text).Select(x => x.StoreID).FirstOrDefault();
                                    decimal drawAvgPre = StoreCosting.GetStoreAvgCost(_db, CoID, rmItemIdPre, storeIdPre);
                                    if (drawAvgPre <= 0)
                                    {
                                        var wrmPre = _db.WorksOrderRMLines.FirstOrDefault(x => x.CompanyID == CoID && x.LineID == rmLineIdPre);
                                        drawAvgPre = (decimal)(wrmPre != null ? wrmPre.UnitCost : 0);
                                    }
                                    serverTotCost += useQtyPre * drawAvgPre;
                                }
                            }
                        }

                        // Key the dedup on the WorksOrderLine LineID (+ a type tag), not just the
                        // item/qty values. Two batches of an equal part-manufacture split (e.g. 10 -> 5+5)
                        // have identical item/store/qty, so a value-only key made the second batch collide
                        // with the first and silently skip its FG produce + component draws.
                        string key = $"H|{lineID}|{selectionId}|{LotNumber}|{store}|{Quantity}";
                        if (TryClaimManfAdj(key))   // durable claim; false = already posted (reload / concurrent) -> skip
                        {
                            int Lid = Convert.ToInt32(lineID);
                            // Prefer the server-computed material value; fall back to the JS total only if
                            // it couldn't be computed (e.g. no store history and no RM unit cost).
                            decimal fgTotCost = serverTotCost > 0 ? serverTotCost : thistotcost;
                            decimal thisunitcost = fgTotCost != 0 && itemqty != 0 ? fgTotCost / itemqty : 0;
                            string RetStr = DoItemAdjustment(Convert.ToInt64(selectionId), LotNumber, store, itemqty, 0, "H", thisunitcost);
                            if (RetStr != "OK")
                            {
                                ReleaseManfAdj(key);   // Sage post failed -> release so a retry can redo it
                                string msg = $"Error 1661 performing Item Adjustmnent in Data Fusion: {RetStr}";
                                return msg;
                            }
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

                                        string gridSelectionId = row.Cells[1].Text;
                                        string gridLotNumber = "";
                                        var WRMLine = _db.WorksOrderRMLines.Where(x => x.CompanyID == CoID && x.LineID == TLineID).FirstOrDefault();

                                        // FIX 4: Corrected || to && to prevent NullReferenceException on SelectedItem.Text
                                        if (CurrentUser.CompanyUseLotNumbers)
                                        {
                                            if (WRMLine != null && WRMLine.IsLotTracked)
                                            {
                                                if (DDlotNum.SelectedItem != null &&
                                                    DDlotNum.Items.Count != 0 &&
                                                    !string.IsNullOrEmpty(DDlotNum.SelectedItem.Text) &&
                                                    !DDlotNum.SelectedItem.Text.Contains("Number"))
                                                {
                                                    gridLotNumber = DDlotNum.SelectedValue.ToString();
                                                }
                                            }
                                        }

                                        if (DDStore.SelectedItem != null)
                                        {
                                            string gridStore = DDStore.SelectedItem.Text;

                                            if (useQty > 0)
                                            {
                                                // Key on the RM LineID (+ type tag) so identical component rows
                                                // on two split batches don't collide (see the FG key note above).
                                                string usageKey = $"use|{TLineID}|{gridSelectionId}|{gridLotNumber}|{gridStore}|{useQty}";
                                                if (TryClaimManfAdj(usageKey))
                                                {
                                                    string RetStr = DoItemAdjustment(Convert.ToInt64(gridSelectionId), gridLotNumber, gridStore, useQty * -1, 0, "L", (decimal)WRMLine.UnitCost);
                                                    if (RetStr != "OK")
                                                    {
                                                        ReleaseManfAdj(usageKey);
                                                        string msg = $"Error performing Item Adjustment for usage: {RetStr}";
                                                        return msg;
                                                    }
                                                }
                                            }

                                            if (scrapQty > 0)
                                            {
                                                // Scrap is consumed material: draw it OUT of the source store
                                                // (negative useqty), exactly like usage, so it is removed from
                                                // both the local store balance and Sage at the store average.
                                                // (Passing it as the reject arg posted a zero-qty Sage adjustment
                                                //  that removed no stock but still revalued the item.)
                                                string scrapKey = $"scrap|{TLineID}|{gridSelectionId}|{gridLotNumber}|{gridStore}|{scrapQty}";
                                                if (TryClaimManfAdj(scrapKey))
                                                {
                                                    string RetStr = DoItemAdjustment(Convert.ToInt64(gridSelectionId), gridLotNumber, gridStore, scrapQty * -1, 0, "L", (decimal)WRMLine.UnitCost);
                                                    if (RetStr != "OK")
                                                    {
                                                        ReleaseManfAdj(scrapKey);
                                                        string msg = $"Error performing Item Adjustment for scrap: {RetStr}";
                                                        return msg;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else if (control is Table tbl)
                            {
                                TextBox txtNewSell = FindControlInTable<TextBox>(tbl, $"txtNewSell_{lineID}");
                                RadioButtonList ddlUpdate = FindControlInTable<RadioButtonList>(tbl, $"ddlUpdate_{lineID}");

                                if (ddlUpdate != null && ddlUpdate.SelectedItem != null)
                                {
                                    string updateChoice = ddlUpdate.SelectedValue;
                                    if (updateChoice == "Yes")
                                    {
                                        if (txtNewSell != null)
                                        {
                                            string newSellingPrice = txtNewSell.Text;
                                            if (!string.IsNullOrEmpty(newSellingPrice))
                                            {
                                                decimal newPrice = 0;
                                                if (decimal.TryParse(newSellingPrice, out newPrice))
                                                {
                                                    ApiUrlCall Api = new ApiUrlCall();
                                                    var updateTask = Api.UpdateSellingPriceOneItem(ItemID, newPrice, CurrentUser);
                                                    string result = await updateTask.ConfigureAwait(false);
                                                    // FIX 5: Removed early return "OK" so remaining panes continue processing
                                                    if (result != "OK")
                                                    {
                                                        return result;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Bulletproof: mark THIS line complete in the SAME context as its draws,
                        // so a failure on a later pane can't leave an already-drawn line open and
                        // re-drawable. With the per-line skip guard above, a finished line can never
                        // be re-posted to Sage.
                        var doneLine = _db.WorksOrderLines.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.LineID == lineGuardId);
                        if (doneLine != null && doneLine.Complete != true)
                        {
                            doneLine.UseQty = itemqty;
                            doneLine.Active = false;
                            doneLine.Complete = true;
                            doneLine.CompleteDate = DateTime.Now;
                            doneLine.CompleteBy = CurrentUser.RoleID;
                            _db.SaveChanges();
                        }
                    }
                }
            }

            // ONLY AFTER ALL API CALLS SUCCEED: Update local DB completion status
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Atomic re-check inside the same context — if another caller (different
                // session / second tab / double-click) already flipped Active to false,
                // bail before re-writing line state. The Sage adjustments above have
                // already been deduped by SentKeys, so this is the last safety net.
                long wonumGuard = Convert.ToInt64(lblwoid.Text);
                var headerGuard = _db.WorksOrderHeaders
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.WONum == wonumGuard)
                    .FirstOrDefault();
                if (headerGuard == null)
                {
                    return "Works Order header missing during completion.";
                }
                if (headerGuard.Active == false || headerGuard.Status == "Complete")
                {
                    return "This Works Order was already completed by another process.";
                }

                // FIX 6: Start from index 0 (was 1, silently skipping the first pane's DB update)
                // Only the panes selected for this run are completed; balance lines are left open.
                for (int i = 0; i < AccordionWOLines.Panes.Count; i++)
                {
                    AccordionPane pane = AccordionWOLines.Panes[i];
                    if (!PaneSelectedForManufacture(pane)) continue;

                    HiddenField hiddenFieldH = null;
                    foreach (Control control in pane.HeaderContainer.Controls)
                    {
                        if (control is HiddenField && control.ID != null && control.ID.StartsWith("HiddenLineIDH_"))
                        {
                            hiddenFieldH = control as HiddenField;
                            break;
                        }
                    }

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

                // Only close the works order header once NO open balance line remains.
                // Otherwise leave it Active and flag it as partially manufactured so the
                // operator can return for the next batch.
                bool anyOpen = _db.WorksOrderLines.Any(x => x.CompanyID == CurrentUser.CoID
                                                            && x.WOID == woid
                                                            && x.Active == true
                                                            && x.Complete != true
                                                            && (x.Quantity ?? 0) > 0);

                // headerGuard is already tracked by this context — reuse it.
                if (anyOpen)
                {
                    headerGuard.Status = "Partially Manufactured";
                    // remains Active
                }
                else
                {
                    headerGuard.Status = "Complete";
                    headerGuard.Active = false;
                    headerGuard.WOrderCloseOffDate = DateTime.Now;
                    headerGuard.WOrderCloseBy = CurrentUser.RoleID.ToString();
                }
                _db.SaveChanges();
            }

            return "OK";
        }

        private T FindControlInTable<T>(Table table, string controlID) where T : Control
        {
            foreach (TableRow row in table.Rows)
            {
                foreach (TableCell cell in row.Cells)
                {
                    T found = FindControlInControls<T>(cell.Controls, controlID);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        private T FindControlInControls<T>(ControlCollection controls, string controlID) where T : Control
        {
            foreach (Control control in controls)
            {
                if (control is T && control.ID == controlID)
                {
                    return control as T;
                }

                // Recursively search nested controls
                if (control.HasControls())
                {
                    T found = FindControlInControls<T>(control.Controls, controlID);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
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
            long itmID = Convert.ToInt64(ddlItemCode.SelectedValue);
            decimal itemQty = 0;
            try
            {
                itemQty = Convert.ToDecimal(txtqty.Text);
            }
            catch { 
                AlertHelper.ShowSweetAlert(this, "Invalid Quantity!", "error");
                return; 
                    }

            long woLid = Convert.ToInt64(woLineID.Text);
            if (itemQty != 0)
            {
                SaveNewRowToDatabase(itmID, itemQty, DDItemAddStore.SelectedItem.Text);
            }
            LoadWOLines();
        }

        protected void ddlItemCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            long itmID = Convert.ToInt64(ddlItemCode.SelectedValue);
            LoadItems();
            lblDescript.Text = _items.FirstOrDefault(x => x.ID == itmID).Description;
           
            LoadItemStores();
            var storeList = _itemST.Where(x => x.ItemID == itmID &&
                                               x.StoreCode != "CoR" &&
                                               x.StoreCode != "CoD" &&
                                               x.StoreCode.ToLower() != "scr").ToList();

            DDItemAddStore.DataSource = storeList;
            DDItemAddStore.DataTextField = "StoreCode";
            DDItemAddStore.DataValueField = "StoreID";
            DDItemAddStore.DataBind();

            if (storeList.Count > 1)
            {
                DDItemAddStore.Items.Insert(0, "-?-");
            }
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

        // ── Durable manufacture-adjustment dedup ───────────────────────────────────
        // Replaces the in-Session "SentKeys" HashSet (wiped on every page reload), so a
        // manufacture that posts irreversible Sage adjustments can never post the same
        // one twice - across a reload after a mid-sequence failure, or two operators on
        // the same WO. ManfPostedAdjustments is outside the EF model (raw SQL only);
        // its UNIQUE index is the atomic gate. See SQL/Add_ManfPostedAdjustments.sql.
        //
        // Claim: INSERT the key. true  = WE claimed it -> go post to Sage.
        //                        false = already claimed/posted -> skip.
        // A real (non-unique) DB error rethrows, so a fault never silently skips a post.
        private bool TryClaimManfAdj(string adjKey)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                try
                {
                    _db.Database.ExecuteSqlCommand(
                        "INSERT INTO dbo.ManfPostedAdjustments (CompanyID, WOID, AdjKey, PostedBy) VALUES (@p0, @p1, @p2, @p3)",
                        CurrentUser.CoID, woid, adjKey, CurrentUser.RoleID);
                    return true;
                }
                catch (Exception ex)
                {
                    var sqlEx = ex.GetBaseException() as System.Data.SqlClient.SqlException;
                    if (sqlEx != null && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                        return false;   // unique violation = already claimed/posted -> skip
                    throw;              // any other error -> do NOT silently skip the Sage post
                }
            }
        }

        // Release a claim after a FAILED Sage post so a retry can redo that adjustment.
        private void ReleaseManfAdj(string adjKey)
        {
            try
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    _db.Database.ExecuteSqlCommand(
                        "DELETE FROM dbo.ManfPostedAdjustments WHERE CompanyID = @p0 AND WOID = @p1 AND AdjKey = @p2",
                        CurrentUser.CoID, woid, adjKey);
            }
            catch { }
        }

        protected string DoItemAdjustment(long itmid, string LotNum, string stor, decimal useqty, decimal rejqty, string LineType, decimal unitcost)
        {
            string success = "OK";
            try
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    if (stor != "")
                    {
                        long store1 = _db.Stores.Where(x => x.StoreCode == stor && x.CompanyID == CurrentUser.CoID).Select(x => x.StoreID).FirstOrDefault();

                        // Call API FIRST before any local DB saves
                        ApiUrlCall api = new ApiUrlCall();
                        api.LoadOneItemNA(itmid, CurrentUser);
                        var itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == itmid);

                        // Draws leave at the draw store's running weighted average (outs never
                        // revalue a store). The same number then drives the Sage adjustment and
                        // the local row below — one cost, every side.
                        if (useqty < 0)
                        {
                            decimal drawAvg = StoreCosting.GetStoreAvgCost(_db, CurrentUser.CoID, itmid, store1);
                            if (drawAvg > 0) unitcost = drawAvg;
                        }

                        // TESTING ONLY
                        //decimal CurrentQOH = 3;
                        //decimal SageCurrentAvCost = 166.80m;
                        // END OF TESTING HARD CODED VALUES

                        decimal CurrentQOH = itm.QuantityOnHand ?? 0;
                        decimal SageCurrentAvCost = itm.AverageCost ?? 0;
                        decimal thisUnitCost = unitcost;
                        decimal thisValue = (useqty + rejqty) * unitcost;

                        decimal SageCurrentValue = 0;
                        decimal newAvCost;

                        // A negative on-hand balance has no meaningful stock value to weight
                        // against — multiplying a negative QOH by the average cost gives a
                        // negative current value and throws the weighted average right out.
                        // In that case ignore the existing value and set the new average cost
                        // to the cost of the transaction being run.
                        if (CurrentQOH < 0)
                        {
                            SageCurrentValue = 0;
                            newAvCost = thisUnitCost;
                        }
                        else
                        {
                            if (CurrentQOH > 0)
                            {
                                SageCurrentValue = CurrentQOH * SageCurrentAvCost;
                            }

                            if (SageCurrentValue > 0)
                            {
                                decimal newQty = CurrentQOH + useqty + rejqty;
                                if (newQty > 0)
                                {
                                    newAvCost = (thisValue + SageCurrentValue) / newQty;
                                }
                                else
                                {
                                    // Resulting balance is zero or negative — there is no
                                    // positive quantity to average over, so use the transaction cost.
                                    newAvCost = thisUnitCost;
                                }
                            }
                            else
                            {
                                newAvCost = thisUnitCost;
                            }
                        }

                        ItemAdjustment iAdj = new ItemAdjustment();
                        iAdj.Date = DateTime.Now;
                        iAdj.ItemID = itmid;
                        iAdj.AverageCost = (decimal)newAvCost;
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
                        // Stamp the store's running average after this movement: a DRAW leaves it
                        // unchanged (unitcost already IS the store average); MANF re-blends it.
                        if (useqty < 0)
                        {
                            ItemTrans.StoreAvgCost = unitcost;
                        }
                        else
                        {
                            decimal manfVal;
                            ItemTrans.StoreAvgCost = StoreCosting.ComputeMovement(_db, CurrentUser.CoID, itmid, store1, useqty, unitcost * useqty, out manfVal);
                        }
                        ItemTrans.TransactionReference = ItemTrans.TransactionType + ": WO" + Convert.ToInt64(lblwoid.Text) + " - " + DateTime.Now.ToString() + " Lot:" + LotNum;
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
                            iAdj.AverageCost = (decimal)newAvCost;
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
                            // Inbound to the scrap store re-blends its running average.
                            decimal scrapVal;
                            ItemTrans.StoreAvgCost = StoreCosting.ComputeMovement(_db, CurrentUser.CoID, itmid, store1, rejqty, unitcost * rejqty, out scrapVal);
                            ItemTrans.TransactionReference = "Scrap WO" + Convert.ToInt64(lblwoid.Text) + " - " + DateTime.Now.ToString() + " Lot:" + LotNum;
                            ItemTrans.ExchRate = 1;
                            _db.ItemTransactions.Add(ItemTrans);
                            _db.SaveChanges();
                        }
                        #endregion
                    }
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

        // Place this inside the WorksOrdersManf class, but outside any other method
        private T FindControlRecursive<T>(Control parent, string id) where T : Control
        {
            foreach (Control child in parent.Controls)
            {
                if (child is T && child.ID == id)
                    return (T)child;
                T found = FindControlRecursive<T>(child, id);
                if (found != null)
                    return found;
            }
            return null;
        }

    }
}