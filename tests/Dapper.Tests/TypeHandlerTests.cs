using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Dapper.Tests
{
    [Collection(NonParallelDefinition.Name)]
    public sealed class SystemSqlClientTypeHandlerTests : TypeHandlerTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection(NonParallelDefinition.Name)]
    public sealed class MicrosoftSqlClientTypeHandlerTests : TypeHandlerTests<MicrosoftSqlClientProvider> { }
#endif

    public abstract class TypeHandlerTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    {
        [Fact]
        public void TestChangingDefaultStringTypeMappingToAnsiString()
        {
            const string sql = "SELECT SQL_VARIANT_PROPERTY(CONVERT(sql_variant, @testParam),'BaseType') AS BaseType";
            var param = new { testParam = "TestString" };

            var result01 = connection.Query<string>(sql, param).FirstOrDefault();
            Assert.Equal("nvarchar", result01);

            SqlMapper.PurgeQueryCache();

            SqlMapper.AddTypeMap(typeof(string), DbType.AnsiString);   // Change Default String Handling to AnsiString
            var result02 = connection.Query<string>(sql, param).FirstOrDefault();
            Assert.Equal("varchar", result02);

            SqlMapper.PurgeQueryCache();
            SqlMapper.AddTypeMap(typeof(string), DbType.String);  // Restore Default to Unicode String
        }

        [Fact]
        public void TestChangingDefaultStringTypeMappingToAnsiStringFirstOrDefault()
        {
            const string sql = "SELECT SQL_VARIANT_PROPERTY(CONVERT(sql_variant, @testParam),'BaseType') AS BaseType";
            var param = new { testParam = "TestString" };

            var result01 = connection.QueryFirstOrDefault<string>(sql, param);
            Assert.Equal("nvarchar", result01);

            SqlMapper.PurgeQueryCache();

            SqlMapper.AddTypeMap(typeof(string), DbType.AnsiString);   // Change Default String Handling to AnsiString
            var result02 = connection.QueryFirstOrDefault<string>(sql, param);
            Assert.Equal("varchar", result02);

            SqlMapper.PurgeQueryCache();
            SqlMapper.AddTypeMap(typeof(string), DbType.String);  // Restore Default to Unicode String
        }

        [Fact]
        public void TestCustomTypeMap()
        {
            // default mapping
            var item = connection.Query<TypeWithMapping>("Select 'AVal' as A, 'BVal' as B").Single();
            Assert.Equal("AVal", item.A);
            Assert.Equal("BVal", item.B);

            // custom mapping
            var map = new CustomPropertyTypeMap(typeof(TypeWithMapping),
                (type, columnName) => type.GetProperties().FirstOrDefault(prop => GetDescriptionFromAttribute(prop) == columnName));
            SqlMapper.SetTypeMap(typeof(TypeWithMapping), map);

            item = connection.Query<TypeWithMapping>("Select 'AVal' as A, 'BVal' as B").Single();
            Assert.Equal("BVal", item.A);
            Assert.Equal("AVal", item.B);

            // reset to default
            SqlMapper.SetTypeMap(typeof(TypeWithMapping), null);
            item = connection.Query<TypeWithMapping>("Select 'AVal' as A, 'BVal' as B").Single();
            Assert.Equal("AVal", item.A);
            Assert.Equal("BVal", item.B);
        }

        private static string GetDescriptionFromAttribute(MemberInfo member)
        {
            if (member == null) return null;
            var attrib = (DescriptionAttribute)Attribute.GetCustomAttribute(member, typeof(DescriptionAttribute), false);
            return attrib?.Description;
        }

        public class TypeWithMapping
        {
            [Description("B")]
            public string A { get; set; }

            [Description("A")]
            public string B { get; set; }
        }

        [Fact]
        public void Issue136_ValueTypeHandlers()
        {
            SqlMapper.ResetTypeHandlers();
            SqlMapper.AddTypeHandler(typeof(LocalDate), LocalDateHandler.Default);
            var param = new LocalDateResult
            {
                NotNullable = new LocalDate { Year = 2014, Month = 7, Day = 25 },
                NullableNotNull = new LocalDate { Year = 2014, Month = 7, Day = 26 },
                NullableIsNull = null,
            };

            var result = connection.Query<LocalDateResult>("SELECT @NotNullable AS NotNullable, @NullableNotNull AS NullableNotNull, @NullableIsNull AS NullableIsNull", param).Single();

            SqlMapper.ResetTypeHandlers();
            SqlMapper.AddTypeHandler(typeof(LocalDate?), LocalDateHandler.Default);
            result = connection.Query<LocalDateResult>("SELECT @NotNullable AS NotNullable, @NullableNotNull AS NullableNotNull, @NullableIsNull AS NullableIsNull", param).Single();
        }

        public class LocalDateHandler : SqlMapper.TypeHandler<LocalDate>
        {
            private LocalDateHandler() { /* private constructor */ }

            // Make the field type ITypeHandler to ensure it cannot be used with SqlMapper.AddTypeHandler<T>(TypeHandler<T>)
            // by mistake.
            public static readonly SqlMapper.ITypeHandler Default = new LocalDateHandler();

            public override LocalDate Parse(object value)
            {
                var date = (DateTime)value;
                return new LocalDate { Year = date.Year, Month = date.Month, Day = date.Day };
            }

            public override void SetValue(IDbDataParameter parameter, LocalDate value)
            {
                parameter.DbType = DbType.DateTime;
                parameter.Value = new DateTime(value.Year, value.Month, value.Day);
            }
        }

        public struct LocalDate
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public int Day { get; set; }
        }

        public class LocalDateResult
        {
            public LocalDate NotNullable { get; set; }
            public LocalDate? NullableNotNull { get; set; }
            public LocalDate? NullableIsNull { get; set; }
        }

        public class LotsOfNumerics
        {
            public enum E_Byte : byte { A = 0, B = 1 }
            public enum E_SByte : sbyte { A = 0, B = 1 }
            public enum E_Short : short { A = 0, B = 1 }
            public enum E_UShort : ushort { A = 0, B = 1 }
            public enum E_Int : int { A = 0, B = 1 }
            public enum E_UInt : uint { A = 0, B = 1 }
            public enum E_Long : long { A = 0, B = 1 }
            public enum E_ULong : ulong { A = 0, B = 1 }

            public E_Byte P_Byte { get; set; }
            public E_SByte P_SByte { get; set; }
            public E_Short P_Short { get; set; }
            public E_UShort P_UShort { get; set; }
            public E_Int P_Int { get; set; }
            public E_UInt P_UInt { get; set; }
            public E_Long P_Long { get; set; }
            public E_ULong P_ULong { get; set; }

            public bool N_Bool { get; set; }
            public byte N_Byte { get; set; }
            public sbyte N_SByte { get; set; }
            public short N_Short { get; set; }
            public ushort N_UShort { get; set; }
            public int N_Int { get; set; }
            public uint N_UInt { get; set; }
            public long N_Long { get; set; }
            public ulong N_ULong { get; set; }

            public float N_Float { get; set; }
            public double N_Double { get; set; }
            public decimal N_Decimal { get; set; }

            public E_Byte? N_P_Byte { get; set; }
            public E_SByte? N_P_SByte { get; set; }
            public E_Short? N_P_Short { get; set; }
            public E_UShort? N_P_UShort { get; set; }
            public E_Int? N_P_Int { get; set; }
            public E_UInt? N_P_UInt { get; set; }
            public E_Long? N_P_Long { get; set; }
            public E_ULong? N_P_ULong { get; set; }

            public bool? N_N_Bool { get; set; }
            public byte? N_N_Byte { get; set; }
            public sbyte? N_N_SByte { get; set; }
            public short? N_N_Short { get; set; }
            public ushort? N_N_UShort { get; set; }
            public int? N_N_Int { get; set; }
            public uint? N_N_UInt { get; set; }
            public long? N_N_Long { get; set; }
            public ulong? N_N_ULong { get; set; }

            public float? N_N_Float { get; set; }
            public double? N_N_Double { get; set; }
            public decimal? N_N_Decimal { get; set; }
        }

        [Fact]
        public void TestBigIntForEverythingWorks()
        {
            TestBigIntForEverythingWorks_ByDataType<long>("bigint");
            TestBigIntForEverythingWorks_ByDataType<int>("int");
            TestBigIntForEverythingWorks_ByDataType<byte>("tinyint");
            TestBigIntForEverythingWorks_ByDataType<short>("smallint");
            TestBigIntForEverythingWorks_ByDataType<bool>("bit");
            TestBigIntForEverythingWorks_ByDataType<float>("float(24)");
            TestBigIntForEverythingWorks_ByDataType<double>("float(53)");
        }

        private void TestBigIntForEverythingWorks_ByDataType<T>(string dbType)
        {
            using (var reader = connection.ExecuteReader("select cast(1 as " + dbType + ")"))
            {
                Assert.True(reader.Read());
                reader.GetFieldType(0).Equals(typeof(T));
                Assert.False(reader.Read());
                Assert.False(reader.NextResult());
            }

            string sql = "select " + string.Join(",", typeof(LotsOfNumerics).GetProperties().Select(
                x => "cast (1 as " + dbType + ") as [" + x.Name + "]"));
            var row = connection.Query<LotsOfNumerics>(sql).Single();

            Assert.True(row.N_Bool);
            Assert.Equal(row.N_SByte, (sbyte)1);
            Assert.Equal(row.N_Byte, (byte)1);
            Assert.Equal(row.N_Int, (int)1);
            Assert.Equal(row.N_UInt, (uint)1);
            Assert.Equal(row.N_Short, (short)1);
            Assert.Equal(row.N_UShort, (ushort)1);
            Assert.Equal(row.N_Long, (long)1);
            Assert.Equal(row.N_ULong, (ulong)1);
            Assert.Equal(row.N_Float, (float)1);
            Assert.Equal(row.N_Double, (double)1);
            Assert.Equal(row.N_Decimal, (decimal)1);

            Assert.Equal(LotsOfNumerics.E_Byte.B, row.P_Byte);
            Assert.Equal(LotsOfNumerics.E_SByte.B, row.P_SByte);
            Assert.Equal(LotsOfNumerics.E_Short.B, row.P_Short);
            Assert.Equal(LotsOfNumerics.E_UShort.B, row.P_UShort);
            Assert.Equal(LotsOfNumerics.E_Int.B, row.P_Int);
            Assert.Equal(LotsOfNumerics.E_UInt.B, row.P_UInt);
            Assert.Equal(LotsOfNumerics.E_Long.B, row.P_Long);
            Assert.Equal(LotsOfNumerics.E_ULong.B, row.P_ULong);

            Assert.True(row.N_N_Bool.Value);
            Assert.Equal(row.N_N_SByte.Value, (sbyte)1);
            Assert.Equal(row.N_N_Byte.Value, (byte)1);
            Assert.Equal(row.N_N_Int.Value, (int)1);
            Assert.Equal(row.N_N_UInt.Value, (uint)1);
            Assert.Equal(row.N_N_Short.Value, (short)1);
            Assert.Equal(row.N_N_UShort.Value, (ushort)1);
            Assert.Equal(row.N_N_Long.Value, (long)1);
            Assert.Equal(row.N_N_ULong.Value, (ulong)1);
            Assert.Equal(row.N_N_Float.Value, (float)1);
            Assert.Equal(row.N_N_Double.Value, (double)1);
            Assert.Equal(row.N_N_Decimal, (decimal)1);

            Assert.Equal(LotsOfNumerics.E_Byte.B, row.N_P_Byte.Value);
            Assert.Equal(LotsOfNumerics.E_SByte.B, row.N_P_SByte.Value);
            Assert.Equal(LotsOfNumerics.E_Short.B, row.N_P_Short.Value);
            Assert.Equal(LotsOfNumerics.E_UShort.B, row.N_P_UShort.Value);
            Assert.Equal(LotsOfNumerics.E_Int.B, row.N_P_Int.Value);
            Assert.Equal(LotsOfNumerics.E_UInt.B, row.N_P_UInt.Value);
            Assert.Equal(LotsOfNumerics.E_Long.B, row.N_P_Long.Value);
            Assert.Equal(LotsOfNumerics.E_ULong.B, row.N_P_ULong.Value);

            TestBigIntForEverythingWorksGeneric<bool>(true, dbType);
            TestBigIntForEverythingWorksGeneric<sbyte>((sbyte)1, dbType);
            TestBigIntForEverythingWorksGeneric<byte>((byte)1, dbType);
            TestBigIntForEverythingWorksGeneric<int>((int)1, dbType);
            TestBigIntForEverythingWorksGeneric<uint>((uint)1, dbType);
            TestBigIntForEverythingWorksGeneric<short>((short)1, dbType);
            TestBigIntForEverythingWorksGeneric<ushort>((ushort)1, dbType);
            TestBigIntForEverythingWorksGeneric<long>((long)1, dbType);
            TestBigIntForEverythingWorksGeneric<ulong>((ulong)1, dbType);
            TestBigIntForEverythingWorksGeneric<float>((float)1, dbType);
            TestBigIntForEverythingWorksGeneric<double>((double)1, dbType);
            TestBigIntForEverythingWorksGeneric<decimal>((decimal)1, dbType);

            TestBigIntForEverythingWorksGeneric(LotsOfNumerics.E_Byte.B, dbType);
            TestBigIntForEverythingWorksGeneric(LotsOfNumerics.E_SByte.B, dbType);
            TestBigIntForEverythingWorksGeneric(LotsOfNumerics.E_Int.B, dbType);
            TestBigIntForEverythingWorksGeneric(LotsOfNumerics.E_UInt.B, dbType);
            TestBigIntForEverythingWorksGeneric(LotsOfNumerics.E_Short.B, dbType);
            TestBigIntForEverythingWorksGeneric(LotsOfNumerics.E_UShort.B, dbType);
            TestBigIntForEverythingWorksGeneric(LotsOfNumerics.E_Long.B, dbType);
            TestBigIntForEverythingWorksGeneric(LotsOfNumerics.E_ULong.B, dbType);

            TestBigIntForEverythingWorksGeneric<bool?>(true, dbType);
            TestBigIntForEverythingWorksGeneric<sbyte?>((sbyte)1, dbType);
            TestBigIntForEverythingWorksGeneric<byte?>((byte)1, dbType);
            TestBigIntForEverythingWorksGeneric<int?>((int)1, dbType);
            TestBigIntForEverythingWorksGeneric<uint?>((uint)1, dbType);
            TestBigIntForEverythingWorksGeneric<short?>((short)1, dbType);
            TestBigIntForEverythingWorksGeneric<ushort?>((ushort)1, dbType);
            TestBigIntForEverythingWorksGeneric<long?>((long)1, dbType);
            TestBigIntForEverythingWorksGeneric<ulong?>((ulong)1, dbType);
            TestBigIntForEverythingWorksGeneric<float?>((float)1, dbType);
            TestBigIntForEverythingWorksGeneric<double?>((double)1, dbType);
            TestBigIntForEverythingWorksGeneric<decimal?>((decimal)1, dbType);

            TestBigIntForEverythingWorksGeneric<LotsOfNumerics.E_Byte?>(LotsOfNumerics.E_Byte.B, dbType);
            TestBigIntForEverythingWorksGeneric<LotsOfNumerics.E_SByte?>(LotsOfNumerics.E_SByte.B, dbType);
            TestBigIntForEverythingWorksGeneric<LotsOfNumerics.E_Int?>(LotsOfNumerics.E_Int.B, dbType);
            TestBigIntForEverythingWorksGeneric<LotsOfNumerics.E_UInt?>(LotsOfNumerics.E_UInt.B, dbType);
            TestBigIntForEverythingWorksGeneric<LotsOfNumerics.E_Short?>(LotsOfNumerics.E_Short.B, dbType);
            TestBigIntForEverythingWorksGeneric<LotsOfNumerics.E_UShort?>(LotsOfNumerics.E_UShort.B, dbType);
            TestBigIntForEverythingWorksGeneric<LotsOfNumerics.E_Long?>(LotsOfNumerics.E_Long.B, dbType);
            TestBigIntForEverythingWorksGeneric<LotsOfNumerics.E_ULong?>(LotsOfNumerics.E_ULong.B, dbType);
        }

        private void TestBigIntForEverythingWorksGeneric<T>(T expected, string dbType)
        {
            var query = connection.Query<T>("select cast(1 as " + dbType + ")").Single();
            Assert.Equal(query, expected);

            var scalar = connection.ExecuteScalar<T>("select cast(1 as " + dbType + ")");
            Assert.Equal(scalar, expected);
        }

        [Fact]
        public void TestSubsequentQueriesSuccess()
        {
            var data0 = connection.Query<Fooz0>("select 1 as [Id] where 1 = 0").ToList();
            Assert.Empty(data0);

            var data1 = connection.Query<Fooz1>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.Buffered)).ToList();
            Assert.Empty(data1);

            var data2 = connection.Query<Fooz2>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.None)).ToList();
            Assert.Empty(data2);

            data0 = connection.Query<Fooz0>("select 1 as [Id] where 1 = 0").ToList();
            Assert.Empty(data0);

            data1 = connection.Query<Fooz1>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.Buffered)).ToList();
            Assert.Empty(data1);

            data2 = connection.Query<Fooz2>(new CommandDefinition("select 1 as [Id] where 1 = 0", flags: CommandFlags.None)).ToList();
            Assert.Empty(data2);
        }

        private class Fooz0
        {
            public int Id { get; set; }
        }

        private class Fooz1
        {
            public int Id { get; set; }
        }

        private class Fooz2
        {
            public int Id { get; set; }
        }

        public class RatingValueHandler : SqlMapper.TypeHandler<RatingValue>
        {
            private RatingValueHandler()
            {
            }

            public static readonly RatingValueHandler Default = new RatingValueHandler();

            public override RatingValue Parse(object value)
            {
                if (value is int)
                {
                    return new RatingValue() { Value = (int)value };
                }

                throw new FormatException("Invalid conversion to RatingValue");
            }

            public override void SetValue(IDbDataParameter parameter, RatingValue value)
            {
                // ... null, range checks etc ...
                parameter.DbType = System.Data.DbType.Int32;
                parameter.Value = value.Value;
            }
        }

        public class RatingValue
        {
            public int Value { get; set; }
            // ... some other properties etc ...
        }

        public class MyResult
        {
            public string CategoryName { get; set; }
            public RatingValue CategoryRating { get; set; }
        }

        [Fact]
        public void SO24740733_TestCustomValueHandler()
        {
            SqlMapper.AddTypeHandler(RatingValueHandler.Default);
            var foo = connection.Query<MyResult>("SELECT 'Foo' AS CategoryName, 200 AS CategoryRating").Single();

            Assert.Equal("Foo", foo.CategoryName);
            Assert.Equal(200, foo.CategoryRating.Value);
        }

        [Fact]
        public void SO24740733_TestCustomValueSingleColumn()
        {
            SqlMapper.AddTypeHandler(RatingValueHandler.Default);
            var foo = connection.Query<RatingValue>("SELECT 200 AS CategoryRating").Single();

            Assert.Equal(200, foo.Value);
        }

        public class StringListTypeHandler : SqlMapper.TypeHandler<List<string>>
        {
            private StringListTypeHandler()
            {
            }

            public static readonly StringListTypeHandler Default = new StringListTypeHandler();
            //Just a simple List<string> type handler implementation
            public override void SetValue(IDbDataParameter parameter, List<string> value)
            {
                parameter.Value = string.Join(",", value);
            }

            public override List<string> Parse(object value)
            {
                return ((value as string) ?? "").Split(',').ToList();
            }
        }

        public class MyObjectWithStringList
        {
            public List<string> Names { get; set; }
        }

        [Fact]
        public void Issue253_TestIEnumerableTypeHandlerParsing()
        {
            SqlMapper.ResetTypeHandlers();
            SqlMapper.AddTypeHandler(StringListTypeHandler.Default);
            var foo = connection.Query<MyObjectWithStringList>("SELECT 'Sam,Kyro' AS Names").Single();
            Assert.Equal(new[] { "Sam", "Kyro" }, foo.Names);
        }

        [Fact]
        public void Issue253_TestIEnumerableTypeHandlerSetParameterValue()
        {
            SqlMapper.ResetTypeHandlers();
            SqlMapper.AddTypeHandler(StringListTypeHandler.Default);

            connection.Execute("CREATE TABLE #Issue253 (Names VARCHAR(50) NOT NULL);");
            try
            {
                const string names = "Sam,Kyro";
                List<string> names_list = names.Split(',').ToList();
                var foo = connection.Query<string>("INSERT INTO #Issue253 (Names) VALUES (@Names); SELECT Names FROM #Issue253;", new { Names = names_list }).Single();
                Assert.Equal(foo, names);
            }
            finally
            {
                connection.Execute("DROP TABLE #Issue253;");
            }
        }

        public class RecordingTypeHandler<T> : SqlMapper.TypeHandler<T>
        {
            public override void SetValue(IDbDataParameter parameter, T value)
            {
                SetValueWasCalled = true;
                parameter.Value = value;
            }

            public override T Parse(object value)
            {
                ParseWasCalled = true;
                return (T)value;
            }

            public bool SetValueWasCalled { get; set; }
            public bool ParseWasCalled { get; set; }
        }

        [Fact]
        public void Test_RemoveTypeMap()
        {
            SqlMapper.ResetTypeHandlers();
            SqlMapper.RemoveTypeMap(typeof(DateTime));

            var dateTimeHandler = new RecordingTypeHandler<DateTime>();
            SqlMapper.AddTypeHandler(dateTimeHandler);

            connection.Execute("CREATE TABLE #Test_RemoveTypeMap (x datetime NOT NULL);");

            try
            {
                connection.Execute("INSERT INTO #Test_RemoveTypeMap VALUES (@Now)", new { DateTime.Now });
                connection.Query<DateTime>("SELECT * FROM #Test_RemoveTypeMap");

                Assert.True(dateTimeHandler.ParseWasCalled);
                Assert.True(dateTimeHandler.SetValueWasCalled);
            }
            finally
            {
                connection.Execute("DROP TABLE #Test_RemoveTypeMap");
                SqlMapper.AddTypeMap(typeof(DateTime), DbType.DateTime); // or an option to reset type map?
            }
        }

        [Fact]
        public void TestReaderWhenResultsChange()
        {
            try
            {
                connection.Execute("create table #ResultsChange (X int);create table #ResultsChange2 (Y int);insert #ResultsChange (X) values(1);insert #ResultsChange2 (Y) values(1);");

                var obj1 = connection.Query<ResultsChangeType>("select * from #ResultsChange").Single();
                Assert.Equal(1, obj1.X);
                Assert.Equal(0, obj1.Y);
                Assert.Equal(0, obj1.Z);

                var obj2 = connection.Query<ResultsChangeType>("select * from #ResultsChange rc inner join #ResultsChange2 rc2 on rc2.Y=rc.X").Single();
                Assert.Equal(1, obj2.X);
                Assert.Equal(1, obj2.Y);
                Assert.Equal(0, obj2.Z);

                connection.Execute("alter table #ResultsChange add Z int null");
                connection.Execute("update #ResultsChange set Z = 2");

                var obj3 = connection.Query<ResultsChangeType>("select * from #ResultsChange").Single();
                Assert.Equal(1, obj3.X);
                Assert.Equal(0, obj3.Y);
                Assert.Equal(2, obj3.Z);

                var obj4 = connection.Query<ResultsChangeType>("select * from #ResultsChange rc inner join #ResultsChange2 rc2 on rc2.Y=rc.X").Single();
                Assert.Equal(1, obj4.X);
                Assert.Equal(1, obj4.Y);
                Assert.Equal(2, obj4.Z);
            }
            finally
            {
                connection.Execute("drop table #ResultsChange;drop table #ResultsChange2;");
            }
        }

        private class ResultsChangeType
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
        }

        public class WrongTypes
        {
            public int A { get; set; }
            public double B { get; set; }
            public long C { get; set; }
            public bool D { get; set; }
        }

        [Fact]
        public void TestWrongTypes_WithRightTypes()
        {
            var item = connection.Query<WrongTypes>("select 1 as A, cast(2.0 as float) as B, cast(3 as bigint) as C, cast(1 as bit) as D").Single();
            Assert.Equal(1, item.A);
            Assert.Equal(2.0, item.B);
            Assert.Equal(3L, item.C);
            Assert.True(item.D);
        }

        [Fact]
        public void TestWrongTypes_WithWrongTypes()
        {
            var item = connection.Query<WrongTypes>("select cast(1.0 as float) as A, 2 as B, 3 as C, cast(1 as bigint) as D").Single();
            Assert.Equal(1, item.A);
            Assert.Equal(2.0, item.B);
            Assert.Equal(3L, item.C);
            Assert.True(item.D);
        }

        [Fact]
        public void TestTreatIntAsABool()
        {
            Assert.True(connection.Query<bool>("select CAST(1 AS BIT)").Single());
            Assert.True(connection.Query<bool>("select 1").Single());
        }

        [Fact]
        public void SO24607639_NullableBools()
        {
            var obj = connection.Query<HazBools>(
                @"declare @vals table (A bit null, B bit null, C bit null);
                insert @vals (A,B,C) values (1,0,null);
                select * from @vals").Single();
            Assert.NotNull(obj);
            Assert.True(obj.A.Value);
            Assert.False(obj.B.Value);
            Assert.Null(obj.C);
        }

        private class HazBools
        {
            public bool? A { get; set; }
            public bool? B { get; set; }
            public bool? C { get; set; }
        }

        [Fact]
        public void Issue130_IConvertible()
        {
            dynamic row = connection.Query("select 1 as [a], '2' as [b]").Single();
            int a = row.a;
            string b = row.b;
            Assert.Equal(1, a);
            Assert.Equal("2", b);

            row = connection.Query<dynamic>("select 3 as [a], '4' as [b]").Single();
            a = row.a;
            b = row.b;
            Assert.Equal(3, a);
            Assert.Equal("4", b);
        }

        [Fact]
        public void Issue149_TypeMismatch_SequentialAccess()
        {
            Guid guid = Guid.Parse("cf0ef7ac-b6fe-4e24-aeda-a2b45bb5654e");
            var ex = Assert.ThrowsAny<Exception>(() => connection.Query<Issue149_Person>("select @guid as Id", new { guid }).First());
            Assert.Equal("Error parsing column 0 (Id=cf0ef7ac-b6fe-4e24-aeda-a2b45bb5654e - Object)", ex.Message);
        }

        public class Issue149_Person { public string Id { get; set; } }

        [Fact]
        public void Issue295_NullableDateTime_SqlServer() => Common.TestDateTime(connection);

        [Fact]
        public void SO29343103_UtcDates()
        {
            const string sql = "select @date";
            var date = DateTime.UtcNow;
            var returned = connection.Query<DateTime>(sql, new { date }).Single();
            var delta = returned - date;
            Assert.True(delta.TotalMilliseconds >= -10 && delta.TotalMilliseconds <= 10);
        }

        [Fact]
        public void Issue461_TypeHandlerWorksInConstructor()
        {
            SqlMapper.AddTypeHandler(new Issue461_BlargHandler());

            connection.Execute(@"CREATE TABLE #Issue461 (
                                      Id                int not null IDENTITY(1,1),
                                      SomeValue         nvarchar(50),
                                      SomeBlargValue    nvarchar(200),
                                    )");
            const string Expected = "abc123def";
            var blarg = new Blarg(Expected);
            connection.Execute(
                "INSERT INTO #Issue461 (SomeValue, SomeBlargValue) VALUES (@value, @blarg)",
                new { value = "what up?", blarg });

            // test: without constructor
            var parameterlessWorks = connection.QuerySingle<Issue461_ParameterlessTypeConstructor>("SELECT * FROM #Issue461");
            Assert.Equal(1, parameterlessWorks.Id);
            Assert.Equal("what up?", parameterlessWorks.SomeValue);
            Assert.Equal(parameterlessWorks.SomeBlargValue.Value, Expected);

            // test: via constructor
            var parameterDoesNot = connection.QuerySingle<Issue461_ParameterisedTypeConstructor>("SELECT * FROM #Issue461");
            Assert.Equal(1, parameterDoesNot.Id);
            Assert.Equal("what up?", parameterDoesNot.SomeValue);
            Assert.Equal(parameterDoesNot.SomeBlargValue.Value, Expected);
        }

        // I would usually expect this to be a struct; using a class
        // so that we can't pass unexpectedly due to forcing an unsafe cast - want
        // to see an InvalidCastException if it is wrong
        private class Blarg
        {
            public Blarg(string value) { Value = value; }
            public string Value { get; }
            public override string ToString()
            {
                return Value;
            }
        }

        private class Issue461_BlargHandler : SqlMapper.TypeHandler<Blarg>
        {
            public override void SetValue(IDbDataParameter parameter, Blarg value)
            {
                parameter.Value = ((object)value.Value) ?? DBNull.Value;
            }

            public override Blarg Parse(object value)
            {
                string s = (value == null || value is DBNull) ? null : Convert.ToString(value);
                return new Blarg(s);
            }
        }

        private class Issue461_ParameterlessTypeConstructor
        {
            public int Id { get; set; }

            public string SomeValue { get; set; }
            public Blarg SomeBlargValue { get; set; }
        }

        private class Issue461_ParameterisedTypeConstructor
        {
            public Issue461_ParameterisedTypeConstructor(int id, string someValue, Blarg someBlargValue)
            {
                Id = id;
                SomeValue = someValue;
                SomeBlargValue = someBlargValue;
            }

            public int Id { get; }

            public string SomeValue { get; }
            public Blarg SomeBlargValue { get; }
        }
    }
}
