using System.Data.Common;
using System.Data.Entity;

namespace SqlMapper.EntityFramework
{
    public class EFContext : DbContext
    {
        public EFContext(DbConnection connection, bool owned = false) : base(connection, owned) { }
        public DbSet<Post> Posts { get;set; }
    }
}
