using System;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    public class PolymorphicTests : TestBase
    {
        class Shape
        {
            public int Type { get; set; }
            public int NumberOfSides { get; set; }
        }

        class Circle : Shape
        {
            public double Radius { get; set; }
        }

        class Triangle : Shape
        {
            public double Angle { get; set; }
        }

        static PolymorphicTests()
        {
            SqlMapper.RegisterPolymorphicLoader<Shape, int>("Type", type =>
            {
                switch (type)
                {
                    case 1:
                        return typeof(Circle);
                    case 2:
                        return typeof(Triangle);
                    default:
                        throw new ArgumentException($"unknown type {type}", nameof(type));
                }
            });
        }

        [Fact]
        public void Query_LoadSubclassAsParent()
        {
            var shape = connection.QuerySingle<Shape>("select 1 as Type, 0 as NumberOfSides, 0.5 as Radius");

            Assert.IsType<Circle>(shape);
        }

        [Fact]
        public void Query_LoadMultipleSublclassesAsParent()
        {
            var shapes = connection.Query<Shape>(
                @"select 0 AS NumberOfSides, 1 as Type, 0.5 as Radius, null as Angle 
                  union all
                  select 3 AS NumberOfSides, 2 as Type, null as Radius, 60.0 as Angle").ToList();

            Assert.IsType<Circle>(shapes[0]);
            Assert.Equal(0.5, ((Circle)shapes[0]).Radius);
            Assert.IsType<Triangle>(shapes[1]);
            Assert.Equal(60.0, ((Triangle)shapes[1]).Angle);            
        }

        [Fact]
        public void Query_MultiMap()
        {
            var res = connection.Query<Shape, Shape, dynamic>(
                "select 0 as NumberOfSides, 1 as Type, 0.5 AS Radius, null as Angle, 3 as NumberOfSides, 2 as Type, NULL as Radius, 60.0 as Angle",
                map: (circle, triangle) => new { circle, triangle },
                splitOn: "NumberOfSides").Single();

            Assert.IsType<Circle>(res.circle);
            Assert.Equal(0.5, ((Circle)res.circle).Radius);
            Assert.IsType<Triangle>(res.triangle);
            Assert.Equal(60.0, ((Triangle)res.triangle).Angle);
        }
    }
}
