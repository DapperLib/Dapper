using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace Dapper.Tests
{
    public partial class TestSuite
    {
        [Fact]
        public void CommonXmlTypesSupported()
        {
            var xml = new XmlDocument();
            xml.LoadXml("<abc/>");
            
            var foo = new Foo
            {
                A = xml,
                B = XDocument.Parse("<def/>"),
                C = XElement.Parse("<ghi/>") 
            };
            var bar = connection.QuerySingle<Foo>("select @a as [A], @b as [B], @c as [C]", new { a = foo.A, b = foo.B, c = foo.C });
            bar.A.DocumentElement.Name.IsEqualTo("abc");
            bar.B.Root.Name.LocalName.IsEqualTo("def");
            bar.C.Name.LocalName.IsEqualTo("ghi");
        }

        public class Foo
        {
            public XmlDocument A { get; set; }
            public XDocument B { get; set; }
            public XElement C { get; set; }
        }
    }
}
