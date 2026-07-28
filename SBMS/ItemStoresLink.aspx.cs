using SBMS.Classes;
using SBMS.Models;
using System;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class ItemStoresLink : BasePage
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
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
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

                ApiUrlCall api = new ApiUrlCall();
                var errors = await api.LoadItems(CurrentUser);
                if (errors.Count > 0)
                {
                    string message = string.Join("\n", errors);
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "alert", $"alert('{message}');", true);
                }
                else
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "alertSuccess", "alert('All items successfully sync\\'ed with Sage successfully');", true);
                }

                ViewState["SortExpression"] = "ID"; // Default sort expression
                ViewState["SortDirection"] = "ASC"; // Default sort direction
                loadstores();
                LoadItems(); // Load data initially
                LoadLinkedItems();
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

        protected void LoadItems()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string searchText = txtfind.Text?.ToLower();

                if (DDStoreTo.SelectedIndex > 0)
                {
                    // Load linked items
                    string storeId = DDStoreTo.SelectedValue;
                    var linkedItemsQuery = (from item in _db.ItemsMasters
                                            join link in _db.ItemStoreLinkMasters on item.ID equals link.ItemID
                                            join store in _db.Stores on link.StoreID equals store.StoreID
                                            where link.CompanyID == CurrentUser.CoID && store.StoreCode == storeId
                                            select item).AsQueryable();

                    // Load all items
                    var itemsQuery = _db.ItemsMasters
                                        .Where(x => x.CompanyID == CurrentUser.CoID && x.Active == true && x.Physical == true
                                        && (string.IsNullOrEmpty(searchText) || x.Code.ToLower().Contains(searchText) || x.CategoryDescript.ToLower().Contains(searchText) || x.Description.ToLower().Contains(searchText)))
                                        .OrderBy(x => x.Code)
                                        .Except(linkedItemsQuery); // Exclude items that are already linked
                                                                   // Apply sorting
                    string sortExpression = ViewState["SortExpression"] as string;
                    string sortDirection = ViewState["SortDirection"] as string;
                    itemsQuery = ApplySorting(itemsQuery, sortExpression, sortDirection);

                    // Execute and bind to grid
                    var itemsList = itemsQuery.ToList();
                    GridItems.DataSource = itemsList;
                    GridItems.DataBind();
                }
                else
                {
                    IQueryable<ItemsMaster> Items = _db.ItemsMasters
                                         .Where(x => x.CompanyID == CurrentUser.CoID && x.Active == true && x.Physical == true
                                         && (string.IsNullOrEmpty(searchText) || x.Code.ToLower().Contains(searchText) || x.CategoryDescript.ToLower().Contains(searchText) || x.Description.ToLower().Contains(searchText)))
                                        .OrderBy(x => x.Code);
                    // Get sort expression and direction from ViewState
                    string sortExpression = ViewState["SortExpression"] as string;
                    string sortDirection = ViewState["SortDirection"] as string;
                    // Apply sorting
                    Items = ApplySorting(Items, sortExpression, sortDirection);
                    var itemsList = Items.ToList(); // Execute the query and get the list
                    GridItems.DataSource = itemsList;
                    GridItems.DataBind();
                }
            }
        }

        protected void LoadLinkedItems()
        {
            string storeId = DDStoreTo.SelectedValue;

            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                string searchText = txtfind.Text?.ToLower();

                // Load linked items
                var linkedItemsQuery = from item in _db.ItemsMasters
                                       join link in _db.ItemStoreLinkMasters on item.ID equals link.ItemID
                                       join store in _db.Stores on link.StoreID equals store.StoreID
                                       where link.CompanyID == CurrentUser.CoID && store.StoreCode == storeId
                                       orderby item.Code
                                       select new
                                       {
                                           item.CategoryDescript,
                                           item.Code,
                                           item.Description
                                       };

                // Execute and bind to grid
                var itemsListL = linkedItemsQuery.ToList();
                GridLinked.DataSource = itemsListL;
                GridLinked.DataBind();
            }
        }

        //protected void LoadItems()
        //{
        //    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //    {
        //        string searchText = txtfind.Text?.ToLower();
        //        IQueryable<ItemsMaster> Items = _db.ItemsMasters
        //                             .Where(x => x.CompanyID == CurrentUser.CoID && x.Active == true && x.Physical == true
        //                             && (string.IsNullOrEmpty(searchText) || x.Code.ToLower().Contains(searchText) || x.CategoryDescript.ToLower().Contains(searchText) || x.Description.ToLower().Contains(searchText)))
        //                            .OrderBy(x => x.Code);
        //        // Get sort expression and direction from ViewState
        //        string sortExpression = ViewState["SortExpression"] as string;
        //        string sortDirection = ViewState["SortDirection"] as string;
        //        // Apply sorting
        //        Items = ApplySorting(Items, sortExpression, sortDirection);
        //        var itemsList = Items.ToList(); // Execute the query and get the list
        //        GridItems.DataSource = itemsList;
        //        GridItems.DataBind();
        //    }
        //}


        //protected void LoadLinkedItems()
        //{
        //    string Storeid = DDStoreTo.SelectedValue;

        //    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //    {
        //        string searchText = txtfind.Text?.ToLower();
        //        var ItemsL = (from item in _db.ItemsMasters
        //                     join link in _db.ItemStoreLinkMasters
        //                     on item.ID equals link.ItemID
        //                     join store in _db.Stores
        //                     on link.StoreID equals store.StoreID
        //                     where link.CompanyID == CurrentUser.CoID && store.StoreCode == Storeid
        //                     orderby item.Code
        //                     select new
        //                     {
        //                         item.CategoryDescript,
        //                         item.Code,
        //                         item.Description
        //                     }).ToList();
        //        var itemsListL = ItemsL.ToList();
        //        GridLinked.DataSource = itemsListL;
        //        GridLinked.DataBind();
        //    }
        //}

        private IQueryable<ItemsMaster> ApplySorting(IQueryable<ItemsMaster> items, string sortExpression, string sortDirection)
        {
            switch (sortExpression)
            {
                case "Code":
                    items = sortDirection == "ASC" ? items.OrderBy(x => x.Code) : items.OrderByDescending(x => x.Code);
                    break;
                case "Description":
                    items = sortDirection == "ASC" ? items.OrderBy(x => x.Description) : items.OrderByDescending(x => x.Description);
                    break;
                case "CategoryDescript":
                    items = sortDirection == "ASC" ? items.OrderBy(x => x.CategoryDescript) : items.OrderByDescending(x => x.CategoryDescript);
                    break;
                default:
                    items = sortDirection == "ASC" ? items.OrderBy(x => x.ID) : items.OrderByDescending(x => x.ID);
                    break;
            }
            return items;
        }
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
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
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

        protected void lbtnUpdateYes_Click(object sender, EventArgs e)
        {
            int linked = 0;
            string storeCode = DDStoreTo.SelectedValue;
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                int Storeid = _db.Stores.FirstOrDefault(x => x.CompanyID == CurrentUser.CoID && x.StoreCode == storeCode).StoreID;
                foreach (GridViewRow gvr in GridItems.Rows)
                {
                    CheckBox chk = new CheckBox();
                    chk = (CheckBox)gvr.FindControl("chkSelect");
                    if (chk.Checked == true)
                    {
                        ItemStoreLinkMaster StLink = new ItemStoreLinkMaster();
                        StLink.CompanyID = CurrentUser.CoID;
                        StLink.ItemID = Convert.ToInt64(gvr.Cells[0].Text);
                        StLink.StoreID = Convert.ToInt32(Storeid);
                        StLink.Active = true;
                        _db.ItemStoreLinkMasters.Add(StLink);
                        linked++;
                    }
                }
                _db.SaveChanges();
            }
            LoadItems();
            LoadLinkedItems();

            if (linked == 0)
            {
                AlertHelper.ShowSweetAlert(this, "No items were selected, so nothing was linked.", "warning");
            }
            else
            {
                string msg = linked == 1
                    ? "Store link successfully saved - 1 item linked to " + storeCode + "."
                    : "Store links successfully saved - " + linked + " items linked to " + storeCode + ".";
                AlertHelper.ShowSweetAlert(this, msg, "success");
            }
        }

        private void loadstores()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var Stores = _db.Stores.Where(x => x.CompanyID == CurrentUser.CoID && x.StoreActive == true && x.StoreCode != "CoD" && x.StoreCode != "CoR").ToList();
                DDStoreTo.DataSource = Stores;
                DDStoreTo.DataTextField = "StoreDescript";
                DDStoreTo.DataValueField = "StoreCode";
                DDStoreTo.DataBind();
                DDStoreTo.Items.Insert(0, "-Select Store-");
            }
        }

        protected void DDStoreTo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDStoreTo.SelectedIndex > 0)
            {
                LoadItems();
                LoadLinkedItems();
            }
        }

        protected void chkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chks = new CheckBox();
            chks = (CheckBox)sender;
            CheckBox chkb = new CheckBox();
            foreach (GridViewRow grv in GridItems.Rows)
            {
                chkb = (CheckBox)grv.FindControl("chkSelect");
                if (chks.Checked)
                {
                    chkb.Checked = true;
                }
                else
                {
                    chkb.Checked = false;
                }
            }
        }
    }
}