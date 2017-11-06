using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace DotNetMessenger.DataLayer.SqlServer
{
    public static class SqlHelper
    {
        /// <summary>
        /// Transforms a list of ids into <see cref="DataTable"/> suitable for SQL Stored Procedure
        /// </summary>
        /// <param name="idList">List of ids</param>
        /// <returns><see cref="DataTable"/> suitable for sql stored procedure</returns>
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
        /// <summary>
        /// Checks whether <paramref name="value"/> exists in <paramref name="tableName"/>'s <paramref name="fieldName"/> field
        /// </summary>
        /// <param name="conn">An existing DB connection</param>
        /// <param name="tableName">The name of the table</param>
        /// <param name="fieldName">The name of the field</param>
        /// <param name="value">Value of the field</param>
        /// <param name="objectType">Field type</param>
        /// <param name="size">Optional: if varchar or varbinary should be length of the object</param>
        /// <returns>Whether a value exists in that table</returns>
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
        /// <summary>
        /// Checks whether a double key exists in a <paramref name="tableName"/>
        /// </summary>
        /// <param name="conn">An existing DB connection</param>
        /// <param name="tableName">The name of the table</param>
        /// <param name="firstField">The name of the first field</param>
        /// <param name="firstValue">The value of the first field</param>
        /// <param name="secondField">The name of the second field</param>
        /// <param name="secondValue">The value of the second field</param>
        /// <returns>Whether a double key exists in table <paramref name="tableName"/></returns>
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
        /// <summary>
        /// Checks whether the field of the row selected with <paramref name="id"/> in <paramref name="tableName"/> 
        /// is in the given <paramref name="range"/>
        /// </summary>
        /// <param name="conn">An existing DB connection</param>
        /// <param name="tableName">The name of the table</param>
        /// <param name="idField">The name of the ID field of that table</param>
        /// <param name="id">The id of the row to be selected</param>
        /// <param name="field">The field which should be in range</param>
        /// <param name="range">The specific range of values</param>
        /// <returns>Whether the attribute is in range</returns>
        public static bool IsSelectedRowFieldInRange(SqlConnection conn, string tableName, string idField, int id,
            string field, IEnumerable<int> range)
        {
            using (var command = conn.CreateCommand())
            {
                var sb = new StringBuilder("SELECT COUNT(*) FROM ");
                sb.Append(tableName).Append(" WHERE ").Append(idField).Append(" = @id AND ").Append(field)
                    .Append(" IN (");

                var i = 0;
                foreach (var rangeValue in range)
                {
                    sb.Append("@value").Append(i).Append(", ");
                    command.Parameters.AddWithValue("@value" + i++, rangeValue);
                }
                sb.Remove(sb.Length - 2, 2).Append(")");
                command.CommandText = sb.ToString();

                command.Parameters.AddWithValue("@id", id);

                return ((int) command.ExecuteScalar()) > 0;
            }
        }

        public static T GetLastResult<T>(SqlDataReader reader, string fieldName)
        {
            var ret = default(T);
            do
            {
                if (!reader.HasRows)
                    continue;
                reader.Read();
                try
                {
                    ret = reader.GetFieldValue<T>(reader.GetOrdinal(fieldName));
                }
                catch
                {
                    ret = default(T);
                }
            } while (reader.NextResult());
            return ret;
        }
    }
}