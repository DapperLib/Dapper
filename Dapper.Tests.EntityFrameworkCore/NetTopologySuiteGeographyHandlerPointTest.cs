using System;
using System.Linq;
using Dapper.EntityFrameworkCore;
using Dapper.Tests.EntityFrameworkCore.Model;
using NetTopologySuite.Geometries;
using Xunit;

namespace Dapper.Tests.EntityFrameworkCore
{
    public class NetTopologySuiteGeographyHandlerPointTest : TestBase<SystemSqlClientProvider>
    {

        public NetTopologySuiteGeographyHandlerPointTest()
        {
            Handlers.RegisterGeography();
        }

        [Fact]
        public void TestPointSelect()
        {
            var data = connection.Query<PointModel>("SELECT TOP 5 Id, CreateTime, Feature.Serialize() AS Feature FROM Coordinates");

            Assert.Equal(5, data.Count());
        }

        [Fact]
        public void TestPointSingleInfo()
        {
            var datum = connection.QueryFirstOrDefault<PointModel>("SELECT Id, CreateTime, Feature.Serialize() AS Feature FROM Coordinates WHERE Id = @id", new { id = new Guid("1f93e204-f91c-ea11-85d1-000d3a824a2e") });

            Assert.Equal(121.57702788867778, datum.Feature.X);
            Assert.Equal(25.073700196616159, datum.Feature.Y);
        }

        [Fact]
        public void TestPointInsert()
        {
            var point = new Point(121, 25) { SRID = 4326 };
            string sql = "INSERT INTO Coordinates (Id, CreateTime, Feature) VALUES (@id, @createTime, @feature)";
            var rowCount = connection.Execute(sql,
                new { id = Guid.NewGuid(), createTime = DateTimeOffset.Now, feature = point });

            Assert.Equal(1, rowCount);
        }
    }
}
