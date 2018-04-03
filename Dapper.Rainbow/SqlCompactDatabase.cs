using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Dapper
{
    /// <summary>
    /// A SQL Compact specific <see cref="Database{TDatabase}"/> implementation.
    /// </summary>
    /// <typeparam name="TDatabase">The type of database.</typeparam>
    public abstract class SqlCompactDatabase<TDatabase> : Database<TDatabase> where TDatabase : Database<TDatabase>, new()
    {
        /// <summary>
        /// A SQL Compact specific table, which handles the syntax correctly across operations.
        /// </summary>
        /// <typeparam name="T">The type in the table.</typeparam>
        public class SqlCompactTable<T> : Table<T>
        {
            /// <summary>
            /// Creates a table for a SQL Compact database.
            /// </summary>
            /// <param name="database"></param>
            /// <param name="likelyTableName"></param>
            public SqlCompactTable(Database<TDatabase> database, string likelyTableName)
                : base(database, likelyTableName)
            {
            }

            /// <summary>
            /// Insert a row into the db
            /// </summary>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <returns></returns>
            public override int? Insert(dynamic data)
            {
                var o = (object)data;
                List<string> paramNames = GetParamNames(o);
                paramNames.Remove("Id");

                string cols = string.Join(",", paramNames);
                string colsParams = string.Join(",", paramNames.Select(p => "@" + p));

                var sql = "insert " + TableName + " (" + cols + ") values (" + colsParams + ")";
                if (database.Execute(sql, o) != 1)
                {
                    return null;
                }

                return (int)database.Query<decimal>("SELECT @@IDENTITY AS LastInsertedId").Single();
            }
        }

        /// <summary>
        /// Initializes the databases.
        /// </summary>
        /// <param name="connection">The connection to use.</param>
        /// <returns>The newly created database.</returns>
        public static TDatabase Init(DbConnection connection)
        {
            var db = new TDatabase();
            db.InitDatabase(connection, 0);
            return db;
        }

        internal override Action<TDatabase> CreateTableConstructorForTable()
        {
            return CreateTableConstructor(typeof(SqlCompactTable<>));
        }
    }
}
