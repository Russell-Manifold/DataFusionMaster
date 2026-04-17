using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class StockCountCreate : BasePage
    {
        int countid;
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

            countid = Convert.ToInt32(Request.QueryString["id"].ToString());

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
                CleanUpEmptyCounts();
                LoadDDs();
                ApplyFilterAndSort();
                // create stock coundID
                using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
                {
                    if (countid == 0)
                    {
                        var maxStCntID = _db.StockCountMasters
                        .Where(x => x.CompanyID == CurrentUser.CoID)
                        .Max(x => (int?)x.StCntID) ?? 0;

                        long LastCnt = maxStCntID;
                        var NewCnt = new StockCountMaster();
                        NewCnt.CompanyID = CurrentUser.CoID;
                        NewCnt.CtCreateDate = DateTime.Now;
                        NewCnt.CreatedBy = CurrentUser.RoleID;
                        _db.StockCountMasters.Add(NewCnt);
                        _db.SaveChanges();
                        lblCountID.Text = NewCnt.StCntID.ToString();
                        lblDate.Text = DateTime.Today.ToString("dd MMM yyyy");
                        lblCountID.Visible = false;
                    }
                    else
                    {
                        lblCountID.Text = countid.ToString();
                        var CntID = _db.StockCountMasters.Where(x => x.CompanyID == CurrentUser.CoID && x.StCntID == countid).FirstOrDefault();
                        txtRef.Text = CntID.CtDescription.ToString();
                        ApplyFilterAndSort();
                        LoadCountList();
                    }  
                }
            }
        }

        private void CleanUpEmptyCounts()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var toDelete = _db.StockCountMasters.Where(x => x.CompanyID == CurrentUser.CoID && (x.CtDescription == null || x.CtDescription.Trim() == "")).ToList();
                if (toDelete.Any())
                {
                    _db.StockCountMasters.RemoveRange(toDelete);
                    _db.SaveChanges();
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
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

       protected void LoadCount()
        {
            ApplyFilterAndSort();
        }

        protected void ApplyFilterAndSort(string sortExpression = null, string sortDirection = "ASC")
        {
           string filterText = txtFilter.Text;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
               
                var StkLines = _db.GetStockCountList(CurrentUser.CoID).AsQueryable();

               // Apply filters
                if (DDCateg.SelectedIndex > 0)
                {
                    string selectedCategory = DDCateg.SelectedValue;
                    StkLines = StkLines.Where(x => x.CategoryDescript == selectedCategory);
                }

                if (DDStore.SelectedIndex > 0)
                {
                    string selectedStore = DDStore.SelectedItem.Text;
                    StkLines = StkLines.Where(x => x.StoreCode == selectedStore);
                }

                if (!string.IsNullOrEmpty(filterText))
                {
                    StkLines = StkLines.Where(x => x.Code.ToLower().Contains(filterText.ToLower()) || x.Description.ToLower().Contains(filterText.ToLower()));
                }

                // Apply sorting using dynamic LINQ
                if (!string.IsNullOrEmpty(sortExpression))
                {
                    StkLines = StkLines.OrderBy($"{sortExpression} {sortDirection}");
                }

                GridItemsSelect.DataSource = StkLines.ToList();
                GridItemsSelect.DataBind();
            }
        }

        protected void GridCntLines_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortExpression = e.SortExpression;
            string sortDirection = e.SortDirection == SortDirection.Ascending ? "ASC" : "DESC";

            ApplyFilterAndSort(sortExpression, sortDirection);
        }

        protected void DDCateg_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadCount();
        }

        protected void DDStore_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadCount();
        }

        protected void lbtnSearch_Click(object sender, EventArgs e)
        {
            LoadCount();
        }

       protected void GridCntLines_RowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e)
        {
            e.Row.Cells[0].Visible = false;
        }

        protected void LoadDDs()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true && x.StoreCode != "CoR" && x.StoreCode != "CoD").OrderBy(x => x.StoreDescript).ToList();
                DDStore.DataSource = Stores;
                DDStore.DataTextField = "StoreCode";
                DDStore.DataValueField = "StoreID";
                DDStore.DataBind();
                DDStore.Items.Insert(0, "-Store-");

                var Categs = _db.ItemsMasters
                        .Where(x => x.CompanyID == CurrentUser.CoID && x.Active == true)
                        .Select(x => x.CategoryDescript)
                        .Distinct()
                        .OrderBy(x => x) // Order by CategoryDescript
                        .ToList();
                DDCateg.DataSource = Categs;
                DDCateg.DataBind();
                DDCateg.Items.Insert(0, "-Category-");
            }
        }

        protected void lbtnAddSelected_Click(object sender, EventArgs e)
        {
            ProcessSelectedRows();
            LoadCountList();
        }

        protected void ProcessSelectedRows()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                foreach (GridViewRow row in GridItemsSelect.Rows)
                {
                    CheckBox chkSelect = (CheckBox)row.FindControl("chkSelect");
                    if (chkSelect != null && chkSelect.Checked)
                    {
                        long itmid = Convert.ToInt64(row.Cells[0].Text);
                        var storesList = _db.GetItemLinkedStores(CurrentUser.CoID, itmid).ToList();
                        foreach (var stor in storesList)
                        {
                            StockCountLine NewLine = new StockCountLine();
                            NewLine.CompanyID = CurrentUser.CoID;
                            NewLine.CountID = Convert.ToInt32(lblCountID.Text);
                            NewLine.ItemID = itmid;
                            NewLine.StoreID = stor.StoreID;
                            NewLine.ItemCode = row.Cells[2].Text;
                            NewLine.ItemDescription = row.Cells[3].Text;
                            NewLine.StoreCode = stor.StoreCode;
                            NewLine.QtyOnHand = stor.QOH; // Set QtyOnHand to the QOH value from the linked store
                            _db.StockCountLines.Add(NewLine);
                        }

                        // Save per grid row — smaller batches, duplicates only affect one row's stores
                        try
                        {
                            _db.SaveChanges();
                        }
                        catch (DbUpdateException)
                        {
                            _db.ChangeTracker.Entries()
                                .Where(e => e.State == EntityState.Added)
                                .ToList()
                                .ForEach(e => e.State = EntityState.Detached);
                        }
                    }
                }
            }  
        }

        protected void LoadCountList()
        {
            long IntCnt = Convert.ToInt64(lblCountID.Text);
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var countList = _db.StockCountLines.Where(x=>x.CompanyID == CurrentUser.CoID && x.CountID == IntCnt).ToList();
                GridCntLines.DataSource = countList;
                GridCntLines.DataBind();
            }
        }

        protected void lbtnSave_Click(object sender, EventArgs e)
        {
            if (txtRef.Text.ToString().Length < 3)
            {
                AlertHelper.ShowSweetAlert(this, "Reference must be at least 3 characters.");
                return;
            }
            if (GridCntLines.Rows.Count == 0)
            {
                AlertHelper.ShowSweetAlert(this, "Please capture at least 1 item to count.");
                return;
            }
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var CntHead = _db.StockCountMasters.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StCntID == countid);
                if (CntHead != null)
                {
                    CntHead.CtDescription = txtRef.Text;
                    CntHead.CtCreateDate = DateTime.Now;
                    _db.Entry(CntHead).State = EntityState.Modified;
                    _db.SaveChanges();
                    AlertHelper.ShowSweetAlert(this, "Stock Count saved successfully.", "success");
                    Response.Redirect("~/StockCountList.aspx?user=" + CurrentUser.UserGuiD, false);
                }
            }
        }

        protected void lbtnBack_Click(object sender, EventArgs e)
        {
            if (CurrentUser != null)
            {
                Response.Redirect("~/StockCounts.aspx?user=" + CurrentUser.UserGuiD, false);
            }
            else
            {
                Response.Redirect("~/Dashboard.aspx", true);
            }
        }
    }
}