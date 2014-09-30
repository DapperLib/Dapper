using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Dapper.Rainbow
{
    public abstract class SqlCompactDatabase<TDatabase> : Database<TDatabase>, IDisposable where TDatabase : Database<TDatabase>, new()
    {
        public class SqlCompactTable<T> : Table<T>
        {
            public SqlCompactTable(Database<TDatabase> database, string likelyTableName)
                : base(database, likelyTableName)
            {
            }

            /// <summary>
            /// Insert a row into the db
            /// </summary>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <param name="removeId">Leave true if the database column is an identity or auto incrementing otherwise set to false and the Id property value will be inserted.</param>
            /// <returns></returns>
            public override int? Insert(dynamic data, bool removeId = true)
            {
                var o = (object)data;
                List<string> paramNames = GetParamNames(o);
                
                if (removeId)
                {
                    paramNames.Remove("Id");
                }

                string cols = string.Join(",", paramNames);
                string cols_params = string.Join(",", paramNames.Select(p => "@" + p));

                var sql = "insert " + TableName + " (" + cols + ") values (" + cols_params + ")";
                if (database.Execute(sql, o) != 1)
                {
                    return null;
                }

                return (int)database.Query<decimal>("SELECT @@IDENTITY AS LastInsertedId").Single();
            }
        }

        public static TDatabase Init(DbConnection connection)
        {
            TDatabase db = new TDatabase();
            db.InitDatabase(connection, 0);
            return db;
        }        

        internal override Action<TDatabase> CreateTableConstructorForTable()
        {
            return CreateTableConstructor(typeof(SqlCompactTable<>));
        }
    }
}
