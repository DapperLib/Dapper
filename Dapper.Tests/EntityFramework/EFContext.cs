#if ENTITY_FRAMEWORK
using System.Data.Common;
using System.Data.Entity;

namespace Dapper.Tests.EntityFramework
{
    public class EFContext : DbContext
    {
        public EFContext(DbConnection connection, bool owned = false) : base(connection, owned) { }
        public DbSet<Post> Posts { get;set; }
    }
}
#endif
