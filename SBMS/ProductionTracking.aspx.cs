using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ProductionTracking : BasePage
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
            Coid = CurrentUser.CoID;
            if (!IsPostBack)
            {
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
                LoadWorkstations();
            }
        }

        private void LoadWorkstations()
        {
            kanbanboard.Controls.Clear();
            
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var workstations = _db.WorkStations
                                      .Where(x => x.CompanyID == Coid && x.WSActive == true)
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

            // Fetch and add jobs to the column
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var PPlines = _db.ProdPlanLines.Where(x=>x.CompanyID == Coid && x.ProdStationID == stationId && x.Active == true).ToList();
                foreach (var Ln in PPlines)
                {
                    var jobDiv = new Panel { CssClass = "job", ID = "job_" + Ln.LineID };

                    if (Ln.ItemCode != null && Ln.PlanQuantity != null)
                    {
                        var lbtnJobCard = new LinkButton
                        {
                            ID = "jobLink_" + Ln.LineID,
                            Text = Ln.ItemCode + " Qty: " + Math.Round((decimal)Ln.PlanQuantity, 0),
                            ToolTip = "View production history"
                        };
                        lbtnJobCard.Attributes.Add("style", "color:#4A82AB; font-weight:600");
                        lbtnJobCard.Attributes.Add("ondragstart", "event.preventDefault();");
                        lbtnJobCard.OnClientClick = $"return openProdPlanLine({Ln.LineID});";
                        jobDiv.Controls.Add(lbtnJobCard);
   
                        jobDiv.Controls.Add(new Literal { Text =" Due:" + Convert.ToDateTime(Ln.PlanDate).ToString("dd MMM") + "<br />" + Ln.ItemDescription});
                        jobDiv.ToolTip = "Drag and Drop to move this Job to the next workflow process";
                        newColumn.Controls.Add(jobDiv);
                    }
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
                // update jobstatus
                var ProdL = _db.ProdPlanLines.Where(x => x.LineID == jobId).FirstOrDefault();
                int FrmSt = (int)ProdL.ProdStationID;
                ProdL.ProdStationID = ToStat;
                string newproc = _db.PickSlipProcesses.Where(x => x.PSPID == ToStat).Select(x => x.PSName).FirstOrDefault();
                ProdL.ProdStatus = newproc;
                var ProdTransaction = new ProdTransaction
                {
                    ProdLineID = jobId,
                    MoveQty = quantity,
                    RejectQty = RejQty,
                    MoveDate = DateTime.Now,
                    FromStationID = FrmSt,
                    ToStationID = ToStat,
                    CompanyID = Coid,
                    MoveBy = CurrentUser.RoleID
                    // Set other properties as needed
                };
                _db.ProdTransactions.Add(ProdTransaction);


                // get users linked to PS notifications
                var PSUsers = _db.RolesMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.NotifyJCMove == true).ToList();
                if (PSUsers.Count > 0)
                {
                    foreach (var usr in PSUsers)
                    {
                        var Notif = new Notification
                        {
                            Message = ProdL.ItemCode + " " + ProdL.PlanQuantity + " Moved to Process " + newproc.ToString(),
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
