using System;
using NetTopologySuite.Geometries;

namespace Dapper.Tests.EntityFrameworkCore.Model
{
    public class LineStringModel
    {
        public Guid Id { get; set; }

        public DateTimeOffset CreateTime { get; set; }

        public LineString Feature { get; set; }
    }
}
