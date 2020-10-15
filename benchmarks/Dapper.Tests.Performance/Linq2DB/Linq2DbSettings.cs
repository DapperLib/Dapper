using System.Collections.Generic;
using System.Linq;
using LinqToDB.Configuration;

namespace Dapper.Tests.Performance.Linq2Db
{
    public class Linq2DBSettings : ILinqToDBSettings
    {
        private readonly string _connectionString;
        public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();

        public string DefaultConfiguration => "SqlServer";
        public string DefaultDataProvider => "SqlServer";

        public Linq2DBSettings(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<IConnectionStringSettings> ConnectionStrings
        {
            get
            {
                yield return
                    new ConnectionStringSettings
                    {
                        Name = "SqlServer",
                        ProviderName = "SqlServer",
                        ConnectionString = _connectionString
                    };
            }
        }
    }
}
