using System.Collections.Specialized;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rudine.Util.Xsds
{
    internal class XsdParameters
    {
        internal const string xsdParametersSchema = "<?xml version='1.0' encoding='UTF-8' ?>\r\n<xs:schema xmlns:tns='http://microsoft.com/dotnet/tools/xsd/' elementFormDefault='qualified' targetNamespace='http://microsoft.com/dotnet/tools/xsd/' xmlns:xs='http://www.w3.org/2001/XMLSchema'>\r\n  <xs:simpleType name='options'>\r\n    <xs:list>\r\n      <xs:simpleType>\r\n        <xs:restriction base='xs:string'>\r\n          <xs:enumeration value='none' />\r\n          <xs:enumeration value='properties' />\r\n          <xs:enumeration value='order' />\r\n          <xs:enumeration value='enableDataBinding' />\r\n        </xs:restriction>\r\n      </xs:simpleType>\r\n    </xs:list>\r\n  </xs:simpleType>\r\n  \r\n  <xs:complexType name='generateObjectModel'>\r\n    <xs:sequence>\r\n      <xs:element name='schema' minOccurs='0' maxOccurs='unbounded' type='xs:string'/>\r\n    </xs:sequence>\r\n    <xs:attribute name='language' default='cs' type='xs:string'/>\r\n    <xs:attribute name='namespace' type='xs:string'/>\r\n  </xs:complexType>\r\n\r\n  <xs:complexType name='generateClasses'>\r\n    <xs:complexContent mixed='false'>\r\n      <xs:extension base='tns:generateObjectModel'>\r\n        <xs:sequence>\r\n          <xs:element name='element' minOccurs='0' maxOccurs='unbounded' type='xs:string'/>\r\n          <xs:element minOccurs='0' name='schemaImporterExtensions'>\r\n            <xs:complexType>\r\n              <xs:sequence>\r\n                <xs:element minOccurs='0' maxOccurs='unbounded' name='type' type='xs:string' />\r\n              </xs:sequence>\r\n            </xs:complexType>\r\n          </xs:element>\r\n        </xs:sequence>\r\n        <xs:attribute name='options' default='properties' type='tns:options'/>\r\n        <xs:attribute name='uri' type='xs:anyURI'/>\r\n      </xs:extension>\r\n    </xs:complexContent>\r\n  </xs:complexType>\r\n\r\n  <xs:complexType name='generateDataSet'>\r\n    <xs:complexContent mixed='false'>\r\n      <xs:extension base='tns:generateObjectModel'>\r\n        <xs:attribute name='enableLinqDataSet' default='false' type='xs:boolean'/>\r\n      </xs:extension>\r\n    </xs:complexContent>\r\n  </xs:complexType>\r\n\r\n  <xs:complexType name='generateSchemas'>\r\n    <xs:choice>\r\n      <xs:element name='xdr' type='xs:string' maxOccurs='unbounded' />\r\n      <xs:element name='xml' type='xs:string' maxOccurs='unbounded' />\r\n      <xs:choice>\r\n        <xs:element name='assembly' type='xs:string' maxOccurs='unbounded' />\r\n        <xs:element name='type' type='xs:string' maxOccurs='unbounded' />\r\n      </xs:choice>\r\n    </xs:choice>\r\n  </xs:complexType>\r\n\r\n  <xs:complexType name='xsdParameters'>\r\n    <xs:choice>\r\n      <xs:element name='generateClasses' type='tns:generateClasses'/>\r\n      <xs:element name='generateDataSet' type='tns:generateDataSet'/>\r\n      <xs:element name='generateSchemas' type='tns:generateSchemas'/>\r\n    </xs:choice>\r\n    <xs:attribute name='output' type='xs:string'/>\r\n    <xs:attribute name='nologo' type='xs:boolean'/>\r\n    <xs:attribute name='help' type='xs:boolean'/>\r\n  </xs:complexType>\r\n  \r\n  <xs:element name='xsd' type='tns:xsdParameters' />\r\n\r\n</xs:schema>";

        private bool nologo;

        private bool help;

        private bool classes;

        private bool dataset;

        private bool enableLinqDataSet;

        private string language;

        private string ns;

        private string outputdir;

        private CodeGenerationOptions options = CodeGenerationOptions.GenerateProperties;

        private bool optionsDefault = true;

        private string uri;

        private StringCollection xsdSchemas;

        private StringCollection xdrSchemas;

        private StringCollection instances;

        private StringCollection dlls;

        private StringCollection elements;

        private StringCollection types;

        private StringCollection schemaImporterExtensions;

        private static XmlSchema schema;

        internal bool Classes
        {
            get { return classes; }
            set { classes = value; }
        }

        internal bool Dataset
        {
            get { return dataset; }
            set { dataset = value; }
        }

        internal bool EnableLinqDataSet
        {
            get { return enableLinqDataSet; }
            set { enableLinqDataSet = value; }
        }

        internal bool Help
        {
            get { return help; }
            set { help = value; }
        }

        internal string Language
        {
            get
            {
                if (language != null)
                    return language;
                return "c#";
            }
            set { language = value; }
        }

        internal string Namespace
        {
            get
            {
                if (ns != null)
                    return ns;
                return string.Empty;
            }
            set { ns = value; }
        }

        internal bool Nologo
        {
            get { return nologo; }
            set { nologo = value; }
        }

        internal string OutputDir
        {
            get
            {
                if (outputdir != null)
                    return outputdir;
                return string.Empty;
            }
            set { outputdir = value; }
        }

        internal CodeGenerationOptions Options
        {
            get { return options; }
            set
            {
                options = value;
                optionsDefault = false;
            }
        }

        internal string Uri
        {
            get
            {
                if (uri != null)
                    return uri;
                return string.Empty;
            }
            set { uri = value; }
        }

        internal StringCollection XsdSchemas
        {
            get
            {
                if (xsdSchemas == null)
                    xsdSchemas = new StringCollection();
                return xsdSchemas;
            }
        }

        internal StringCollection XdrSchemas
        {
            get
            {
                if (xdrSchemas == null)
                    xdrSchemas = new StringCollection();
                return xdrSchemas;
            }
        }

        internal StringCollection Instances
        {
            get
            {
                if (instances == null)
                    instances = new StringCollection();
                return instances;
            }
        }

        internal StringCollection Assemblies
        {
            get
            {
                if (dlls == null)
                    dlls = new StringCollection();
                return dlls;
            }
        }

        internal StringCollection Elements
        {
            get
            {
                if (elements == null)
                    elements = new StringCollection();
                return elements;
            }
        }

        internal StringCollection Types
        {
            get
            {
                if (types == null)
                    types = new StringCollection();
                return types;
            }
        }

        internal StringCollection SchemaImporterExtensions
        {
            get
            {
                if (schemaImporterExtensions == null)
                    schemaImporterExtensions = new StringCollection();
                return schemaImporterExtensions;
            }
        }
    }
}