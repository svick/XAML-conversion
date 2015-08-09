using System;
using System.CodeDom;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Xaml.Schema;
using System.Xml.Linq;

namespace XamlConversion.Parsers
{
    abstract class ParserBase
    {
        protected XamlConvertor.State State { get; set; }

        protected ParserBase(XamlConvertor.State state)
        {
            State = state;
        }

        protected string CreateObject(Type type, string proposedName)
        {
            var variableName = State.GetVariableName(proposedName);
            var variableDeclaration = new CodeVariableDeclarationStatement(
                type.Name, variableName, new CodeObjectCreateExpression(type.Name));
            State.AddStatement(variableDeclaration);
            return variableName;
        }

        protected static Type GetTypeFromXName(XName xName)
        {
            string ns = xName.Namespace.NamespaceName;
            if (string.IsNullOrEmpty(ns))
                ns = "http://schemas.microsoft.com/netfx/2007/xaml/presentation";
            var xamlSchemaContext = new XamlSchemaContextWithDefault();
            return xamlSchemaContext.GetXamlType(new XamlTypeName(ns, xName.LocalName)).UnderlyingType;
        }

        protected static Type GetPropertyType(string name, Type type)
        {
            return type.GetProperty(name).PropertyType;
        }

        protected CodeExpression ConvertTo(string value, Type type)
        {
            var valueExpression = new CodePrimitiveExpression(value);

            var converter = TypeDescriptor.GetConverter(type);

            if (type == typeof(string) || type == typeof(object))
                return valueExpression;

            if (type == typeof(double))
                return new CodePrimitiveExpression(double.Parse(value, CultureInfo.InvariantCulture));

            if (type == typeof(BindingBase))
            {
                var bindingParser = new BindingParser(State);
                var bindingVariableName = bindingParser.Parse(value);
                return new CodeVariableReferenceExpression(bindingVariableName);
            }

            // there is no conversion availabe, the generated code won't compile, but there is nothing we can do about that
            if (converter == null)
                return valueExpression;

            if (converter.CanConvertFrom(typeof(String)))
            {
                var conversion = new CodeCastExpression(
                    type.Name,
                    new CodeMethodInvokeExpression(
                        new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("TypeDescriptor"), "GetConverter",
                            new CodeTypeOfExpression(type.Name)), "ConvertFromInvariantString",
                            new CodePrimitiveExpression(value)));

                return conversion;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected void SetProperty(string variableName, Type variableType, string propertyName, string value)
        {

            if (value.StartsWith("{"))
            {
                throw new NotImplementedException();
            }

            // is it a Attached property?
            if (propertyName.Contains("."))
            {
                var s = propertyName.Split('.');
                string staticObjName = s[0];
                CodeMethodInvokeExpression addExpression = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression(GetTypeFromXName(s[0])),
                    "Set" + s[1],
                    new CodeExpression[] 
                    { new CodeVariableReferenceExpression(variableName), 
                        new CodePrimitiveExpression(value)});

                State.AddStatement(new CodeExpressionStatement(addExpression));
            }
            else
            {
                var left = new CodePropertyReferenceExpression(
                    new CodeVariableReferenceExpression(variableName), propertyName);
                var right = ConvertTo(value, GetPropertyType(propertyName, variableType));
                var assignment = new CodeAssignStatement(left, right);
                State.AddStatement(assignment);
            }

        }
    }
}
