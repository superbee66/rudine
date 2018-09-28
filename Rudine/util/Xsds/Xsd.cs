using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.CSharp;
using Rudine.Web.Util;

namespace Rudine.Util.Xsds {
    [Obsolete("use Xsd")]
    public class XsdExporter : Xsd {}

    public class Xsd {
        internal string AsCSharp(string[] DocXsds, string ns, CodeGenerationOptions options, StringCollection schemaImporterExtensions) {
            CodeDomProvider _CodeDomProvider = new CSharpCodeProvider();

            string uri = string.Empty;
            string[] elements = { };

            XmlSchemas userSchemas = new XmlSchemas();

            foreach (string xsd in DocXsds)
                userSchemas.Add(ReadSchema(xsd, true));

            userSchemas.Compile(ValidationCallbackWithErrorCode, true);

            CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
            CodeNamespace codeNamespace = new CodeNamespace(ns);
            codeCompileUnit.Namespaces.Add(codeNamespace);

            XmlCodeExporter codeExporter = new XmlCodeExporter(codeNamespace, codeCompileUnit, _CodeDomProvider, options, null);
            XmlSchemaImporter xmlSchemaImporter = new XmlSchemaImporter(userSchemas, options, _CodeDomProvider, new ImportContext(new CodeIdentifiers(), false));
            xmlSchemaImporter.Extensions.Add(new DataSetSchemaImporterExtension());

            foreach (string current in schemaImporterExtensions) {
                Type type = Type.GetType(current.Trim(), true, false);
                xmlSchemaImporter.Extensions.Add(type.FullName, type);
            }

            Type[] types = { typeof(XmlAttributeAttribute) };
            Hashtable hashtable = new Hashtable();
            foreach (Type type in types) {
                string fullName = type.FullName;
                int num = fullName.LastIndexOf('.');
                if (num > 0)
                    hashtable[fullName.Substring(0, num)] = type;
            }
            string[] array = new string[hashtable.Keys.Count];
            hashtable.Keys.CopyTo(array, 0);
            string[] namespaces = array;
            foreach (string nameSpace in namespaces)
                codeNamespace.Imports.Add(new CodeNamespaceImport(nameSpace));

            for (int i = 0; i < userSchemas.Count; i++)
                ImportSchemaAsClasses(userSchemas[i], uri, elements, xmlSchemaImporter, codeExporter);

            CodeGenerator.ValidateIdentifiers(codeNamespace);
            StringBuilder _StringBuilder = new StringBuilder();

            using (StringWriter _StringWriter = new StringWriter(_StringBuilder))
                _CodeDomProvider.GenerateCodeFromCompileUnit(codeCompileUnit, _StringWriter, new CodeGeneratorOptions { IndentString = "    " });

            return _StringBuilder.ToString();
        }

        internal static List<string> AsXsd(Assembly assembly, List<string> typeNames = null, string xmlDefaultNamespace = null) {
            if (typeNames == null)
                typeNames = new List<string>();

            XmlReflectionImporter xmlReflectionImporter = new XmlReflectionImporter(xmlDefaultNamespace);
            XmlSchemas xmlSchemas = new XmlSchemas();
            XmlSchemaExporter xmlSchemaExporter = new XmlSchemaExporter(xmlSchemas);

            try {
                Type[] types = assembly.GetExportedTypes2();
                foreach (Type type in types.Where(type =>
                                                      typeNames.Count == 0
                                                      ||
                                                      (type.IsPublic
                                                       &&
                                                       (!type.IsAbstract || !type.IsSealed)
                                                       &&
                                                       !type.IsInterface
                                                       &&
                                                       !type.ContainsGenericParameters))) {
                    bool flag;
                    if (typeNames.Count == 0)
                        flag = true;
                    else {
                        flag = false;
                        foreach (string text2 in typeNames)
                            if (type.FullName == text2 || type.Name == text2 || (text2.EndsWith(".*") && type.FullName.StartsWith(text2.Substring(0, text2.Length - 2)))) {
                                flag = true;
                                break;
                            }
                    }
                    if (flag) {
                        XmlTypeMapping xmlTypeMapping = xmlReflectionImporter.ImportTypeMapping(type);
                        xmlSchemaExporter.ExportTypeMapping(xmlTypeMapping);
                    }
                }

                xmlSchemas.Compile(ValidationCallbackWithErrorCode, false);
            } catch (Exception ex) {
                if (ex is ThreadAbortException || ex is StackOverflowException || ex is OutOfMemoryException)
                    throw;
                throw new InvalidOperationException("General Error", ex);
            }

            List<string> xmlSchemasList = new List<string>(xmlSchemas.Count);

            foreach (XmlSchema _XmlSchema in xmlSchemas)
                using (StringWriter _StringWriter = new StringWriter()) {
                    _XmlSchema.Write(_StringWriter, new XmlNamespaceManager(new NameTable()));
                    xmlSchemasList.Add(_StringWriter.ToString());
                }

            return xmlSchemasList;
        }

        [Obsolete("use AsXsd")]
        internal static List<string> ExportSchemas(Assembly assembly, List<string> typeNames = null, string xmlDefaultNamespace = null) =>
            AsXsd(assembly, typeNames, xmlDefaultNamespace);

        private void ImportSchemaAsClasses(XmlSchema schema, string uri, IList elements, XmlSchemaImporter schemaImporter, XmlCodeExporter codeExporter) {
            if (schema == null)
                return;

            ArrayList arrayList = new ArrayList();

            foreach (XmlSchemaElement xmlSchemaElement in schema.Elements.Values)
                if (!xmlSchemaElement.IsAbstract && (uri.Length == 0 || xmlSchemaElement.QualifiedName.Namespace == uri)) {
                    bool flag;
                    if (elements.Count == 0)
                        flag = true;
                    else {
                        flag = false;
                        foreach (string a in elements)
                            if (a == xmlSchemaElement.Name) {
                                flag = true;
                                break;
                            }
                    }
                    if (flag)
                        arrayList.Add(schemaImporter.ImportTypeMapping(xmlSchemaElement.QualifiedName));
                }
            foreach (XmlTypeMapping xmlTypeMapping in arrayList)
                codeExporter.ExportTypeMapping(xmlTypeMapping);
        }

        [Obsolete("use AsCSharp")]
        internal string ImportSchemasAsClasses(string[] DocXsds, string ns, CodeGenerationOptions options, StringCollection schemaImporterExtensions) =>
            AsCSharp(DocXsds, ns, options, schemaImporterExtensions);

        private static XmlSchema ReadSchema(string xsd, bool throwOnAbsent) {
            using (StringReader _StringReader = new StringReader(xsd))
            using (XmlTextReader _XmlTextReader = new XmlTextReader(_StringReader) {
                XmlResolver = null
            })
                return XmlSchema.Read(_XmlTextReader, ValidationCallbackWithErrorCode);
        }

        private static void ValidationCallbackWithErrorCode(object sender, ValidationEventArgs args) {
            if (args.Severity == XmlSeverityType.Error)
                throw new XmlSchemaValidationException(args.Message);
        }
    }
}