using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;

/// <summary>
/// The SQLite database adapter.
/// </summary>
public class SQLiteAdapter : ISqlAdapter
{
    /// <summary>
    /// Inserts <paramref name="entityToInsert"/> into the database, returning the Id of the row created.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout to use.</param>
    /// <param name="tableName">The table to insert into.</param>
    /// <param name="columnList">The columns to set with this insert.</param>
    /// <param name="parameterList">The parameters to set for this insert.</param>
    /// <param name="keyProperties">The key columns in this table.</param>
    /// <param name="entityToInsert">The entity to insert.</param>
    /// <returns>The Id of the row created.</returns>
    public int Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, string tableName,
        string columnList, string parameterList, IEnumerable<PropertyInfo> keyProperties, object entityToInsert)
    {
        var cmd = $"INSERT INTO {tableName} ({columnList}) VALUES ({parameterList}); SELECT last_insert_rowid() id";
        var multi = connection.QueryMultiple(cmd, entityToInsert, transaction, commandTimeout);

        var id = (int) multi.Read().First().id;
        var propertyInfos = keyProperties as PropertyInfo[] ?? keyProperties.ToArray();
        if (propertyInfos.Length == 0) return id;

        var idProperty = propertyInfos[0];
        idProperty.SetValue(entityToInsert, Convert.ChangeType(id, idProperty.PropertyType), null);

        return id;
    }

    /// <summary>
    /// Inserts <paramref name="entityToInsert"/> into the database, returning the Id of the row created.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout to use.</param>
    /// <param name="tableName">The table to insert into.</param>
    /// <param name="columnList">The columns to set with this insert.</param>
    /// <param name="parameterList">The parameters to set for this insert.</param>
    /// <param name="keyProperties">The key columns in this table.</param>
    /// <param name="entityToInsert">The entity to insert.</param>
    /// <returns>The Id of the row created.</returns>
    public async Task<int> InsertAsync(IDbConnection connection, IDbTransaction transaction, int? commandTimeout,
        string tableName, string columnList, string parameterList, IEnumerable<PropertyInfo> keyProperties,
        object entityToInsert)
    {
        var cmd = $"INSERT INTO {tableName} ({columnList}) VALUES ({parameterList}); SELECT last_insert_rowid() id";
        var multi = await connection.QueryMultipleAsync(cmd, entityToInsert, transaction, commandTimeout)
            .ConfigureAwait(false);

        var id = (int)multi.Read().First().id;
        var pi = keyProperties as PropertyInfo[] ?? keyProperties.ToArray();
        if (pi.Length == 0) return id;

        var idp = pi[0];
        idp.SetValue(entityToInsert, Convert.ChangeType(id, idp.PropertyType), null);

        return id;
    }

    /// <summary>
    /// Adds the name of a column.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnName(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("\"{0}\"", columnName);
    }

    /// <summary>
    /// Adds a column equality to a parameter.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnNameEqualsValue(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("\"{0}\" = @{1}", columnName, columnName);
    }
}
