using System.Collections.Generic;
using System.Data;

namespace DotNetMessenger.DataLayer.SqlServer
{
    public static class DataTableHelper
    {
        public static DataTable IdListToDataTable(IEnumerable<int> idList)
        {
            var dataTable = new DataTable("IdListType");
            var column = new DataColumn("ID", typeof(int));

            dataTable.Columns.Add(column);

            foreach (var id in idList)
            {
                var row = dataTable.NewRow();
                row[0] = id;
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }
    }
}