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
    public partial class BOMCreate : BasePage
    {
        private List<ItemsMaster> _itemsB;
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
                _itemsB = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CurrentUser.CoID && i.Active == true && (i.IsBOMComponent != null && i.IsBOMComponent == true)).ToList();

                if (!IsPostBack)
                {
                    LoadBom();
                    var items = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CurrentUser.CoID && i.Active == true && i.IsFromBOM == true).ToList();
                    DDFGCode.DataSource = items;
                    DDFGCode.DataTextField = "Code";
                    DDFGCode.DataValueField = "ID";
                    DDFGCode.DataBind();
                    DDFGCode.Items.Insert(0, "-Select-");
                }
            }
        }

        protected void LoadBom()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var bomH = _db.BOMHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.BomHID == bomid).FirstOrDefault();
                if (bomH.AddCost01 != null) txtAdd1.Text = bomH.AddCost01.ToString() ?? "";
                if (bomH.AddCost02 != null) txtAdd2.Text = bomH.AddCost02.ToString() ?? "";
                if (bomH.AddCost03 != null) txtAdd3.Text = bomH.AddCost03.ToString() ?? "";

                decimal adc1 = 0, adc2 = 0, adc3 = 0;
                if (txtAdd1.Text.ToString().Trim().Length > 0) adc1 = Convert.ToDecimal(txtAdd1.Text, CultureInfo.InvariantCulture);
                if (txtAdd2.Text.ToString().Trim().Length > 0) adc2 = Convert.ToDecimal(txtAdd2.Text, CultureInfo.InvariantCulture);
                if (txtAdd3.Text.ToString().Trim().Length > 0) adc3 = Convert.ToDecimal(txtAdd3.Text, CultureInfo.InvariantCulture);
                txtTotCost.Text = (adc1 + adc2 + adc3).ToString();

                var BomL = _db.GetBOMLinesFromBomHeaderID(bomH.BomHID, CurrentUser.CoID)
                        .Select(bom => new BoMLineN
                        {
                            BLID = bom.BLID,
                            ItemCode = bom.ItemCode ?? "",
                            BomCode = bom.BomCode ?? "",
                            ItemID = bom.ItemID.GetValueOrDefault(),
                            Description = bom.Description ?? "",
                            RMQty = bom.RMQty.GetValueOrDefault(),
                            AvCost = bom.AvCost.GetValueOrDefault(),
                            BomUnit = bom.BomUnit ?? ""
                        }).ToList();

                if (BomL != null)
                {
                    BOMLine NewBomLine = new BOMLine();
                    NewBomLine.BomHID = bomH.BomHID;
                    NewBomLine.CompanyID = CurrentUser.CoID;
                    NewBomLine.BomCode = "NEW";
                    _db.BOMLines.Add(NewBomLine);
                    _db.SaveChanges();
                }
                else
                {
                    var lastLine = BomL.Last();
                    if (lastLine != null && lastLine.Description != null && lastLine.Description != "") // Check if NewJCLine and JCID are not null
                    {
                        BOMLine NewBomLine = new BOMLine();
                        NewBomLine.CompanyID = CurrentUser.CoID;
                        NewBomLine.BomCode = bomH.BOMCode;
                        _db.BOMLines.Add(NewBomLine);
                        _db.SaveChanges();

                    }
                }
                    
                BomL = _db.GetBOMLinesFromBomHeaderID(bomH.BomHID, CurrentUser.CoID)
                    .Select(bom => new BoMLineN
                    {
                        BLID = bom.BLID,
                        ItemCode = bom.ItemCode ?? "",
                        BomCode = bom.BomCode ?? "",
                        ItemID = bom.ItemID.GetValueOrDefault(),
                        Description = bom.Description ?? "",
                        RMQty = bom.RMQty.GetValueOrDefault(),
                        AvCost = bom.AvCost.GetValueOrDefault(),
                        BomUnit = bom.BomUnit ?? ""
                    }).ToList();

                    foreach (BoMLineN bl in BomL)
                    {
                        if (bl.AvCost > 0 && bl.RMQty > 0)
                        {
                            bl.AvRMCost = bl.AvCost * bl.RMQty;
                        }
                    }

                var totalRMQty = BomL.Sum(bomLine => bomLine.RMQty);
                var totalRMCost = BomL.Sum(bomLine => bomLine.AvRMCost);

                if (BomL.Count > 0)
                {
                    GridBOMLines.DataSource = BomL;
                    GridBOMLines.DataBind();
                    GridBOMLines.FooterRow.Cells[2].Text = "Total";
                    GridBOMLines.FooterRow.Cells[3].Text = totalRMQty.ToString("N4");
                    GridBOMLines.FooterRow.Cells[6].Text = totalRMCost.ToString("N4");
                }
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
                BL.BomUnit = row.Cells[4].Text.ToString();
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
                var item = (BoMLineN)e.Row.DataItem;  
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
            string message = "";
            if (txtBomCode.Text.ToString().Trim().Length < 3)
           {
                message = "Please capture a BOM Code longer than 3 characters";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }
            if (txtBomDescript.Text.ToString().Trim().Length < 5)
            {
                message = "Please capture a BOM Description longer that 5 charatcters";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }
            if (DDFGCode.SelectedIndex == 0)
            {
                message = "Please select a valid Finished Goods code before continuing";
                AlertHelper.ShowSweetAlert(this, message, "warning");
                return;
            }

            decimal adc1 = 0, adc2 = 0, adc3 = 0;
            if (txtAdd1.Text.ToString().Trim().Length > 0) adc1 = Convert.ToDecimal(txtAdd1.Text, CultureInfo.InvariantCulture);
            if (txtAdd2.Text.ToString().Trim().Length > 0) adc2 = Convert.ToDecimal(txtAdd2.Text, CultureInfo.InvariantCulture);
            if (txtAdd3.Text.ToString().Trim().Length > 0) adc3 = Convert.ToDecimal(txtAdd3.Text, CultureInfo.InvariantCulture);

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var bomH = _db.BOMHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.BomHID == bomid).FirstOrDefault();
                bomH.BOMCode = txtBomCode.Text;
                bomH.BomDescript = txtBomDescript.Text;
                bomH.FGCode = DDFGCode.SelectedItem.Text;
                bomH.FGDescript = lblFGDescript.Text.ToString() ?? "";
                bomH.FGID = Convert.ToInt64(DDFGCode.SelectedValue);
                bomH.AddCost01 = adc1;
                bomH.AddCost02 = adc2;
                bomH.AddCost03 = adc3;
                bomH.BomActive = true;
                var BomL = _db.BOMLines.Where(x => x.BomCode == "NEW").ToList();
                foreach (var bl in BomL)
                {
                    bl.BomCode = txtBomCode.Text;
                }

                _db.SaveChanges();
            }

           message = "Succesfully Saved";
            AlertHelper.ShowSweetAlert(this, message, "success");
            Response.Redirect("~/BOMDetailed.aspx?bomid=" + bomid, false);
        }

        private class BoMLineN
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

        protected void DDFGCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            long ItemID = Convert.ToInt64(DDFGCode.SelectedValue);
            if (ItemID > 0) {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    string itemdesc = _db.ItemsMasters.Where(x => x.ID == ItemID).FirstOrDefault().Description.ToString();
                    lblFGDescript.Text = itemdesc;
                }
            }
        }

        protected void lbtnDeleteBom_Click1(object sender, EventArgs e)
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
    }
}