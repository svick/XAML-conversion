using System;
using System.Collections;
using System.Xml.Linq;

namespace XamlConversion.Parsers
{
    class PropertyParser : XamlParser
    {
        public string Name { get; protected set; }

        public Type Type { get; protected set; }

        protected ObjectParser Parent { get; private set; }

        private PropertyParser m_child;

        public PropertyParser(XamlConvertor.State state, ObjectParser parent)
            : base(state)
        {
            Parent = parent;
        }

        protected override void ParseName(XName name)
        {
            Name = name.LocalName.Split('.')[1];
            Type = GetPropertyType(Name, Parent.Type);
            // is it a collection?
            if (typeof(IEnumerable).IsAssignableFrom(Type) && Type != typeof(string))
                m_child = new PropertyCollectionParser(State, Parent);
            else
                m_child = new PropertyObjectParser(State, Parent);
            m_child.ParseName(name);
        }

        protected override void ParseAttribute(XAttribute attribute)
        {
            throw new InvalidOperationException();
        }

        protected override void ParseElement(XElement element)
        {
            m_child.ParseElement(element);
        }

        protected override void ParseEnd()
        {
            m_child.ParseEnd();
        }
    }
}