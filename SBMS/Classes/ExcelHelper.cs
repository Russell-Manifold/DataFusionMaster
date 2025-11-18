using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;

namespace SBMS.Classes
{
    public class ExcelHelper
    {
        public static void ExportToExcel(DataTable dt, string fileName, string wsName)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                // Add DataTable as Worksheet
                wb.Worksheets.Add(dt, wsName);

                // Prepare the response for downloading the Excel file
                HttpContext context = HttpContext.Current;
                context.Response.Clear();
                context.Response.Buffer = true;
                context.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                context.Response.AddHeader("content-disposition", $"attachment;filename={fileName}.xlsx");

                // Write the Excel file to the response
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    wb.SaveAs(memoryStream);
                    memoryStream.WriteTo(context.Response.OutputStream);
                    memoryStream.Close();
                }

                context.Response.Flush();
                context.Response.End();
            }
        }
    }
}