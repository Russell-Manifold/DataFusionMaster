using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Services.Description;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class KitDetailed : BasePage
    {
        long CoID;
        private List<ItemsMaster> _items;
        long kitid = 0;
        string kitCode = "";
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
            kitid = Convert.ToInt64(Request.QueryString["kitid"].ToString());
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                _items = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CoID && i.Active == true && (i.IsKitComponent != null && i.IsKitComponent == true)).OrderBy(x => x.Code).ToList();
            }
            if (!IsPostBack)
            {
                deleteNewUnused();
                LoadKit();
            }
        }

        protected void LoadKit()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var kitH = _db.KitHeaders.Where(x => x.CompanyID == CoID && x.KitHID == kitid).FirstOrDefault();
                if (kitH != null)
                {
                    kitCode = kitH.KitCode;
                    var item = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.Code == kitCode);
                    decimal avgCost = item?.AverageCost ?? 0; // Use 0 as default if null
                    lblItemCost.Text = avgCost.ToString("N2");                  
                    
                    lblFGID.Text = kitH.FGID.ToString();
                    if (kitH.FGCode != null) lblFGCode.Text = kitH.FGCode.ToString() ?? "";
                    if (kitH.FGDescript != null) lblFGDescript.Text = kitH.FGDescript.ToString() ?? "";
                    if (kitH.AddCost01 != null) txtAdd1.Text = kitH.AddCost01.ToString() ?? "";
                    if (kitH.AddCost02 != null) txtAdd2.Text = kitH.AddCost02.ToString() ?? "";
                    if (kitH.AddCost03 != null) txtAdd3.Text = kitH.AddCost03.ToString() ?? "";

                    decimal adc1 = 0, adc2 = 0, adc3 = 0;
                    if (txtAdd1.Text.ToString().Trim().Length > 0) adc1 = Convert.ToDecimal(txtAdd1.Text, CultureInfo.InvariantCulture);
                    if (txtAdd2.Text.ToString().Trim().Length > 0) adc2 = Convert.ToDecimal(txtAdd2.Text, CultureInfo.InvariantCulture);
                    if (txtAdd3.Text.ToString().Trim().Length > 0) adc3 = Convert.ToDecimal(txtAdd3.Text, CultureInfo.InvariantCulture);
                    txtTotCost.Text = (adc1 + adc2 + adc3).ToString();

                    var KitL = _db.GetKitLinesFromKitCode(kitH.KitCode, CoID)
                            .Select(bom => new KitLineT
                            {
                                KLID = bom.KLID,
                                ItemCode = bom.ItemCode ?? "",
                                KitCode = bom.KitCode ?? "",
                                ItemID = bom.ItemID.GetValueOrDefault(),
                                Description = bom.Description ?? "",
                                FGQty = bom.FGQty.GetValueOrDefault(),
                                AvCost = bom.AvCost.GetValueOrDefault()
                            }).ToList();
                    if (KitL == null)
                    {
                        KitLine NewKitLine = new KitLine();
                        NewKitLine.CompanyID = CoID;
                        NewKitLine.KitCode = "NEW";
                        _db.KitLines.Add(NewKitLine);
                        _db.SaveChanges();
                    }
                    else if (KitL.Count == 0)
                    {
                        KitLine NewKitLine = new KitLine();
                        NewKitLine.CompanyID = CoID;
                        NewKitLine.KitCode = kitH.KitCode;
                        _db.KitLines.Add(NewKitLine);
                        _db.SaveChanges();
                    }
                    else
                    {
                        var lastLine = KitL.Last();
                        if (lastLine != null && lastLine.Description != null && lastLine.Description != "") // Check if NewJCLine and JCID are not null
                        {
                            KitLine NewKitLine = new KitLine();
                            NewKitLine.CompanyID = CoID;
                            NewKitLine.KitCode = kitH.KitCode;
                            _db.KitLines.Add(NewKitLine);
                            _db.SaveChanges();

                        }
                    }
                    KitL = _db.GetKitLinesFromKitCode(kitH.KitCode, CoID)
                            .Select(bom => new KitLineT
                            {
                                KLID = bom.KLID,
                                ItemCode = bom.ItemCode ?? "",
                                KitCode = bom.KitCode ?? "",
                                ItemID = bom.ItemID.GetValueOrDefault(),
                                Description = bom.Description ?? "",
                                FGQty = bom.FGQty.GetValueOrDefault(),
                                AvCost = bom.AvCost.GetValueOrDefault()
                            }).ToList();

                    foreach (KitLineT bl in KitL)
                    {
                        if (bl.AvCost > 0 && bl.FGQty > 0)
                        {
                            bl.AvRMCost = bl.AvCost * bl.FGQty;
                        }
                    }

                    var totalRMQty = KitL.Sum(KitLine => KitLine.FGQty);
                    var totalRMCost = KitL.Sum(KitLine => KitLine.AvRMCost);

                    if (KitL.Count > 0)
                    {
                        GridKitLines.DataSource = KitL;
                        GridKitLines.DataBind();
                        GridKitLines.FooterRow.Cells[2].Text = "Total";
                        GridKitLines.FooterRow.Cells[3].Text = totalRMQty.ToString("N4");
                        GridKitLines.FooterRow.Cells[5].Text = totalRMCost.ToString("N4");
                        lblNewKitCost.Text = (adc1 + adc2 + adc3 + totalRMCost).ToString("N4");
                    }
                }
            }
        }

        protected void lbtnLineSave_Click(object sender, EventArgs e)
        {
            LinkButton lbtn = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtn.NamingContainer;
            DropDownList DDItemCode = (DropDownList)row.FindControl("DDItemCode");

            TextBox txtBOMQty = (TextBox)row.FindControl("txtBOMQty");

            int Lid = Convert.ToInt32(lbtn.CommandArgument);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var BL = _db.KitLines.Where(x => x.KLID == Lid).FirstOrDefault();
                BL.ItemCode = DDItemCode.SelectedItem.Text.ToString();
                BL.ItemID = Convert.ToInt64(DDItemCode.SelectedValue);
                BL.FGQty = Convert.ToDecimal(txtBOMQty.Text);
                _db.SaveChanges();
                LoadKit();
            }
        }

        protected void lbtnDeleteLine_Click(object sender, EventArgs e)
        {
            LinkButton lbtn = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtn.NamingContainer;
            int Lid = Convert.ToInt32(lbtn.CommandArgument);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var BL = _db.KitLines.Where(x => x.KLID == Lid);
                _db.KitLines.RemoveRange(BL);
                _db.SaveChanges();
                LoadKit();
            }
        }

        protected void GridKitLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var item = (KitLineT)e.Row.DataItem;  
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
                    row.Cells[4].Text = item.AverageCost.ToString();
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
                var kitH = _db.KitHeaders.Where(x => x.CompanyID == CoID && x.KitHID == kitid).FirstOrDefault();
                KitHeader NewBH = new KitHeader();
                NewBH.AddCost01 = kitH.AddCost01;
                NewBH.AddCost02 = kitH.AddCost02;
                NewBH.AddCost03 = kitH.AddCost03;
                NewBH.CompanyID = (int)CoID;
                NewBH.KitCode = "NEW";
                _db.KitHeaders.Add(NewBH);
                _db.SaveChanges();
                newbhid = NewBH.KitHID;

                var KitL = _db.KitLines.Where(x => x.CompanyID == CoID && x.KitCode == kitH.KitCode).ToList();
                foreach(var bl in KitL)
                {
                    KitLine NewBL = new KitLine();
                    NewBL.KitCode = "NEW";
                    NewBL.ItemID = (long)bl.ItemID;
                    NewBL.ItemCode = bl.ItemCode;
                    NewBL.FGQty =(decimal) bl.FGQty;
                    //NewBL.CompanyID = CoID;
                    _db.KitLines.Add(NewBL);
                }
                _db.SaveChanges();
            }
                // get new KitID and redirect
                Response.Redirect("~/KitCreate.aspx?kitid=" + newbhid, false);
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
                var kitH = _db.KitHeaders.Where(x => x.CompanyID == CoID && x.KitHID == kitid).FirstOrDefault();
                kitH.AddCost01 = adc1;
                kitH.AddCost02 = adc2;
                kitH.AddCost03 = adc3;

                var KitL = _db.KitLines.Where(x => x.CompanyID == CoID && x.KitCode == kitH.KitCode && x.ItemID == 0).ToList();
                _db.KitLines.RemoveRange(KitL);
                _db.SaveChanges();
            }
            string message = "Succesfully Saved";
            ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "alert", $"alert('{message}');", true);
        }

        private class KitLineT
        {
            public long KLID { get; set; }
            public string ItemCode { get; set; }
            public string KitCode { get; set; }
            public long ItemID { get; set; }
            public string Description { get; set; }
            public decimal FGQty { get; set; }
            public decimal AvCost { get; set; }
            public decimal AvRMCost { get; set; }
           
        }

        private void deleteNewUnused()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var BD = _db.KitHeaders.Where(x => x.KitCode == "NEW" && x.CompanyID == CoID);
                _db.KitHeaders.RemoveRange(BD);

                var LinesD = _db.KitLines.Where(x=> x.CompanyID == CoID && (x.KitCode == "NEW" || x.ItemCode == null));
                _db.KitLines.RemoveRange(LinesD);
                _db.SaveChanges();
            }
        }

        protected void lbtnDeleteBom_Click1(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var kitH = _db.KitHeaders.Where(x => x.CompanyID == CoID && x.KitHID == kitid);
                _db.KitHeaders.RemoveRange(kitH);

                var BomCde = kitH.FirstOrDefault();
                var KitL = _db.KitLines.Where(x => x.CompanyID == CoID && x.KitCode == BomCde.KitCode);
                _db.KitLines.RemoveRange(KitL);
                _db.SaveChanges();

                string message = "Successfully Deleted";
                ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "alert", $"alert('{message}');", true);
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

        protected async void lbtnSBCAUpdate_Click(object sender, EventArgs e)
        {
            long FGID = Convert.ToInt64(lblFGID.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var ThisItem = _db.ItemsMasters.Where(x=>x.CompanyID == CurrentUser.CoID && x.ID == FGID).FirstOrDefault();
                ThisItem.AverageCost = Convert.ToDecimal(lblNewKitCost.Text);
                _db.SaveChanges();
            }
            
            ItemAdjustment iAdj = new ItemAdjustment();
            iAdj.Date = DateTime.Now;
            iAdj.ItemID = FGID;
            iAdj.AverageCost = Convert.ToDecimal(lblNewKitCost.Text);
            iAdj.Quantity = (decimal)0;
            iAdj.Reason = "Kit Cost Adjustment Only";
            iAdj.Created = DateTime.Now;
            string jsonBody = JsonConvert.SerializeObject(iAdj, Formatting.Indented);
            if (CurrentUser.UATMode == false)
            {
                await SendItemAdjustment(jsonBody);
            }
            string message = "Successfully Saved";
            ScriptManager.RegisterClientScriptBlock(this, this.GetType(), "alert", $"alert('{message}');", true);
            return;
        }
        public async Task SendItemAdjustment(string Item)
        {
            string doctype = "";
            doctype = "ItemAdjustment";
            ApiUrlCall Api = new ApiUrlCall();
            JObject parsedJSON = await Api.APIPostDocumentAsync(doctype, Item, CurrentUser);
        }

        
    }
}