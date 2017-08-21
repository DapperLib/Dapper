#if ENTITY_FRAMEWORK
using System.Data.Entity.Spatial;
using Xunit;

namespace Dapper.Tests.Providers
{
    [Collection("TypeHandlerTests")]
    public class EntityFrameworkTests : TestBase
    {
        public EntityFrameworkTests()
        {
            EntityFramework.Handlers.Register();
        }

        [Fact]
        public void Issue570_DbGeo_HasValues()
        {
            EntityFramework.Handlers.Register();
            const string redmond = "POINT (122.1215 47.6740)";
            DbGeography point = DbGeography.PointFromText(redmond, DbGeography.DefaultCoordinateSystemId);
            DbGeography orig = point.Buffer(20);

            var fromDb = connection.QuerySingle<DbGeography>("declare @geos table(geo geography); insert @geos(geo) values(@val); select * from @geos",
                new { val = orig });

            Assert.NotNull(fromDb.Area);
            Assert.Equal(orig.Area, fromDb.Area);
        }

        [Fact]
        public void Issue22_ExecuteScalar_EntityFramework()
        {
            var geo = DbGeography.LineFromText("LINESTRING(-122.360 47.656, -122.343 47.656 )", 4326);
            var geo2 = connection.ExecuteScalar<DbGeography>("select @geo", new { geo });
            Assert.NotNull(geo2);
        }
    }
}
#endif
