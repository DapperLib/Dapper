using System.Data;
using System.Xml;
using System.Xml.Linq;

namespace Dapper
{
    internal abstract class XmlTypeHandler<T> : SqlMapper.StringTypeHandler<T>
    {
        public override void SetValue(IDbDataParameter parameter, T value)
        {
            base.SetValue(parameter, value);
            parameter.DbType = DbType.Xml;
        }
    }
    internal sealed class XmlDocumentHandler : XmlTypeHandler<XmlDocument>
    {
        protected override XmlDocument Parse(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc;
        }
        protected override string Format(XmlDocument xml) => xml.OuterXml;
    }
    internal sealed class XDocumentHandler : XmlTypeHandler<XDocument>
    {
        protected override XDocument Parse(string xml) => XDocument.Parse(xml);
        protected override string Format(XDocument xml) => xml.ToString();
    }
    internal sealed class XElementHandler : XmlTypeHandler<XElement>
    {
        protected override XElement Parse(string xml) => XElement.Parse(xml);
        protected override string Format(XElement xml) => xml.ToString();
    }
}
