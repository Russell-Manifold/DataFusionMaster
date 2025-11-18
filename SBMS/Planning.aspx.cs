using ClosedXML.Excel;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class Planning : BasePage
    {
        long Coid;
        private List<ItemsMaster> _items;
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
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                _items = _db.ItemsMasters.Where(i => i.Active == true && i.CompanyID == Coid && i.Physical == true).OrderBy(x => x.Code).ToList();
            }
            if (!IsPostBack)
            {
                //LoadCategories();
                lbtnViewAnalysis_Click(sender, EventArgs.Empty);
                //BindGrid();
            }
        }

         protected void lbtnViewAnalysis_Click(object sender, EventArgs e)
        {
            CollectData();
            StringBuilder sb = new StringBuilder();
            sb.Append("<script type=" + "\"text/javascript\">");
            sb.Append("var derivers = $.pivotUtilities.derivers;");
            sb.Append("var sortAs  = $.pivotUtilities.sortAs;");
            sb.Append("var renderers = $.extend($.pivotUtilities.renderers,");
            sb.Append("$.pivotUtilities.plotly_renderers,");
            sb.Append(" $.pivotUtilities.export_renderers);");

            sb.Append("function createPivotTable(abc){");
            sb.Append("$(function () {");
            sb.Append("$(\"#pivotoutput\").html(\"" + "<div style='text-align:center; background-color:#f9f9f9'><img src='images/tenorwait.gif' id='myAnimatedImage' align='absmiddle' class='funkygif'/></div>\");");
            sb.Append("Papa.parse(abc,{");
            sb.Append("download: true,");
            sb.Append("skipEmptyLines: true,");
            sb.Append("complete: function(parsed) {");

            sb.Append("$('#pivotoutput').pivotUI(");
            sb.Append("parsed.data,");
            sb.Append("{");

            sb.Append("rows: [");
            sb.Append("\"ItemCode\"");
            sb.Append(",");
            sb.Append("\"Description\"");
            sb.Append("],");
            sb.Append("cols: [");
            sb.Append("\"Year_Mth_Week\"");
            sb.Append(",");
            sb.Append("\"Trn\"");
            sb.Append("],");
            sb.Append("vals: [");
            sb.Append("\"Qty\"");
            sb.Append("],");
            sb.Append("aggregatorName: [");
            sb.Append("\"Sum\"");
            sb.Append("],");
            sb.Append("rendererName: [");
            sb.Append("\"Table\"");
            sb.Append("]");
            sb.Append("});");
            sb.Append("}");
            sb.Append("});");
            sb.Append("});");
            sb.Append("};");
            sb.Append("</script>");
            Literal1.Text = sb.ToString();
            string inputpath = "inputcsv/" + CurrentUser.UserGuiD.ToString().Replace(" ", "").Replace("-", "") + "-Plan.csv";
            Page.ClientScript.RegisterStartupScript(this.GetType(), "CreatePivotTable", "createPivotTable('" + inputpath + "');", true);
        }

        protected string CollectData()
        {
            string filePath = Server.MapPath("~/inputcsv/" + CurrentUser.UserGuiD.ToString().Replace(" ", "").Replace("-", "") + "-Plan.csv"); ;
            if (File.Exists(filePath)) File.Delete(filePath);

            DataTable dtbl = new DataTable();
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var GetData = _db.GetFGDemmandsAll(Coid).ToList();
                WriteToCsv(GetData, filePath);
            }
            return "OK";
        }

        protected void lbtnDownload_Click(object sender, EventArgs e)
        {
            string csvFilePath = Server.MapPath("~/inputcsv/" + CurrentUser.UserGuiD.ToString().Replace(" ", "").Replace("-", "") + "-Plan.csv");
            XLWorkbook wb = new XLWorkbook();
            csvReader csvr = new csvReader();
            DataTable data = csvr.ReadCsvToDataTable(csvFilePath);

            /// // Add pivot table sheet
            var pivotSheet = wb.Worksheets.Add("FGRequirements");
            ///// add source data
            var dataSheet = wb.Worksheets.Add("SourceData");
            dataSheet.Cell(1, 1).InsertTable(data);

            // Create pivot table
            var pivotTable = pivotSheet.PivotTables.Add("PivotTable", pivotSheet.Cell("A1"), dataSheet.RangeUsed());
            // Add values to pivot table
            var NameField = pivotTable.RowLabels.Add("ItemCode");
            var NameField2 = pivotTable.RowLabels.Add("Description");
            var MthYrField = pivotTable.ColumnLabels.Add("Year_Mth_Week");
            var MthYrField2 = pivotTable.ColumnLabels.Add("Trn");
            var amountField = pivotTable.Values.Add("Qty");

            amountField.SummaryFormula = XLPivotSummary.Sum;
            //dataSheet.Visibility = XLWorksheetVisibility.Hidden;

            wb.SaveAs(Server.MapPath("~/inputcsv/" + CurrentUser.UserGuiD.ToString().Replace(" ", "").Replace("-", "") + "-Plan.xlsx"));
            string file = "Planning.xlsx";
            string filepath = (Server.MapPath("~/inputcsv/" + CurrentUser.UserGuiD.ToString().Replace(" ", "").Replace("-", "") + "-Plan.xlsx"));
            Response.Clear();
            Response.ContentType = "application/vnd.ms-excel";
            Response.AppendHeader("Content-Disposition", "attachment; filename=" + file);
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.TransmitFile(filepath);
            Response.Flush();
            Response.End();

        }

        public static void WriteToCsv<T>(List<T> items, string filePath, bool append = false, bool addstk = true)
        {
            StringBuilder csvContent = new StringBuilder();

            // Get all the properties
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Write the header if not appending
            if (!append)
            {
                csvContent.AppendLine(string.Join(",", props.Select(p => p.Name)));
            }

            // Write the data rows
            foreach (var item in items)
            {
                var values = props.Select(p =>
                {
                    var value = p.GetValue(item, null);
                    //Console.WriteLine($"p.Name: '{p.Name}', value: '{value}', value type: {value.GetType().FullName}");
                    if (p.Name.Trim() == "Year_Mth_Week" && DateTime.TryParseExact(value.ToString(), "yyyy-MM-dd hh:mm:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateValue))
                    {
                        // Custom date format "yyyy-MM" + "weeknumber"
                        var weekNumber = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                            dateValue,
                            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                            DayOfWeek.Monday);
                        return dateValue.ToString($"yyyy-MM") + $" Week{weekNumber}";
                    }
                    else
                    if (value is DateTime dateValue2)
                    {
                        return dateValue2.ToString($"dd MMM yyyy");
                    }
                    return value?.ToString().Replace(",", " ");
                }).ToArray();

                if (values[3] == "JC" || values[3] == "SO" || values[3] == "FC")
                {
                    values[5] = "-" + values[5].ToString();
                }
                csvContent.AppendLine(string.Join(",", values));
            }

            // Write to file
            if (append)
            {
                File.AppendAllText(filePath, csvContent.ToString());
            }
            else
            {
                File.WriteAllText(filePath, csvContent.ToString());
            }
        }

        protected async void lbtnRefresh_Click(object sender, EventArgs e)
        {
            ApiUrlCall api = new ApiUrlCall();
            var errors =await api.LoadItems(CurrentUser);
            lbtnViewAnalysis_Click(sender, EventArgs.Empty);
        }

        //protected void BindGrid()
        //{
        //    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //    {
        //        var PPlan = _db.ProdPlanLines.Where(x => x.CompanyID == Coid && x.Active == true).ToList();

        //        //if (DDCateg.SelectedIndex == 0)
        //        // {
        //        //     PPlan = _db.ProdPlanLines.Where(x => x.CompanyID == Coid && x.Active == true && x.Category == DDCateg.SelectedItem.Text).ToList();
        //        // }

        //        if (!PPlan.Any()) // If there are no lines
        //        {
        //            ProdPlanLine NewPPLine = new ProdPlanLine();
        //            NewPPLine.CompanyID = Coid;
        //            //NewPPLine.Category = DDCateg.SelectedItem.Text;
        //            NewPPLine.Active = true;
        //            _db.ProdPlanLines.Add(NewPPLine);
        //            _db.SaveChanges();
        //        }
        //        else
        //        {
        //            var lastLine = PPlan.Last();
        //            if (lastLine != null && lastLine.ItemCode != null) // Check if NewJCLine and JCID are not null
        //            {
        //                ProdPlanLine NewPPLine = new ProdPlanLine();
        //                NewPPLine.CompanyID = Coid;
        //                //NewPPLine.Category = DDCateg.SelectedItem.Text;
        //                NewPPLine.Active = true;
        //                _db.ProdPlanLines.Add(NewPPLine);
        //                _db.SaveChanges();
        //            }
        //        }

        //        //if (DDCateg.SelectedIndex == 0)
        //        //{
        //        PPlan = _db.ProdPlanLines.Where(x => x.CompanyID == Coid && x.Active == true).ToList();
        //        //}
        //        //else
        //        //{
        //        //    PPlan = _db.ProdPlanLines.Where(x => x.CompanyID == Coid && x.Active == true && x.Category == DDCateg.SelectedItem.Text).ToList();
        //        //}
        //        GridPPLines.DataSource = PPlan.OrderBy(x => x.PlanDate);
        //        GridPPLines.DataBind();

        //    }
        //}

        //protected void GridPPLines_RowDataBound(object sender, GridViewRowEventArgs e)
        //{
        //    //e.Row.Cells[0].Visible = false;
        //    if (e.Row.RowType == DataControlRowType.DataRow)
        //    {
        //        var item = (ProdPlanLine)e.Row.DataItem;
        //        // Find the DropDownList in the current row
        //        var ddlItemCode = (DropDownList)e.Row.FindControl("DDItemCode");
        //        //Set the selected value of the DropDownList to the value from the data item
        //        if (ddlItemCode != null)
        //        {
        //            //if (DDCateg.SelectedIndex == 0)
        //            // {
        //            ddlItemCode.DataSource = _items.Where(x=>x.IsFinishedGoods == true);
        //            // }
        //            //else
        //            //  {
        //            //      ddlItemCode.DataSource = _items.Where(x=>x.CategoryDescript == DDCateg.Text).ToList();
        //            //  }
        //            ddlItemCode.DataTextField = "Code";
        //            ddlItemCode.DataValueField = "ID";
        //            ddlItemCode.DataBind();
        //            ddlItemCode.Items.Insert(0, new ListItem("Select", "0"));
        //            ddlItemCode.SelectedValue = item.SelectionId.ToString();
        //        }
        //    }
        //}

        //protected void DDItemCode_SelectedIndexChanged(object sender, EventArgs e)
        //{

        //}

        //protected void lbtnLineSave_Click(object sender, EventArgs e)
        //{

        //    LinkButton lbtnLineSave = (LinkButton)sender;
        //    GridViewRow row = (GridViewRow)lbtnLineSave.NamingContainer;
        //    DropDownList DDItemCode = (DropDownList)row.FindControl("DDItemCode");
        //    TextBox txtDate = (TextBox)row.FindControl("txtDate");
        //    TextBox txtQty = (TextBox)row.FindControl("txtQty");
        //    DateTime DtP; decimal ProdQ;
        //    if (DDItemCode.SelectedIndex == 0)
        //    {
        //        ShowMessage(sender, EventArgs.Empty, "Invalid Item Code, unable to save");
        //        return;
        //    }
        //    try
        //    {
        //        DtP = Convert.ToDateTime(txtDate.Text.ToString());
        //    }
        //    catch
        //    {
        //        ShowMessage(sender, EventArgs.Empty, "Invalid Date, unable to save");
        //        return;
        //    }
        //    try
        //    {
        //        ProdQ = Convert.ToDecimal(txtQty.Text.ToString());
        //    }
        //    catch
        //    {
        //        ShowMessage(sender, EventArgs.Empty, "Invalid Quantity, unable to save");
        //        return;
        //    }

        //    long PPLineId = Convert.ToInt64(lbtnLineSave.CommandArgument);
        //    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //    {
        //        var PPlan = _db.ProdPlanLines.Where(x => x.CompanyID == Coid && x.LineID == PPLineId).FirstOrDefault();
        //        PPlan.PlanDate = DtP;
        //        PPlan.ItemCode = DDItemCode.SelectedItem.Text.ToString();
        //        PPlan.SelectionId = Convert.ToInt64(DDItemCode.SelectedValue);
        //        PPlan.ItemDescription = _db.ItemsMasters.Where(x => x.ID == PPlan.SelectionId).FirstOrDefault().Description;
        //        PPlan.PlanQuantity = ProdQ;
        //        _db.SaveChanges();
        //        lbtnViewAnalysis_Click(sender, EventArgs.Empty);
        //        //BindGrid();
        //        PopMessage("Successfully Saved");
        //    }
        //}

        //protected void lbtnDeleteLine_Click(object sender, EventArgs e)
        //{
        //    LinkButton lbtnLineSave = (LinkButton)sender;
        //    GridViewRow row = (GridViewRow)lbtnLineSave.NamingContainer;
        //    long PPLineId = Convert.ToInt64(lbtnLineSave.CommandArgument);
        //    using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
        //    {
        //        var PPlan = _db.ProdPlanLines.Where(x => x.CompanyID == Coid && x.LineID == PPLineId);
        //        _db.ProdPlanLines.RemoveRange(PPlan);
        //        _db.SaveChanges();
        //        lbtnViewAnalysis_Click(sender, EventArgs.Empty);
        //       // BindGrid();
        //        PopMessage("Successfully Saved");
        //    }
        //}

        protected void ShowMessage(object sender, EventArgs e, string msg)
        {
            string message = "alert('" + msg + "')";
            ScriptManager.RegisterClientScriptBlock((sender as Control), this.GetType(), "alert", message, true);
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
        //protected void DDCateg_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    BindGrid();
        //}
        protected void lbtnDwnLToExcel_Click(object sender, EventArgs e)
        {
            DataTable FGDataTable = GetPlanLines();
            ExcelHelper.ExportToExcel(FGDataTable, "FGDemands", "FGDemands");
        }
        public DataTable GetPlanLines()
        {   
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
              var FGDemands = _db.GetFGDemmandsAll(Coid)
                   .Select(d => new
                   {
                       d.Document,
                       d.ItemCode,
                       d.Category,
                       d.Trn,
                       d.Description,
                       d.Qty,
                       d.Due_Date
                   })
                   .ToList();

                return DataTableHelper.ConvertToDataTable(FGDemands);
            }        
        }

        public class FGDemandLines
        {
            //public long ItemID { get; set; }
            public string Document { get; set; }
            public string ItemCode { get; set; }
            public string Category { get; set; }
            public string Trn { get; set; }    
            public string Description { get; set; }
            public decimal Qty { get; set; }
            public DateTime Due_Date { get; set; }
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