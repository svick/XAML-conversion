using System;
using System.CodeDom;
using System.Xml.Linq;

namespace XamlConversion.Parsers
{
    class PropertyObjectParser : PropertyParser
    {
        private bool m_firstElement = true;

        public PropertyObjectParser(XamlConvertor.State state, ObjectParser parent)
            : base(state, parent)
        {}

        protected override void ParseName(XName name)
        {
            Name = name.LocalName.Split('.')[1];
            Type = GetPropertyType(Name, Parent.Type);
        }

        protected override void ParseElement(XElement element)
        {
            if (!m_firstElement)
                throw new InvalidOperationException();

            var objectParser = new ObjectParser(State);
            objectParser.Parse(element);

            var left = new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(Parent.VariableName), Name);
            var right = new CodeVariableReferenceExpression(objectParser.VariableName);
            var assignment = new CodeAssignStatement(left, right);
            State.AddStatement(assignment);
        }

        protected override void ParseEnd()
        {}
    }
}