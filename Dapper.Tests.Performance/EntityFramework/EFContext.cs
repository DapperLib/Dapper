#if NET4X
using System.Data.Common;
using System.Data.Entity;

namespace Dapper.Tests.Performance.EntityFramework
{
    public class EFContext : DbContext
    {
        public EFContext(DbConnection connection, bool owned = false) : base(connection, owned)
        {
        }

        public DbSet<Post> Posts { get; set; }
    }
}
#endif
