using System.Collections.Generic;
using Dapper.Tests.TestEntities;

namespace Dapper.Tests.TestCases
{
    public class EnumTestCases
    {
        public static IEnumerable<object[]> TestEnumParsingWithHandlerCases
        {
            get
            {
                yield return new object[] { "SELECT 1 AS Id, 'F' AS Type", new List<Item>() { new Item(1, ItemType.Foo) } };
                yield return new object[] { "SELECT 22 AS Id, 'B' AS Type", new List<Item>() { new Item(22, ItemType.Bar) } };
            }
        }
    }
}
