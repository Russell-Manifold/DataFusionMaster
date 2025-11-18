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
    public partial class TransferHeaders : BasePage
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
                LoadTrfs();
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

        protected void LoadTrfs(string sortExpression = null, string sortDirection = null)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Trfs = from ith in _db.ItemTransferHeaders
                                        where ith.CompanyID == CoID
                                        let fromStore = _db.Stores.Where(s => s.StoreID == ith.TrfFromID).Select(s => s.StoreCode).FirstOrDefault()
                                        let toStore = _db.Stores.Where(s => s.StoreID == ith.TrfToID).Select(s => s.StoreCode).FirstOrDefault()
                                        select new
                                        {
                                            ith.TrfID,
                                            FrmStore = fromStore,
                                            ToStore = toStore,
                                            ith.TrfStatus,
                                            ith.TrfNotes,
                                            ith.TrfDate,
                                            ith.TrfActive,
                                            ith.TrfStarted,
                                            ith.TrfComplete,
                                            ith.TransferID,
                                            ith.TrfReference
                                        };

                // filter by status
                if (DDStatus.SelectedValue == "0")
                    Trfs = Trfs.Where(x => x.TrfActive == true);
                else if (DDStatus.SelectedValue == "2")
                    Trfs = Trfs.Where(x => x.TrfActive == false);
                else if (DDStatus.SelectedValue == "3")
                    Trfs = Trfs.Where(x => x.TrfStatus == "Deleted");

                // search filter
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    string search = txtSearch.Text.ToLower();
                    int woSearch;
                    bool isNumber = int.TryParse(search, out woSearch);

                    if (isNumber)
                    {
                        Trfs = Trfs.Where(x =>
                            x.TransferID == woSearch ||
                            x.TrfNotes.ToLower().Contains(search)||
                            x.TrfReference.ToLower().Contains(search));
                    }
                    else
                    {
                        Trfs = Trfs.Where(x =>
                            x.TrfNotes.ToLower().Contains(search) ||
                            x.TrfReference.ToLower().Contains(search));
                    }
                }

                // sort
                if (!string.IsNullOrEmpty(sortExpression) && !string.IsNullOrEmpty(sortDirection))
                {
                    Trfs = Trfs.OrderBy($"{sortExpression} {sortDirection}");
                }

                GridTrfs.DataSource = Trfs.ToList();
                GridTrfs.DataBind();
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

            LoadTrfs(e.SortExpression, sortDirection);
        }

       protected void lbtnWO_Click(object sender, EventArgs e)
        {
            LinkButton lbtnFC = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnFC.NamingContainer;
            Response.Redirect("~/TransferSlip.aspx?trfid=" + lbtnFC.CommandArgument);
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
            ItemTransferHeader TrfHead = new ItemTransferHeader();
            TrfHead.CompanyID = CoID;
            TrfHead.TrfStatus = "New";
            TrfHead.TrfActive = true;
            TrfHead.TrfDate = DateTime.Now;
              using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                int newTrf = _db.ItemTransferHeaders.Where(x => x.CompanyID == CoID).Count();
                TrfHead.TransferID = newTrf + 1;
                _db.ItemTransferHeaders.Add(TrfHead);
                _db.SaveChanges();       
               
                ItemTransferLine TrfLine = new ItemTransferLine();
                TrfLine.CompanyID = CoID;
                TrfLine.TrfID = TrfHead.TrfID;  
                _db.ItemTransferLines.Add(TrfLine);
                _db.SaveChanges();
                Response.Redirect("~/TransferSlip.aspx?trfid=" + TrfHead.TrfID, false);
            }
        }

        protected void lbtnDelete_Click(object sender, EventArgs e)
        {
            LinkButton lbtnFC = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnFC.NamingContainer;
            int fcid = Convert.ToInt32(lbtnFC.CommandArgument);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var woHead = _db.ItemTransferHeaders.Where(x => x.CompanyID == CoID && x.TrfID == fcid).FirstOrDefault();
                if (woHead != null)
                {
                    woHead.TrfActive = false;
                    woHead.TrfStatus = "Deleted";
                }
                
                var TrfLines = _db.ItemTransferLines.Where(x => x.CompanyID == CoID && x.TrfID == fcid).ToList();
                if (TrfLines != null)
                {
                    _db.ItemTransferLines.RemoveRange(TrfLines);
                }
                _db.SaveChanges();
                LoadTrfs();
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

       protected void lbtnSearch_Click(object sender, EventArgs e)
        {
            LoadTrfs();
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