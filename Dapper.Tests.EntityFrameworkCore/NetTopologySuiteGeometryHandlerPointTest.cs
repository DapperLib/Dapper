using System;
using System.Linq;
using Dapper.EntityFrameworkCore;
using Dapper.Tests.EntityFrameworkCore.Model;
using NetTopologySuite.Geometries;
using Xunit;

namespace Dapper.Tests.EntityFrameworkCore
{
    public class NetTopologySuiteGeometryHandlerPointTest : TestBase<SystemSqlClientProvider>
    {

        public NetTopologySuiteGeometryHandlerPointTest()
        {
            Handlers.RegisterGeometry();
        }

        [Fact]
        public void TestPointGeometrySelect()
        {
            var data = connection.Query<PointModel>("SELECT TOP 5 Id, CreateTime, Feature.Serialize() AS Feature FROM CoordinateGeometrys");

            Assert.Equal(5, data.Count());
        }

        [Fact]
        public void TestPointGeometrySingleInfo()
        {
            var datum = connection.QueryFirstOrDefault<PointModel>("SELECT Id, CreateTime, Feature.Serialize() AS Feature FROM CoordinateGeometrys WHERE Id = @id", new { id = new Guid("0e62ac3e-b334-4220-8881-5085bb2ef0f9") });

            Assert.Equal(121.22, datum.Feature.X);
            Assert.Equal(25.11, datum.Feature.Y);
        }

        [Fact]
        public void TestPointGeometryInsert()
        {
            var point = new Point(121, 25) { SRID = 4326 };
            string sql = "INSERT INTO CoordinateGeometrys (Id, CreateTime, Feature) VALUES (@id, @createTime, @feature)";
            var rowCount = connection.Execute(sql,
                new { id = Guid.NewGuid(), createTime = DateTimeOffset.Now, feature = point });

            Assert.Equal(1, rowCount);
        }
    }
}
