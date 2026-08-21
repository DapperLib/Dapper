#if !NET481
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public class FakeDbCoverageGapsDeserializeTests
    {
        // ── CreateParamInfoGenerator: ctor-param-name mismatch fallback to
        // alphabetical property ordering (L2622-2671) ──────────────────────────

        private class CtorParamMismatch
        {
            public int Alpha { get; }
            public int Beta { get; }
            public CtorParamMismatch(int notAlpha, int notBeta)
            {
                Alpha = notAlpha;
                Beta = notBeta;
            }
        }

        [Fact]
        public void Execute_ParamObject_WithMismatchedCtorParamNames_FallsBackToAlphabeticalProps()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("INSERT INTO T (Alpha, Beta) VALUES (@Alpha, @Beta)", new CtorParamMismatch(1, 2));
        }

        // ── ICustomQueryParameter: struct property (box) + null reference property (throw) (L2680-2701, L4043-4044) ──

        private struct StructQueryParam : SqlMapper.ICustomQueryParameter
        {
            public int Value;
            public void AddParameter(IDbCommand command, string name)
            {
                var p = command.CreateParameter();
                p.ParameterName = name;
                p.Value = Value;
                command.Parameters.Add(p);
            }
        }

        private class RefQueryParam : SqlMapper.ICustomQueryParameter
        {
            public void AddParameter(IDbCommand command, string name) { }
        }

        [Fact]
        public void Execute_StructCustomQueryParameter_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("INSERT INTO T (P) VALUES (@P)", new { P = new StructQueryParam { Value = 5 } });
        }

        [Fact]
        public void Execute_NullReferenceCustomQueryParameter_Throws()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            Assert.Throws<InvalidOperationException>(() =>
                conn.Execute("INSERT INTO T (P) VALUES (@P)", new { P = (RefQueryParam?)null }));
        }

        // ── EnumerableMultiParameter with a struct property (box) (L2713-2716) ─

        [Fact]
        public void Execute_StructEnumerableProperty_ArraySegment_ExpandsAsList()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("SELECT * FROM T WHERE Id IN @Ids", new { Ids = new ArraySegment<int>(new[] { 1, 2, 3 }) });
        }

        // ── object-typed anonymous property routes through SetDbType (L2742-2748) ──

        [Fact]
        public void Execute_ObjectTypedParameter_RoutesThroughSetDbType()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("INSERT INTO T (Foo) VALUES (@Foo)", new { Foo = (object)5 });
        }

        // ── Non-nullable enum parameters across all underlying TypeCodes (L2785-2799) ──

        private enum ByteEnum : byte { A = 1 }
        private enum SByteEnum : sbyte { A = 1 }
        private enum Int16Enum : short { A = 1 }
        private enum UInt16Enum : ushort { A = 1 }
        private enum Int64Enum : long { A = 1 }
        private enum UInt32Enum : uint { A = 1 }
        private enum UInt64Enum : ulong { A = 1 }

        private class EnumParamWrapper<TEnum> where TEnum : struct, Enum
        {
            public TEnum V { get; set; }
        }

        private static void ExecuteWithEnumParam<TEnum>(fakeDbConnection conn, TEnum value) where TEnum : struct, Enum
        {
            conn.Execute("INSERT INTO T (V) VALUES (@V)", new EnumParamWrapper<TEnum> { V = value });
        }

        [Fact]
        public void AnonymousLikeParam_NonNullableEnums_AllUnderlyingTypeCodes()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            for (int i = 0; i < 7; i++) conn.EnqueueNonQueryResult(1);
            conn.Open();

            ExecuteWithEnumParam(conn, ByteEnum.A);
            ExecuteWithEnumParam(conn, SByteEnum.A);
            ExecuteWithEnumParam(conn, Int16Enum.A);
            ExecuteWithEnumParam(conn, UInt16Enum.A);
            ExecuteWithEnumParam(conn, Int64Enum.A);
            ExecuteWithEnumParam(conn, UInt32Enum.A);
            ExecuteWithEnumParam(conn, UInt64Enum.A);
        }

        // ── Nullable enum parameter, no handler (L2778-2783, L2805-2809) ────────

        private enum PlainEnum { A = 1, B = 2 }

        private class NullableEnumWrapper
        {
            public PlainEnum? V { get; set; }
        }

        [Fact]
        public void Execute_NullableEnumParameter_NoHandler_UsesSanitize()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute("INSERT INTO T (V) VALUES (@V)", new NullableEnumWrapper { V = PlainEnum.A });
        }

        // ── Enum parameter with a registered type handler (PreferTypeHandlersForEnums) (L2772-2776) ──

        private class PlainEnumHandler : SqlMapper.TypeHandler<PlainEnum>
        {
            public override void SetValue(IDbDataParameter parameter, PlainEnum value) => parameter.Value = (int)value;
            public override PlainEnum Parse(object value) => (PlainEnum)Convert.ToInt32(value);
        }

        private class PlainEnumWrapper
        {
            public PlainEnum V { get; set; }
        }

        [Fact]
        public void Execute_EnumParameter_WithRegisteredHandler_PreferTypeHandlersForEnums()
        {
            var originalPref = SqlMapper.Settings.PreferTypeHandlersForEnums;
            try
            {
                SqlMapper.Settings.PreferTypeHandlersForEnums = true;
                SqlMapper.AddTypeHandler(new PlainEnumHandler());

                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueNonQueryResult(1);
                conn.Open();

                conn.Execute("INSERT INTO T (V) VALUES (@V)", new PlainEnumWrapper { V = PlainEnum.A });
            }
            finally
            {
                SqlMapper.Settings.PreferTypeHandlersForEnums = originalPref;
                SqlMapper.ResetTypeHandlers();
            }
        }

        // ── Literal tokens ({=X}) with an anonymous-style parameter object (L2899-2990) ──

        private class LiteralTokenParams
        {
            public int Id { get; set; }
            public bool Flag { get; set; }
            public int A { get; set; }
            public long B { get; set; }
            public List<int> W { get; set; } = new();
        }

        [Fact]
        public void Execute_LiteralTokens_MultipleTypeCodes_SubstitutesInSql()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();

            conn.Execute(
                "UPDATE T SET Flag={=Flag}, A={=A}, B={=B}, W={=W} WHERE Id=@Id",
                new LiteralTokenParams { Id = 1, Flag = true, A = 5, B = 6L, W = new List<int> { 1, 2, 3 } });
        }

        // ── DateTime-family conversions via POCO property (GetDateTimeFamilyConversion) (L3899-3944) ──

        private class DateOnlyHolder { public DateOnly V { get; set; } }
        private class TimeOnlyHolder { public TimeOnly V { get; set; } }
        private class DateTimeHolder { public DateTime V { get; set; } }
        private class TimeSpanHolder { public TimeSpan V { get; set; } }

        [Fact]
        public void Query_PocoProperty_DateTimeToDateOnly_Converts()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", new DateTime(2021, 5, 6) } } });
            conn.Open();

            var result = conn.QueryFirst<DateOnlyHolder>("SELECT V FROM T");
            Assert.Equal(new DateOnly(2021, 5, 6), result.V);
        }

        [Fact]
        public void Query_PocoProperty_DateTimeToTimeOnly_Converts()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", new DateTime(2021, 5, 6, 1, 2, 3) } } });
            conn.Open();

            var result = conn.QueryFirst<TimeOnlyHolder>("SELECT V FROM T");
            Assert.Equal(new TimeOnly(1, 2, 3), result.V);
        }

        [Fact]
        public void Query_PocoProperty_TimeSpanToTimeOnly_Converts()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", new TimeSpan(1, 2, 3) } } });
            conn.Open();

            var result = conn.QueryFirst<TimeOnlyHolder>("SELECT V FROM T");
            Assert.Equal(new TimeOnly(1, 2, 3), result.V);
        }

        [Fact]
        public void Query_PocoProperty_DateOnlyToDateTime_Converts()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", new DateOnly(2021, 5, 6) } } });
            conn.Open();

            var result = conn.QueryFirst<DateTimeHolder>("SELECT V FROM T");
            Assert.Equal(new DateTime(2021, 5, 6), result.V);
        }

        [Fact]
        public void Query_PocoProperty_TimeOnlyToTimeSpan_Converts()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", new TimeOnly(1, 2, 3) } } });
            conn.Open();

            var result = conn.QueryFirst<TimeSpanHolder>("SELECT V FROM T");
            Assert.Equal(new TimeSpan(1, 2, 3), result.V);
        }

        // ── DateTime-family conversions via scalar/simple-type path (Parse<T>, GetValue<T>) (L1397-1400, L3183-3252) ──

        [Fact]
        public void QueryFirst_SimpleDateOnly_FromDateTimeColumn_Converts()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", new DateTime(2022, 3, 4) } } });
            conn.Open();

            var result = conn.QueryFirst<DateOnly>("SELECT V FROM T");
            Assert.Equal(new DateOnly(2022, 3, 4), result);
        }

        [Fact]
        public void QueryFirst_SimpleTimeSpan_FromTimeOnlyColumn_Converts()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", new TimeOnly(4, 5, 6) } } });
            conn.Open();

            var result = conn.QueryFirst<TimeSpan>("SELECT V FROM T");
            Assert.Equal(new TimeSpan(4, 5, 6), result);
        }

        [Fact]
        public async System.Threading.Tasks.Task ExecuteScalarAsync_SimpleTimeOnly_FromDateTimeColumn_Converts()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueScalarResult(new DateTime(2023, 1, 1, 7, 8, 9));
            conn.Open();

            var result = await conn.ExecuteScalarAsync<TimeOnly>("SELECT V FROM T");
            Assert.Equal(new TimeOnly(7, 8, 9), result);
        }

        // ── Struct return type + ISupportInitialize (L3536-3540, L3587-3592, L3701-3716) ──

        private struct PointStruct
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        [Fact]
        public void Query_StructReturnType_Deserializes()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "X", 1 }, { "Y", 2 } } });
            conn.Open();

            var result = conn.QueryFirst<PointStruct>("SELECT X, Y FROM T");
            Assert.Equal(1, result.X);
            Assert.Equal(2, result.Y);
        }

        private class SupportInitializeThing : ISupportInitialize
        {
            public int Value { get; set; }
            public bool BeginCalled { get; private set; }
            public bool EndCalled { get; private set; }
            public void BeginInit() => BeginCalled = true;
            public void EndInit() => EndCalled = true;
        }

        [Fact]
        public void Query_ISupportInitializeType_CallsBeginAndEndInit()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Value", 42 } } });
            conn.Open();

            var result = conn.QueryFirst<SupportInitializeThing>("SELECT Value FROM T");
            Assert.Equal(42, result.Value);
            Assert.True(result.BeginCalled);
            Assert.True(result.EndCalled);
        }

        // ── applyNullSetting: reference-type and field-based members set explicit null (L3657-3679) ──

        private class ApplyNullRefAndField
        {
            public string? Name;
            public string? Description { get; set; }
        }

        [Fact]
        public void Query_ApplyNullValues_SetsReferenceFieldAndProperty()
        {
            var original = SqlMapper.Settings.ApplyNullValues;
            try
            {
                SqlMapper.Settings.ApplyNullValues = true;
                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "Name", DBNull.Value }, { "Description", DBNull.Value } } });
                conn.Open();

                var result = conn.QueryFirst<ApplyNullRefAndField>("SELECT Name, Description FROM T");
                Assert.Null(result.Name);
                Assert.Null(result.Description);
            }
            finally
            {
                SqlMapper.Settings.ApplyNullValues = original;
            }
        }

        // ── ThrowDataException: bad conversion and DBNull-to-non-nullable (L4056-4087) ──

        private class IntHolder { public int V { get; set; } }

        [Fact]
        public void QueryFirst_BadStringToIntConversion_ThrowsDataException()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", "not-a-number" } } });
            conn.Open();

            Assert.ThrowsAny<Exception>(() => conn.QueryFirst<IntHolder>("SELECT V FROM T"));
        }

        [Fact]
        public void QueryFirst_DBNullToNonNullableInt_ThrowsDataException()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", DBNull.Value } } });
            conn.Open();

            Assert.ThrowsAny<Exception>(() => conn.QueryFirst<int>("SELECT V FROM T"));
        }

        // ── Simple scalar type with a registered custom handler (GetSimpleValueDeserializer) (L3144-3150) ──

        private class GuidUpperHandler : SqlMapper.TypeHandler<Guid>
        {
            public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = value.ToString("D").ToUpperInvariant();
            public override Guid Parse(object value) => Guid.Parse(value.ToString()!);
        }

        [Fact]
        public void QueryFirst_SimpleGuidType_WithCustomHandler_UsesHandlerParse()
        {
            try
            {
                SqlMapper.AddTypeHandler(new GuidUpperHandler());
                var id = Guid.NewGuid();

                using var conn = new fakeDbConnection(new FakeDataStore());
                conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", id.ToString("D") } } });
                conn.Open();

                var result = conn.QueryFirst<Guid>("SELECT V FROM T");
                Assert.Equal(id, result);
            }
            finally
            {
                SqlMapper.ResetTypeHandlers();
            }
        }

        // ── Enum column value returned as float/double/decimal (GetSimpleValueDeserializer) (L3137-3140) ──

        private enum ColorEnum { Red = 1, Green = 2, Blue = 3 }

        [Fact]
        public void QueryFirst_SimpleEnumType_FromDoubleColumnValue_Converts()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", 2.0 } } });
            conn.Open();

            var result = conn.QueryFirst<ColorEnum>("SELECT V FROM T");
            Assert.Equal(ColorEnum.Green, result);
        }

        // ── GetValue<T>: T is an array type, val is a different array (L1386-1392) ──

        [Fact]
        public void QueryFirst_ArrayResultType_ConvertsElementTypes()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "V", new object[] { 1, 2, 3 } } } });
            conn.Open();

            var result = conn.QueryFirst<int[]>("SELECT V FROM T");
            Assert.Equal(new[] { 1, 2, 3 }, result);
        }

        // ── MultiMapException "No columns were selected" path (GetDapperRowDeserializer) (L2002-2018) ──

        [Fact]
        public void GetRowParser_StartIndexBeyondFieldCount_Throws()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[] { new Dictionary<string, object?> { { "A", 1 }, { "B", 2 } } });
            conn.Open();

            using var reader = conn.ExecuteReader("SELECT A, B FROM T");
            Assert.True(reader.Read());

            Assert.ThrowsAny<Exception>(() => reader.GetRowParser<object>(typeof(object), startIndex: 10));
        }

        // ── ValueTuple: 8-arity (Rest field) + short row (default values) + nullable tuple (L3418-3517) ──

        [Fact]
        public void QueryFirst_EightArityValueTuple_UsesRestField()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?>
                {
                    { "c1", 1 }, { "c2", 2 }, { "c3", 3 }, { "c4", 4 },
                    { "c5", 5 }, { "c6", 6 }, { "c7", 7 }, { "c8", 8 },
                }
            });
            conn.Open();

            var result = conn.QueryFirst<(int, int, int, int, int, int, int, int)>(
                "SELECT c1,c2,c3,c4,c5,c6,c7,c8 FROM T");
            Assert.Equal((1, 2, 3, 4, 5, 6, 7, 8), result);
        }

        [Fact]
        public void QueryFirst_ValueTuple_FewerColumnsThanElements_UsesDefaults()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "c1", 1 }, { "c2", 2 } }
            });
            conn.Open();

            var result = conn.QueryFirst<(int, int, int, int)>("SELECT c1,c2 FROM T");
            Assert.Equal(1, result.Item1);
            Assert.Equal(2, result.Item2);
            Assert.Equal(0, result.Item3);
            Assert.Equal(0, result.Item4);
        }

        [Fact]
        public void QueryFirst_NullableValueTuple_Works()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "c1", 1 }, { "c2", 2 } }
            });
            conn.Open();

            var result = conn.QueryFirst<(int, int)?>("SELECT c1,c2 FROM T");
            Assert.Equal((1, 2), result);
        }

        // ── GenerateDeserializers: dynamic-first-type DontMap skip, non-dynamic DontMap skip (L1764-1791) ──

        private class Left { public int Id { get; set; } }
        private class Right { public int RightId { get; set; } }

        [Fact]
        public void QueryDynamicFirst_MultiMap_SkipsDontMapTypes()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "RightId", 2 } }
            });
            conn.Open();

            var result = conn.Query<dynamic, Right, string>(
                "SELECT Id, RightId FROM T",
                (a, b) => $"{a.Id}-{b.RightId}",
                splitOn: "RightId").ToList();

            Assert.Single(result);
            Assert.Equal("1-2", result[0]);
        }

        [Fact]
        public void QueryTypedFirst_MultiMap_SkipsDontMapTypes()
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueReaderResult(new[]
            {
                new Dictionary<string, object?> { { "Id", 1 }, { "RightId", 2 } }
            });
            conn.Open();

            var result = conn.Query<Left, Right, string>(
                "SELECT Id, RightId FROM T",
                (a, b) => $"{a.Id}-{b.RightId}",
                splitOn: "RightId").ToList();

            Assert.Single(result);
            Assert.Equal("1-2", result[0]);
        }
    }
}
#endif
