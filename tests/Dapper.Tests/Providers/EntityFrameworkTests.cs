#if ENTITY_FRAMEWORK
using System;
using System.Data.Entity.Spatial;
using System.Linq;
using Xunit;

namespace Dapper.Tests.Providers
{
    public sealed class SystemSqlClientEntityFrameworkTests : EntityFrameworkTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    public sealed class MicrosoftSqlClientEntityFrameworkTests : EntityFrameworkTests<MicrosoftSqlClientProvider> { }
#endif

    [Collection("TypeHandlerTests")]
    public abstract class EntityFrameworkTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    {
        public EntityFrameworkTests()
        {
            EntityFramework.Handlers.Register();
        }

        [Fact]
        public void Issue570_DbGeo_HasValues()
        {
            SkipIfMsDataClient();

            EntityFramework.Handlers.Register();
            const string redmond = "POINT (-122.1215 47.6740)";
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
            SkipIfMsDataClient();

            var geo = DbGeography.LineFromText("LINESTRING(-122.360 47.656, -122.343 47.656 )", 4326);
            var geo2 = connection.ExecuteScalar<DbGeography>("select @geo", new { geo });
            Assert.NotNull(geo2);
        }

        [Fact]
        public void TestGeometryParsingRetainsSrid()
        {
            const int srid = 27700;
            var s = $@"DECLARE @EdinburghPoint GEOMETRY = geometry::STPointFromText('POINT(258647 665289)', {srid});
SELECT @EdinburghPoint";
            var edinPoint = connection.Query<DbGeometry>(s).Single();
            Assert.NotNull(edinPoint);
            Assert.Equal(srid, edinPoint.CoordinateSystemId);
        }

        [Fact]
        public void TestGeographyParsingRetainsSrid()
        {
            const int srid = 4324;
            var s = $@"DECLARE @EdinburghPoint GEOGRAPHY = geography::STPointFromText('POINT(-3.19 55.95)', {srid});
SELECT @EdinburghPoint";
            var edinPoint = connection.Query<DbGeography>(s).Single();
            Assert.NotNull(edinPoint);
            Assert.Equal(srid, edinPoint.CoordinateSystemId);
        }

    }
}
#endif
