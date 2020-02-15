using System;
using System.Linq;
using Dapper.EntityFrameworkCore;
using Dapper.Tests.EntityFrameworkCore.Model;
using NetTopologySuite.Geometries;
using Xunit;

namespace Dapper.Tests.EntityFrameworkCore
{
    public class NetTopologySuiteGeographyHandlerPolygonTest : TestBase<SystemSqlClientProvider>
    {

        public NetTopologySuiteGeographyHandlerPolygonTest()
        {
            Handlers.RegisterGeography();
        }

        [Fact]
        public void TestPolygonSelect()
        {
            var data = connection.Query<PolygonModel>("SELECT TOP 5 Id, CreateTime, Feature.Serialize() AS Feature FROM Polygons");

            Assert.Equal(5, data.Count());
        }

        [Fact]
        public void TestPolygonSingleInfo()
        {
            var datum = connection.QueryFirstOrDefault<PolygonModel>("SELECT Id, CreateTime, Feature.Serialize() AS Feature FROM Polygons WHERE Id = @id", new { id = new Guid("95be5c25-d8bc-4120-9f75-270272cd2491") });

            Assert.Equal(6, datum.Feature.Coordinates.Length);
            Assert.Equal(121.51616569074, datum.Feature.Coordinates[0].X);
            Assert.Equal(25.05357544032, datum.Feature.Coordinates[0].Y);
            Assert.Equal(121.53341334419, datum.Feature.Coordinates[1].X);
            Assert.Equal(25.04372812789, datum.Feature.Coordinates[1].Y);
            Assert.Equal(121.55946448743, datum.Feature.Coordinates[2].X);
            Assert.Equal(25.03811236424, datum.Feature.Coordinates[2].Y);
            Assert.Equal(121.55641021547, datum.Feature.Coordinates[3].X);
            Assert.Equal(25.05919049573, datum.Feature.Coordinates[3].Y);
            Assert.Equal(121.53098789292, datum.Feature.Coordinates[4].X);
            Assert.Equal(25.06073662523, datum.Feature.Coordinates[4].Y);
            Assert.Equal(121.51616569074, datum.Feature.Coordinates[5].X);
            Assert.Equal(25.05357544032, datum.Feature.Coordinates[5].Y);
        }

        [Fact]
        public void TestPolygonInsert()
        {
            var point = new Polygon(new LinearRing(new[]
            {
                new Coordinate(121.51616569074, 25.05357544032),
                new Coordinate(121.53341334419,25.04372812789),
                new Coordinate(121.55946448743,25.03811236424),
                new Coordinate(121.55641021547,25.05919049573),
                new Coordinate(121.53098789292,25.06073662523),
                new Coordinate(121.51616569074,25.05357544032)
            }))
            { SRID = 4326 };
            string sql = "INSERT INTO Polygons (Id, CreateTime, Feature) VALUES (@id, @createTime, @feature)";
            var rowCount = connection.Execute(sql,
                new { id = Guid.NewGuid(), createTime = DateTimeOffset.Now, feature = point });

            Assert.Equal(1, rowCount);
        }
    }
}
