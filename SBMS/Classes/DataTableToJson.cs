using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Script.Serialization;

namespace SBMS.Classes
{
    public class DataTableToJson
    {
        public static string ConvertDataTableToJson(DataTable dataTable)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = Int32.MaxValue;
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();

            foreach (DataRow dataRow in dataTable.Rows)
            {
                Dictionary<string, object> row = new Dictionary<string, object>();

                foreach (DataColumn column in dataTable.Columns)
                {
                    row.Add(column.ColumnName, dataRow[column]);
                }
                rows.Add(row);
            }
            return serializer.Serialize(rows);
        }

    }
}