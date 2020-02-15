using System;
using System.Linq;
using Dapper.EntityFrameworkCore;
using Dapper.Tests.EntityFrameworkCore.Model;
using NetTopologySuite.Geometries;
using Xunit;

namespace Dapper.Tests.EntityFrameworkCore
{
    public class NetTopologySuiteGeometryHandlerLineStringTest : TestBase<SystemSqlClientProvider>
    {

        public NetTopologySuiteGeometryHandlerLineStringTest()
        {
            Handlers.RegisterGeometry();
        }

        [Fact]
        public void TestLineStringGeometrySelect()
        {
            var data = connection.Query<LineStringModel>("SELECT TOP 5 Id, CreateTime, Feature.Serialize() AS Feature FROM LineStringGeometrys");

            Assert.Equal(5, data.Count());
        }

        [Fact]
        public void TestLineStringGeometrySingleInfo()
        {
            var datum = connection.QueryFirstOrDefault<LineStringModel>("SELECT Id, CreateTime, Feature.Serialize() AS Feature FROM LineStringGeometrys WHERE Id = @id", new { id = new Guid("fad03e32-add8-4b70-a670-5fc6d9470e6d") });

            Assert.Equal(4, datum.Feature.Coordinates.Length);
            Assert.Equal(121, datum.Feature.Coordinates[0].X);
            Assert.Equal(25, datum.Feature.Coordinates[0].Y);
            Assert.Equal(121, datum.Feature.Coordinates[1].X);
            Assert.Equal(26, datum.Feature.Coordinates[1].Y);
            Assert.Equal(122, datum.Feature.Coordinates[2].X);
            Assert.Equal(25, datum.Feature.Coordinates[2].Y);
            Assert.Equal(122, datum.Feature.Coordinates[3].X);
            Assert.Equal(27, datum.Feature.Coordinates[3].Y);
        }

        [Fact]
        public void TestLineStringGeometryInsert()
        {
            var point = new LineString(new[]
            {
                new Coordinate(121, 25),
                new Coordinate(121, 26),
                new Coordinate(122, 25),
                new Coordinate(122, 27)
            })
            { SRID = 4326 };
            string sql = "INSERT INTO LineStringGeometrys (Id, CreateTime, Feature) VALUES (@id, @createTime, @feature)";
            var rowCount = connection.Execute(sql,
                new { id = Guid.NewGuid(), createTime = DateTimeOffset.Now, feature = point });

            Assert.Equal(1, rowCount);
        }
    }
}
