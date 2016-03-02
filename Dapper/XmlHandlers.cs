using System.Xml;
using System.Xml.Linq;

namespace Dapper
{
    internal sealed class XmlDocumentHandler : SqlMapper.StringTypeHandler<XmlDocument>
    {
        protected override XmlDocument Parse(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc;
        }
        protected override string Format(XmlDocument xml) => xml.OuterXml;
    }
    internal sealed class XDocumentHandler : SqlMapper.StringTypeHandler<XDocument>
    {
        protected override XDocument Parse(string xml) => XDocument.Parse(xml);
        protected override string Format(XDocument xml) => xml.ToString();
    }
    internal sealed class XElementHandler : SqlMapper.StringTypeHandler<XElement>
    {
        protected override XElement Parse(string xml) => XElement.Parse(xml);
        protected override string Format(XElement xml) => xml.ToString();
    }
}
