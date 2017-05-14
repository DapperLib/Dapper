﻿using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    public partial class DataReaderTests : TestBase
    {
        [Fact]
        public void GetSameReaderForSameShape()
        {
            var origReader = connection.ExecuteReader("select 'abc' as Name, 123 as Id");
            var origParser = origReader.GetRowParser(typeof(HazNameId));

            var typedParser = origReader.GetRowParser<HazNameId>();

            ReferenceEquals(origParser, typedParser).IsEqualTo(true);

            var list = origReader.Parse<HazNameId>().ToList();
            list.Count.IsEqualTo(1);
            list[0].Name.IsEqualTo("abc");
            list[0].Id.IsEqualTo(123);
            origReader.Dispose();

            var secondReader = connection.ExecuteReader("select 'abc' as Name, 123 as Id");
            var secondParser = secondReader.GetRowParser(typeof(HazNameId));
            var thirdParser = secondReader.GetRowParser(typeof(HazNameId), 1);

            list = secondReader.Parse<HazNameId>().ToList();
            list.Count.IsEqualTo(1);
            list[0].Name.IsEqualTo("abc");
            list[0].Id.IsEqualTo(123);
            secondReader.Dispose();

            // now: should be different readers, but same parser
            ReferenceEquals(origReader, secondReader).IsEqualTo(false);
            ReferenceEquals(origParser, secondParser).IsEqualTo(true);
            ReferenceEquals(secondParser, thirdParser).IsEqualTo(false);
        }

        [Fact]
        public void DiscriminatedUnion()
        {
            List<Discriminated_BaseType> result = new List<Discriminated_BaseType>();
            using (var reader = connection.ExecuteReader(@"
select 'abc' as Name, 1 as Type, 3.0 as Value
union all
select 'def' as Name, 2 as Type, 4.0 as Value"))
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

            result.Count.IsEqualTo(2);
            result[0].Type.IsEqualTo(1);
            result[1].Type.IsEqualTo(2);
            var foo = (Discriminated_Foo)result[0];
            foo.Name.IsEqualTo("abc");
            var bar = (Discriminated_Bar)result[1];
            bar.Value.IsEqualTo((float)4.0);
        }

        [Fact]
        public void DiscriminatedUnionWithMultiMapping()
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

                    var toFoo = reader.GetRowParser<DiscriminatedWithMultiMapping_BaseType>(typeof(DiscriminatedWithMultiMapping_Foo),0, splitOn);
                    var toBar = reader.GetRowParser<DiscriminatedWithMultiMapping_BaseType>(typeof(DiscriminatedWithMultiMapping_Bar),0, splitOn);
                    var toHaz = reader.GetRowParser<HazNameId>(typeof(HazNameId),splitOn, reader.FieldCount - splitOn);

                    do
                    {
                        DiscriminatedWithMultiMapping_BaseType obj = null;
                        switch (reader.GetInt32(col))
                        {
                            case 1:
                                obj = toFoo(reader);
                            break;
                            case 2:
                                obj = toBar(reader);
                            break;
                        }

                        obj.IsNotNull();
                        obj.HazNameIdObject = toHaz(reader);
                        result.Add(obj);

                    } while (reader.Read());
                }
            }

            result.Count.IsEqualTo(2);
            result[0].Type.IsEqualTo(1);
            result[1].Type.IsEqualTo(2);
            var foo = (DiscriminatedWithMultiMapping_Foo)result[0];
            foo.Name.IsEqualTo("abc");
            foo.HazNameIdObject.Id.IsEqualTo(1);
            foo.HazNameIdObject.Name.IsEqualTo("zxc");
            var bar = (DiscriminatedWithMultiMapping_Bar)result[1];
            bar.Value.IsEqualTo((float)4.0);
            bar.HazNameIdObject.Id.IsEqualTo(2);
            bar.HazNameIdObject.Name.IsEqualTo("qwe");
        }

        private abstract class Discriminated_BaseType
        {
            public abstract int Type { get; }
        }

        private class Discriminated_Foo : Discriminated_BaseType
        {
            public string Name { get; set; }
            public override int Type {
                get { return 1; }
            }
        }

        private class Discriminated_Bar : Discriminated_BaseType
        {
            public float Value { get; set; }
            public override int Type
            {
                get { return 2; }
            }
        }

        private abstract class DiscriminatedWithMultiMapping_BaseType : Discriminated_BaseType
        {
            public abstract HazNameId HazNameIdObject { get; set; }
        }

        private class DiscriminatedWithMultiMapping_Foo : DiscriminatedWithMultiMapping_BaseType
        {
            public override HazNameId HazNameIdObject { get; set; }
            public string Name { get; set; }
            public override int Type
            {
                get { return 1; }
            }
        }

        private class DiscriminatedWithMultiMapping_Bar : DiscriminatedWithMultiMapping_BaseType
        {
            public override HazNameId HazNameIdObject { get; set; }
            public float Value { get; set; }
            public override int Type
            {
                get { return 2; }
            }
        }
    }
}