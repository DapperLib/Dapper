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
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<Point>(true));
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<Polygon>(true));
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<LineString>(true));
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<LinearRing>(true));
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<MultiLineString>(true));
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<MultiPoint>(true));
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<MultiPolygon>(true));
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<Geometry>(true));
        }

        /// <summary>
        /// Register geometry type-handlers in this package
        /// </summary>
        public static void RegisterGeometry()
        {
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<Point>(false));
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<Polygon>(false));
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<LineString>(false));
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<LinearRing>(false));
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<MultiLineString>(false));
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<MultiPoint>(false));
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<MultiPolygon>(false));
            SqlMapper.AddTypeHandler(new NetTopologySuiteGeometryHandler<Geometry>(false));
        }
    }
}
