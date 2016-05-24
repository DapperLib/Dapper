using Dapper;
using System;
using System.Data;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    public partial class TestSuite
    {
        [Fact]
        public void TestEnumWeirdness()
        {
            connection.Query<TestEnumClass>("select null as [EnumEnum]").First().EnumEnum.IsEqualTo(null);
            connection.Query<TestEnumClass>("select cast(1 as tinyint) as [EnumEnum]").First().EnumEnum.IsEqualTo(TestEnum.Bla);
        }

        [Fact]
        public void TestEnumStrings()
        {
            connection.Query<TestEnumClassNoNull>("select 'BLA' as [EnumEnum]").First().EnumEnum.IsEqualTo(TestEnum.Bla);
            connection.Query<TestEnumClassNoNull>("select 'bla' as [EnumEnum]").First().EnumEnum.IsEqualTo(TestEnum.Bla);

            connection.Query<TestEnumClass>("select 'BLA' as [EnumEnum]").First().EnumEnum.IsEqualTo(TestEnum.Bla);
            connection.Query<TestEnumClass>("select 'bla' as [EnumEnum]").First().EnumEnum.IsEqualTo(TestEnum.Bla);
        }

        [Fact]
        public void TestEnumParamsWithNullable()
        {
            EnumParam a = EnumParam.A;
            EnumParam? b = EnumParam.B, c = null;
            var obj = connection.Query<EnumParamObject>("select @a as A, @b as B, @c as C",
                new { a, b, c }).Single();
            obj.A.IsEqualTo(EnumParam.A);
            obj.B.IsEqualTo(EnumParam.B);
            obj.C.IsEqualTo(null);
        }

        [Fact]
        public void TestEnumParamsWithoutNullable()
        {
            EnumParam a = EnumParam.A;
            EnumParam b = EnumParam.B, c = 0;
            var obj = connection.Query<EnumParamObjectNonNullable>("select @a as A, @b as B, @c as C",
                new { a, b, c }).Single();
            obj.A.IsEqualTo(EnumParam.A);
            obj.B.IsEqualTo(EnumParam.B);
            obj.C.IsEqualTo((EnumParam)0);
        }
        enum EnumParam : short
        {
            None, A, B
        }
        class EnumParamObject
        {
            public EnumParam A { get; set; }
            public EnumParam? B { get; set; }
            public EnumParam? C { get; set; }
        }
        class EnumParamObjectNonNullable
        {
            public EnumParam A { get; set; }
            public EnumParam? B { get; set; }
            public EnumParam? C { get; set; }
        }




        enum TestEnum : byte
        {
            Bla = 1
        }
        class TestEnumClass
        {
            public TestEnum? EnumEnum { get; set; }
        }
        class TestEnumClassNoNull
        {
            public TestEnum EnumEnum { get; set; }
        }
    }
}
