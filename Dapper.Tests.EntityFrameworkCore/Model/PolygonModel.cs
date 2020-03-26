using System;
using NetTopologySuite.Geometries;

namespace Dapper.Tests.EntityFrameworkCore.Model
{
    public class PolygonModel
    {
        public Guid Id { get; set; }

        public DateTimeOffset CreateTime { get; set; }

        public Polygon Feature { get; set; }
    }
}
