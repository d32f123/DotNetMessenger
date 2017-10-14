using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace DotNetMessenger.DataLayer.SqlServer
{
    public static class SqlHelper
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

        public static bool DoesFieldValueExist(SqlConnection conn, string tableName, string fieldName, object value, SqlDbType objectType, int size = -1)
        {
            using (var command = conn.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM " + tableName + " WHERE " + fieldName + " = @value";
                var param =
                    size == -1 ? new SqlParameter("@value", objectType) { Value = value }
                        : new SqlParameter("@value", objectType, size) { Value = value };
                command.Parameters.Add(param);



                return ((int)command.ExecuteScalar()) > 0;
            }
        }

        public static bool DoesDoubleKeyExist(SqlConnection conn, string tableName, string firstField, int firstValue, string secondField,
            int secondValue)
        {
            using (var command = conn.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM " + tableName + " WHERE " + firstField +
                                      " = @firstValue AND " + secondField + " = @secondValue";

                command.Parameters.AddWithValue("@firstValue", firstValue);
                command.Parameters.AddWithValue("@secondValue", secondValue);

                return ((int)command.ExecuteScalar()) > 0;
            }
        }
    }
}