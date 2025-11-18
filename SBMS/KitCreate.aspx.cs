using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class KitCreate : BasePage
    {
        long CoID;
        private List<ItemsMaster> _itemsB;
        long kitid = 0;
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
                _itemsB = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CoID && i.Active == true && (i.IsKitComponent != null && i.IsKitComponent == true)).ToList();

                if (!IsPostBack)
                {
                    LoadKit();
                    var items = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CoID && i.Active == true && (i.IsFromKit != null && i.IsFromKit == true)).ToList();
                    DDFGCode.DataSource = items;
                    DDFGCode.DataTextField = "Code";
                    DDFGCode.DataValueField = "ID";
                    DDFGCode.DataBind();
                    DDFGCode.Items.Insert(0, "-Select-");
                }
            }
        }

        protected void LoadKit()
        {
            kitid = Convert.ToInt64(Request.QueryString["kitid"].ToString());
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var kitH = _db.KitHeaders.Where(x => x.CompanyID == CoID && x.KitHID == kitid).FirstOrDefault();
                if (kitH.AddCost01 != null) txtAdd1.Text = kitH.AddCost01.ToString() ?? "";
                if (kitH.AddCost02 != null) txtAdd2.Text = kitH.AddCost02.ToString() ?? "";
                if (kitH.AddCost03 != null) txtAdd3.Text = kitH.AddCost03.ToString() ?? "";

                decimal adc1 = 0, adc2 = 0, adc3 = 0;
                if (txtAdd1.Text.ToString().Trim().Length > 0) adc1 = Convert.ToDecimal(txtAdd1.Text, CultureInfo.InvariantCulture);
                if (txtAdd2.Text.ToString().Trim().Length > 0) adc2 = Convert.ToDecimal(txtAdd2.Text, CultureInfo.InvariantCulture);
                if (txtAdd3.Text.ToString().Trim().Length > 0) adc3 = Convert.ToDecimal(txtAdd3.Text, CultureInfo.InvariantCulture);
                txtTotCost.Text = (adc1 + adc2 + adc3).ToString();

                var kitL = _db.GetKitLinesFromKitCode(kitH.KitCode, CoID)
                        .Select(Kit => new kitLine
                        {
                            KLID = Kit.KLID,
                            ItemCode = Kit.ItemCode ?? "",
                            KitCode = Kit.KitCode ?? "",
                            ItemID = Kit.ItemID.GetValueOrDefault(),
                            Description = Kit.Description ?? "",
                            FGQty = Kit.FGQty.GetValueOrDefault(),
                            AvCost = Kit.AvCost.GetValueOrDefault()
                        }).ToList();

                if (kitL != null)
                {
                    KitLine NewkitLine = new KitLine();
                    NewkitLine.CompanyID = CoID;
                    NewkitLine.KitCode = "NEW";
                    _db.KitLines.Add(NewkitLine);
                    _db.SaveChanges();
                }
                else
                {
                    var lastLine = kitL.Last();
                    if (lastLine != null && lastLine.Description != null && lastLine.Description != "") // Check if NewJCLine and JCID are not null
                    {
                        KitLine NewkitLine = new KitLine();
                        NewkitLine.CompanyID = CoID;
                        NewkitLine.KitCode = kitH.KitCode;
                        _db.KitLines.Add(NewkitLine);
                        _db.SaveChanges();

                    }
                }
                    
                kitL = _db.GetKitLinesFromKitCode(kitH.KitCode, CoID)
                    .Select(Kit => new kitLine
                    {
                        KLID = Kit.KLID,
                        ItemCode = Kit.ItemCode ?? "",
                        KitCode = Kit.KitCode ?? "",
                        ItemID = Kit.ItemID.GetValueOrDefault(),
                        Description = Kit.Description ?? "",
                        FGQty = Kit.FGQty.GetValueOrDefault(),
                        AvCost = Kit.AvCost.GetValueOrDefault()
                    }).ToList();

                    foreach (kitLine bl in kitL)
                    {
                        if (bl.AvCost > 0 && bl.FGQty > 0)
                        {
                            bl.AvRMCost = bl.AvCost * bl.FGQty;
                        }
                    }

                var totalRMQty = kitL.Sum(kitLine => kitLine.FGQty);
                var totalRMCost = kitL.Sum(kitLine => kitLine.AvRMCost);

                if (kitL.Count > 0)
                {
                    GridkitLines.DataSource = kitL;
                    GridkitLines.DataBind();
                    GridkitLines.FooterRow.Cells[2].Text = "Total";
                    GridkitLines.FooterRow.Cells[3].Text = totalRMQty.ToString("N4");
                    GridkitLines.FooterRow.Cells[5].Text = totalRMCost.ToString("N4");
                }
            }
        }

        protected void lbtnLineSave_Click(object sender, EventArgs e)
        {
            LinkButton lbtn = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtn.NamingContainer;
            DropDownList DDItemCode = (DropDownList)row.FindControl("DDItemCode");

            TextBox txtKitQty = (TextBox)row.FindControl("txtKitQty");

            int Lid = Convert.ToInt32(lbtn.CommandArgument);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var BL = _db.KitLines.Where(x => x.KLID == Lid).FirstOrDefault();
                BL.ItemCode = DDItemCode.SelectedItem.Text.ToString();
                BL.ItemID = Convert.ToInt64(DDItemCode.SelectedValue);
                BL.FGQty = Convert.ToDecimal(txtKitQty.Text);
                _db.SaveChanges();
                LoadKit();
            }
        }

        protected void lbtnDeleteLine_Click(object sender, EventArgs e)
        {
            LinkButton lbtn = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtn.NamingContainer;
            int Lid = Convert.ToInt32(lbtn.CommandArgument);
            deleteNewUnused();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var BL = _db.KitLines.Where(x => x.KLID == Lid);
                _db.KitLines.RemoveRange(BL);
                _db.SaveChanges();
                LoadKit();
            }
        }

        protected void GridkitLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var item = (kitLine)e.Row.DataItem;  
                var ddlItemCode = (DropDownList)e.Row.FindControl("DDItemCode");                            
                {
                  if (ddlItemCode != null)
                    {
                        ddlItemCode.DataSource = _itemsB;
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
            TextBox txtKitQty = (TextBox)row.FindControl("txtKitQty");
            if (DDItemCode.SelectedValue.ToString() != "0")
            {
                long selectedItemid = Convert.ToInt64(ddl.SelectedValue);
                if (selectedItemid > 0)
                {
                    PopulateItemDetails(row, selectedItemid);
                }
            }
            ScriptManager.RegisterStartupScript(this, this.GetType(), "SetFocus", $"document.getElementById('{txtKitQty.ClientID}').focus();", true);
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
                    TextBox txtKitQty = (TextBox)row.FindControl("txtKitQty");
                    row.Cells[2].Text = item.Description;
                    row.Cells[4].Text = item.AverageCost.ToString();
                    txtKitQty.Text = "0"; // Adjust based on your item properties
                }
            }
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
            decimal LinesVal = Convert.ToDecimal(GridkitLines.FooterRow.Cells[5].Text);
        }

        protected void LbtnSaveKit_Click(object sender, EventArgs e)
        {
            string message = "";
           if (DDFGCode.SelectedIndex == 0)
            {
                message = "Please select a valid Finished Goods code before continuing";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                foreach (GridViewRow grv in GridkitLines.Rows)
                {
                DropDownList DDItemCode = (DropDownList)grv.FindControl("DDItemCode");
                TextBox txtKitQty = (TextBox)grv.FindControl("txtKitQty");
                LinkButton lbtnLineSave = (LinkButton)grv.FindControl("lbtnLineSave");
                int Lid = Convert.ToInt32(lbtnLineSave.CommandArgument);
               
                    var BL = _db.KitLines.Where(x => x.KLID == Lid).FirstOrDefault();
                    BL.ItemCode = DDItemCode.SelectedItem.Text.ToString();
                    BL.ItemID = Convert.ToInt64(DDItemCode.SelectedValue);
                    BL.FGQty = Convert.ToDecimal(txtKitQty.Text);
                    
                }
                _db.SaveChanges();
            }

            decimal adc1 = 0, adc2 = 0, adc3 = 0;
            if (txtAdd1.Text.ToString().Trim().Length > 0) adc1 = Convert.ToDecimal(txtAdd1.Text, CultureInfo.InvariantCulture);
            if (txtAdd2.Text.ToString().Trim().Length > 0) adc2 = Convert.ToDecimal(txtAdd2.Text, CultureInfo.InvariantCulture);
            if (txtAdd3.Text.ToString().Trim().Length > 0) adc3 = Convert.ToDecimal(txtAdd3.Text, CultureInfo.InvariantCulture);

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var kitH = _db.KitHeaders.Where(x => x.CompanyID == CoID && x.KitHID == kitid).FirstOrDefault();
                kitH.KitCode = DDFGCode.SelectedItem.Text;
                kitH.kitDescript = lblFGDescript.Text;
                kitH.FGCode = DDFGCode.SelectedItem.Text;
                kitH.FGDescript = lblFGDescript.Text.ToString() ?? "";
                kitH.FGID = Convert.ToInt64(DDFGCode.SelectedValue);
                kitH.AddCost01 = adc1;
                kitH.AddCost02 = adc2;
                kitH.AddCost03 = adc3;
                kitH.KitActive = true;
                var KitL = _db.KitLines.Where(x => x.KitCode == "NEW").ToList();
                foreach (var bl in KitL)
                {
                    bl.KitCode = DDFGCode.SelectedItem.Text;
                }
                // update Item MAster showing it is a Kit
                long itmID = Convert.ToInt64(DDFGCode.SelectedValue);
                var itmM = _db.ItemsMasters.Where(x => x.CompanyID == CoID && x.ID == itmID).FirstOrDefault();
                itmM.IsFromKit = true;
                _db.SaveChanges();
            }

            message = "Successfully Saved";
            AlertHelper.ShowSweetAlert(this, message, "success");
        }


        private class kitLine
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

        protected void DDFGCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            long ItemID = Convert.ToInt64(DDFGCode.SelectedValue);
            if (ItemID > 0) {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var itemdesc = _db.ItemsMasters.Where(x => x.ID == ItemID).FirstOrDefault();
                    lblFGDescript.Text = itemdesc.Description;
                }
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

        private void deleteNewUnused()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var BD = _db.KitHeaders.Where(x => x.KitCode == "NEW" && x.CompanyID == CoID);
                _db.KitHeaders.RemoveRange(BD);

                var LinesD = _db.KitLines.Where(x => x.CompanyID == CoID && (x.KitCode == "NEW" || x.ItemCode == null));
                _db.KitLines.RemoveRange(LinesD);
                _db.SaveChanges();
            }
        }
    }
}