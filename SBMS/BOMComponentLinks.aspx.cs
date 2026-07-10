using ClosedXML.Excel;
using SBMS.Classes;
using SBMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

namespace SBMS
{
    public partial class BOMComponentLinks : BasePage
    {
        long itmid;
        private UserDetails CurrentUser => Session["UserDetails"] as UserDetails;

        public class BOMLink
        {
            public int BomHID { get; set; }
            public string BOMCode { get; set; }
            public string BomDescript { get; set; }
            public string FGCode { get; set; }
            public string FGDescript { get; set; }
            public decimal RMQty { get; set; }
            public string BomActive { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (CurrentUser == null)
            {
                Response.Redirect("~/Login.aspx", false); Context.ApplicationInstance.CompleteRequest();
                return;
            }
            itmid = Convert.ToInt64(Request.QueryString["itm"]);

            string imgPath = $"~/images/CoImages/{CurrentUser.CoID}.png";
            imgCoImg.ImageUrl = ResolveUrl(File.Exists(Server.MapPath(imgPath)) ? imgPath : "~/images/CoImages/0000.png");

            if (!IsPostBack) BindData();
        }

        // Every BOM that uses this item as a component: BOMLine.ItemID == itmid,
        // joined to its header. One row per line match (a BOM listing the item on
        // two lines shows twice, each with its own qty).
        private List<BOMLink> GetLinks()
        {
            using (SBMSEntities _db = new SBMSEntities(Config.GetConnectionString()))
            {
                var links = (from bl in _db.BOMLines
                             join bh in _db.BOMHeaders on bl.BomHID equals bh.BomHID
                             where bl.CompanyID == CurrentUser.CoID && bl.ItemID == itmid
                             orderby bh.BOMCode
                             select new BOMLink
                             {
                                 BomHID = bh.BomHID,
                                 BOMCode = bh.BOMCode,
                                 BomDescript = bh.BomDescript,
                                 FGCode = bh.FGCode,
                                 FGDescript = bh.FGDescript,
                                 RMQty = bl.RMQty ?? 0,
                                 BomActive = bh.BomActive ? "Yes" : "No"
                             }).ToList();

                lblItemCode.Text = _db.ItemsMasters
                    .Where(x => x.CompanyID == CurrentUser.CoID && x.ID == itmid)
                    .Select(x => x.Code).FirstOrDefault() ?? "";

                return links;
            }
        }

        private void BindData()
        {
            GridBOMs.DataSource = GetLinks();
            GridBOMs.DataBind();
        }

        protected void GridBOMs_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "lbtnBOM")
            {
                Response.Redirect("~/BOMDetailed.aspx?bomid=" + e.CommandArgument, false);
            }
        }

        protected void lbtnHome_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Dashboard.aspx?user=" + CurrentUser.UserGuiD, false);
        }

        protected void lbtnBack_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/ItemEdit.aspx?itm=" + itmid, false);
        }

        protected void lbtnDownload_Click(object sender, EventArgs e)
        {
            var data = GetLinks();
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("BOMs Using Item");
                var headers = new[] { "BOM Code", "BOM Description", "Finished Good Code", "Finished Good Description", "Qty Used", "Active" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0, 112, 192);
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                int row = 2;
                foreach (var item in data)
                {
                    ws.Cell(row, 1).Value = item.BOMCode;
                    ws.Cell(row, 2).Value = item.BomDescript;
                    ws.Cell(row, 3).Value = item.FGCode;
                    ws.Cell(row, 4).Value = item.FGDescript;
                    ws.Cell(row, 5).Value = item.RMQty;
                    ws.Cell(row, 6).Value = item.BomActive;
                    if (row % 2 == 0) ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(235, 241, 250);
                    row++;
                }
                ws.Columns().AdjustToContents();

                string filepath = Server.MapPath($"~/inputcsv/{CurrentUser.UserGuiD.ToString().Replace(" ", "").Replace("-", "")}-BOMLinks.xlsx");
                workbook.SaveAs(filepath);
                string file = $"BOMsUsing_{lblItemCode.Text}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                Response.Clear();
                Response.ContentType = "application/vnd.ms-excel";
                Response.AppendHeader("Content-Disposition", "attachment; filename=" + file);
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.TransmitFile(filepath);
                Response.Flush();
                System.IO.File.Delete(filepath);
                Response.End();
            }
        }
    }
}
