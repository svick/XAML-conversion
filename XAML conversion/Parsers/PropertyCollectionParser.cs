using System.CodeDom;
using System.Xml.Linq;

namespace XamlConversion.Parsers
{
    class PropertyCollectionParser : PropertyParser
    {
        public PropertyCollectionParser(XamlConvertor.State state, ObjectParser parent)
            : base(state, parent)
        {}

        protected override void ParseName(XName name)
        {
            Name = name.LocalName.Split('.')[1];
            Type = GetPropertyType(Name, Parent.Type);
        }

        protected override void ParseElement(XElement element)
        {
            var objectParser = new ObjectParser(State);
            objectParser.Parse(element);
            var addExpression =
                new CodeMethodInvokeExpression(
                    new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(Parent.VariableName), Name),
                    "Add", new CodeVariableReferenceExpression(objectParser.VariableName));
            State.AddStatement(new CodeExpressionStatement(addExpression));
        }

        protected override void ParseEnd()
        {}
    }
}