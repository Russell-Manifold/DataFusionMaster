using SBMS.Classes;
using SBMS.Models;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class TransferHistory : BasePage
    {
        long Coid;
        //long roleid = 1;
       long FrmItemid;
        string categ;
        string TrType;
        int FrmStore = 0;
        int ToStore = 0;
        DateTime Frmdt;
        DateTime Todt;
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
            if (Request.QueryString["frmcde"] != "-All-") FrmItemid = Convert.ToInt64(Request.QueryString["frmcde"]);
            if (Request.QueryString["categ"] != "-All-") categ = Request.QueryString["categ"];
            if (Request.QueryString["frmst"] != "-All-") FrmStore = Convert.ToInt32(Request.QueryString["frmst"]);
            if (Request.QueryString["tost"] != "-All-") ToStore = Convert.ToInt32(Request.QueryString["tost"]); 
            if (Request.QueryString["tpe"] != "-All-") TrType = Request.QueryString["tpe"]; 

            Frmdt = Convert.ToDateTime(Request.QueryString["frmdt"].Replace("-"," "), CultureInfo.InvariantCulture);
            Todt = Convert.ToDateTime(Request.QueryString["todt"].Replace("-", " "), CultureInfo.InvariantCulture);
            Todt = Todt.AddDays(1);

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

                LoadGrid();
            }
        }       
        protected void GridFromItems_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
        }
        protected void lbtnfind_Click(object sender, EventArgs e)
        {
            
        }

        private void LoadGrid()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var query = _db.GetItemTransferHistoryByDate(Coid, Frmdt, Todt).ToList();

                if (FrmItemid != 0)
                {
                    query = query.Where(it => it.ItemID >= FrmItemid).ToList();
                }

                if (categ != null)
                {
                    query = query.Where(it => it.Category == categ).ToList();
                }

                if (FrmStore != 0 && ToStore != 0)
                {
                    query = query.Where(it => it.FromID >= FrmStore && it.ToID == ToStore).ToList();
                } 
                else                
                if (FrmStore != 0)
                {
                    query = query.Where(it => it.FromID == FrmStore).ToList();
                }
                else 
                if (ToStore != 0)
                {
                    query = query.Where(it => it.ToID == ToStore).ToList();
                }
                if (TrType != null)
                {
                    if (TrType.Contains("JC-Dr") || TrType.Contains("JC-Mf"))
                    {
                        query = query.Where(it => it.TransactionType == "JC-Dr" || it.TransactionType == "JC-Mf" ).ToList();
                    }
                    else
                    {
                        query = query.Where(it => it.TransactionType == TrType).ToList();
                    }     
                }
                query = query.Where(it => it.Qty != 0).ToList();

                DataTable Dtb = DataTableHelper.ConvertToDataTable(query);
                foreach (DataRow dr in Dtb.Rows) 
                {
                    if (dr["DocNum"] == null || dr["DocNum"].ToString() == "")
                    {
                        if (dr["JCNum"] != null)
                        {
                            dr["DocNum"] = dr["JCNum"];
                        }                  
                    }
                }

                GridHistory.DataSource = Dtb;
                GridHistory.DataBind();
            }
        }

       protected void GridHistory_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridHistory.PageIndex = e.NewPageIndex;
            LoadGrid(); // Rebind data to the GridV
        }

        protected void GridHistory_Sorting(object sender, GridViewSortEventArgs e)
        {

        }

        protected void lbtndwnload_Click(object sender, EventArgs e)
        {
            DataTable TrfTable = GetTRFLines(Coid, Frmdt, Todt);
            TrfTable.Columns.Remove("ToID");
            TrfTable.Columns.Remove("FromID");

            ExcelHelper.ExportToExcel(TrfTable, "ItemMovements", "ItemMovements");
        }
        public DataTable GetTRFLines(long Coid, DateTime Frmdt, DateTime Todt)
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var query = _db.GetItemTransferHistoryByDate(Coid, Frmdt, Todt).ToList();
                query = query.Where(it => it.Qty > 0).ToList();
                return DataTableHelper.ConvertToDataTable(query);


            }
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
    }
}