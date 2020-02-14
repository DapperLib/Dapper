using NetTopologySuite.Geometries;

namespace Dapper.EntityFrameworkCore
{
    /// <summary>
    /// Acts on behalf of all type-handlers in this package
    /// </summary>
    public static class Handlers
    {
        /// <summary>
        /// Register geography type-handlers in this package
        /// </summary>
        public static void RegisterGeography()
        {
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeographyHandler<Point>());
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeographyHandler<Polygon>());
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeographyHandler<LineString>());
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeographyHandler<LinearRing>());
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeographyHandler<MultiLineString>());
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeographyHandler<MultiPoint>());
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeographyHandler<MultiPolygon>());
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeographyHandler<Geometry>());
        }

        /// <summary>
        /// Register geometry type-handlers in this package
        /// </summary>
        public static void RegisterGeometry()
        {
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<Point>());
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<Polygon>());
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<LineString>());
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<LinearRing>());
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<MultiLineString>());
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<MultiPoint>());
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<MultiPolygon>());
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<Geometry>());
        }
    }
}
