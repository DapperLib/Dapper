#if ENTITY_FRAMEWORK
using System.Data.Common;
using System.Data.Entity;

namespace Dapper.Tests.EntityFramework
{
    public class EFContext : DbContext
    {
        public EFContext(DbConnection connection, bool owned = false) : base(connection, owned)
        {

            Configuration.AutoDetectChangesEnabled = false;
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
            Configuration.ValidateOnSaveEnabled = false;
            
        }
        public DbSet<Post> Posts { get;set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Post>().HasKey(x => x.Id);
            base.OnModelCreating(modelBuilder);
        }
    }
}
#endif
