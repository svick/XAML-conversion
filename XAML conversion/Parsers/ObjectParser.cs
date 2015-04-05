using System;
using System.Xml.Linq;
using System.Windows.Markup;

namespace XamlConversion.Parsers
{
    class ObjectParser : XamlParser
    {
        public ObjectParser(XamlConvertor.State state)
            : base(state)
        { }

        public string VariableName { get; protected set; }

        public Type Type { get; protected set; }

        protected override void ParseName(XName name)
        {
            Type = GetTypeFromXName(name);

            VariableName = CreateObject(Type, name.LocalName);
        }

        protected override void ParseAttribute(XAttribute attribute)
        {
            if (attribute.IsNamespaceDeclaration)
                return;
            try
            {
                var propertyName = attribute.Name.LocalName;
                SetProperty(VariableName, Type, propertyName, attribute.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(attribute.Name);
            }

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
                var propertyParser = new PropertyParser(State, this);
                object[] attributes = Type.GetCustomAttributes(typeof(ContentPropertyAttribute), true);

                if (attributes.Length > 0)
                {
                    XElement e = new XElement(VariableName + "." +
                        (attributes[0] as ContentPropertyAttribute).Name, element);
                    propertyParser.Parse(e);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        protected override void ParseEnd()
        { }
    }
}
