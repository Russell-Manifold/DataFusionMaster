using ClosedXML.Excel;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class BOMDetailed : BasePage
    {

        private List<ItemsMaster> _items;
        long bomid = 0;
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            bomid = Convert.ToInt64(Request.QueryString["bomid"].ToString());
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                _items = _db.ItemsMasters.Where(i =>i.Active == true && i.CompanyID == CurrentUser.CoID && (i.IsBOMComponent == true || i.Physical == false)).OrderBy(x => x.Code).ToList();
            }
            if (!IsPostBack)
            {
                //deleteNewUnused();
                LoadBom();
            }
        }

        protected void LoadBom()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {   
                var bomH = _db.BOMHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.BomHID == bomid).FirstOrDefault();
                if (bomH != null)
                {
                    lblFGID.Text = bomH.FGID.ToString();
                    long fgID = Convert.ToInt64(lblFGID.Text);
                    var item = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == fgID);
                    
                    decimal avgCost = item?.AverageCost ?? 0; // Use 0 as default if null
                    lblItemCost.Text = avgCost.ToString("N2");

                    decimal SageSell = item?.PriceExclusive ?? 0; // Use 0 as default if null
                    lblSageSell.Text = SageSell.ToString("N2");

                    decimal GPPerc = item?.GPPercentage ?? 0;
                    txtNewGP.Text = GPPerc.ToString();

                    lblBOMCode.Text = bomH.BOMCode.ToString() ?? "";
                    if (bomH.BomDescript != null) lblBOMDescipt.Text = bomH.BomDescript.ToString() ?? "";
                    if (bomH.FGCode != null) lblFGCode.Text = bomH.FGCode.ToString() ?? "";
                    if (bomH.FGDescript != null) lblFGDescript.Text = bomH.FGDescript.ToString() ?? "";
                    if (bomH.AddCost01 != null) txtAdd1.Text = ApiUrlCall.NumberToDecimal(bomH.AddCost01.ToString() ?? "", CurrentUser.CompanyDecPlaces);
                    if (bomH.AddCost02 != null) txtAdd2.Text = ApiUrlCall.NumberToDecimal(bomH.AddCost02.ToString() ?? "", CurrentUser.CompanyDecPlaces);
                    if (bomH.AddCost03 != null) txtAdd3.Text = ApiUrlCall.NumberToDecimal(bomH.AddCost03.ToString() ?? "", CurrentUser.CompanyDecPlaces);

                    decimal adc1 = 0, adc2 = 0, adc3 = 0;
                    if (txtAdd1.Text.ToString().Trim().Length > 0) adc1 = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(txtAdd1.Text, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
                    if (txtAdd2.Text.ToString().Trim().Length > 0) adc2 = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(txtAdd2.Text, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
                    if (txtAdd3.Text.ToString().Trim().Length > 0) adc3 = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(txtAdd3.Text, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
                    txtTotCost.Text = (adc1 + adc2 + adc3).ToString();

                    chkActive.Checked = bomH.BomActive;

                    var BomL = GetSortedBomLines(_db, bomH.BomHID);
                    //var BomL = _db.GetBOMLinesFromBomHeaderID(bomH.BomHID, CurrentUser.CoID)
                    //        .Select(bom => new BoMLine
                    //        {
                    //            BLID = bom.BLID,
                    //            ItemCode = bom.ItemCode ?? "",
                    //            BomCode = bom.BomCode ?? "",
                    //            ItemID = bom.ItemID.GetValueOrDefault(),
                    //            Description = bom.Description ?? "",
                    //            RMQty = ApiUrlCall.NumberToDecimal(bom.RMQty.GetValueOrDefault(), CurrentUser.CompanyDecPlaces),
                    //            AvCost = ApiUrlCall.NumberToDecimal(bom.AvCost.GetValueOrDefault(), CurrentUser.CompanyDecPlaces),
                    //            BomUnit = bom.BomUnit ?? ""
                    //        }).ToList();
                    if (BomL == null)
                    {
                        BOMLine NewBomLine = new BOMLine();
                        NewBomLine.CompanyID = CurrentUser.CoID;
                        NewBomLine.BomCode = "NEW";
                        NewBomLine.BomHID = (int)bomid;
                        _db.BOMLines.Add(NewBomLine);
                        _db.SaveChanges();
                    }
                    else
                    {
                        if (BomL.Count > 0)
                        {
                            var lastLine = BomL.OrderByDescending(x => x.BLID).First();
                            if (lastLine != null && lastLine.Description != null && lastLine.Description != "") // Check if NewJCLine and JCID are not null
                            {
                                BOMLine NewBomLine = new BOMLine();
                                NewBomLine.CompanyID = CurrentUser.CoID;
                                NewBomLine.BomCode = bomH.BOMCode;
                                NewBomLine.BomHID = (int)bomid;
                                _db.BOMLines.Add(NewBomLine);
                                _db.SaveChanges();
                            }
                        }
                        else
                        {
                            BOMLine NewBomLine = new BOMLine();
                            NewBomLine.CompanyID = CurrentUser.CoID;
                            NewBomLine.BomCode = bomH.BOMCode;
                            NewBomLine.BomHID = (int)bomid;
                            _db.BOMLines.Add(NewBomLine);
                            _db.SaveChanges();
                        }
                    }
                    BomL = GetSortedBomLines(_db, bomH.BomHID);
                    //BomL = _db.GetBOMLinesFromBomHeaderID(bomH.BomHID, CurrentUser.CoID)
                    //        .Select(bom => new BoMLine
                    //        {
                    //            BLID = bom.BLID,
                    //            ItemCode = bom.ItemCode ?? "",
                    //            BomCode = bom.BomCode ?? "",
                    //            ItemID = bom.ItemID.GetValueOrDefault(),
                    //            Description = bom.Description ?? "",
                    //            RMQty = ApiUrlCall.NumberToDecimal(bom.RMQty.GetValueOrDefault(), CurrentUser.CompanyDecPlaces),
                    //            AvCost = ApiUrlCall.NumberToDecimal(bom.AvCost.GetValueOrDefault(), CurrentUser.CompanyDecPlaces),
                    //            BomUnit = bom.BomUnit ?? ""
                    //        }).ToList();

                    foreach (BoMLine bl in BomL)
                    {
                        if (bl.AvCost > 0 && bl.RMQty > 0)
                        {
                            bl.AvRMCost = bl.AvCost * bl.RMQty;
                            bl.AvRMCost = ApiUrlCall.NumberToDecimal(bl.AvRMCost, CurrentUser.CompanyDecPlaces);
                        }
                    }

                    var totalRMQty = BomL.Sum(bomLine => bomLine.RMQty);
                    var totalRMCost = BomL.Sum(bomLine => bomLine.AvRMCost);
                    decimal BOMCost = adc1 + adc2 + adc3 + totalRMCost;
                    if (BomL.Count > 0)
                    {
                        GridBOMLines.DataSource = BomL;
                        GridBOMLines.DataBind();
                        GridBOMLines.FooterRow.Cells[2].Text = "Total";
                        GridBOMLines.FooterRow.Cells[4].Text = ApiUrlCall.NumberToDecimal(totalRMQty.ToString(),CurrentUser.CompanyDecPlaces);
                        GridBOMLines.FooterRow.Cells[6].Text = ApiUrlCall.NumberToDecimal(totalRMCost.ToString(), CurrentUser.CompanyDecPlaces);
                        lblNewBOMCost.Text = "0";
                        if (BOMCost >0)  lblNewBOMCost.Text = ApiUrlCall.NumberToDecimal((BOMCost).ToString(), CurrentUser.CompanyDecPlaces);
                    }
                    // calculate current GP
                    lblCurrGP.Text = "0";
                    if (SageSell > 0 && BOMCost > 0 && SageSell > 0)
                    {
                        decimal currgp = ((SageSell - BOMCost) / SageSell) * 100;
                        lblCurrGP.Text = ApiUrlCall.NumberToDecimal((currgp).ToString(), CurrentUser.CompanyDecPlaces);
                        if (currgp < 0)
                        {
                            lblCurrGP.Text = $"<span style='color:red;font-weight:bold;'>{ApiUrlCall.NumberToDecimal(currgp.ToString(), CurrentUser.CompanyDecPlaces)}% (Loss)</span>";
                        }
                        else
                        {
                            lblCurrGP.Text = $"{ApiUrlCall.NumberToDecimal(currgp.ToString(), CurrentUser.CompanyDecPlaces)} %";
                        }
                    }      
                }
            }
        }

        protected void lbtnDeleteBom_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var bomH = _db.BOMHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.BomHID == bomid);
                _db.BOMHeaders.RemoveRange(bomH);

                var BomCde = bomH.FirstOrDefault();
                var BomL = _db.BOMLines.Where(x => x.CompanyID == CurrentUser.CoID && x.BomHID == bomid);
                _db.BOMLines.RemoveRange(BomL);
                _db.SaveChanges();
                
                string message = "Successfully Deleted";
                AlertHelper.ShowSweetAlert(this, message, "success");
            }
        }

        protected void lbtnLineSave_Click(object sender, EventArgs e)
        {
            LinkButton lbtn = sender as LinkButton;
            if (lbtn == null) return;
            GridViewRow row = lbtn.NamingContainer as GridViewRow;
            if (row == null) return;
            DropDownList DDItemCode = row.FindControl("DDItemCode") as DropDownList;
            TextBox txtBOMQty = row.FindControl("txtBOMQty") as TextBox;

            // Validation: Ensure all required values are present
            if (DDItemCode == null || txtBOMQty == null || DDItemCode.SelectedIndex == 0 || string.IsNullOrWhiteSpace(txtBOMQty.Text))
            {
                AlertHelper.ShowSweetAlert(this, "Please select an item and enter a valid quantity before saving.", "warning");
                return;
            }

            long itemId;
            decimal rmQty;
            if (!long.TryParse(DDItemCode.SelectedValue, out itemId) || itemId == 0)
            {
                AlertHelper.ShowSweetAlert(this, "Invalid item selected.", "warning");
                return;
            }
            if (!decimal.TryParse(txtBOMQty.Text, out rmQty) || rmQty <= 0)
            {
                AlertHelper.ShowSweetAlert(this, "Invalid quantity entered.", "warning");
                return;
            }

            int Lid;
            if (!int.TryParse(lbtn.CommandArgument.ToString(), out Lid))
            {
                AlertHelper.ShowSweetAlert(this, "Invalid line ID.", "error");
                return;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var BL = _db.BOMLines.Where(x => x.BLID == Lid).FirstOrDefault();
                if (BL == null)
                {
                    AlertHelper.ShowSweetAlert(this, "BOM line not found.", "error");
                    return;
                }
                BL.ItemCode = DDItemCode.SelectedItem.Text ?? string.Empty;
                BL.ItemID = itemId;
                BL.RMQty = rmQty;
                if (row.Cells[4].Text.ToString() != "&nbsp;") BL.BomUnit = row.Cells[4].Text.ToString();
                _db.SaveChanges();
                LoadBom();
            }
        }

        protected void lbtnDeleteLine_Click(object sender, EventArgs e)
        {
            LinkButton lbtn = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtn.NamingContainer;
            int Lid = Convert.ToInt32(lbtn.CommandArgument);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var BL = _db.BOMLines.Where(x => x.BLID == Lid);
                _db.BOMLines.RemoveRange(BL);
                _db.SaveChanges();
                LoadBom();
            }
        }

        protected void GridBOMLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var item = (BoMLine)e.Row.DataItem;  
                var ddlItemCode = (DropDownList)e.Row.FindControl("DDItemCode");                            
                {
                  if (ddlItemCode != null)
                    {
                        ddlItemCode.DataSource = _items;
                        ddlItemCode.DataTextField = "Code";
                        ddlItemCode.DataValueField = "ID";
                        ddlItemCode.DataBind();
                        ddlItemCode.Items.Insert(0, new ListItem("Select", "0"));
                        ddlItemCode.SelectedValue = item.ItemID.ToString();
                    }
                }
            }
        }

        protected void DDItemCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            GridViewRow row = (GridViewRow)ddl.NamingContainer;
            DropDownList DDItemCode = (DropDownList)row.FindControl("DDItemCode");
            TextBox txtBOMQty = (TextBox)row.FindControl("txtBOMQty");
            if (DDItemCode.SelectedValue.ToString() != "0")
            {
                long selectedItemid = Convert.ToInt64(ddl.SelectedValue);
                if (selectedItemid > 0)
                {
                    PopulateItemDetails(row, selectedItemid);
                }
            }
           ScriptManager.RegisterStartupScript(this, this.GetType(), "SetFocus", $"document.getElementById('{txtBOMQty.ClientID}').focus();", true);
        }

        private void PopulateItemDetails(GridViewRow row, long itemid)
        {
            // Example LINQ query based on itemCode+

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var item = _db.ItemsMasters.FirstOrDefault(i => i.ID == itemid);

                if (item != null)
                {
                    TextBox txtDescription = (TextBox)row.FindControl("txtDescription");
                    TextBox txtBOMQty = (TextBox)row.FindControl("txtBOMQty");
                    row.Cells[2].Text = item.Description;
                    row.Cells[4].Text = item.Unit.ToString();
                    row.Cells[5].Text = item.AverageCost.ToString();
                    txtBOMQty.Text = "0"; // Adjust based on your item properties
                }
            }
        }

        protected void LbtnCopy_Click(object sender, EventArgs e)
        {
            long newbhid = 0;
            // create new bom and add all other details
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var bomH = _db.BOMHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.BomHID == bomid).FirstOrDefault();
                BOMHeader NewBH = new BOMHeader();
                NewBH.AddCost01 = bomH.AddCost01;
                NewBH.AddCost02 = bomH.AddCost02;
                NewBH.AddCost03 = bomH.AddCost03;
                NewBH.CompanyID = CurrentUser.CoID;
                NewBH.BOMCode = "NEW";
                NewBH.BomActive = true;
                _db.BOMHeaders.Add(NewBH);
                _db.SaveChanges();
                newbhid = NewBH.BomHID;

                var BomL = _db.BOMLines.Where(x => x.CompanyID == CurrentUser.CoID && x.BomHID == bomid).ToList();
                foreach(var bl in BomL)
                {
                    BOMLine NewBL = new BOMLine();
                    NewBL.BomCode = "NEW";
                    NewBL.ItemID = bl.ItemID;
                    NewBL.ItemCode = bl.ItemCode;
                    NewBL.RMQty = bl.RMQty;
                    NewBL.CompanyID = CurrentUser.CoID;
                    _db.BOMLines.Add(NewBL);
                }
                _db.SaveChanges();
            }
                // get new BOMID and redirect
                Response.Redirect("~/BOMCreate.aspx?bomid=" + newbhid, false);
        }

        protected void txtAdd1_TextChanged(object sender, EventArgs e)
        {
            decimal adc1 = 0, adc2 = 0, adc3 = 0;
            if (txtAdd1.Text.ToString().Trim().Length >0) adc1 = Convert.ToDecimal(txtAdd1.Text, CultureInfo.InvariantCulture);
            if (txtAdd2.Text.ToString().Trim().Length >0) adc2 = Convert.ToDecimal(txtAdd2.Text, CultureInfo.InvariantCulture);
            if (txtAdd3.Text.ToString().Trim().Length >0) adc3 = Convert.ToDecimal(txtAdd3.Text, CultureInfo.InvariantCulture);
            txtTotCost.Text = (adc1 + adc2 + adc3).ToString();
            TextBox textBox = sender as TextBox;
            if (textBox.ID == "txtAdd1")
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "SetFocus", $"document.getElementById('{txtAdd2.ClientID}').focus();", true);
            } else
                if (textBox.ID == "txtAdd2")
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "SetFocus", $"document.getElementById('{txtAdd3.ClientID}').focus();", true);
            }
            else
                if (textBox.ID == "txtAdd3")
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "SetFocus", $"document.getElementById('{txtAdd1.ClientID}').focus();", true);
            }
            
        }

        protected void LbtnSaveBOM_Click(object sender, EventArgs e)
        {
            decimal adc1 = 0, adc2 = 0, adc3 = 0;
            if (txtAdd1.Text.ToString().Trim().Length > 0) adc1 = Convert.ToDecimal(txtAdd1.Text, CultureInfo.InvariantCulture);
            if (txtAdd2.Text.ToString().Trim().Length > 0) adc2 = Convert.ToDecimal(txtAdd2.Text, CultureInfo.InvariantCulture);
            if (txtAdd3.Text.ToString().Trim().Length > 0) adc3 = Convert.ToDecimal(txtAdd3.Text, CultureInfo.InvariantCulture);

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var bomH = _db.BOMHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.BomHID == bomid).FirstOrDefault();
                bomH.BOMCode = lblBOMCode.Text.ToString().Trim();
                bomH.BomDescript = lblBOMDescipt.Text.ToString().Trim();
                bomH.AddCost01 = adc1;
                bomH.AddCost02 = adc2;
                bomH.AddCost03 = adc3;
                bomH.BomActive = chkActive.Checked;
                var Boml = _db.BOMLines.Where(x => x.CompanyID == CurrentUser.CoID && x.BomHID == bomid).ToList();
                foreach (var ln in Boml)
                {
                    ln.BomCode = bomH.BOMCode;
                }
                _db.SaveChanges();
            }
            LoadBom();
            string message = "Succesfully Saved";
            AlertHelper.ShowSweetAlert(this, message, "success");
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

            public string BomUnit { get; set; }
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

        protected async void lbtnSBCAUpdate_Click(object sender, EventArgs e)
        {
            // The New Selling Price is calculated client-side and is deliberately
            // blanked when Required GP% >= 100, when GP%/cost is missing, or when the
            // result is non-finite. An empty box therefore means the inputs were
            // invalid — not a number-format fault — so report that plainly.
            if (string.IsNullOrWhiteSpace(txtNewSell.Text))
            {
                AlertHelper.ShowSweetAlert(this,
                    "No selling price could be calculated. Check that the Required GP% is below 100% and that the BOM has a cost.",
                    "warning");
                return;
            }

            if (!decimal.TryParse(txtNewSell.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal newSell)
                || newSell <= 0)
            {
                AlertHelper.ShowSweetAlert(this, "Please enter a valid selling price greater than zero.", "warning");
                return;
            }

            if (!long.TryParse(lblFGID.Text, out long bomfgID) || bomfgID <= 0)
            {
                AlertHelper.ShowSweetAlert(this, "The finished good item could not be identified.", "error");
                return;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // Authoritative BOM cost — recomputed here from current Additional Costs
                // plus saved RM lines, so an unsaved edit can never push a stale figure.
                decimal bomcost = ComputeBomCost(_db);

                var thisItem = _db.ItemsMasters
                    .FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == bomfgID);
                if (thisItem == null)
                {
                    AlertHelper.ShowSweetAlert(this, "Finished good item not found.", "error");
                    return;
                }

                int dp = CurrentUser.CompanyDecPlaces;
                decimal newPriceEx = ApiUrlCall.NumberToDecimal(newSell, dp);
                decimal newGp = newPriceEx > 0
                    ? ApiUrlCall.NumberToDecimal(((newPriceEx - bomcost) / newPriceEx) * 100m, dp)
                    : 0m;

                bool costApplied = false;
                bool priceApplied = false;
                decimal newPriceInc;

                if (CurrentUser.UATMode)
                {
                    // UAT: never contact Sage. Update local records only, deriving the
                    // tax rate from the local item (sales tax %, else inc/excl ratio).
                    decimal taxRate = ResolveLocalTaxRate(thisItem);
                    newPriceInc = ApiUrlCall.NumberToDecimal(newPriceEx * (1 + taxRate), dp);
                    costApplied = true;
                    priceApplied = true;
                }
                else
                {
                    ApiUrlCall api = new ApiUrlCall();

                    // Pull the live Sage item so the price post preserves every other field.
                    JObject sageItem = await api.LoadOneItemJson($"ID eq {bomfgID}", CurrentUser);
                    JArray results = sageItem?["Results"] as JArray;
                    if (results == null || results.Count == 0)
                    {
                        AlertHelper.ShowSweetAlert(this, "Could not load the item from Sage. Nothing was updated.", "error");
                        return;
                    }
                    JObject itemObj = (JObject)results[0];

                    // VAT rate from the item's CONFIGURED sales tax type (authoritative), looked up
                    // via TaxTypesMasters from the live item's TaxTypeIdSales. The old inc/excl price
                    // ratio is NOT used as the primary source: it is wrong whenever the stored prices
                    // don't embed VAT (Inc == Ex) and it drifts with rounding. A configured 0% (zero-
                    // rated) is respected; only an unknown tax type falls back to the local heuristic.
                    decimal? taxPerc = null;
                    int taxTypeId = itemObj.Value<int?>("TaxTypeIdSales") ?? 0;
                    if (taxTypeId > 0)
                    {
                        taxPerc = _db.TaxTypesMasters
                            .Where(x => x.CompanyID == CurrentUser.CoID && x.TaxTypeID == taxTypeId)
                            .Select(x => (decimal?)x.TaxPerc).FirstOrDefault();
                    }
                    decimal taxRate = taxPerc.HasValue ? taxPerc.Value / 100m : ResolveLocalTaxRate(thisItem);
                    if (taxRate < 0) taxRate = 0m;
                    newPriceInc = ApiUrlCall.NumberToDecimal(newPriceEx * (1 + taxRate), dp);

                    // --- Sage post #1: cost adjustment (quantity 0, cost only) ---
                    ItemAdjustment iAdj = new ItemAdjustment
                    {
                        Date = DateTime.Now,
                        ItemID = bomfgID,
                        AverageCost = bomcost,
                        Quantity = 0m,
                        Reason = "BOM Cost Adjustment Only",
                        Created = DateTime.Now
                    };
                    JObject costResp = await SendItemAdjustment(JsonConvert.SerializeObject(iAdj, Formatting.Indented));
                    if (IsSageError(costResp, out string costErr))
                    {
                        // Nothing committed locally — SBMS and Sage stay aligned on cost.
                        AlertHelper.ShowSweetAlert(this, $"Average cost was NOT updated. {costErr}", "error");
                        return;
                    }
                    costApplied = true;

                    // --- Sage post #2: price update (clone item, override the two price fields) ---
                    JObject pricePayload = new JObject(itemObj)
                    {
                        ["PriceExclusive"] = newPriceEx,
                        ["PriceInclusive"] = newPriceInc
                    };
                    JObject priceResp = await api.APIPostDocumentAsync("Item", pricePayload.ToString(), CurrentUser);
                    if (IsSageError(priceResp, out string priceErr))
                    {
                        // The cost adjustment already posted to Sage; persist that one field
                        // locally so the two systems stay in step, then report the price failure.
                        thisItem.AverageCost = bomcost;
                        _db.SaveChanges();
                        AlertHelper.ShowSweetAlert(this,
                            $"Average cost was updated, but the selling price was NOT. {priceErr}", "error");
                        return;
                    }
                    priceApplied = true;
                }

                // Commit local records to mirror exactly what was accepted.
                if (costApplied) thisItem.AverageCost = bomcost;
                if (priceApplied)
                {
                    thisItem.PriceExclusive = newPriceEx;
                    thisItem.PriceInclusive = newPriceInc;
                    thisItem.GPPercentage = newGp;
                }
                _db.SaveChanges();

                AlertHelper.ShowSweetAlert(this,
                    CurrentUser.UATMode
                        ? "UAT mode: Sage was not contacted. Local cost and selling price updated."
                        : "Item cost and selling price updated successfully.",
                    "success");
            }
        }

        public async Task<JObject> SendItemAdjustment(string Item)
        {
            ApiUrlCall Api = new ApiUrlCall();
            return await Api.APIPostDocumentAsync("ItemAdjustment", Item, CurrentUser);
        }

        // Recomputes the BOM cost from the on-screen Additional Costs and the saved RM
        // lines, rounded to company decimal places. Used as the single source of truth
        // for what is sent to Sage so a stale label can never be posted.
        private decimal ComputeBomCost(SBMSEntities _db)
        {
            decimal adc1 = ParseMoney(txtAdd1.Text);
            decimal adc2 = ParseMoney(txtAdd2.Text);
            decimal adc3 = ParseMoney(txtAdd3.Text);

            decimal totalRMCost = 0m;
            foreach (var bl in GetSortedBomLines(_db, (int)bomid))
            {
                if (bl.AvCost > 0 && bl.RMQty > 0)
                    totalRMCost += ApiUrlCall.NumberToDecimal(bl.AvCost * bl.RMQty, CurrentUser.CompanyDecPlaces);
            }

            return ApiUrlCall.NumberToDecimal(adc1 + adc2 + adc3 + totalRMCost, CurrentUser.CompanyDecPlaces);
        }

        private static decimal ParseMoney(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0m;
            return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal v) ? v : 0m;
        }

        // Tax rate as a fraction (0.15 = 15%) derived from the local item: prefer the
        // configured sales tax %, fall back to the existing inclusive/exclusive ratio.
        private static decimal ResolveLocalTaxRate(ItemsMaster item)
        {
            if (item.TaxTypeSalesPerc.HasValue && item.TaxTypeSalesPerc.Value > 0)
                return item.TaxTypeSalesPerc.Value / 100m;

            if (item.PriceExclusive.HasValue && item.PriceExclusive.Value > 0 && item.PriceInclusive.HasValue)
            {
                decimal r = (item.PriceInclusive.Value / item.PriceExclusive.Value) - 1;
                return r > 0 ? r : 0m;
            }

            return 0m;
        }

        // True when the Sage response represents a failure (explicit Success:false, a
        // null response, or an empty body). Provides a clean message for the alert.
        private static bool IsSageError(JObject resp, out string message)
        {
            if (resp == null)
            {
                message = "No response was returned from Sage.";
                return true;
            }

            JToken successTok = resp["Success"];
            if (successTok != null && successTok.Type == JTokenType.Boolean && !successTok.Value<bool>())
            {
                string status = resp["StatusCode"]?.ToString() ?? "Unknown";
                string msg = resp["Message"]?.ToString() ?? "No message returned.";
                message = $"Sage error ({status}): {msg}";
                return true;
            }

            if (!resp.HasValues)
            {
                message = "Sage returned an empty response.";
                return true;
            }

            message = null;
            return false;
        }

        protected void lbtnDownload_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var bomH = _db.BOMHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.BomHID == bomid).FirstOrDefault();
                if (bomH == null) return;

                var bomLines = GetSortedBomLines(_db, (int)bomid)
                    .Where(x => x.Description != null && x.Description != "")
                    .ToList();

                foreach (var bl in bomLines)
                {
                    if (bl.AvCost > 0 && bl.RMQty > 0)
                        bl.AvRMCost = ApiUrlCall.NumberToDecimal(bl.AvCost * bl.RMQty, CurrentUser.CompanyDecPlaces);
                }

                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("BOM");

                    // --- BOM Header section ---
                    ws.Cell(1, 1).Value = "BOM Code";
                    ws.Cell(1, 2).Value = bomH.BOMCode ?? "";
                    ws.Cell(2, 1).Value = "Description";
                    ws.Cell(2, 2).Value = bomH.BomDescript ?? "";
                    ws.Cell(3, 1).Value = "Finished Good Code";
                    ws.Cell(3, 2).Value = bomH.FGCode ?? "";
                    ws.Cell(4, 1).Value = "Finished Good Description";
                    ws.Cell(4, 2).Value = bomH.FGDescript ?? "";
                    ws.Cell(5, 1).Value = "Active";
                    ws.Cell(5, 2).Value = bomH.BomActive ? "Yes" : "No";
                    ws.Cell(6, 1).Value = "Additional Cost 1";
                    ws.Cell(6, 2).Value = bomH.AddCost01 ?? 0;
                    ws.Cell(7, 1).Value = "Additional Cost 2";
                    ws.Cell(7, 2).Value = bomH.AddCost02 ?? 0;
                    ws.Cell(8, 1).Value = "Additional Cost 3";
                    ws.Cell(8, 2).Value = bomH.AddCost03 ?? 0;

                    for (int r = 1; r <= 8; r++)
                        ws.Cell(r, 1).Style.Font.Bold = true;

                    // --- BOM Lines header ---
                    int headerRow = 10;
                    var lineHeaders = new[] { "Item Code", "Description", "RM Qty", "UOM", "Unit Cost", "Cost" };
                    for (int i = 0; i < lineHeaders.Length; i++)
                    {
                        var cell = ws.Cell(headerRow, i + 1);
                        cell.Value = lineHeaders[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0, 112, 192);
                        cell.Style.Font.FontColor = XLColor.White;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    // --- BOM Lines data ---
                    int row = headerRow + 1;
                    foreach (var bl in bomLines)
                    {
                        ws.Cell(row, 1).Value = bl.ItemCode;
                        ws.Cell(row, 2).Value = bl.Description;
                        ws.Cell(row, 3).Value = bl.RMQty;
                        ws.Cell(row, 4).Value = bl.BomUnit;
                        ws.Cell(row, 5).Value = bl.AvCost;
                        ws.Cell(row, 6).Value = bl.AvRMCost;
                        if (row % 2 == 0)
                            ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(235, 241, 250);
                        row++;
                    }

                    // Totals row
                    ws.Cell(row, 2).Value = "Total";
                    ws.Cell(row, 2).Style.Font.Bold = true;
                    ws.Cell(row, 3).Value = bomLines.Sum(x => x.RMQty);
                    ws.Cell(row, 6).Value = bomLines.Sum(x => x.AvRMCost);
                    ws.Cell(row, 6).Style.Font.Bold = true;

                    ws.Columns().AdjustToContents();

                    string fileName = $"BOM_{bomH.BOMCode}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                    using (var ms = new System.IO.MemoryStream())
                    {
                        workbook.SaveAs(ms);
                        ms.Seek(0, System.IO.SeekOrigin.Begin);
                        Response.Clear();
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AppendHeader("Content-Disposition", "attachment; filename=" + fileName);
                        Response.Cache.SetCacheability(HttpCacheability.NoCache);
                        Response.BinaryWrite(ms.ToArray());
                        Response.Flush();
                        Response.End();
                    }
                }
            }
        }

        protected void GridBOMLines_Sorting(object sender, GridViewSortEventArgs e)
        {
            ViewState["BOMSortExpression"] = e.SortExpression;
            ViewState["BOMSortDirection"] = ViewState["BOMSortDirection"] as string == "ASC" ? "DESC" : "ASC";
            LoadBom();
        }

        private List<BoMLine> GetSortedBomLines(SBMSEntities _db, int bomHID)
        {
            string sortExpression = ViewState["BOMSortExpression"] as string ?? "BLID";
            string sortDirection = ViewState["BOMSortDirection"] as string ?? "ASC";

            var BomL = _db.GetBOMLinesFromBomHeaderID(bomHID, CurrentUser.CoID)
                .Select(bom => new BoMLine
                {
                    BLID = bom.BLID,
                    ItemCode = bom.ItemCode ?? "",
                    BomCode = bom.BomCode ?? "",
                    ItemID = bom.ItemID.GetValueOrDefault(),
                    Description = bom.Description ?? "",
                    RMQty = ApiUrlCall.NumberToDecimal(bom.RMQty.GetValueOrDefault(), CurrentUser.CompanyDecPlaces),
                    AvCost = ApiUrlCall.NumberToDecimal(bom.AvCost.GetValueOrDefault(), CurrentUser.CompanyDecPlaces),
                    BomUnit = bom.BomUnit ?? ""
                }).ToList();

            // Separate blank line and sort the rest
            var blankLine = BomL.Where(x => x.Description == "").ToList();
            var sortedLines = BomL.Where(x => x.Description != "");

            sortedLines = sortDirection == "ASC"
                ? sortedLines.OrderBy(x => typeof(BoMLine).GetProperty(sortExpression)?.GetValue(x))
                : sortedLines.OrderByDescending(x => typeof(BoMLine).GetProperty(sortExpression)?.GetValue(x));

            // Append blank line at the end after sorting
            return sortedLines.ToList().Concat(blankLine).ToList();
        }
    }
}