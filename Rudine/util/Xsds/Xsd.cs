using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.CSharp;
using TypedDataSetGeneratorException = System.Data.Design.TypedDataSetGeneratorException;

namespace Rudine.Util.Xsds
{
    public class Xsd
    {
        private static bool schemaCompileErrors;

        private static readonly Dictionary<string, string> pseudoFileStreams = new Dictionary<string, string>();

        private static void AddImports(CodeNamespace codeNamespace, string[] namespaces)
        {
            for (int i = 0; i < namespaces.Length; i++)
            {
                string nameSpace = namespaces[i];
                codeNamespace.Imports.Add(new CodeNamespaceImport(nameSpace));
            }
        }

        private static void CollectIncludes(XmlSchema schema, Uri baseUri, Hashtable includeSchemas, string topUri)
        {
            if (schema == null)
                return;
            foreach (XmlSchemaExternal xmlSchemaExternal in schema.Includes)
            {
                string schemaLocation = xmlSchemaExternal.SchemaLocation;
                if (xmlSchemaExternal is XmlSchemaImport)
                    xmlSchemaExternal.SchemaLocation = null;
                else if (xmlSchemaExternal.Schema == null && schemaLocation != null && schemaLocation.Length > 0)
                {
                    Uri uri = ResolveUri(baseUri, schemaLocation);
                    string text = uri.ToString().ToLower(CultureInfo.InvariantCulture);
                    if (topUri == text)
                    {
                        xmlSchemaExternal.Schema = new XmlSchema();
                        xmlSchemaExternal.Schema.TargetNamespace = schema.TargetNamespace;
                        xmlSchemaExternal.SchemaLocation = null;
                        break;
                    }
                    XmlSchema xmlSchema = (XmlSchema) includeSchemas[text];
                    if (xmlSchema == null)
                    {
                        string text2 = schemaLocation;
                        string pathFromUri = GetPathFromUri(uri);
                        bool flag = pathFromUri != null && File.Exists(pathFromUri);
                        if (File.Exists(text2))
                        {
                            if (flag)
                            {
                                string text3 = Path.GetFullPath(text2).ToLower(CultureInfo.InvariantCulture);
                                if (text3 != pathFromUri)
                                    Warning(Res.GetString("MultipleFilesFoundMatchingInclude4", schemaLocation, GetPathFromUri(baseUri), text3, pathFromUri));
                            }
                        } else if (flag)
                            text2 = pathFromUri;
                        xmlSchema = ReadSchema(text2, false);
                        includeSchemas[text] = xmlSchema;
                        CollectIncludes(xmlSchema, uri, includeSchemas, topUri);
                    }
                    if (xmlSchema != null)
                    {
                        xmlSchemaExternal.Schema = xmlSchema;
                        xmlSchemaExternal.SchemaLocation = null;
                    }
                }
            }
        }

        private static void Compile(XmlSchemas userSchemas, Hashtable uris, Hashtable includeSchemas)
        {
            foreach (XmlSchema xmlSchema in userSchemas)
            {
                if (xmlSchema.TargetNamespace != null && xmlSchema.TargetNamespace.Length == 0)
                    xmlSchema.TargetNamespace = null;
                Uri uri = (Uri) uris[xmlSchema];
                CollectIncludes(xmlSchema, uri, includeSchemas, uri.ToString().ToLower(CultureInfo.InvariantCulture));
            }
            try
            {
                userSchemas.Compile(ValidationCallbackWithErrorCode, true);
            } catch (Exception ex)
            {
                if (ex is ThreadAbortException || ex is StackOverflowException || ex is OutOfMemoryException)
                    throw;
                Console.WriteLine(string.Concat(Environment.NewLine, Res.GetString("SchemaValidationWarning"), Environment.NewLine, ex.Message, Environment.NewLine));
            }
            if (!userSchemas.IsCompiled)
                Console.WriteLine(Environment.NewLine + Res.GetString("SchemaValidationWarning") + Environment.NewLine);
        }

        private static void Error(Exception e, string prefix)
        {
            Console.Error.WriteLine(prefix + e.Message);
            if (e is TypedDataSetGeneratorException)
                foreach (string str in ((TypedDataSetGeneratorException) e).ErrorList)
                    Console.WriteLine(prefix + str);
            if (e.InnerException != null)
            {
                Error(e.InnerException, "  - ");
                return;
            }
            Console.WriteLine(Res.GetString("MoreHelp", "/?"));
        }

        private static string[] GetNamespacesForTypes(Type[] types)
        {
            Hashtable hashtable = new Hashtable();
            for (int i = 0; i < types.Length; i++)
            {
                string fullName = types[i].FullName;
                int num = fullName.LastIndexOf('.');
                if (num > 0)
                    hashtable[fullName.Substring(0, num)] = types[i];
            }
            string[] array = new string[hashtable.Keys.Count];
            hashtable.Keys.CopyTo(array, 0);
            return array;
        }

        private static string GetPathFromUri(Uri uri)
        {
            if (uri != null)
                try { return Path.GetFullPath(uri.LocalPath).ToLower(CultureInfo.InvariantCulture); } catch (Exception ex)
                {
                    if (ex is ThreadAbortException || ex is StackOverflowException || ex is OutOfMemoryException || ex is ConfigurationException)
                        throw;
                }
            return null;
        }

        private void ImportSchemaAsClasses(XmlSchema schema, string uri, IList elements, XmlSchemaImporter schemaImporter, XmlCodeExporter codeExporter)
        {
            if (schema == null)
                return;

            ArrayList arrayList = new ArrayList();

            foreach (XmlSchemaElement xmlSchemaElement in schema.Elements.Values)
                if (!xmlSchemaElement.IsAbstract && (uri.Length == 0 || xmlSchemaElement.QualifiedName.Namespace == uri))
                {
                    bool flag;
                    if (elements.Count == 0)
                        flag = true;
                    else
                    {
                        flag = false;
                        foreach (string a in elements)
                            if (a == xmlSchemaElement.Name)
                            {
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

        public string ImportSchemasAsClasses(string[] DocXsds, string ns, CodeGenerationOptions options, StringCollection schemaImporterExtensions)
        {
            CodeDomProvider _CodeDomProvider = new CSharpCodeProvider();

            string xsdOut = string.Empty;

            for (int i = 0; i < DocXsds.Length; i++)
                pseudoFileStreams[string.Format("urn:rudine.progablab.com/pseudoFile_{0}.xsd", i).ToLower()] = DocXsds[i];

            string[] fileNames = pseudoFileStreams.Keys.ToArray();

            string uri = string.Empty;
            string[] elements =
                { };

            XmlSchemas userSchemas = new XmlSchemas();
            Hashtable filePath_xmlSchema = new Hashtable();
            Hashtable xmlSchema_uri = new Hashtable();

            foreach (string filename in fileNames)
                if (!string.IsNullOrEmpty(filename))
                {
                    string filePath = filename.ToLower(CultureInfo.InvariantCulture);
                    if (filePath_xmlSchema[filePath] == null)
                    {
                        XmlSchema xmlSchema = ReadSchema(filePath, true);
                        filePath_xmlSchema.Add(filePath, xmlSchema);

                        Uri uri2 = new Uri(filePath);
                        xmlSchema_uri.Add(xmlSchema, uri2);
                        userSchemas.Add(xmlSchema, uri2);
                    }
                }

            Hashtable includeSchemas = new Hashtable();

            Compile(userSchemas, xmlSchema_uri, includeSchemas);

            CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
            CodeNamespace codeNamespace = new CodeNamespace(ns);
            codeCompileUnit.Namespaces.Add(codeNamespace);

            //GenerateVersionComment(codeNamespace);

            XmlCodeExporter codeExporter = new XmlCodeExporter(codeNamespace, codeCompileUnit, _CodeDomProvider, options, null);
            XmlSchemaImporter xmlSchemaImporter = new XmlSchemaImporter(userSchemas, options, _CodeDomProvider, new ImportContext(new CodeIdentifiers(), false));
            xmlSchemaImporter.Extensions.Add(new DataSetSchemaImporterExtension());

            foreach (string current in schemaImporterExtensions)
            {
                Type type = Type.GetType(current.Trim(), true, false);
                xmlSchemaImporter.Extensions.Add(type.FullName, type);
            }

            AddImports(codeNamespace, GetNamespacesForTypes(new[]
            {
                typeof(XmlAttributeAttribute)
            }));

            for (int i = 0; i < userSchemas.Count; i++)
                ImportSchemaAsClasses(userSchemas[i], uri, elements, xmlSchemaImporter, codeExporter);

            foreach (XmlSchema schema in includeSchemas.Values)
                ImportSchemaAsClasses(schema, uri, elements, xmlSchemaImporter, codeExporter);

            CodeGenerator.ValidateIdentifiers(codeNamespace);
            StringBuilder _StringBuilder = new StringBuilder();

            using (StringWriter _StringWriter = new StringWriter(_StringBuilder))
                _CodeDomProvider.GenerateCodeFromCompileUnit(codeCompileUnit, _StringWriter, new CodeGeneratorOptions
                {
                    IndentString = "    "
                });

            return _StringBuilder.ToString();
        }

        private static XmlSchema ReadSchema(string location, bool throwOnAbsent)
        {
            XmlSchema result = null;
            schemaCompileErrors = false;

            if (pseudoFileStreams.ContainsKey(location))
                using (StringReader _StringReader = new StringReader(pseudoFileStreams[location]))
                using (XmlTextReader _XmlTextReader = new XmlTextReader(_StringReader)
                {
                    XmlResolver = null
                })
                    result = XmlSchema.Read(_XmlTextReader, ValidationCallbackWithErrorCode);
            else if (!File.Exists(location))
            {
                if (throwOnAbsent)
                    throw new FileNotFoundException(Res.GetString("FileNotFound", location));
            } else
                using (XmlTextReader _XmlTextReader = new XmlTextReader(location, new StreamReader(location).BaseStream)
                {
                    XmlResolver = null
                })
                    result = XmlSchema.Read(_XmlTextReader, ValidationCallbackWithErrorCode);

            if (schemaCompileErrors)
                throw new InvalidOperationException(Res.GetString("SchemaValidationError", location));

            return result;
        }

        private static Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            if (baseUri == null || (!baseUri.IsAbsoluteUri && baseUri.OriginalString.Length == 0))
            {
                Uri uri = new Uri(relativeUri, UriKind.RelativeOrAbsolute);
                if (!uri.IsAbsoluteUri)
                    uri = new Uri(Path.GetFullPath(relativeUri));
                return uri;
            }
            if (relativeUri == null || relativeUri.Length == 0)
                return baseUri;
            return new Uri(baseUri, relativeUri);
        }

        private static void ValidationCallbackWithErrorCode(object sender, ValidationEventArgs args)
        {
            string @string;
            if (args.Exception.LineNumber == 0 && args.Exception.LinePosition == 0)
                @string = Res.GetString("SchemaValidationWarningDetails", args.Message);
            else
                @string = Res.GetString("SchemaValidationWarningDetailsSource", args.Message, args.Exception.LineNumber.ToString(CultureInfo.InvariantCulture), args.Exception.LinePosition.ToString(CultureInfo.InvariantCulture));
            if (args.Severity == XmlSeverityType.Error)
            {
                Console.WriteLine(@string);
                schemaCompileErrors = true;
            }
        }

        private static void Warning(string message) { Console.WriteLine(Res.GetString("Warning", message)); }
    }
}