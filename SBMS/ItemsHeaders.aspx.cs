using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ItemsHeaders : BasePage
    {
        private UserDetails CurrentUser
        {
            get
            {
                return Session["UserDetails"] as UserDetails;
            }
        }
        protected async void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                string returnUrl = HttpUtility.UrlEncode(Request.RawUrl);
                Response.Redirect("~/Login.aspx?returnUrl=" + returnUrl, false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            
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

                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    var CompanySettings = _db.CompanyMasters.Where(x => x.SBCACoID == CurrentUser.CoID).FirstOrDefault();
                    if (CompanySettings.UseLotTracking == false)
                    {
                        ViewState["UseLotTrack"] = false;
                        var itms = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID).ToList();
                        foreach (var itm in itms)
                        {
                            itm.IsLotTracked = false;
                        }
                    }
                    _db.SaveChanges();
                }
                
                ViewState["SortExpression"] = "Code"; // Default sort expression
                ViewState["SortDirection"] = "ASC"; // Default sort direction
                LoadItems(); // Load data initially
            }
        }

        protected void GridItems_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortExpression = e.SortExpression;
            string sortDirection = GetSortDirection(sortExpression);

            ViewState["SortExpression"] = sortExpression;
            ViewState["SortDirection"] = sortDirection;

            LoadItems();
        }

        private string GetSortDirection(string sortExpression)
        {
            string sortDirection = "ASC";
            string previousSortExpression = ViewState["SortExpression"] as string;

            if (previousSortExpression != null)
            {
                if (previousSortExpression == sortExpression)
                {
                    string previousSortDirection = ViewState["SortDirection"] as string;
                    if (previousSortDirection != null && previousSortDirection == "ASC")
                    {
                        sortDirection = "DESC";
                    }
                }
            }

            return sortDirection;
        }

        // DTO classes with exact property names from ItemsMaster
        public class ItemBaseInfo
        {
            public long ID { get; set; }
            public string Code { get; set; }
            public string Description { get; set; }
            public string CategoryDescript { get; set; }
            public decimal QuantityOnHand { get; set; }
            public decimal QuantityReserved { get; set; }
            public decimal ReorderLevel { get; set; }
        }

        // DTO class for stored procedure results
        public class ItemDisplayDTO
        {
            public long ID { get; set; }
            public string Code { get; set; }
            public string Description { get; set; }
            public string CategoryDescript { get; set; }
            public bool Physical { get; set; }
            public bool IsLotTracked { get; set; }
            public bool IsFinishedGoods { get; set; }
            public bool IsFromBOM { get; set; }
            public bool IsFromKit { get; set; }
            public bool IsBOMComponent { get; set; }
            public bool IsKitComponent { get; set; }
            public decimal QuantityOnHand { get; set; }
            public decimal QuantityReserved { get; set; }
            public decimal ReorderLevel { get; set; }
            public decimal TotQOH_MDF { get; set; }
            public decimal TotQPicked { get; set; }
            public decimal TotQJCard { get; set; }
        }

        protected void LoadItems()
        {
            try
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    string searchText = txtfind.Text;
                    bool excludeZero = chkZero.Checked;

                    // Call stored procedure
                    var itemsList = _db.Database.SqlQuery<ItemDisplayDTO>(
                        "EXEC sp_GetItemsForDisplay @CompanyID, @SearchText, @ExcludeZeroQty",
                        new System.Data.SqlClient.SqlParameter("@CompanyID", CurrentUser.CoID),
                        new System.Data.SqlClient.SqlParameter("@SearchText", string.IsNullOrEmpty(searchText) ? (object)DBNull.Value : searchText),
                        new System.Data.SqlClient.SqlParameter("@ExcludeZeroQty", excludeZero)
                    ).ToList();

                    // Apply number formatting
                    foreach (var item in itemsList)
                    {
                        item.QuantityOnHand = ApiUrlCall.NumberToDecimal(item.QuantityOnHand, CurrentUser.CompanyDecPlaces);
                        item.QuantityReserved = ApiUrlCall.NumberToDecimal(item.QuantityReserved, CurrentUser.CompanyDecPlaces);
                        item.TotQOH_MDF = ApiUrlCall.NumberToDecimal(item.TotQOH_MDF, CurrentUser.CompanyDecPlaces);
                        item.TotQPicked = ApiUrlCall.NumberToDecimal(item.TotQPicked, CurrentUser.CompanyDecPlaces);
                        item.TotQJCard = ApiUrlCall.NumberToDecimal(item.TotQJCard, CurrentUser.CompanyDecPlaces);
                    }

                    // Apply sorting in memory
                    string sortExpression = ViewState["SortExpression"] as string;
                    string sortDirection = ViewState["SortDirection"] as string;

                    if (!string.IsNullOrEmpty(sortExpression))
                    {
                        itemsList = ApplyInMemorySorting(itemsList, sortExpression, sortDirection);
                    }

                    if (chkService.Checked)
                    {
                        itemsList = itemsList.Where(x => x.Physical == false).ToList();
                    }
                    GridItems.DataSource = itemsList;
                    GridItems.DataBind();

                    if (CurrentUser.UseAutoManf)
                    {
                        GridItems.Columns[8].Visible = false;
                    }

                    lblReccount.Text = itemsList.Count.ToString() + " Records Found";
                }
            }
            catch (Exception ex)
            {
                lblReccount.Text = "Error loading items: " + ex.Message;
            }
        }
        // In-memory sorting helper
        private List<ItemDisplayDTO> ApplyInMemorySorting(List<ItemDisplayDTO> items, string sortExpression, string sortDirection)
        {
            switch (sortExpression)
            {
                case "Code":
                    return sortDirection == "DESC" ?
                        items.OrderByDescending(x => x.Code).ToList() :
                        items.OrderBy(x => x.Code).ToList();
                case "Description":
                    return sortDirection == "DESC" ?
                        items.OrderByDescending(x => x.Description).ToList() :
                        items.OrderBy(x => x.Description).ToList();
                case "CategoryDescript":
                    return sortDirection == "DESC" ?
                        items.OrderByDescending(x => x.CategoryDescript).ToList() :
                        items.OrderBy(x => x.CategoryDescript).ToList();
                case "QuantityOnHand":
                    return sortDirection == "DESC" ?
                        items.OrderByDescending(x => x.QuantityOnHand).ToList() :
                        items.OrderBy(x => x.QuantityOnHand).ToList();
                case "QuantityReserved":
                    return sortDirection == "DESC" ?
                        items.OrderByDescending(x => x.QuantityReserved).ToList() :
                        items.OrderBy(x => x.QuantityReserved).ToList();
                case "TotQOH_MDF":
                    return sortDirection == "DESC" ?
                        items.OrderByDescending(x => x.TotQOH_MDF).ToList() :
                        items.OrderBy(x => x.TotQOH_MDF).ToList();
                default:
                    return items.OrderBy(x => x.Code).ToList();
            }
        }
        //protected void LoadItems()
        //{
        //    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //    {
        //        string searchText = txtfind.Text?.ToLower();

        //        IQueryable<ItemsMaster> Items = _db.ItemsMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.Active == true && x.Physical == true);

        //        if (!string.IsNullOrEmpty(searchText)) Items = Items.Where(x => x.Code.ToLower().Contains(searchText) || x.CategoryDescript.ToLower().Contains(searchText) || x.Description.ToLower().Contains(searchText));

        //        // Get sort expression and direction from ViewState
        //        string sortExpression = ViewState["SortExpression"] as string;
        //        string sortDirection = ViewState["SortDirection"] as string;

        //        if (chkZero.Checked == true) Items = Items.Where(x => x.QuantityOnHand != 0);
        //        // Apply sorting
        //        Items = ApplySorting(Items, sortExpression, sortDirection);
        //        var itemsList = Items.ToList(); // Execute the query and get the list

        //        foreach (var Itm in itemsList)
        //        {
        //            try
        //            {
        //                if (Convert.ToDecimal(Itm.ReorderLevel) < 0) Itm.ReorderLevel = Itm.ReorderLevel * -1;
        //                Itm.TotQOH_MDF = _db.ItemTransactions.Where(it => it.CompanyID == CurrentUser.CoID && it.ItemID == Itm.ID).Sum(it => it.Qty);
        //                Itm.TotQPicked = (from line in _db.PickSlipLines join master in _db.PickingSlipMasters on line.PSID equals master.PSID where master.PSActive == true && line.ItemCode == Itm.Code && line.PickComplete == true select line.Quantity).Sum();
        //                Itm.TotQJCard = (from line in _db.JobCardLines join master in _db.JobCardsMasters on line.JCID equals master.JCID where line.ItemCode == Itm.Code && line.PickComplete == true && master.JCActive == true && master.JCStatus != "Complete" select line.Quantity).Sum();

        //                Itm.TotQOH_MDF =  ApiUrlCall.NumberToDecimal(Convert.ToDecimal(Itm.TotQOH_MDF, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
        //                Itm.TotQPicked =  ApiUrlCall.NumberToDecimal(Convert.ToDecimal(Itm.TotQPicked, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
        //                Itm.TotQJCard =  ApiUrlCall.NumberToDecimal(Convert.ToDecimal(Itm.TotQJCard, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
        //                Itm.QuantityOnHand = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(Itm.QuantityOnHand, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
        //                Itm.QuantityReserved = ApiUrlCall.NumberToDecimal(Convert.ToDecimal(Itm.QuantityReserved, CultureInfo.InvariantCulture), CurrentUser.CompanyDecPlaces);
        //            }
        //            catch { }
        //        }
        //        GridItems.DataSource = itemsList;
        //        GridItems.DataBind();
        //        if (CurrentUser.UseAutoManf)
        //        {
        //            GridItems.Columns[8].Visible = false;
        //        }
        //        lblReccount.Text = itemsList.Count.ToString() + " Records Found";
        //    }
        //}

        protected void lbtnBOM_Click(object sender, EventArgs e)
        {
            LinkButton lbtnBOM = (LinkButton)sender;
            GridViewRow row = (GridViewRow)lbtnBOM.NamingContainer;
            int bomid = Convert.ToInt32(lbtnBOM.CommandArgument);
            Response.Redirect("~/ItemEdit.aspx?itm=" + lbtnBOM.CommandArgument);
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
            Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

        protected void GridItems_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            
            decimal SBCAQty = 0, MDFQty = 0, PickQty = 0, JCQty = 0;
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                try { SBCAQty = Convert.ToDecimal(e.Row.Cells[2].Text); } catch { }
                try { MDFQty = Convert.ToDecimal(e.Row.Cells[3].Text); } catch { }
                try { PickQty = Convert.ToDecimal(e.Row.Cells[4].Text); } catch { }
                try { JCQty = Convert.ToDecimal(e.Row.Cells[5].Text); } catch { }

                if (SBCAQty < MDFQty + PickQty + JCQty)
                {
                    e.Row.Cells[2].BackColor = System.Drawing.Color.Red;
                    e.Row.Cells[2].ForeColor = System.Drawing.Color.White;
                }
                else if (SBCAQty > MDFQty + PickQty + JCQty)
                {
                    e.Row.Cells[3].BackColor = System.Drawing.Color.Red;
                    e.Row.Cells[3].ForeColor = System.Drawing.Color.White;
                }
            }
            if (e.Row.RowType != DataControlRowType.Pager)
            { 
                if (CurrentUser.UseModule2 == false)
                {
                    e.Row.Cells[5].Visible = false;
                    e.Row.Cells[6].Visible = false;
                    e.Row.Cells[7].Visible = false;
                    e.Row.Cells[8].Visible = false;
                    e.Row.Cells[9].Visible = false;
                    e.Row.Cells[11].Visible = false;
                    e.Row.Cells[13].Visible = false;
                }
                if (CurrentUser.UseModule3 == false)
                {
                    e.Row.Cells[5].Visible = false;
                    e.Row.Cells[6].Visible = false;
                    e.Row.Cells[7].Visible = false;
                    e.Row.Cells[8].Visible = false;
                    e.Row.Cells[9].Visible = false;
                    e.Row.Cells[10].Visible = false;
                    e.Row.Cells[12].Visible = false;
                }
            }
         }
            
        protected void lbtnfind_Click(object sender, EventArgs e)
        {
            LoadItems();
        }
        protected void GridItems_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridItems.PageIndex = e.NewPageIndex;
            LoadItems();
        }

        protected void chkZero_CheckedChanged(object sender, EventArgs e)
        {
            LoadItems();
        }

        protected void chkIsFromBOM_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chkFromBom = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chkFromBom.NamingContainer;
            LinkButton lbtnBOM = new LinkButton();
            lbtnBOM = row.FindControl("lbtnBOM") as LinkButton;

            CheckBox chkisBom = new CheckBox();
            chkisBom = row.FindControl("chkIsBom") as CheckBox;

            int itemid = Convert.ToInt32(lbtnBOM.CommandArgument);
            bool IsBom = false;
            if (chkFromBom.Checked)
            {
                IsBom = true;
            }
          
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == itemid);
                 Itm.IsFromBOM = IsBom;
                _db.SaveChanges();
            }
        }
        protected void chkIsFromKit_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chkIsFromKit = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chkIsFromKit.NamingContainer;
            LinkButton lbtnBOM = new LinkButton();
            lbtnBOM = row.FindControl("lbtnBOM") as LinkButton;

            CheckBox chkIskit = new CheckBox();
            chkIskit = row.FindControl("chkIskit") as CheckBox;

            int itemid = Convert.ToInt32(lbtnBOM.CommandArgument);
            bool IsBom = false, isComp = false;
            if (chkIsFromKit.Checked)
            {
                IsBom = true;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == itemid);
                Itm.IsKitComponent = isComp;
                Itm.IsFromKit = IsBom;
                _db.SaveChanges();
            }
        }

        protected void chkIsBom_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chkisBom = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chkisBom.NamingContainer;
            LinkButton lbtnBOM = new LinkButton();
            lbtnBOM = row.FindControl("lbtnBOM") as LinkButton;

            CheckBox chkFromBom = new CheckBox();
            chkFromBom = row.FindControl("chkIsFromBOM") as CheckBox;

            int itemid = Convert.ToInt32(lbtnBOM.CommandArgument);
            bool isComp = false;
            if (chkisBom.Checked)
            {
                isComp = true;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == itemid);
                Itm.IsBOMComponent = isComp;
                _db.SaveChanges();
            }
        }

        protected void chkIskit_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chkIskit = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chkIskit.NamingContainer;
            LinkButton lbtnBOM = new LinkButton();
            lbtnBOM = row.FindControl("lbtnBOM") as LinkButton;

            CheckBox chkIsFromKit = new CheckBox();
            chkIsFromKit = row.FindControl("chkIsFromKit") as CheckBox;

            int itemid = Convert.ToInt32(lbtnBOM.CommandArgument);
            bool IsBom = false, isComp = true;
            if (chkIskit.Checked)
            {
                isComp = true;
                chkIsFromKit.Checked = false;
            }

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Itm = _db.ItemsMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.ID == itemid);
                Itm.IsKitComponent = isComp;
                Itm.IsFromKit = IsBom;
                _db.SaveChanges();
            }
        }

        protected void lbtndwnload_Click(object sender, EventArgs e)
        {
            ExportItemsToExcel();
        }
        protected void ExportItemsToExcel()
        {
            try
            {
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    string searchText = txtfind.Text;
                    bool excludeZero = chkZero.Checked;

                    // Call the same stored procedure for export
                    var itemsList = _db.Database.SqlQuery<ItemDisplayDTO>(
                        "EXEC sp_GetItemsForDisplay @CompanyID, @SearchText, @ExcludeZeroQty",
                        new System.Data.SqlClient.SqlParameter("@CompanyID", CurrentUser.CoID),
                        new System.Data.SqlClient.SqlParameter("@SearchText", string.IsNullOrEmpty(searchText) ? (object)DBNull.Value : searchText),
                        new System.Data.SqlClient.SqlParameter("@ExcludeZeroQty", excludeZero)
                    ).ToList();

                    // Apply number formatting
                    foreach (var item in itemsList)
                    {
                        item.QuantityOnHand = ApiUrlCall.NumberToDecimal(item.QuantityOnHand, CurrentUser.CompanyDecPlaces);
                        item.QuantityReserved = ApiUrlCall.NumberToDecimal(item.QuantityReserved, CurrentUser.CompanyDecPlaces);
                        item.TotQOH_MDF = ApiUrlCall.NumberToDecimal(item.TotQOH_MDF, CurrentUser.CompanyDecPlaces);
                        item.TotQPicked = ApiUrlCall.NumberToDecimal(item.TotQPicked, CurrentUser.CompanyDecPlaces);
                        item.TotQJCard = ApiUrlCall.NumberToDecimal(item.TotQJCard, CurrentUser.CompanyDecPlaces);
                    }

                    // Convert to DataTable for Excel export
                    DataTable dt = ConvertToDataTable(itemsList);

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        string fileName = $"Items_Report_{DateTime.Now:yyyyMMdd_HHmmss}";
                        string wsName = "ItemsData";
                        ExcelHelper.ExportToExcel(dt, fileName, wsName);
                    }
                    else
                    {
                        lblReccount.Text = "No data available for export.";
                    }
                }
            }
            catch (Exception ex)
            {
                lblReccount.Text = "Error exporting to Excel: " + ex.Message;
            }
        }

        private DataTable ConvertToDataTable(List<ItemDisplayDTO> items)
        {
            DataTable dt = new DataTable();

            // Always include these columns
            dt.Columns.Add("Code", typeof(string));
            dt.Columns.Add("Description", typeof(string));
            dt.Columns.Add("Category", typeof(string));
            dt.Columns.Add("Physical", typeof(bool));  
            
            // Conditionally include Module3 columns (BOM related)
            if (CurrentUser.UseModule3)
            {
                dt.Columns.Add("IsFromBOM", typeof(bool));
                dt.Columns.Add("IsBOMComponent", typeof(bool));
            }

            // Conditionally include Module2 columns (Kit related)
            if (CurrentUser.UseModule2)
            {
                dt.Columns.Add("IsFromKit", typeof(bool));
                dt.Columns.Add("IsKitComponent", typeof(bool));
                dt.Columns.Add("QuantityReserved", typeof(decimal));
                dt.Columns.Add("TotalJobCard", typeof(decimal));
            }

            if (CurrentUser.CompanyUseLotNumbers)
            {
                dt.Columns.Add("IsLotTracked", typeof(bool));
            }

            dt.Columns.Add("Sage_QuantityOnHand", typeof(decimal));
            dt.Columns.Add("TotalPicked", typeof(decimal));
            dt.Columns.Add("ReorderLevel", typeof(decimal));
            dt.Columns.Add("DataFusion_QOH", typeof(decimal));

            foreach (var item in items)
            {
                DataRow dr = dt.NewRow();

                // Always set these values
                dr["Code"] = item.Code;
                dr["Description"] = item.Description;
                dr["Category"] = item.CategoryDescript;
                dr["Physical"] = item.Physical;
                
                // Conditionally set Module3 values (BOM related)
                if (CurrentUser.UseModule3)
                {
                    dr["IsFromBOM"] = item.IsFromBOM;
                    dr["IsBOMComponent"] = item.IsBOMComponent;
                }

                // Conditionally set Module2 values (Kit related)
                if (CurrentUser.UseModule2)
                {
                    dr["IsFromKit"] = item.IsFromKit;
                    dr["IsKitComponent"] = item.IsKitComponent;
                    dr["QuantityReserved"] = item.QuantityReserved;
                    dr["TotalJobCard"] = item.TotQJCard;
                }
                if (CurrentUser.CompanyUseLotNumbers)
                {
                    dr["IsLotTracked"] = item.IsLotTracked;
                } 
                    dr["Sage_QuantityOnHand"] = item.QuantityOnHand;
                dr["TotalPicked"] = item.TotQPicked;
                dr["ReorderLevel"] = item.ReorderLevel;
                dr["DataFusion_QOH"] = item.TotQOH_MDF;
                dt.Rows.Add(dr);
            }

            return dt;
        }
    }
}