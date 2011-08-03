using System;
using System.Xml.Linq;

namespace XamlConversion.Parsers
{
    class ObjectParser : XamlParser
    {
        public ObjectParser(XamlConvertor.State state)
            : base(state)
        {}

        public string VariableName { get; protected set; }

        public Type Type { get; protected set; }

        protected override void ParseName(XName name)
        {
            Type = GetTypeFromXName(name);

            VariableName = CreateObject(Type, name.LocalName);
        }

        protected override void ParseAttribute(XAttribute attribute)
        {
            var propertyName = attribute.Name.LocalName;
            SetProperty(VariableName, Type, propertyName, attribute.Value);
        }

        protected override void ParseElement(XElement element)
        {
            // is it a property?
            if (element.Name.LocalName.Contains("."))
            {
                var propertyParser = new PropertyParser(State, this);
                propertyParser.Parse(element);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected override void ParseEnd()
        {}
    }
}