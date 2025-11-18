using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class WorksOrdersHeaders : BasePage
    {
        long CoID;
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
            SessionValidator.ValidateUserSession(CurrentUser);
            if (CurrentUser.UsePickSlipTracking != true) ibtnPickTrack.Style.Add("display", "none");
            if (CurrentUser == null) Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();

            CoID = CurrentUser.CoID;
            lblUsername.Text = $":.. {CurrentUser.UserName} ..:";
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
                showhidebuttons();
                //LoadWorkStations();
                LoadWOs();
            }
        }

        private void showhidebuttons()
        {
            if (CurrentUser.CanReceive != true) ibtmWorksOrders.Style.Add("display", "none");
            if (CurrentUser.CanViewPickSlips != true) ibtnPickSlips.Style.Add("display", "none");
            if (CurrentUser.CanTrackPickSlips != true) ibtnPickTrack.Style.Add("display", "none");
            if (CurrentUser.CanStockControl != true) ibtnStckCtl.Style.Add("display", "none");
            if (CurrentUser.UseModule2 == true)
            {
                if (CurrentUser.CanSalesForecast != true) ibtnFCasts.Style.Add("display", "none");
                if (CurrentUser.CanSeeFGDemands != true) ibtnmrp.Style.Add("display", "none");
                if (CurrentUser.CanTrackJobCards != true) ibtnJobTrack.Style.Add("display", "none");
            }
            else
            {
                ibtnFCasts.Style.Add("display", "none");
                ibtnmrp.Style.Add("display", "none");
                ibtnJobTrack.Style.Add("display", "none");
            }
            if (CurrentUser.UseModule3 == true)
            {
                if (CurrentUser.CanViewWorksOrders != true) ibtmWorksOrders.Style.Add("display", "none");
                if (CurrentUser.CanFillWorksOrders != true) ibtmWOrdMgment.Style.Add("display", "none");
                if (CurrentUser.CanViewRMD != true) ibtnRMD.Style.Add("display", "none");
            }
            else
            {
                ibtmWorksOrders.Style.Add("display", "none");
                ibtmWOrdMgment.Style.Add("display", "none");
                ibtnRMD.Style.Add("display", "none");
            }

        }

        protected void LoadWOs(string sortExpression = null, string sortDirection = null)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var WOs = _db.WorksOrderHeaders.Where(x => x.CompanyID == CoID);

                // filter by status
                if (DDStatus.SelectedValue == "0")
                    WOs = WOs.Where(x => x.Active == true);
                else if (DDStatus.SelectedValue == "2")
                    WOs = WOs.Where(x => x.Active == false);

                // search filter
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    string search = txtSearch.Text.ToLower();
                    int woSearch;
                    bool isNumber = int.TryParse(search, out woSearch);

                    if (isNumber)
                    {
                        WOs = WOs.Where(x =>
                            x.WONum == woSearch ||
                            x.Reference.ToLower().Contains(search) ||
                            x.LinkedDocumentNum.ToLower().Contains(search) ||
                            x.CustSupName.ToLower().Contains(search));
                    }
                    else
                    {
                        WOs = WOs.Where(x =>
                            x.Reference.ToLower().Contains(search) ||
                            x.LinkedDocumentNum.ToLower().Contains(search) ||
                            x.CustSupName.ToLower().Contains(search));
                    }
                }

                // sort
                if (!string.IsNullOrEmpty(sortExpression) && !string.IsNullOrEmpty(sortDirection))
                {
                    WOs = WOs.OrderBy($"{sortExpression} {sortDirection}");
                }

                GridWOs.DataSource = WOs.ToList();
                GridWOs.DataBind();
            }
        }

        protected void GridWOs_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortDirection = "ASC";

            if (ViewState["SortExpression"] as string == e.SortExpression)
            {
                // toggle
                sortDirection = (ViewState["SortDirection"] as string == "ASC") ? "DESC" : "ASC";
            }

            ViewState["SortDirection"] = sortDirection;
            ViewState["SortExpression"] = e.SortExpression;

            LoadWOs(e.SortExpression, sortDirection);
        }

        protected void GridWOs_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridWOs.PageIndex = e.NewPageIndex;

            string sortExpression = ViewState["SortExpression"] as string;
            string sortDirection = ViewState["SortDirection"] as string;

            LoadWOs(sortExpression, sortDirection);
        }

        //protected void LoadWOs()
        //{
        //    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //    {
        //        var WOs = _db.WorksOrderHeaders.Where(x => x.CompanyID == CoID);
        //        if (DDStatus.SelectedValue.ToString() == "0")
        //        {
        //            WOs = WOs.Where(x => x.Active == true);
        //        } 
        //        else if (DDStatus.SelectedValue.ToString() == "2")
        //        {
        //            WOs = WOs.Where(x => x.Active == false);
        //        }

        //        // Check if sorting is already set in ViewState
        //        string sortExpression = ViewState["SortExpression"] as string;
        //        string sortDirection = ViewState["SortDirection"] as string;

        //        if (txtSearch.Text.Length > 0)
        //        {
        //            WOs = WOs.Where(x=>x.Reference.ToLower().Contains(txtSearch.Text.ToLower()) || x.LinkedDocumentNum.ToLower().Contains(txtSearch.Text.ToLower()) || x.CustSupName.ToLower().Contains(txtSearch.Text.ToLower()));
        //        }

        //        if (!string.IsNullOrEmpty(sortExpression) && !string.IsNullOrEmpty(sortDirection))
        //        {
        //            // Create dynamic sort expression
        //            string sortQuery = $"{sortExpression} {sortDirection}";
        //            WOs = WOs.OrderBy(sortQuery);
        //        }

        //        GridWOs.DataSource = WOs.ToList();
        //        GridWOs.DataBind();
        //    }
        //}

        //protected void GridWOs_Sorting(object sender, GridViewSortEventArgs e)
        //{
        //    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //    {
        //        var WOs = _db.WorksOrderHeaders.Where(x => x.CompanyID == CoID);

        //        if (txtSearch.Text.Length > 0)
        //        {
        //            WOs = WOs.Where(x => x.Reference.ToLower().Contains(txtSearch.Text.ToLower()) || x.LinkedDocumentNum.ToLower().Contains(txtSearch.Text.ToLower()));
        //        }
        //        if (DDStatus.SelectedIndex > 0)
        //        {
        //            WOs = WOs.Where(x => x.Status == DDStatus.Text);
        //        }

        //        // Check if sorting direction is already stored in ViewState
        //        string sortDirection = ViewState["SortDirection"] as string ?? "ASC";
        //        if (ViewState["SortExpression"] as string == e.SortExpression)
        //        {
        //            // Toggle sorting direction
        //            sortDirection = (sortDirection == "ASC") ? "DESC" : "ASC";
        //        }
        //        else
        //        {
        //            // Default to ascending if a new column is being sorted
        //            sortDirection = "ASC";
        //        }

        //        ViewState["SortDirection"] = sortDirection;
        //        ViewState["SortExpression"] = e.SortExpression;

        //        // Apply dynamic sorting
        //        string sortExpression = $"{e.SortExpression} {sortDirection}";
        //        WOs = WOs.OrderBy(sortExpression);

        //        // Rebind sorted data to GridView
        //        GridWOs.DataSource = WOs.ToList();
        //        GridWOs.DataBind();
        //    }
        //}

        protected void lbtnWO_Click(object sender, EventArgs e)
        {
            LinkButton lbtnFC = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnFC.NamingContainer;
            Response.Redirect("~/WorksOrdersDetailed.aspx?woid=" + lbtnFC.CommandArgument);
        }

        protected void GridWOs_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var item = (WorksOrderHeader)e.Row.DataItem;
                if (item.Active == false)
                {
                    var lbtn = (LinkButton)e.Row.FindControl("lbtnDelete");
                    lbtn.Visible = false;
                }
            }
        }

       protected void lbtnCreateNew_Click(object sender, EventArgs e)
        {
            WorksOrderHeader WCHead = new WorksOrderHeader();
            WCHead.CompanyID = CoID;
            WCHead.Status = "NEW";
            WCHead.Active = true;
            WCHead.WOrderDate = DateTime.Now;
              using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                _db.WorksOrderHeaders.Add(WCHead);
                _db.SaveChanges();
                int newfcid = WCHead.ID;

                int woNumb = _db.WorksOrderHeaders.Where(x=>x.CompanyID == CoID)
                    .OrderByDescending(x => x.WONum)
                    .Select(x => x.WONum)
                    .FirstOrDefault();
                WCHead.WONum = woNumb+1;

                WorksOrderLine WoL = new WorksOrderLine();
                WoL.CompanyID = CoID;
                WoL.WOID = newfcid;
                WoL.Active = true;
                _db.WorksOrderLines.Add(WoL);
                _db.SaveChanges();
                Response.Redirect("~/WorksOrdersDetailed.aspx?woid=" + newfcid, false);
            }
        }

        protected void lbtnDelete_Click(object sender, EventArgs e)
        {
            LinkButton lbtnFC = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnFC.NamingContainer;
            int fcid = Convert.ToInt32(lbtnFC.CommandArgument);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var woHead = _db.WorksOrderHeaders.Where(x => x.CompanyID == CoID && x.ID == fcid).FirstOrDefault();
                if (woHead != null)
                {
                    woHead.Active = false;
                    woHead.WOrderCloseOffDate = DateTime.Now;
                    woHead.WOrderCloseBy = CurrentUser.UserName;
                    woHead.Message = "Deleted";
                }
                
                var woLines = _db.WorksOrderLines.Where(x => x.CompanyID == CoID && x.WOID == fcid).ToList();
                if (woLines != null)
                {
                    _db.WorksOrderLines.RemoveRange(woLines);
                }

                var psheader = _db.PickingSlipMasters.Where(x => x.CustomerID == CoID && x.LinkedWONumber == fcid).FirstOrDefault();
                if (psheader != null)
                {
                    psheader.LinkedWONumber = 0;
                }
                _db.SaveChanges();
                LoadWOs();
                ShowMessage(sender, EventArgs.Empty, "Successfully Deleted");
            }
         }

        protected void ShowMessage(object sender, EventArgs e, string msg)
        {
            string message = "alert('" + msg + "')";
            ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
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

        //protected void LoadWorkStations()
        //{
        //    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //    {
        //        var WoProcs = _db.WorkStations.Where(x => x.CompanyID == CurrentUser.CoID).OrderBy(x => x.Seq).ToList();
        //        if (WoProcs != null)
        //        {
        //            DDStatus.DataSource = WoProcs;
        //            DDStatus.DataTextField = "WSName";
        //            DDStatus.DataValueField = "WSName";
        //            DDStatus.DataBind();
        //            DDStatus.Items.Insert(0, "- Select -");
        //            DDStatus.Items.Insert(1, "NEW");
        //        }
        //    }
        //}

        protected void lbtnSearch_Click(object sender, EventArgs e)
        {
            LoadWOs();
        }

        protected void lbtnLinkedDoc_Click(object sender, EventArgs e)
        {
            LinkButton lbtnLinkedDoc = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnLinkedDoc.NamingContainer;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {                
                if (lbtnLinkedDoc.CommandArgument.StartsWith("SO"))
                {
                    var soh = _db.DocHeaders.Where(x => x.CompanyID == CoID && x.DocumentNumber == lbtnLinkedDoc.Text.Trim()).FirstOrDefault();
                    if (soh != null) Response.Redirect("~/SalesOrder.aspx?docid=" + soh.DocGUID.ToString(), true);
                }
                else if (lbtnLinkedDoc.CommandArgument.StartsWith("PS"))
                {
                    var psh = (from pickingSlip in _db.PickingSlipMasters
                               join docHeader in _db.DocHeaders
                               on pickingSlip.PSID equals docHeader.LinkedPSID
                               where docHeader.CompanyID == CoID
                               && pickingSlip.PSIntNumber == lbtnLinkedDoc.CommandArgument.Trim()
                               select docHeader.DocGUID).FirstOrDefault();     
                    if (psh != null) Response.Redirect("~/PickingSlip.aspx?docid=" + psh.ToString(), true);
                }
               else if (lbtnLinkedDoc.CommandArgument.StartsWith("JC"))
                {
                    var jch = (from docHeader in _db.DocHeaders
                               join jobCard in _db.JobCardsMasters
                               on docHeader.LinkedJCID equals jobCard.JCID
                               where docHeader.CompanyID == CoID
                                     && jobCard.JCNumber == lbtnLinkedDoc.CommandArgument.Trim()
                               select docHeader.DocGUID).FirstOrDefault();
                    if (jch != null) Response.Redirect("~/JobCard.aspx?docid=" + jch.ToString(), true);
                } else
                {
                    ShowMessage(sender, EventArgs.Empty, "No document available with this Document Number");
                }
            }
        }

        protected void imgbRec_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/OSPurchaseOrders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnPickSlips_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/OSSalesOrders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void imgbTrf_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/Transfer.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnPickTrack_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/PickingSlipTracking.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnStckCtl_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/StockControl.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnFCasts_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/ForeCastHeaders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnmrp_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/FGDemands.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnJobTrack_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/JobTracking.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtmWorksOrders_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/WorksOrdersHeaders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtmWOrdMgment_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/WorksOrdersManfHeaders.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void ibtnRMD_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/ProductionRMD.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }
        protected void imgdash_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }
    }
}