using System.Data;

namespace CRMWebServices.Helpers
{
    public static class DataTableExtensions
    {
        public static List<Dictionary<string, object>> ToList(this DataTable table)
        {
            var rows = new List<Dictionary<string, object>>();

            foreach (DataRow dr in table.Rows)
            {
                var row = new Dictionary<string, object>();

                foreach (DataColumn col in table.Columns)
                {
                    row[col.ColumnName] = dr[col];
                }

                rows.Add(row);
            }

            return rows;
        }
    }
}
