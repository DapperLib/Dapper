using Dashing.Configuration;

namespace Dapper.Tests.Performance.Dashing
{
    public class DashingConfiguration : BaseConfiguration
    {
        public DashingConfiguration()
        {
            Add<Post>();
        }
    }
}
