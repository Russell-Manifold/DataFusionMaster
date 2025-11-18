using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class PickingSlipTracking : BasePage
    {
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
            if (!IsPostBack)
            {
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
                var pickprocesses = _db.PickSlipProcesses
                                      .Where(x => x.CompanyID == CurrentUser.CoID && x.PSActive == true && x.PSName.ToLower() != "complete")
                                      .Select(x => new { x.PSPID, x.PSName, x.Seq })
                                      .OrderBy(x => x.Seq)
                                      .ToList();
                foreach (var proc in pickprocesses)
                {
                    AddColumn(proc.PSPID, proc.PSName);
                }
            }
         }

        private void AddColumn(int stationId, string stationName)
        {
            UserDetails userDetails = CurrentUser;
            DateTime duedt = Convert.ToDateTime("01 Jan 1900");
            var newColumn = new Panel { CssClass = "column", ID = stationId.ToString() };
            newColumn.Controls.Add(new Literal { Text = $"<h2>{stationName}</h2>" });

            // Fetch and add jobs to the column
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string findstr = txtfind.Text.ToString().ToLower();
                IQueryable<GetAllActivePickingSlips_Result> pslips;
                try
                {
                    duedt = Convert.ToDateTime(txtDueDt.Text);
                }
                catch { }
                if (duedt != Convert.ToDateTime("01 Jan 1900"))
                {
                    pslips = _db.GetAllActivePickingSlips(userDetails.CoID).Where(j => j.PSStationID == stationId && j.DueDelDate <= duedt).OrderBy(x => x.DueDelDate).AsQueryable();
                }
                else
                {
                    pslips = _db.GetAllActivePickingSlips(userDetails.CoID).Where(j => j.PSStationID == stationId).OrderBy(x => x.DueDelDate).AsQueryable();
                }

                if (txtfind.Text.ToString().Trim().Length > 1)
                {
                    pslips = pslips.Where(x => x.DocumentNumber.ToLower().Contains(findstr) || x.CustSupName.ToLower().Contains(findstr) || x.PSIntNumber.ToLower().Contains(findstr));
                }

                foreach (var slip in pslips.ToList())
                {
                    var jobDiv = new Panel { CssClass = "job", ID = "ps_" + slip.PSID };
                    var lbtnPS = new LinkButton
                    {
                        ID = "psLink_" + slip.PSID,
                        Text = slip.PSIntNumber.ToString(),
                        ToolTip = "View Picking Slip"
                    };
                    lbtnPS.Attributes.Add("style", "color:#4A82AB; font-weight:600");

                    // Ensure DocGUID is properly escaped for JavaScript
                    string docGuid = slip.DocGUID.ToString().Replace("'", "\\'");

                    // Add an ondragstart attribute to prevent dragging of LinkButton
                    lbtnPS.Attributes.Add("ondragstart", "event.preventDefault();");

                    lbtnPS.OnClientClick = $"return openPickingSlip('{docGuid}');";
                    jobDiv.Controls.Add(lbtnPS);
                    string CustName = slip.CustSupName;
                    if (CustName.Length > 15) CustName = CustName.Substring(0, 12) + "...";
                    jobDiv.Controls.Add(new Literal { Text = "Due: " + Convert.ToDateTime(slip.DueDelDate).ToString("dd MMM") + "<br/>" + CustName});
                    jobDiv.ToolTip = "Drag and Drop to move this Picking Slip to the next workflow process";
                    newColumn.Controls.Add(jobDiv);
                }
            }
            kanbanboard.Controls.Add(newColumn);
        }

        protected void btnSaveQuantity_Click(object sender, EventArgs e)
        {
            try
            {
                int jobId = Convert.ToInt32(hiddenJobId.Value.Replace("ps_",""));
                int toWS = Convert.ToInt32(newWsID.Value);

                // Save slip transaction
                SavePSTransaction(jobId, toWS);
                LoadWorkstations();
               
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
        private void SavePSTransaction(int slipId, int ToStat)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                // need to get this picking slip and check if items have marked as picked and validate lot number tacking etc.

                
                
                // get max stat id, 
                var result = _db.PickSlipProcesses.Where(ws => ws.CompanyID == CurrentUser.CoID).OrderByDescending(ws => ws.Seq).Select(ws => new { ws.Seq, ws.PSPID }).FirstOrDefault();

                // update jobstatus
                var JobM = _db.PickingSlipMasters.Where(x => x.PSID == slipId).FirstOrDefault();
                int FrmSt = (int)JobM.PSStationID;
                JobM.PSStationID = ToStat;
                var newproc = _db.PickSlipProcesses.Where(x => x.PSPID == ToStat).Select(x => new { x.PSName, x.Seq }).FirstOrDefault();               
                JobM.PSStatus = newproc.PSName;
                JobM.PSStationID = ToStat;

                if (result.Seq > newproc.Seq)
                {
                    var DocH = _db.DocHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.LinkedPSID == slipId).FirstOrDefault();// set status complete to false  
                    DocH.Complete = false;
                    DocH.CompleteDate = null;
                    DocH.CompBy = null;
                }
                else if (result.Seq == newproc.Seq)
                {
                    var DocH = _db.DocHeaders.Where(x => x.CompanyID == CurrentUser.CoID && x.LinkedPSID == slipId).FirstOrDefault();
                    DocH.Complete = true;
                    DocH.CompleteDate = DateTime.Now;
                    DocH.CompBy = CurrentUser.RoleID;
                }
                _db.SaveChanges();

                var SlipTransaction = new PickSlipTransaction
                {
                    PSID = slipId,
                    MoveDate = DateTime.Now,
                    FromStationID = FrmSt,
                    ToStationID = ToStat,
                    CompanyID = CurrentUser.CoID,
                     MoveBy = CurrentUser.RoleID,
                    MoveQty = 1,
                    RejectQty = 0
                    // Set other properties as needed
                };
                _db.PickSlipTransactions.Add(SlipTransaction);


                // get users linked to PS notifications
                var PSUsers = _db.RolesMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.NotifyPSMove == true).ToList();
                if (PSUsers.Count > 0)
                {
                    foreach (var usr in PSUsers) 
                    {
                        var Notif = new Notification
                        {
                            Message = JobM.PSIntNumber + " Moved to " + newproc.PSName,
                            IsRead = false,
                            CreatedAt = DateTime.Now,
                            CompanyID = CurrentUser.CoID,
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

        protected void lbtnRefresh_Click(object sender, EventArgs e)
        {
            //string DS = ApiUrlCall.LoadSalesOrders(CurrentUser);
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
