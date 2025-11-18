using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class Production : BasePage
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

            Coid = CurrentUser.CoID;
            if (!IsPostBack)
            {
                LoadProdPlan();
            }
        }

        protected void LoadProdPlan()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var PPlan = _db.ProdPlanLines.Where(x => x.CompanyID == Coid && x.Active == true && x.PlanQuantity != null && x.PlanQuantity >0).OrderBy(x=>x.PlanDate).ToList();
                GridProdPlan.DataSource = PPlan;
                GridProdPlan.DataBind();
            }
        }

        protected void lbtnLineSave_Click(object sender, EventArgs e)
        {
            LinkButton lbtnLotGen = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnLotGen.NamingContainer;
            TextBox txtLotNum = (TextBox)row.FindControl("txtLotNum");
            TextBox txtDate = (TextBox)row.FindControl("txtDate");
            TextBox txtActQty = (TextBox)row.FindControl("txtActQty");
            TextBox txtComments = (TextBox)row.FindControl("txtComments");
            TextBox txtReject = (TextBox)row.FindControl("txtReject");
            CheckBox ChkComp = (CheckBox)row.FindControl("chkComplete");
            DateTime prodDt = Convert.ToDateTime("01 Jan 2000");

            decimal ProdQty = 0;
            string RetMsg = "";
            try
            {
                prodDt = Convert.ToDateTime(txtDate.Text);
            }
            catch
            {

                RetMsg = "Invalid date, unable to generate a Lot Number";
            }
            try
            {
                ProdQty = Convert.ToDecimal(txtActQty.Text);
            }
            catch
            {
                RetMsg = "Invalid Quantity, unable to generate a Lot Number";
            }

            if (prodDt == Convert.ToDateTime("01 Jan 2000"))
            {
                txtActQty.Text = string.Empty;
                txtLotNum.Text = string.Empty;
                RetMsg = string.Empty;
                txtDate.Text = string.Empty;
            }

            if (RetMsg.Length > 0)
            {
                PopMessage(RetMsg);
                return;
            }

            decimal rejQty = 0;
            if (!string.IsNullOrEmpty(txtReject.Text))
            {
                // Try to parse the text to a decimal
                decimal.TryParse(txtReject.Text, out rejQty);        
            }

            long LineNum = Convert.ToInt64(lbtnLotGen.CommandArgument);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var PPlanL = _db.ProdPlanLines.Where(x => x.LineID == LineNum).FirstOrDefault();
                PPlanL.ProdLotNum = txtLotNum.Text;
                if (prodDt != Convert.ToDateTime("01 Jan 2000"))
                {
                    PPlanL.ActualDate = prodDt;
                }
                else
                {
                    PPlanL.ActualDate = null;
                }
                PPlanL.ActualQuantity = ProdQty;
                PPlanL.Comments = txtComments.Text.Replace("'", "''");
                PPlanL.ProdComplete = ChkComp.Checked;
                PPlanL.RejectQty = rejQty;
                _db.SaveChanges();
                PopMessage("Successfully Saved");
            }
        }

        protected void GridProdPlan_RowDataBound(object sender, GridViewRowEventArgs e)
        {

        }

        protected void ddProdPlanWeeks_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        protected int GetLotNum(long CoID, DateTime dt)
        {
            int lotno = 0;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var count = _db.ItemTransactions
                        .Where(it => it.CompanyID == CoID && it.TransactionDate == dt)
                        .Count();
                var count2 = _db.ProdPlanLines
                        .Where(it => it.CompanyID == CoID && it.ActualDate == dt)
                        .Count();
                lotno = count + count2 +1;
            }
            return lotno;
        }

        protected void lbtnLotGen_Click(object sender, EventArgs e)
        {
            LinkButton lbtnLotGen = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnLotGen.NamingContainer;
            TextBox txtLotNum = (TextBox)row.FindControl("txtLotNum");
            TextBox txtDate = (TextBox)row.FindControl("txtDate");
            TextBox txtActQty = (TextBox)row.FindControl("txtActQty");
            DateTime prodDt =Convert.ToDateTime("01 Jan 2000");
            CheckBox chkCompl = (CheckBox)row.FindControl("chkComplete");
            decimal ProdQty = 0;
            string RetMsg = "";
            try
            {
                prodDt = Convert.ToDateTime(txtDate.Text);
            }
            catch {
               
                RetMsg = "Invalid date, unable to generate a Lot Number";
            }
            try
            {
                ProdQty = Convert.ToDecimal(txtActQty.Text);
            }
            catch
            {
                RetMsg = "Invalid Quantity, unable to generate a Lot Number";
            }

            if (RetMsg.Length > 0)
            {
                PopMessage(RetMsg);
                return;
            }

            long LineNum = Convert.ToInt64(lbtnLotGen.CommandArgument);
            int recnum = GetLotNum(Coid, prodDt);
            chkCompl.Checked = true;
            txtLotNum.Text = prodDt.ToString("ddMMyyyy") + "INT" + recnum.ToString();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var PPlanL = _db.ProdPlanLines.Where(x => x.LineID == LineNum).FirstOrDefault();
                PPlanL.ProdLotNum = txtLotNum.Text;
                PPlanL.ActualDate = prodDt;
                PPlanL.ActualQuantity = ProdQty;
                PPlanL.ProdComplete = true;
                _db.SaveChanges();
            }
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

        protected void LbtnSaveProd_Click(object sender, EventArgs e)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var PPlanLines = _db.ProdPlanLines.Where(x => x.CompanyID == Coid && x.ActualQuantity > 0 && x.ProdComplete == true && x.Active==true).ToList();
                // add items to stock and removed RM's from stock.





            }
       }

        protected void btnDownloadToExcel_Click(object sender, EventArgs e)
        {
            DateTime dtP = DateTime.Today;
            try
            {
                dtP = Convert.ToDateTime(txtFromDate.Text);
            }
            catch
            {
                PopMessage("Invalid date, unable to continue");
                return;
            }
            int incl = RBIncl.SelectedIndex;
                
            DataTable planLinesDataTable = GetPlanLines(Coid, dtP.AddDays(-1), incl);
            planLinesDataTable.Columns.Remove("LineID");
            planLinesDataTable.Columns.Remove("CompanyID"); 
            // send to excel
            ExcelHelper.ExportToExcel(planLinesDataTable, "ProdPlanLines", "ProdPlanLines");
        }

        public DataTable GetPlanLines(long Coid, DateTime dtP, int incl)
        {
            List<ProdPlanLine> planLines;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                if (incl == 0)
                {
                    planLines = _db.ProdPlanLines
                        .Where(x => x.CompanyID == Coid && x.PlanDate > dtP)
                        .ToList();
                }
                else if (incl == 1)
                {
                    planLines = _db.ProdPlanLines
                        .Where(x => x.CompanyID == Coid && x.PlanDate > dtP && x.ProdComplete == false)
                        .ToList();
                }
                else 
                {
                    planLines = _db.ProdPlanLines
                        .Where(x => x.CompanyID == Coid && x.PlanDate > dtP && x.ProdComplete == true)
                        .ToList();
                }
            }
            return DataTableHelper.ConvertToDataTable(planLines);
        }

       
        protected void btnSaveConfirm_Click(object sender, EventArgs e)
        {

        }

        protected void lbtnDownload_Click(object sender, EventArgs e)
        {

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