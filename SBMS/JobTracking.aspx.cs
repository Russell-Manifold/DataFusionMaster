using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class JobTracking : BasePage
    {
        long Coid;
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

            lblUsername.Text = $":.. {CurrentUser.UserName} ..:";
            Coid = CurrentUser.CoID;
            if (!IsPostBack)
            {
                showhidebuttons();
                LoadWorkstations();
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
        private void LoadWorkstations()
        {
            kanbanboard.Controls.Clear();
            
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var workstations = _db.WorkStations
                                      .Where(x => x.CompanyID == Coid && x.WSActive == true && x.WSName.ToLower() != "complete")
                                      .Select(x => new { x.WSID, x.WSName, x.Seq, x.IsWIP })
                                      .OrderBy(x => x.Seq)
                                      .ToList();

                foreach (var station in workstations)
                {
                    Boolean iswip = false;
                    if (station.IsWIP == true) { iswip = true; };
                    AddColumn(station.WSID, station.WSName, iswip);
                }
            }
         }

        private void AddColumn(int stationId, string stationName, Boolean isWIP)
        {
            var newColumn = new Panel { CssClass = "column", ID = stationId.ToString() };
            if (isWIP)
            {
                newColumn.Controls.Add(new Literal { Text = $"<h2>{stationName} (WIP)</h2>" });
            }
            else
            {
                newColumn.Controls.Add(new Literal { Text = $"<h2>{stationName}</h2>" });
            }
            DateTime duedt = Convert.ToDateTime("01 Jan 1900");
            // Fetch and add jobs to the column
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string findstr = txtfind.Text.ToString().ToLower();
                IQueryable<GetAllActiveJobCards_Result> jobs;
                try
                {
                    duedt = Convert.ToDateTime(txtDueDt.Text);
                }
                catch { }
                if (duedt != Convert.ToDateTime("01 Jan 1900"))
                {
                    jobs = _db.GetAllActiveJobCards(Coid).Where(j => j.JCWSID == stationId && j.DueDelDate <= duedt).AsQueryable();
                }
                else
                {
                    jobs = _db.GetAllActiveJobCards(Coid).Where(j => j.JCWSID == stationId).AsQueryable();
                }

                if (txtfind.Text.ToString().Trim().Length > 1)
                {
                    jobs = jobs.Where(x => x.DocumentNumber.ToLower().Contains(findstr) || x.CustSupName.ToLower().Contains(findstr) || x.JCNumber.ToLower().Contains(findstr));
                }

                foreach (var job in jobs)
                {
                    var jobDiv = new Panel { CssClass = "job", ID = "job_" + job.JCID };
                    var lbtnJobCard = new LinkButton
                    {
                        ID = "jobLink_" + job.JCID,
                        Text = job.JCNumber.ToString(),
                        ToolTip = "View Job Card"
                    };
                    lbtnJobCard.Attributes.Add("style", "color:#4A82AB; font-weight:600");

                    // Ensure DocGUID is properly escaped for JavaScript
                    string docGuid = job.DocGUID.ToString().Replace("'", "\\'");

                    // Add an ondragstart attribute to prevent dragging of LinkButton
                    lbtnJobCard.Attributes.Add("ondragstart", "event.preventDefault();");

                    lbtnJobCard.OnClientClick = $"return openJobCard('{docGuid}');";
                    jobDiv.Controls.Add(lbtnJobCard);
                    string CustName = job.CustSupName;
                    if (CustName.Length > 15) CustName = CustName.Substring(0, 12) + "...";
                    string FCSumm = job.JCSummary;
                    if (FCSumm != null && FCSumm.ToString().Length > 25) FCSumm = FCSumm.Substring(0, 22) + "...";
                    if (job.JCQtyOfItems != null && job.DueDelDate != null) jobDiv.Controls.Add(new Literal { Text = CustName + " : " + Convert.ToDateTime(job.DueDelDate).ToString("dd MMM") +  "<br />" + "JC Qty: " + Math.Round((decimal)job.JCQtyOfItems,0) + " - " + FCSumm});
                    jobDiv.ToolTip = "Drag and Drop to move this Job Card to the next workflow process";
                    newColumn.Controls.Add(jobDiv);
                }
            }
            kanbanboard.Controls.Add(newColumn);
        }

        protected void btnSaveQuantity_Click(object sender, EventArgs e)
        {
            try
            {
                int jobId = Convert.ToInt32(hiddenJobId.Value);
                int toWS = Convert.ToInt32(newWsID.Value);

                decimal quantity = 0;
                decimal RejQty = 0;

                if (decimal.TryParse(txtQuantity.Text, out quantity)) {}
                if (decimal.TryParse(txtRejQuantity.Text, out RejQty)) {}

                    // Save job transaction
                SaveJobTransaction(jobId, quantity, RejQty, toWS);
                LoadWorkstations();
                // Clear modal inputs
                txtQuantity.Text = string.Empty;

                // Close the modal and refresh the kanban board
                ScriptManager.RegisterStartupScript(this, GetType(), "CloseModal", "closeModal();", true);
                LoadWorkstations();
                UpdatePanel1.Update();
            }
            catch (Exception ex)
            {
                // Handle exception
                Console.WriteLine("Error: " + ex.Message);
            }
        }
        // Example method to save job transaction
        private void SaveJobTransaction(int jobId, decimal quantity, decimal RejQty, int ToStat)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // get max stat id, 
                var result = _db.WorkStations.Where(ws => ws.CompanyID == Coid).OrderByDescending(ws => ws.Seq).Select(ws => new { ws.Seq, ws.WSID }).FirstOrDefault();

                // update jobstatus
                var JobM = _db.JobCardsMasters.Where(x => x.JCID == jobId).FirstOrDefault();
                int FrmSt = (int)JobM.JCWSID;
                var NewStat = _db.WorkStations.Where(x => x.CompanyID == Coid && x.WSID == ToStat).Select(x => new { x.WSName, x.Seq }).FirstOrDefault();
                JobM.JCStatus = NewStat.WSName;
                JobM.JCWSID = ToStat;

                if (result.Seq > NewStat.Seq)
                {
                    var DocH = _db.DocHeaders.Where(x => x.CompanyID == Coid && x.LinkedJCID == jobId).FirstOrDefault();// set status complete to false  
                    DocH.Complete = false;
                    DocH.CompleteDate = null;
                    DocH.CompBy = null;
                }
                else if (result.Seq == NewStat.Seq)
                {
                    var DocH = _db.DocHeaders.Where(x => x.CompanyID == Coid && x.LinkedJCID == jobId).FirstOrDefault();
                    DocH.Complete = true;
                    DocH.CompleteDate = DateTime.Now;
                    DocH.CompBy = CurrentUser.RoleID;
                }
                _db.SaveChanges();

                var jobTransaction = new JobTransaction
                {
                    JCID = jobId,
                    MoveQty = quantity,
                    RejectQty = RejQty,
                    MoveDate = DateTime.Now,
                    FromStationID = FrmSt,
                    ToStationID = ToStat,
                    CompanyID = Coid ,
                    MoveBy = CurrentUser.RoleID
                    // Set other properties as needed
                };
                _db.JobTransactions.Add(jobTransaction);

                // get users linked to PS notifications
                var PSUsers = _db.RolesMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.NotifyJCMove == true).ToList();
                if (PSUsers.Count > 0)
                {
                    foreach (var usr in PSUsers)
                    {
                        var Notif = new Notification
                        {
                            Message = JobM.JCNumber + " Moved to Process " + JobM.JCStatus.ToString(),
                            IsRead = false,
                            CreatedAt = DateTime.Now,
                            CompanyID = Coid,
                            UserRoleID = usr.RoleID
                        };
                        _db.Notifications.Add(Notif);
                    }
                }
                _db.SaveChanges();

                // ADD EMAILER TO station
            }
        }

        protected void lbtnCancel_Click(object sender, EventArgs e)
        {
            // Close the modal and refresh the kanban board
            ScriptManager.RegisterStartupScript(this, GetType(), "CloseModal", "closeModal();", true);
            LoadWorkstations();
            UpdatePanel1.Update();
        }

        protected void lbtnfind_Click(object sender, EventArgs e)
        {
            LoadWorkstations();
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
