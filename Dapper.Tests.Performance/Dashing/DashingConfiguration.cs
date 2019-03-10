using Dashing.Configuration;

namespace Dapper.Tests.Performance.Dashing
{
    public class DashingConfiguration : BaseConfiguration
    {
        public DashingConfiguration()
        {
            this.Add<Post>();
        }
    }
}