using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Tests
{
    public partial class TestSuite
    {
        [Fact]
        public void GetSameReaderForSameShape()
        {
            var origReader = connection.ExecuteReader("select 'abc' as Name, 123 as Id");
            var origParser = origReader.GetRowParser(typeof(HazNameId));

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
        public class HazNameId
        {
            public string Name { get; set; }
            public int Id { get; set; }
        }
    }
}
