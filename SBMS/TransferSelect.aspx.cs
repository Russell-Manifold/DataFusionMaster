using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class TransferSelect : BasePage
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

                txtDtFrom.Text = DateTime.Today.AddMonths(-1).ToString("dd MMM yyyy");
                txtDtTo.Text = DateTime.Today.ToString("dd MMM yyyy");
                LoadItems();
                LoadStores();
            }
        }

        private void LoadItems()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Items = (from itm in _db.ItemsMasters
                             where itm.CompanyID == Coid
                             select new ItemsList
                             {
                                 ItemID = itm.ID,
                                 descript = itm.Code + " - " + itm.Description
                             }); ; 
                DDFromItem.DataSource = Items.ToList();
                DDFromItem.DataTextField = "descript";
                DDFromItem.DataValueField = "ItemID";
                DDFromItem.DataBind();
                DDFromItem.Items.Insert(0, "-All-");

                var Categs = _db.ItemsMasters.Where(itm => itm.CompanyID == CurrentUser.CoID).Select(x=>x.CategoryDescript).Distinct().ToList();
                DDCategory.DataSource = Categs.ToList();
                DDCategory.DataBind();
                DDCategory.Items.Insert(0, "-All-");

                var TrTypes = _db.ItemTransactions.Where(itm => itm.CompanyID == CurrentUser.CoID).Select(x => x.TransactionType).Distinct().ToList();
                DDType.DataSource = TrTypes.ToList();
                DDType.DataBind();
                DDType.Items.Insert(0, "-All-");
            }
        }

        private void LoadStores()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Items = (from str in _db.Stores
                             where str.CompanyID == Coid
                             select new StoresList
                             {
                                 StoreID = str.StoreID,
                                 StoreDesc = str.StoreDescript
                             }); ;
                DDFrmStore.DataSource = Items.ToList();
                DDFrmStore.DataTextField = "StoreDesc";
                DDFrmStore.DataValueField = "StoreID";
                DDFrmStore.DataBind();
                DDFrmStore.Items.Insert(0, "-All-");
               
                DDToStore.DataSource = Items.ToList();
                DDToStore.DataTextField = "StoreDesc";
                DDToStore.DataValueField = "StoreID";
                DDToStore.DataBind();
                DDToStore.Items.Insert(0, "-All-");
            }
        }

        public class ItemsList
        {
            public long ItemID { get; set; }
            public string descript { get; set; }
        }
        public class StoresList
        {
            public int StoreID { get; set; }
            public string StoreDesc { get; set; }
        }

       protected void lbtnViewTrf_Click(object sender, EventArgs e)
        {
            DateTime FrmDt = Convert.ToDateTime(txtDtFrom.Text, CultureInfo.InvariantCulture);
            DateTime ToDt = Convert.ToDateTime(txtDtTo.Text, CultureInfo.InvariantCulture);
            Response.Redirect("~/TransferHistory.aspx?coid=" + Coid + "&userid=0" + "&frmcde=" + DDFromItem.SelectedValue.ToString() + "&categ=" + DDCategory.SelectedValue.ToString() +
                "&frmdt=" + FrmDt.ToString("dd-MMM-yyyy") + "&todt=" + ToDt.ToString("dd-MMM-yyyy") +
                "&frmst=" + DDFrmStore.SelectedValue.ToString() + "&tost=" + DDToStore.SelectedValue.ToString() +
                "&tpe=" + DDType.SelectedValue.ToString());
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