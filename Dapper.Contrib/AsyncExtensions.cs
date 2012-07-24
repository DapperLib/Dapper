using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Data;

namespace Dapper.Contrib
{
    public static class AsyncExtensions
    {
        /// <summary>
        /// Executes a query asyncronously, returning the data typed as per T
        /// </summary>
        /// <remarks>the dynamic param may seem a bit odd, but this works around a major usability issue in vs, if it is Object vs completion gets annoying. Eg type new [space] get new object</remarks>
        /// <returns> IAsyncResult used to wait on. The completion action returns the sequence of data of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static Task<IEnumerable<T>> QueryAsync<T>(
            this IDbConnection cnn,
            string sql,
            dynamic param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {           
            var identity = new Dapper.SqlMapper.Identity(sql, commandType, cnn, typeof(T), param == null ? null : param.GetType(), null);
            var info = Dapper.SqlMapper.GetCacheInfo(identity);

            SqlCommand cmd = Dapper.SqlMapper.SetupCommand(cnn, transaction, sql, info.ParamReader, param, commandTimeout, commandType) as SqlCommand;

            var task = Task.Factory.FromAsync(
                (callback, state) => cmd.BeginExecuteReader(callback, state),
                ar => cmd.EndExecuteReader(ar),
                TaskCreationOptions.AttachedToParent);

            return task.ContinueWith<IEnumerable<T>>(t =>
                {
                    if (!t.Result.HasRows)
                        return new List<T>();
                    else
                        return Dapper.SqlMapper.ExecuteReaderInternal<T>(t.Result, identity, info).ToArray();
                },
                TaskContinuationOptions.AttachedToParent);
        }
    }
}
