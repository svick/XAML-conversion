using System.CodeDom;
using System.Xml.Linq;
using System.Collections;
using System;

namespace XamlConversion.Parsers
{
    class PropertyCollectionParser : PropertyParser
    {
        public PropertyCollectionParser(XamlConvertor.State state, ObjectParser parent)
            : base(state, parent)
        { }

        protected override void ParseName(XName name)
        {
            Name = name.LocalName.Split('.')[1];
            Type = GetPropertyType(Name, Parent.Type);
        }

        protected override void ParseElement(XElement element)
        {
            var objectParser = new ObjectParser(State);
            objectParser.Parse(element);

            CodeMethodInvokeExpression addExpression;
            if (typeof(IDictionary).IsAssignableFrom(Type))
            {
                string stringKey = GetKeyAttribute(element);
                CodeExpression key;
                if (stringKey != "")
                {
                    key = new CodePrimitiveExpression(stringKey);
                }
                else
                {
                    key = new CodeVariableReferenceExpression(objectParser.VariableName);
                }

                addExpression = new CodeMethodInvokeExpression(
                    new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(Parent.VariableName), Name),
                    "Add", new CodeExpression[] { key, new CodeVariableReferenceExpression(objectParser.VariableName) });

            }
            else
            {
                addExpression = new CodeMethodInvokeExpression(
                    new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(Parent.VariableName), Name),
                    "Add", new CodeVariableReferenceExpression(objectParser.VariableName));
            }


            State.AddStatement(new CodeExpressionStatement(addExpression));
        }

        private string GetKeyAttribute(XElement element)
        {
            foreach (var a in element.Attributes())
            {
                if (a.Name.LocalName == "Key") return a.Value;
            }

            return "";
        }

        protected override void ParseEnd()
        { }
    }
}
