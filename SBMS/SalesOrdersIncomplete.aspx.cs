using ClosedXML.Excel;
using SBMS.Classes;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class SalesOrdersIncomplete : BasePage
    {

        public static DataTable MainDtbl;
        private string filtstr = string.Empty;
        private string rowSort = string.Empty;
        public static byte[] key = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
        public static byte[] iv = { 8, 7, 6, 5, 4, 3, 2, 1 };

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
                if (CurrentUser.UsePickSlipTracking != true) ibtnPickTrack.Style.Add("display", "none");
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
                if (ApiUrlCall.CheckForInternetConnection())
                {
                    filtstr = string.Empty;
                    rowSort = string.Empty;
                    lblDir.Text = "ASC";

                    showhidebuttons();
                    await LoadInvoices();
                    CollectData();
                    loadgrid(MainDtbl);
                }
                else
                {
                    ShowMessage(sender, EventArgs.Empty, "No internet connection, Unable to continue");
                }
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

        private void CollectData()
        {
            DataSet ds = new DataSet();
            string filtstr = string.Empty, CoFilt = string.Empty, dtfilt = string.Empty;
            string cmdstring = "";

            DataSet MyDS = new DataSet();

            cmdstring = "Select Number, [Date] as SO_Date, Customer_Name, Reference, Item, Description, Quantity As SO_Qty, [Nett_Line_Total] as Nett_Line_Value, DateDue as Due_Date," +
                            "(SELECT COUNT(Quantity) FROM  DITransactionsTbl As DITransactionsTbl1 WHERE From_Document = DITransactionsTbl.Number AND Description = DITransactionsTbl.Description) As CountInv, " +
                            "(SELECT SUM(Quantity) FROM  DITransactionsTbl As DITransactionsTbl1 WHERE From_Document = DITransactionsTbl.Number AND Description = DITransactionsTbl.Description) As QtyInv, Unit_Price_Excl," +
                           " QtyRemain, ValueRemain, DocumentMessage, LineMessage" +
                           "  FROM DITransactionsTbl WHERE CoID = '" + CurrentUser.CoID + "' AND [Type] = '" + "Sales_Order" + "' AND (Status = '" + "Pending" + "' OR Status = '" + "Overdue" + " ')";

            string conString = ApiUrlCall.constrP;
#if DEBUG
            conString = ApiUrlCall.constr;
#endif

            using (SqlConnection con = new SqlConnection(conString))
            {
                using (var command = new SqlCommand())
                {
                    using (var oda = new SqlDataAdapter())
                    {
                        command.CommandText = cmdstring;
                        command.Connection = con;
                        con.Open();
                        oda.SelectCommand = command;
                        oda.Fill(MyDS);
                        con.Close();
                    }
                }
            }

            if (MyDS.Tables[0].Rows.Count > 0)
            {
                double OSQty = 0, thisqty = 0, lineval = 0, invqty = 0, UnitPrExcl = 0;
                foreach (DataRow dr in MyDS.Tables[0].Rows)
                {
                    thisqty = Convert.ToDouble(dr[6], CultureInfo.InvariantCulture);
                    lineval = Convert.ToDouble(dr[7], CultureInfo.InvariantCulture);
                    if (dr[7].ToString() != "") UnitPrExcl = Math.Round((lineval / thisqty), 2);

                    if (dr[10].ToString() != "")
                    {
                        invqty = Convert.ToDouble(dr[10], CultureInfo.InvariantCulture);
                        OSQty = thisqty - invqty;
                        dr[12] = OSQty.ToString("#.00");
                        dr[13] = Math.Round((OSQty * (UnitPrExcl)), 2).ToString("#.00");
                    }
                    else
                    {
                        dr[12] = thisqty.ToString("#.00");
                        dr[13] = lineval.ToString("#.00");
                    }
                }
                if (chkZero.Checked)
                {
                    MyDS.Tables[0].DefaultView.RowFilter = "QtyRemain > 0";
                }
                else
                {
                    MyDS.Tables[0].DefaultView.RowFilter = "";
                }
                MainDtbl = MyDS.Tables[0].DefaultView.ToTable();
            }
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?userid=" + CurrentUser.UserGuiD+ "", false);
        }

        private void loadgrid(DataTable DtblMain)
        {
            string numfilt = string.Empty, reffilt = string.Empty, itemstr = string.Empty, custfilt = string.Empty, countInvFilt = string.Empty, filledfilt = string.Empty, partfilt = string.Empty;

            if (txtNumber.Text.ToString().Length > 0)
            {
                numfilt = "[Number] Like '%" + txtNumber.Text.Trim() + "%'";
                if (filtstr == string.Empty)
                {
                    filtstr = numfilt;
                }
                else
                {
                    filtstr = filtstr + " AND " + numfilt;
                }
            }

            if (txtReference.Text.ToString().Length > 0)
            {
                reffilt = "[Reference] Like '%" + txtReference.Text.Trim() + "%'";
                if (filtstr == string.Empty)
                {
                    filtstr = reffilt;
                }
                else
                {
                    filtstr = filtstr + " AND " + reffilt;
                }
            }

            if (txtItem.Text.ToString().Length > 0)
            {
                itemstr = "[Description] Like '%" + txtItem.Text.Trim() + "%' OR [Item] Like '%" + txtItem.Text.Trim() + "%'";
                if (filtstr == string.Empty)
                {
                    filtstr = itemstr;
                }
                else
                {
                    filtstr = filtstr + " AND " + itemstr;
                }
            }
            if (txtCustomer.Text.ToString().Length > 0)
            {
                custfilt = "[Customer_Name] Like '%" + txtCustomer.Text.Trim() + "%'";
                if (filtstr == string.Empty)
                {
                    filtstr = custfilt;
                }
                else
                {
                    filtstr = filtstr + " AND " + custfilt;
                }
            }

            if (chkHideUnInv.Checked)
            {
                countInvFilt = "CountInv > 0";
                if (filtstr == string.Empty)
                {
                    filtstr = countInvFilt;
                }
                else
                {
                    filtstr = filtstr + " AND " + countInvFilt;
                }
            }

            if (MainDtbl != null)
            {
                if (MainDtbl.Rows.Count > 0)
                {
                    MainDtbl.DefaultView.Sort = rowSort;
                    MainDtbl.DefaultView.RowFilter = filtstr;
                }
                // iterate through MainDtbl to get totals
                double QtyTot = 0, OSValTot = 0, QtyInvd = 0;
                foreach (System.Data.DataRowView dr in MainDtbl.DefaultView)
                {
                    try
                    {
                        if (dr[12].ToString() != "")
                        {
                            if (Convert.ToDouble(dr[12].ToString(), CultureInfo.InvariantCulture) > 0)
                            {
                                QtyTot += Convert.ToDouble(dr[12].ToString(), CultureInfo.InvariantCulture);
                            }
                        }

                        if (dr[13].ToString() != "")
                        {
                            if (Convert.ToDouble(dr[13].ToString(), CultureInfo.InvariantCulture) > 0)
                            {
                                OSValTot += Convert.ToDouble(dr[13].ToString(), CultureInfo.InvariantCulture);
                            }
                        }

                        if (dr[10].ToString() != "")
                        {
                            if (Convert.ToDouble(dr[10].ToString(), CultureInfo.InvariantCulture) > 0)
                            {
                                QtyInvd += Convert.ToDouble(dr[10].ToString(), CultureInfo.InvariantCulture);
                            }
                        }
                    }
                    catch { }
                }

                GridViewGL.DataSource = MainDtbl.DefaultView;
                GridViewGL.DataBind();
                if (MainDtbl.DefaultView.Count > 0)
                {
                    GridViewGL.FooterRow.Cells[10].Text = "Total";
                    GridViewGL.FooterRow.Cells[11].Text = QtyInvd.ToString();
                    GridViewGL.FooterRow.Cells[12].Text = QtyTot.ToString();
                    GridViewGL.FooterRow.Cells[13].Text = OSValTot.ToString("# ### ###.00");
                }

                lblReccount.Text = "(" + MainDtbl.DefaultView.Count + " Lines)";
                GridViewGL.Columns[14].Visible = false;
                GridViewGL.Columns[15].Visible = false;
            }
        }

        protected void GridViewGL_PageIndexChanging(object sender, System.Web.UI.WebControls.GridViewPageEventArgs e)
        {
            GridViewGL.PageIndex = e.NewPageIndex;
            loadgrid(MainDtbl);
        }

        protected void GridViewGL_Sorting(object sender, System.Web.UI.WebControls.GridViewSortEventArgs e)
        {
            if (lblDir.Text == "ASC")
            {
                lblDir.Text = "DESC";
            }
            else
            {
                lblDir.Text = "ASC";
            }
            rowSort = e.SortExpression + " " + lblDir.Text;
            loadgrid(MainDtbl);
        }

        protected void lbtnClear_Click(object sender, EventArgs e)
        {
            txtNumber.Text = string.Empty;
            txtReference.Text = string.Empty;
            txtCustomer.Text = string.Empty;
            txtItem.Text = string.Empty;
            filtstr = string.Empty;
            loadgrid(MainDtbl);
        }

        protected void ShowMessage(object sender, EventArgs e, string msg)
        {
            string message = "alert('" + msg + "')";
            ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
        }

        protected void btnAppSubmit2_Click(object sender, EventArgs e)
        {
            XLWorkbook wb = new XLWorkbook();
            var worksheet = wb.Worksheets.Add(MainDtbl, "MergedData");

            // Set the format for columns 12 and 13
            int columnNumber1 = 13; // Change this to the actual column number
            int columnNumber2 = 14; // Change this to the actual column number

            var column1 = worksheet.Column(columnNumber1);
            var column2 = worksheet.Column(columnNumber2);

            // Set the data type and number format for the columns starting from row 2
            for (int rowNumber = 2; rowNumber <= worksheet.LastRowUsed().RowNumber(); rowNumber++)
            {
                var cell1 = worksheet.Cell(rowNumber, columnNumber1);
                var cell2 = worksheet.Cell(rowNumber, columnNumber2);

                cell1.DataType = XLDataType.Number;
                cell1.Style.NumberFormat.Format = "0.00"; // Set the desired number format for column 12

                cell2.DataType = XLDataType.Number;
                cell2.Style.NumberFormat.Format = "0.00"; // Set the desired number format for column 13
            }

            wb.SaveAs(Server.MapPath($"~/workbooks/{CurrentUser.CoID}_Incomplete_Sales_Orders.xlsx"));
            string file = "Incomplete_S_Os.xlsx";
            string filepath = (Server.MapPath($"~/workbooks/{CurrentUser.CoID}_Incomplete_Sales_Orders.xlsx"));
            Response.Clear();
            Response.ContentType = "application/vnd.ms-excel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=" + file);
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.TransmitFile(filepath);
            Response.Flush();
            Response.End();
        }

        protected void lbtnLogOut_Click(object sender, EventArgs e)
        {
            ApiUrlCall.LogOut(CurrentUser.UserGuiD); Response.Cookies["Login"].Expires = DateTime.Now.AddDays(-1);
            Session.Clear();
            Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
        }

        protected void GridViewGL_RowDataBound(object sender, System.Web.UI.WebControls.GridViewRowEventArgs e)
        {

        }

        protected void txtNumber_TextChanged(object sender, EventArgs e)
        {
            filtstr = string.Empty;
            loadgrid(MainDtbl);
        }

        protected void GridViewGL_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "Expand")
            {
                int rowIndex = Convert.ToInt32(e.CommandArgument);
                GridViewRow row = GridViewGL.Rows[rowIndex];
                GridView nestedGridView = row.FindControl("NestedGridView") as GridView;
                LinkButton lnkExpand = row.FindControl("lnkExpand") as LinkButton;
                LinkButton lnkCollapse = row.FindControl("lnkCollapse") as LinkButton;

                DataSet DS = ApiUrlCall.GetSQLDataFromString("Select [Date], Number, Quantity FROM DITransactionsTbl WHERE CoID = '" + CurrentUser.CoID + "' AND From_Document = '" + row.Cells[1].Text.ToString() + "' AND Item = '" + row.Cells[6].Text.ToString() + "'");
                nestedGridView.DataSource = DS;
                nestedGridView.DataBind();
                nestedGridView.Visible = true;

                // Hide the "Expand" button and show the "Collapse" button
                lnkExpand.Visible = false;
                lnkCollapse.Visible = true;
            }
            else if (e.CommandName == "Collapse")
            {
                int rowIndex = Convert.ToInt32(e.CommandArgument);
                GridViewRow row = GridViewGL.Rows[rowIndex];
                GridView nestedGridView = row.FindControl("NestedGridView") as GridView;
                LinkButton lnkExpand = row.FindControl("lnkExpand") as LinkButton;
                LinkButton lnkCollapse = row.FindControl("lnkCollapse") as LinkButton;

                nestedGridView.Visible = false;
                lnkExpand.Visible = true;
                lnkCollapse.Visible = false;
            }
        }

        protected void chkZero_CheckedChanged(object sender, EventArgs e)
        {
            CollectData();
            loadgrid(MainDtbl);
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


        private async Task LoadInvoices()
        {
            //string dbpath = Server.MapPath("~/DataInsightsAPI/App_Data/" + Request.QueryString["userid"].ToString());
            await ApiUrlCall.DILoadSalesOrders(CurrentUser);
            await ApiUrlCall.LoadInvoices(CurrentUser);
            await ApiUrlCall.LoadCustAdjustments(CurrentUser);
            await ApiUrlCall.LoadSalesCreditNotes(CurrentUser);  
        }

      private void UpdateTransTableItems(string CoID)
        {
            string dbpath = Server.MapPath("~/DataInsightsAPI/App_Data/" + Request.QueryString["userid"].ToString());
            string connectionString = "Data Source=" + dbpath + ";Version=3;";

            DataSet MyDS = new DataSet();
           // MyDS = ApiUrlCall.GetSQLiteData("Select ID, Description FROM TransactionsTbl WHERE (LENGTH(Item) < 1)  AND (LineTypeID = '" + "0" + "')  AND " +
                //"((TransactionsTbl.Type Like '" + "%Sales%" + "') OR " +
                //"(TransactionsTbl.Type Like '" + "%Tax%" + "') OR " +
                //"(TransactionsTbl.Type Like '" + "%Credit%" + "') OR " +
                //"(TransactionsTbl.Type Like '" + "%Quotation%" + "'))", dbpath);

            foreach (DataRow dr in MyDS.Tables[0].Rows)
            {
                try
                {
                    //DataSet ItemDS = ApiUrlCall.GetSQLiteData("Select Code FROM ItemTbl WHERE Description = '" + dr["Description"].ToString().Replace("'", "''") + "'", dbpath);
                   // if (ItemDS.Tables[0].Rows.Count > 0)
                 //  {
                       // ApiUrlCall.SetSQLiteData("UPDATE TransactionsTbl SET Item = '" + ItemDS.Tables[0].Rows[0]["Code"].ToString().Replace("'", "''") + "'" + "' WHERE ID = '" + dr["ID"] + "'", dbpath);
                  //  }
                }
                catch (Exception ex) { string str = ex.Message; }

            }
        }

       

        protected void showhidebuttons(DataSet ds)
        {
            //if (Convert.ToBoolean(ds.Tables[0].Rows[0]["AccSales"]) != true) ImgBtnSales.Style.Add("display", "none");
            //if (Convert.ToBoolean(ds.Tables[0].Rows[0]["AccItems"]) != true) ImgBtnItem.Style.Add("display", "none");
            //if (Convert.ToBoolean(ds.Tables[0].Rows[0]["AccAnCodes"]) != true) ImgAnalysis.Style.Add("display", "none");
            //if (Convert.ToBoolean(ds.Tables[0].Rows[0]["AccPivot"]) != true) BtnAdvanced.Style.Add("display", "none");
            //if (Convert.ToBoolean(ds.Tables[0].Rows[0]["AccCustomers"]) != true) ImgBtnCust.Style.Add("display", "none");
            //if (Convert.ToBoolean(ds.Tables[0].Rows[0]["AccReps"]) != true) ImgBtnConvert.Style.Add("display", "none");
            //if (Convert.ToBoolean(ds.Tables[0].Rows[0]["AccSOs"]) != true) ImgBtnPendSO.Style.Add("display", "none");
            //if (Convert.ToBoolean(ds.Tables[0].Rows[0]["AccPOs"]) != true) imgPOs.Style.Add("display", "none");
            //if (Convert.ToBoolean(ds.Tables[0].Rows[0]["AccItemDems"]) != true) ImgInvDemands.Style.Add("display", "none");
            //if (Convert.ToBoolean(ds.Tables[0].Rows[0]["AccAI"]) != true) imgBtnAi.Style.Add("display", "none");
            //if (Convert.ToBoolean(ds.Tables[0].Rows[0]["AccMgmReps"]) != true) imgBtnMPack.Style.Add("display", "none");
            //if (Convert.ToBoolean(ds.Tables[0].Rows[0]["AccCustom"]) != true) imgBtnCustom.Style.Add("display", "none");
        }
    }
}