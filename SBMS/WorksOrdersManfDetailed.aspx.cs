using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class WorksOrdersManfDetailed : BasePage
    {
        long CoID;
        private List<ItemsMaster> _items;
        private List<BOMHeader> _boms;
        private List<KitHeader> _kits;

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
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                    {
                        _items = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == CoID && i.Physical ==true && i.IsFinishedGoods == true).OrderBy(x => x.Code).ToList();
                        _boms = _db.BOMHeaders.Where(i => i.BomActive == true && i.CompanyID == CoID).OrderBy(x => x.BOMCode).ToList();
                        _kits = _db.KitHeaders.Where(i => i.KitActive == true && i.CompanyID == CoID).OrderBy(x => x.KitCode).ToList();
                    }
           
                LoadWorkStations();
                LoadWOHeader();
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
                    woheader.InnerText = "WO-" + WOHeader.WONum;
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
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var WOLines = _db.WorksOrderLines.Where(x => x.CompanyID == CoID && x.WOID == woid && x.Quantity >0).ToList();
                foreach (var itm in WOLines)
                {
                    if (itm.Quantity != null)
                    {
                        itm.Quantity = Convert.ToDecimal(ApiUrlCall.NumberToDecimal(itm.Quantity.ToString(), CurrentUser.CompanyDecPlaces));
                    }
                }
                GridWOLines.DataSource = WOLines.OrderBy(x=>x.LineID);
                GridWOLines.DataBind();       
            }
        }

       protected void LbtnSaveWO_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var FCHeader = _db.WorksOrderHeaders.Where(x => x.CompanyID == CoID && x.ID == woid).FirstOrDefault();
                if (FCHeader != null)
                {
                    FCHeader.Reference = lblFCRef.Text.ToString().Trim();
                    FCHeader.CustSupName = lblFCCustName.Text.ToString();
                    FCHeader.WOrderBy = lblCreatedBy.Text.ToString();
                    FCHeader.LinkedDocumentNum = txtLinkedDoc.Text.ToString();
                    FCHeader.Message = txtwomsg.Text.ToString();
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

        protected void LoadWorkStations()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var WoProcs = _db.WorkStations.Where(x => x.CompanyID == CurrentUser.CoID).OrderBy(x => x.Seq).ToList();
                if (WoProcs != null)
                {
                    DDStatus.DataSource = WoProcs;
                    DDStatus.DataTextField = "WSName";
                    DDStatus.DataValueField = "WSName";
                    DDStatus.DataBind();
                    DDStatus.Items.Insert(0, "- Select -");
                    DDStatus.Items.Insert(1, "NEW");
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

        }

        protected void lbtnAutoManf_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var WOHeader = _db.WorksOrderHeaders
                    .Where(x => x.CompanyID == CoID && x.ID == woid)
                    .FirstOrDefault();
                if (WOHeader == null)
                {
                    AlertHelper.ShowSweetAlert(this, "Works Order not found.", "error");
                    return;
                }
                if (WOHeader.Active == false || WOHeader.Status == "Complete")
                {
                    AlertHelper.ShowSweetAlert(this,
                        "This Works Order has already been manufactured and cannot be processed again.",
                        "warning");
                    return;
                }
            }
            Response.Redirect($"~/WorksOrdersManf.aspx?woid={woid}");
        }
    }
}