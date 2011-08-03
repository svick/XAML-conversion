using System;
using System.CodeDom;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xml.Linq;

namespace XamlConversion
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
            return XamlTypeMapper.DefaultMapper.GetType(ns, xName.LocalName);
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

            var conversion = new CodeCastExpression(
                type.Name,
                new CodeMethodInvokeExpression(
                    new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("TypeDescriptor"), "GetConverter",
                                                   new CodeTypeOfExpression(type.Name)), "ConvertFromInvariantString",
                    new CodePrimitiveExpression(value)));

            return conversion;
        }

        protected void SetProperty(string variableName, Type variableType, string propertyName, string value)
        {
            var left = new CodePropertyReferenceExpression(
                new CodeVariableReferenceExpression(variableName), propertyName);
            var right = ConvertTo(value, GetPropertyType(propertyName, variableType));
            var assignment = new CodeAssignStatement(left, right);
            State.AddStatement(assignment);
        }
    }

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

    class RootObjectParser : ObjectParser
    {
        public RootObjectParser(XamlConvertor.State state)
            : base(state)
        {}

        protected override void ParseEnd()
        {
            var returnStatement = new CodeMethodReturnStatement(new CodeVariableReferenceExpression(VariableName));
            State.AddStatement(returnStatement);
            State.SetReturnType(Type);
        }
    }

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

    class BindingParser : ParserBase
    {
        public BindingParser(XamlConvertor.State state)
            : base(state)
        {}

        static readonly Regex BindingRegex = new Regex(@"\{([\w]+)(\s+\w+=\w+)*\}");

        static readonly Regex BindingPropertyRegex = new Regex(@"(\w+)=(\w+)");

        public string Parse(string text)
        {
            var match = BindingRegex.Match(text);

            if (!match.Success)
                throw new InvalidOperationException();

            var type = GetTypeFromXName(match.Groups[1].Value);

            string variableName = CreateObject(type, type.Name);
            foreach (Capture capture in match.Groups[2].Captures)
            {
                var propertyMatch = BindingPropertyRegex.Match(capture.Value);
                if (!propertyMatch.Success)
                    throw new InvalidOperationException();
                SetProperty(variableName, type, propertyMatch.Groups[1].Value, propertyMatch.Groups[2].Value);
            }

            return variableName;
        }
    }
}