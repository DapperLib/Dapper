using LinqToDB;
using Microsoft.EntityFrameworkCore;

namespace Dapper.Tests.Performance.Linq2Db
{
    public class Linq2DBContext : LinqToDB.Data.DataConnection
    {
        public ITable<Post> Posts => GetTable<Post>();
    }
}
