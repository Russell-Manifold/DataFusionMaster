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
            
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {

                decimal newSell, bomcost;

                if (!decimal.TryParse(
                        txtNewSell.Text,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out newSell))
                {
                    // Handle invalid input safely
                    // e.g. show message or default value
                    AlertHelper.ShowSweetAlert(this, "Invalid number format.", "error");
                    return;
                }

                if (!decimal.TryParse(
                        lblNewBOMCost.Text,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out bomcost))
                {
                    // Handle invalid input safely
                    // e.g. show message or default value
                    AlertHelper.ShowSweetAlert(this, "Invalid BOM cost.", "error");
                    return;
                }

                long bomfgID = Convert.ToInt64(lblFGID.Text);
                var ThisItem = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == bomfgID).FirstOrDefault();
                ThisItem.AverageCost = bomcost;
                _db.SaveChanges();

                ItemAdjustment iAdj = new ItemAdjustment();
                iAdj.Date = DateTime.Now;
                iAdj.ItemID = Convert.ToInt64(lblFGID.Text);
                iAdj.AverageCost = bomcost;
                iAdj.Quantity = (decimal)0;
                iAdj.Reason = "BOM Cost Adjustment Only";
                iAdj.Created = DateTime.Now;
                string jsonBody = JsonConvert.SerializeObject(iAdj, Formatting.Indented);
                if (CurrentUser.UATMode == false)
                {
                    await SendItemAdjustment(jsonBody);
                }
                string filt = $"ID eq {bomfgID}";
               
                JObject thisitem = new JObject();
                ApiUrlCall api = new ApiUrlCall();
                thisitem = await api.LoadOneItemJson(filt, CurrentUser);
                JArray arr = (JArray)thisitem["Results"];
                JObject itemObj = (JObject)arr[0];
                decimal priceEx = (decimal)itemObj["PriceExclusive"];
                decimal priceInc = (decimal)itemObj["PriceInclusive"];

                decimal taxRate = (priceInc / priceEx) - 1;   // e.g. 0.15 = 15%
                decimal newPriceEx = newSell;
                decimal newPriceInc = newPriceEx * (1 + taxRate);
                
                JObject returnPayload = new JObject(itemObj);   // full clone

                returnPayload["PriceExclusive"] = newPriceEx;
                returnPayload["PriceInclusive"] = newPriceInc;

                JObject parsedJSON = await api.APIPostDocumentAsync("Item", returnPayload.ToString(), CurrentUser);

                // 1 — Check for API/Exception error
                if (parsedJSON["Success"] != null && parsedJSON["Success"].ToString() == "false")
                {
                    string status = parsedJSON["StatusCode"]?.ToString() ?? "Unknown";
                    string msg = parsedJSON["Message"]?.ToString() ?? "No message returned.";

                    AlertHelper.ShowSweetAlert(this,"API Error ({status}): {msg}", "error'");
                    return;
                }

                // 2 — Check if nothing returned at all (edge-case)
                if (parsedJSON == null || !parsedJSON.HasValues)
                {
                    AlertHelper.ShowSweetAlert(this,"No response returned from API. - Item NOT Updated", "warning'");
                    return;
                }

                // 3 — SUCCESS
                AlertHelper.ShowSweetAlert(this,"Item updated successfully.", "success");
            }
        }
        public async Task SendItemAdjustment(string Item)
        {
            string doctype = "";
            doctype = "ItemAdjustment";
            ApiUrlCall Api = new ApiUrlCall();
            JObject parsedJSON = await Api.APIPostDocumentAsync(doctype, Item, CurrentUser);
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