using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ConfigProcesses : BasePage
    {
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }

        protected override void Render(System.Web.UI.HtmlTextWriter writer)
        {
            if (GridPickProc.Rows.Count > 0)
            {
                foreach (GridViewRow row in GridPickProc.Rows)
                {
                    if (row.RowType == DataControlRowType.DataRow)
                    {
                        row.Attributes.Add("onclick", Page.ClientScript.GetPostBackEventReference(GridPickProc, "Select$" + row.RowIndex, true));
                        row.Attributes["onmouseover"] = "this.style.cursor='hand';this.style.textDecoration='underline';";
                        row.Attributes["onmouseout"] = "this.style.textDecoration='none';";
                    }
                }
            }

            if (GridJCProc.Rows.Count > 0)
            {
                foreach (GridViewRow row in GridJCProc.Rows)
                {
                    if (row.RowType == DataControlRowType.DataRow)
                    {
                        row.Attributes.Add("onclick", Page.ClientScript.GetPostBackEventReference(GridJCProc, "Select$" + row.RowIndex, true));
                        row.Attributes["onmouseover"] = "this.style.cursor='hand';this.style.textDecoration='underline';";
                        row.Attributes["onmouseout"] = "this.style.textDecoration='none';";
                    }
                }
            }
            base.Render(writer);
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

            if (!IsPostBack)
            {
                PnlJCProcesses.Style.Add("display", "inline-block");
                if (CurrentUser.UseModule2 == false)
                {
                    PnlJCProcesses.Style.Add("display", "none");
                } else if (CurrentUser.UseModule2 == false && CurrentUser.UseModule3 == false)
                {
                    PnlJCProcesses.Style.Add("display", "none");
                }
                GetProcesses();
            }
        }

        private void GetProcesses()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var PSProcs = _db.PickSlipProcesses.Where(x => x.CompanyID == CurrentUser.CoID).OrderBy(x => x.Seq).ToList();
                if (PSProcs != null)
                {
                    GridPickProc.DataSource = PSProcs;
                    GridPickProc.DataBind();
                }
                var JCProcs = _db.WorkStations.Where(x => x.CompanyID == CurrentUser.CoID).OrderBy(x => x.Seq).ToList();
                if (JCProcs != null)
                {
                    GridJCProc.DataSource = JCProcs;
                    GridJCProc.DataBind();
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
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
            }
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

        protected void PopMessage(string retmsg)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("<script type = 'text/javascript'>");
            sb.Append("window.onload=function(){");
            sb.Append("alert('");
            sb.Append(retmsg);
            sb.Append("')};");
            sb.Append("</script>");
            ClientScript.RegisterClientScriptBlock(this.GetType(), "alert", sb.ToString());
        }

        #region PickingProcesses
        protected void GridPickProc_RowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
        }

        protected void GridPickProc_SelectedIndexChanged(object sender, EventArgs e)
        {
            int prodid = Convert.ToInt16(GridPickProc.SelectedRow.Cells[0].Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var PSProc = _db.PickSlipProcesses.Where(x => x.CompanyID == CurrentUser.CoID && x.PSPID == prodid).FirstOrDefault();
                lblProcID.Text = prodid.ToString();
                txtprocess.Text = PSProc.PSName.ToString();
                txtseq.Text = PSProc.Seq.ToString();
                chkActive.Checked = Convert.ToBoolean(PSProc.PSActive);
            }
            Button25_ModalPopupExtender.Show();
        }

        protected void btnSaveConfirm_Click(object sender, EventArgs e)
        {
            int prodid =0;
            try
            {
                prodid = Convert.ToInt16(lblProcID.Text);
            }
            catch { }

            try
            {
                int Seqs = Convert.ToInt16(txtseq.Text.ToString());
            }
            catch
            {
                PopMessage("Invalid Sequence Number, unable to continue");
                return;
            }
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (prodid > 0)
                {
                    var PSProc = _db.PickSlipProcesses.Where(x => x.CompanyID == CurrentUser.CoID && x.PSPID == prodid).FirstOrDefault();
                    PSProc.PSName = txtprocess.Text.ToString().Replace("'", "''");
                    PSProc.Seq = Convert.ToInt16(txtseq.Text.ToString());
                    PSProc.PSActive = chkActive.Checked;
                }
                else
                {
                    PickSlipProcess PSProc = new PickSlipProcess();
                    PSProc.PSName = txtprocess.Text.ToString().Replace("'", "''");
                    PSProc.Seq = Convert.ToInt16(txtseq.Text.ToString());
                    PSProc.CompanyID = CurrentUser.CoID;
                    PSProc.PSActive = chkActive.Checked;
                    _db.PickSlipProcesses.Add(PSProc);
                }
                _db.SaveChanges();
                lblProcID.Text = "0";
                txtprocess.Text = string.Empty;
                txtseq.Text = string.Empty;
                chkActive.Checked = false;
                GetProcesses();
            }
        }

        protected void lbtnCancel_Click(object sender, EventArgs e)
        {
            lblProcID.Text = "0";
            txtprocess.Text = string.Empty;
            txtseq.Text = string.Empty;
            chkActive.Checked = false;
        }

        #endregion


        protected void GridJCProc_SelectedIndexChanged(object sender, EventArgs e)
        {
            int Jprodid = Convert.ToInt16(GridJCProc.SelectedRow.Cells[0].Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var JCProc = _db.WorkStations.Where(x => x.CompanyID == CurrentUser.CoID && x.WSID == Jprodid).FirstOrDefault();
                lblJProcID.Text = Jprodid.ToString();
                txtJCProcess.Text = JCProc.WSName.ToString();
                txtJCSeq.Text = JCProc.Seq.ToString();
                chkWIPJC.Checked = Convert.ToBoolean(JCProc.IsWIP);
                chkJCActive.Checked = Convert.ToBoolean(JCProc.WSActive);
            }
            ModalPopupExtender1.Show();
        }

        protected void GridJCProc_RowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
        }

        protected void lbtnCancelJ_Click(object sender, EventArgs e)
        {
            lblJProcID.Text = "0";
            txtJCProcess.Text = string.Empty;
            txtJCSeq.Text = string.Empty;
            chkWIPJC.Checked = false;
            chkJCActive.Checked = false;
        }

        protected void btnSaveConfirmJ_Click(object sender, EventArgs e)
        {
            int WSid = 0;
            try
            {
                WSid = Convert.ToInt16(lblJProcID.Text);
            }
            catch { }

            try
            {
                int Seqs = Convert.ToInt16(txtJCSeq.Text.ToString());
            } catch
            {
                PopMessage("Invalid Sequence Number, unable to continue");
                return;
            }
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (WSid > 0)
                {
                    var WSProc = _db.WorkStations.Where(x => x.CompanyID == CurrentUser.CoID && x.WSID == WSid).FirstOrDefault();
                    WSProc.WSName = txtJCProcess.Text.ToString().Replace("'", "''");
                    WSProc.Seq = Convert.ToInt16(txtJCSeq.Text.ToString());
                    WSProc.IsWIP = chkWIPJC.Checked;
                    WSProc.WSActive = chkJCActive.Checked;
                }
                else
                {
                    WorkStation WSProc = new WorkStation();
                    WSProc.WSName = txtJCProcess.Text.ToString().Replace("'", "''");
                    WSProc.Seq = Convert.ToInt16(txtJCSeq.Text.ToString());
                    WSProc.IsWIP = chkWIPJC.Checked;
                    WSProc.WSActive = chkJCActive.Checked;
                    WSProc.CompanyID = CurrentUser.CoID;
                    _db.WorkStations.Add(WSProc);
                }
                _db.SaveChanges();
                lblJProcID.Text = "0";
                txtJCProcess.Text = string.Empty;
                txtJCSeq.Text = string.Empty;
                chkWIPJC.Checked = false;
                chkJCActive.Checked = false;
                GetProcesses();
            }
        }
    }
}