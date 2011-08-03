using System.Xml.Linq;

namespace XamlConversion.Parsers
{
    abstract class XamlParser : ParserBase
    {
        protected XamlParser(XamlConvertor.State state)
            : base(state)
        {}

        public void Parse(XElement element)
        {
            ParseName(element.Name);

            foreach (var attribute in element.Attributes())
                ParseAttribute(attribute);

            foreach (var childElement in element.Elements())
                ParseElement(childElement);

            ParseEnd();
        }

        protected abstract void ParseName(XName name);

        protected abstract void ParseAttribute(XAttribute attribute);

        protected abstract void ParseElement(XElement element);

        protected abstract void ParseEnd();
    }
}