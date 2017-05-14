using System.Data;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    public class EnumTests : TestBase
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

        private enum EnumParam : short
        {
            None = 0,
            A = 1,
            B = 2
        }

        private class EnumParamObject
        {
            public EnumParam A { get; set; }
            public EnumParam? B { get; set; }
            public EnumParam? C { get; set; }
        }

        private class EnumParamObjectNonNullable
        {
            public EnumParam A { get; set; }
            public EnumParam? B { get; set; }
            public EnumParam? C { get; set; }
        }

        private enum TestEnum : byte
        {
            Bla = 1
        }

        private class TestEnumClass
        {
            public TestEnum? EnumEnum { get; set; }
        }

        private class TestEnumClassNoNull
        {
            public TestEnum EnumEnum { get; set; }
        }

        [Fact]
        public void AdoNetEnumValue()
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "select @foo";
                var p = cmd.CreateParameter();
                p.ParameterName = "@foo";
                p.DbType = DbType.Int32; // it turns out that this is the key piece; setting the DbType
                p.Value = AnEnum.B;
                cmd.Parameters.Add(p);
                object value = cmd.ExecuteScalar();
                AnEnum val = (AnEnum)value;
                val.IsEqualTo(AnEnum.B);
            }
        }

        [Fact]
        public void DapperEnumValue_SqlServer() => Common.DapperEnumValue(connection);

        private enum SO27024806Enum
        {
            Foo = 0,
            Bar = 1
        }

        private class SO27024806Class
        {
            public SO27024806Class(SO27024806Enum myField)
            {
                MyField = myField;
            }

            public SO27024806Enum MyField { get; set; }
        }

        [Fact]
        public void SO27024806_TestVarcharEnumMemberWithExplicitConstructor()
        {
            var foo = connection.Query<SO27024806Class>("SELECT 'Foo' AS myField").Single();
            foo.MyField.IsEqualTo(SO27024806Enum.Foo);
        }
    }
}
