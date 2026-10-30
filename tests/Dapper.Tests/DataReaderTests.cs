using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    [Collection("DataReaderTests")]
    public sealed class SystemSqlClientDataReaderTests : DataReaderTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection("DataReaderTests")]
    public sealed class MicrosoftSqlClientDataReaderTests : DataReaderTests<MicrosoftSqlClientProvider> { }
#endif

    public abstract class DataReaderTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    {
        [Fact]
        public void GetSameReaderForSameShape_IDataReader()
        {
            var origReader = connection.ExecuteReader("select 'abc' as Name, 123 as Id");
#pragma warning disable CS0618 // Type or member is obsolete
            var origParser = origReader.GetRowParser(typeof(HazNameId));

            var typedParser = origReader.GetRowParser<HazNameId>();
#pragma warning restore CS0618 // Type or member is obsolete

            // because wrapped for IDataReader, not same instance each time
            Assert.False(ReferenceEquals(origParser, typedParser));

            var list = origReader.Parse<HazNameId>().ToList();
            Assert.Single(list);
            Assert.Equal("abc", list[0].Name);
            Assert.Equal(123, list[0].Id);
            origReader.Dispose();

            var secondReader = connection.ExecuteReader("select 'abc' as Name, 123 as Id");
#pragma warning disable CS0618 // Type or member is obsolete
            var secondParser = secondReader.GetRowParser(typeof(HazNameId));
            var thirdParser = secondReader.GetRowParser(typeof(HazNameId), 1);
#pragma warning restore CS0618 // Type or member is obsolete

            list = secondReader.Parse<HazNameId>().ToList();
            Assert.Single(list);
            Assert.Equal("abc", list[0].Name);
            Assert.Equal(123, list[0].Id);
            secondReader.Dispose();

            // now: should be different readers, and because wrapped for IDataReader, not same parser
            Assert.False(ReferenceEquals(origReader, secondReader));
            Assert.False(ReferenceEquals(origParser, secondParser));
            Assert.False(ReferenceEquals(secondParser, thirdParser));
        }

        [Fact]
        public void GetSameReaderForSameShape_DbDataReader()
        {
            var origReader = Assert.IsAssignableFrom<DbDataReader>(connection.ExecuteReader("select 'abc' as Name, 123 as Id"));
            var origParser = origReader.GetRowParser(typeof(HazNameId));

            var typedParser = origReader.GetRowParser<HazNameId>();

            Assert.True(ReferenceEquals(origParser, typedParser));

            var list = origReader.Parse<HazNameId>().ToList();
            Assert.Single(list);
            Assert.Equal("abc", list[0].Name);
            Assert.Equal(123, list[0].Id);
            origReader.Dispose();

            var secondReader = Assert.IsAssignableFrom<DbDataReader>(connection.ExecuteReader("select 'abc' as Name, 123 as Id"));
            var secondParser = secondReader.GetRowParser(typeof(HazNameId));
            var thirdParser = secondReader.GetRowParser(typeof(HazNameId), 1);

            list = secondReader.Parse<HazNameId>().ToList();
            Assert.Single(list);
            Assert.Equal("abc", list[0].Name);
            Assert.Equal(123, list[0].Id);
            secondReader.Dispose();

            // now: should be different readers, but same parser
            Assert.False(ReferenceEquals(origReader, secondReader));
            Assert.True(ReferenceEquals(origParser, secondParser));
            Assert.False(ReferenceEquals(secondParser, thirdParser));
        }

        [Fact]
        public void TestTreatIntAsABool()
        {
            // Test we are consistent with direct call to database, see TypeHandlerTests.TestTreatIntAsABool
            using(var reader = connection.ExecuteReader("select CAST(1 AS BIT)"))
                Assert.True(SqlMapper.Parse<bool>(reader).Single());
            using (var reader = connection.ExecuteReader("select 1"))
                Assert.True(SqlMapper.Parse<bool>(reader).Single());
        }

        [Fact]
        public void DiscriminatedUnion_IDataReader()
        {
            var result = new List<Discriminated_BaseType>();
            using (var reader = connection.ExecuteReader(@"
select 'abc' as Name, 1 as Type, 3.0 as Value
union all
select 'def' as Name, 2 as Type, 4.0 as Value"))
            {
                if (reader.Read())
                {
#pragma warning disable CS0618
                    var toFoo = reader.GetRowParser<Discriminated_BaseType>(typeof(Discriminated_Foo));
                    var toBar = reader.GetRowParser<Discriminated_BaseType>(typeof(Discriminated_Bar));
#pragma warning restore CS0618

                    var col = reader.GetOrdinal("Type");
                    do
                    {
                        switch (reader.GetInt32(col))
                        {
                            case 1:
                                result.Add(toFoo(reader));
                                break;
                            case 2:
                                result.Add(toBar(reader));
                                break;
                        }
                    } while (reader.Read());
                }
            }

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Type);
            Assert.Equal(2, result[1].Type);
            var foo = (Discriminated_Foo)result[0];
            Assert.Equal("abc", foo.Name);
            var bar = (Discriminated_Bar)result[1];
            Assert.Equal((float)4.0, bar.Value);
        }

        [Fact]
        public void DiscriminatedUnion_DbDataReader()
        {
            var result = new List<Discriminated_BaseType>();
            using (var reader = Assert.IsAssignableFrom<DbDataReader>(connection.ExecuteReader(@"
select 'abc' as Name, 1 as Type, 3.0 as Value
union all
select 'def' as Name, 2 as Type, 4.0 as Value")))
            {
                if (reader.Read())
                {
                    var toFoo = reader.GetRowParser<Discriminated_BaseType>(typeof(Discriminated_Foo));
                    var toBar = reader.GetRowParser<Discriminated_BaseType>(typeof(Discriminated_Bar));

                    var col = reader.GetOrdinal("Type");
                    do
                    {
                        switch (reader.GetInt32(col))
                        {
                            case 1:
                                result.Add(toFoo(reader));
                                break;
                            case 2:
                                result.Add(toBar(reader));
                                break;
                        }
                    } while (reader.Read());
                }
            }

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Type);
            Assert.Equal(2, result[1].Type);
            var foo = (Discriminated_Foo)result[0];
            Assert.Equal("abc", foo.Name);
            var bar = (Discriminated_Bar)result[1];
            Assert.Equal((float)4.0, bar.Value);
        }

        [Fact]
        public void DiscriminatedUnionWithMultiMapping_IDataReader()
        {
            var result = new List<DiscriminatedWithMultiMapping_BaseType>();
            using (var reader = connection.ExecuteReader(@"
select 'abc' as Name, 1 as Type, 3.0 as Value, 1 as Id, 'zxc' as Name
union all
select 'def' as Name, 2 as Type, 4.0 as Value, 2 as Id, 'qwe' as Name"))
            {
                if (reader.Read())
                {
                    var col = reader.GetOrdinal("Type");
                    var splitOn = reader.GetOrdinal("Id");

#pragma warning disable CS0618
                    var toFoo = reader.GetRowParser<DiscriminatedWithMultiMapping_BaseType>(typeof(DiscriminatedWithMultiMapping_Foo), 0, splitOn);
                    var toBar = reader.GetRowParser<DiscriminatedWithMultiMapping_BaseType>(typeof(DiscriminatedWithMultiMapping_Bar), 0, splitOn);
                    var toHaz = reader.GetRowParser<HazNameId>(typeof(HazNameId), splitOn, reader.FieldCount - splitOn);
#pragma warning restore CS0618

                    do
                    {
                        DiscriminatedWithMultiMapping_BaseType? obj = null;
                        switch (reader.GetInt32(col))
                        {
                            case 1:
                                obj = toFoo(reader);
                                break;
                            case 2:
                                obj = toBar(reader);
                                break;
                        }

                        Assert.NotNull(obj);
                        obj!.HazNameIdObject = toHaz(reader);
                        result.Add(obj);

                    } while (reader.Read());
                }
            }

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Type);
            Assert.Equal(2, result[1].Type);
            var foo = (DiscriminatedWithMultiMapping_Foo)result[0];
            Assert.Equal("abc", foo.Name);
            Assert.NotNull(foo.HazNameIdObject);
            Assert.Equal(1, foo.HazNameIdObject.Id);
            Assert.Equal("zxc", foo.HazNameIdObject!.Name);
            var bar = (DiscriminatedWithMultiMapping_Bar)result[1];
            Assert.Equal((float)4.0, bar.Value);
            Assert.NotNull(bar.HazNameIdObject);
            Assert.Equal(2, bar.HazNameIdObject.Id);
            Assert.Equal("qwe", bar.HazNameIdObject.Name);
        }

        [Fact]
        public void DiscriminatedUnionWithMultiMapping_DbDataReader()
        {
            var result = new List<DiscriminatedWithMultiMapping_BaseType>();
            using (var reader = Assert.IsAssignableFrom<DbDataReader>(connection.ExecuteReader(@"
select 'abc' as Name, 1 as Type, 3.0 as Value, 1 as Id, 'zxc' as Name
union all
select 'def' as Name, 2 as Type, 4.0 as Value, 2 as Id, 'qwe' as Name")))
            {
                if (reader.Read())
                {
                    var col = reader.GetOrdinal("Type");
                    var splitOn = reader.GetOrdinal("Id");

                    var toFoo = reader.GetRowParser<DiscriminatedWithMultiMapping_BaseType>(typeof(DiscriminatedWithMultiMapping_Foo), 0, splitOn);
                    var toBar = reader.GetRowParser<DiscriminatedWithMultiMapping_BaseType>(typeof(DiscriminatedWithMultiMapping_Bar), 0, splitOn);
                    var toHaz = reader.GetRowParser<HazNameId>(typeof(HazNameId), splitOn, reader.FieldCount - splitOn);

                    do
                    {
                        DiscriminatedWithMultiMapping_BaseType? obj = null;
                        switch (reader.GetInt32(col))
                        {
                            case 1:
                                obj = toFoo(reader);
                                break;
                            case 2:
                                obj = toBar(reader);
                                break;
                        }

                        Assert.NotNull(obj);
                        obj.HazNameIdObject = toHaz(reader);
                        result.Add(obj);

                    } while (reader.Read());
                }
            }

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Type);
            Assert.Equal(2, result[1].Type);
            var foo = (DiscriminatedWithMultiMapping_Foo)result[0];
            Assert.Equal("abc", foo.Name);
            Assert.NotNull(foo.HazNameIdObject);
            Assert.Equal(1, foo.HazNameIdObject.Id);
            Assert.Equal("zxc", foo.HazNameIdObject.Name);
            var bar = (DiscriminatedWithMultiMapping_Bar)result[1];
            Assert.Equal((float)4.0, bar.Value);
            Assert.NotNull(bar.HazNameIdObject);
            Assert.Equal(2, bar.HazNameIdObject.Id);
            Assert.Equal("qwe", bar.HazNameIdObject.Name);
        }

        [Fact]
        public void GetFieldValue_ErrorMessageShowsTheValue()
        {
            // the message names the value that was read whichever reader method read it; on the
            // GetFieldValue<T> path the value is kept unboxed and boxed only when its column fails
            SqlMapper.AddTypeMap(typeof(long), DbType.Int64, useGetFieldValue: true);
            SqlMapper.AddTypeMap(typeof(int), DbType.Int32, useGetFieldValue: true);
            try
            {
                var ex = Assert.Throws<DataException>(() => connection.Query<HazThrowingSetter>("select cast(7 as bigint) as Id, 42 as Value -- GetFieldValue"));
                Assert.Equal("Error parsing column 1 (Value=42 - Int32)", ex.Message);
                Assert.IsType<InvalidOperationException>(ex.InnerException);

                ex = Assert.Throws<DataException>(() => connection.Query<HazThrowingNullableSetter>("select cast(7 as bigint) as Id, 42 as Value -- GetFieldValue"));
                Assert.Equal("Error parsing column 1 (Value=42 - Int32)", ex.Message);
                Assert.IsType<InvalidOperationException>(ex.InnerException);
            }
            finally
            {
                SqlMapper.AddTypeMap(typeof(long), DbType.Int64, useGetFieldValue: false);
                SqlMapper.AddTypeMap(typeof(int), DbType.Int32, useGetFieldValue: false);
            }

            // the same message as the GetValue path gives; a separate type, because deserializers are cached
            // per type and column shape, so the same type would reuse the deserializer compiled above
            var viaGetValue = Assert.Throws<DataException>(() => connection.Query<HazThrowingSetterViaGetValue>("select cast(7 as bigint) as Id, 42 as Value -- GetValue"));
            Assert.Equal("Error parsing column 1 (Value=42 - Int32)", viaGetValue.Message);
        }

        [Fact]
        public void GetFieldValue_ErrorMessageWhenTheReadItselfFails()
        {
            // when GetFieldValue<T> throws there is no value to show, and the message must not show the previous column's
            SqlMapper.AddTypeMap(typeof(long), DbType.Int64, useGetFieldValue: true);
            SqlMapper.AddTypeMap(typeof(int), DbType.Int32, useGetFieldValue: true);
            try
            {
                var ex = Assert.Throws<DataException>(() => connection.Query<HazThrowingSetter>("select cast(7 as bigint) as Id, 'abc' as Value -- GetFieldValue"));
                Assert.StartsWith("Error parsing column 1 (Value=n/a - ", ex.Message);
                Assert.NotNull(ex.InnerException);
            }
            finally
            {
                SqlMapper.AddTypeMap(typeof(long), DbType.Int64, useGetFieldValue: false);
                SqlMapper.AddTypeMap(typeof(int), DbType.Int32, useGetFieldValue: false);
            }
        }

        private class HazThrowingSetter
        {
            public long Id { get; set; }
            public int Value { get => 0; set => throw new InvalidOperationException("Value cannot be set"); }
        }

        private class HazThrowingNullableSetter
        {
            public long Id { get; set; }
            public int? Value { get => null; set => throw new InvalidOperationException("Value cannot be set"); }
        }

        private class HazThrowingSetterViaGetValue
        {
            public long Id { get; set; }
            public int Value { get => 0; set => throw new InvalidOperationException("Value cannot be set"); }
        }

        private abstract class Discriminated_BaseType
        {
            public abstract int Type { get; }
        }

        private class Discriminated_Foo : Discriminated_BaseType
        {
            public string? Name { get; set; }
            public override int Type => 1;
        }

        private class Discriminated_Bar : Discriminated_BaseType
        {
            public float Value { get; set; }
            public override int Type => 2;
        }

        private abstract class DiscriminatedWithMultiMapping_BaseType : Discriminated_BaseType
        {
            public abstract HazNameId? HazNameIdObject { get; set; }
        }

        private class DiscriminatedWithMultiMapping_Foo : DiscriminatedWithMultiMapping_BaseType
        {
            public override HazNameId? HazNameIdObject { get; set; }
            public string? Name { get; set; }
            public override int Type => 1;
        }

        private class DiscriminatedWithMultiMapping_Bar : DiscriminatedWithMultiMapping_BaseType
        {
            public override HazNameId? HazNameIdObject { get; set; }
            public float Value { get; set; }
            public override int Type => 2;
        }
    }
}
