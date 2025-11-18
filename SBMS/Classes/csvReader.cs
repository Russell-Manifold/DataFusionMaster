using CsvHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;

namespace SBMS.Classes
{
    public class csvReader
    {
        public DataTable ReadCsvToDataTable(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<Item>().ToList();
                return ConvertToDataTable(records);
            }
        }

        private DataTable ConvertToDataTable(List<Item> items)
        {
            DataTable dataTable = new DataTable();

            // Define columns
            dataTable.Columns.Add("Document", typeof(string));
            dataTable.Columns.Add("ItemCode", typeof(string));
            dataTable.Columns.Add("Category", typeof(string));
            dataTable.Columns.Add("Trn", typeof(string));
            dataTable.Columns.Add("Description", typeof(string));
            dataTable.Columns.Add("Qty", typeof(double));
            dataTable.Columns.Add("Year_Mth_Week", typeof(string));
            dataTable.Columns.Add("Due_Date", typeof(string));

            // Populate rows
            foreach (var item in items)
            {
                //if (item.Trn == "SO" || item.Trn == "JC" || item.Trn == "FC")
                //{
                //    item.Qty = item.Qty * -1;
                //}
                dataTable.Rows.Add(item.Document, item.ItemCode, item.Category, item.Trn, item.Description, item.Qty, item.Year_Mth_Week, item.Due_Date);
            }
            dataTable.DefaultView.Sort = "Due_Date";
            return dataTable.DefaultView.ToTable();
        }

        public class Item
        {
            public string Document { get; set; }
            public string ItemCode { get; set; }
            public string Category { get; set; }
            public string Trn { get; set; }
            public string Description { get; set; }
            public double Qty { get; set; }
            public string Year_Mth_Week { get; set; }
            public string Due_Date { get; set; }
        }
    }
}