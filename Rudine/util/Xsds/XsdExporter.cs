using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rudine.Util.Xsds
{
    //TODO:Combine with XsdClassGen

    /// <summary>
    ///     same thing that xsd.exe does
    /// </summary>
    public static class XsdExporter
    {
        private static bool schemaCompileErrors;

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

        /// <summary>
        ///     .net assemblies (dll(s) when on disk) go in and .xsd file contents comes out
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="typeNames"></param>
        /// <returns></returns>
        public static List<string> ExportSchemas(Assembly assembly, List<string> typeNames = null, string xmlDefaultNamespace = null)
        {
            if (typeNames == null)
                typeNames = new List<string>();

            XmlReflectionImporter xmlReflectionImporter = new XmlReflectionImporter(xmlDefaultNamespace);
            XmlSchemas xmlSchemas = new XmlSchemas();
            XmlSchemaExporter xmlSchemaExporter = new XmlSchemaExporter(xmlSchemas);

            try
            {
                Type[] types = assembly.GetTypes();
                for (int i = 0; i < types.Length; i++)
                {
                    Type type = types[i];
                    if (type.IsPublic && (!type.IsAbstract || !type.IsSealed) && !type.IsInterface && !type.ContainsGenericParameters)
                    {
                        bool flag;
                        if (typeNames.Count == 0)
                            flag = true;
                        else
                        {
                            flag = false;
                            foreach (string text2 in typeNames)
                                if (type.FullName == text2 || type.Name == text2 || (text2.EndsWith(".*") && type.FullName.StartsWith(text2.Substring(0, text2.Length - 2))))
                                {
                                    flag = true;
                                    break;
                                }
                        }
                        if (flag)
                        {
                            XmlTypeMapping xmlTypeMapping = xmlReflectionImporter.ImportTypeMapping(type);
                            xmlSchemaExporter.ExportTypeMapping(xmlTypeMapping);
                        }
                    }
                }

                xmlSchemas.Compile(ValidationCallbackWithErrorCode, false);
            } catch (Exception ex)
            {
                if (ex is ThreadAbortException || ex is StackOverflowException || ex is OutOfMemoryException)
                    throw;
                throw new InvalidOperationException("General Error", ex);
            }

            List<string> xmlSchemasList = new List<string>(xmlSchemas.Count);

            foreach (XmlSchema _XmlSchema in xmlSchemas)
                try
                {
                    using (StringWriter _StringWriter = new StringWriter())
                    {
                        XmlNamespaceManager namespaceManager = new XmlNamespaceManager(new NameTable());
                        //// the default namespace prefix of xs makes Microsoft word upset, it was found that a
                        //namespaceManager.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");
                        _XmlSchema.Write(_StringWriter, namespaceManager);
                        xmlSchemasList.Add(_StringWriter.ToString());
                    }
                } catch (Exception ex2)
                {
                    if (ex2 is ThreadAbortException || ex2 is StackOverflowException || ex2 is OutOfMemoryException)
                        throw;
                    throw new InvalidOperationException(Res.GetString("ErrGeneral", _XmlSchema.TargetNamespace), ex2);
                }

            return xmlSchemasList;
        }

        private static string GetPathFromUri(Uri uri)
        {
            if (uri != null)
                try
                {
                    return Path.GetFullPath(uri.LocalPath).ToLower(CultureInfo.InvariantCulture);
                } catch (Exception ex)
                {
                    if (ex is ThreadAbortException || ex is StackOverflowException || ex is OutOfMemoryException || ex is ConfigurationException)
                        throw;
                }
            return null;
        }

        private static XmlSchema ReadSchema(string location, bool throwOnAbsent)
        {
            if (!File.Exists(location))
            {
                if (throwOnAbsent)
                    throw new FileNotFoundException(Res.GetString("FileNotFound", location));
                Console.WriteLine(Res.GetString("SchemaValidationWarningDetails", Res.GetString("FileNotFound", location)));
                return null;
            }
            XmlTextReader xmlTextReader = new XmlTextReader(location, new StreamReader(location).BaseStream);
            xmlTextReader.XmlResolver = null;
            schemaCompileErrors = false;
            XmlSchema result = XmlSchema.Read(xmlTextReader, ValidationCallbackWithErrorCode);
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