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
    public partial class ForeCastDetailed : BasePage
    {
        private List<ItemsMaster> _items;
        long fcid = 0;

        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            fcid = Convert.ToInt64(Request.QueryString["fcid"].ToString());
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

            if (!IsPostBack)
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    _items = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CurrentUser.CoID && i.Physical ==true).OrderBy(x => x.Code).ToList();
                }
                LoadFCHeader();
                LoadFCLines();
            }      
        }

        protected void LoadFCHeader()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var FCHeader = _db.ForCastHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == fcid).FirstOrDefault();
                if (FCHeader != null)
                {
                    if (FCHeader.CustSupName != null) lblFCCustName.Text = FCHeader.CustSupName.ToString() ?? "";
                    if (FCHeader.Reference != null) lblFCRef.Text = FCHeader.Reference.ToString() ?? "";
                    lblcreatedDate.Text = Convert.ToDateTime(FCHeader.DocDate, CultureInfo.InvariantCulture).ToString("dd MMM yyyy");
                    if (FCHeader.FCastBy != null) lblCreatedBy.Text = FCHeader.FCastBy.ToString() ?? "";
                }
            }
        }

        protected void LoadFCLines()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var FCLines = _db.ForCastLines.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == fcid).ToList();
                if (!FCLines.Any())
                {
                    ForCastLine NewFCLine = new ForCastLine();
                    NewFCLine.DocID = fcid;
                    NewFCLine.CompanyID = CurrentUser.CoID;
                    NewFCLine.Active = true;
                    _db.ForCastLines.Add(NewFCLine);
                    _db.SaveChanges();
                }
                else
                {
                    var lastLine = FCLines.Last();
                    if (lastLine != null && lastLine.ItemDescription != null) // Check if NewJCLine and JCID are not null
                    {
                        ForCastLine NewFCLine = new ForCastLine();
                        NewFCLine.DocID = fcid;
                        NewFCLine.CompanyID = CurrentUser.CoID;
                        NewFCLine.Active = true;
                        _db.ForCastLines.Add(NewFCLine);
                        _db.SaveChanges();
                    }
                }
                FCLines = _db.ForCastLines.Where(x => x.CompanyID == CurrentUser.CoID && x.DocID == fcid).ToList();
                GridFCLines.DataSource = FCLines.OrderBy(x=>x.LineID);
                GridFCLines.DataBind();
                
            }
        }

        protected void lbtnDeleteBom_Click(object sender, EventArgs e)
        {

        }

        protected void lbtnDeleteFC_Click(object sender, EventArgs e)
        {

        }

        protected void LbtnSaveFC_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var FCHeader = _db.ForCastHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.ID == fcid).FirstOrDefault();
                if (FCHeader != null)
                {
                    FCHeader.Reference = lblFCRef.Text.ToString().Trim();
                    FCHeader.CustSupName = lblFCCustName.Text.ToString();
                    FCHeader.FCastBy = lblCreatedBy.Text.ToString();
                    _db.SaveChanges();
                    ShowMessage(sender, EventArgs.Empty, "Successfully Saved");
                }
            }
        }

        protected void GridFCLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var item = (ForCastLine)e.Row.DataItem;
               var ddlItemCode = (DropDownList)e.Row.FindControl("DDItemCode");

               if (ddlItemCode != null)
                    {
                        ddlItemCode.DataSource = _items;
                        ddlItemCode.DataTextField = "Code";
                        ddlItemCode.DataValueField = "ID";
                        ddlItemCode.DataBind();
                        ddlItemCode.Items.Insert(0, new ListItem("Select", "0"));
                        ddlItemCode.SelectedValue = item.SelectionId.ToString();
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
            LbtnSaveFC_Click(sender, EventArgs.Empty);

            LinkButton lbtn = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtn.NamingContainer;
            TextBox txtQty = (TextBox)row.FindControl("txtQty");
            DropDownList ddl = (DropDownList)row.FindControl("DDItemCode");
            TextBox txtDescription = (TextBox)row.FindControl("txtDescription");
            TextBox txtLineDate = (TextBox)row.FindControl("txtLineDate");

            DateTime dt;
            try {dt = Convert.ToDateTime(txtLineDate.Text.ToString(), CultureInfo.InvariantCulture);}
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
                var ThisFCLine = _db.ForCastLines.Where(x => x.CompanyID == CurrentUser.CoID && x.LineID == rowid).FirstOrDefault();
                ThisFCLine.SelectionId = ItemID;
                ThisFCLine.CompanyID = CurrentUser.CoID;
                ThisFCLine.Quantity = Convert.ToDecimal(txtQty.Text);
                ThisFCLine.ItemCode = ddl.SelectedItem.Text.ToString();
                ThisFCLine.ItemDescription = txtDescription.Text.ToString().Trim();
                ThisFCLine.DueDelDate = dt;
                ThisFCLine.Active = true;
                _db.SaveChanges();
                LoadFCLines();
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
                var ThisFCLine = _db.ForCastLines.Where(x => x.CompanyID == CurrentUser.CoID && x.LineID == rowid);
                _db.ForCastLines.RemoveRange(ThisFCLine);
                _db.SaveChanges();
                LoadFCLines();
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
                var ThisFCLine = _db.ForCastLines.Where(x => x.CompanyID == CurrentUser.CoID && x.LineID == rowid).FirstOrDefault();
                ThisFCLine.Comments = txtMsgBody.Text.Trim().ToString();
                _db.SaveChanges();
                LoadFCLines();
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
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }
    }
}