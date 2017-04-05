using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Rudine.Template;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine.Interpreters.Xsn
{
    /// <summary>
    ///     DocInterpreter factory implementation supporting the text (xml) based InfoPath document
    /// </summary>
    public class XsnXmlInterpreter : DocTextInterpreter
    {
        public const string mso_infoPathSolution = "solutionVersion=\"{0}\" productVersion=\"14.0.0\" PIVersion=\"1.0.0.0\" href=\"{1}\" name=\"{2}\""; //TASK: Code stub the solution version so the InfoPath UI does not complain
        public const string mso_application = "progid=\"InfoPath.Document\" versionProgid=\"InfoPath.Document.3\"";
        public const string ipb_application = "DocId=\"{0}\" DocTitle=\"{1}\" DocTypeName=\"{2}\" DocChecksum=\"{3}\"";
        private const string XmlProcessingInstructionMatch = @"<\?.*\?>";
        internal const string XmlRootAttributeNamespaces = @"(?:xmlns:)(\w+)(?:="")([^""]+)";

        /// <summary>
        ///     "One XML processing instruction tag named mso-infoPathSolution MUST be specified as part of the form file. This XML
        ///     processing instruction tag specifies properties, as defined by the following attributes, of this form file and the
        ///     associated form template."
        /// </summary>
        /// <summary>
        /// </summary>
        /// <param name="context"></param>
        /// <summary>
        ///     The value from the key specified by {0} parsed from the XML processing instructions
        /// </summary>
        private const string parseRegex = @"(?<=(^|[^\w]+){0}="")([^""""]+)(?="".*)";

        /// <summary>
        ///     matches 0001-01-01T00:00:00, 0 & false; things considered default values in this solution
        /// </summary>
        private static readonly Regex
            DocXmlInvalidDateStringRegEx = new Regex("0001-01-01T00:00:00[^<]*", RegexOptions.IgnoreCase),
            DocXmlDefaultValueRegEx = new Regex("(?<=>)(0001-01-01T00:00:00[^<]*)(?=<)", RegexOptions.IgnoreCase),
            XmlEmptyTagWithoutNullableElementsRegEx = new Regex(@"<(([_:A-Za-z][-._:A-Za-z0-9]*))\s*(/>|>\s*</\2>)", RegexOptions.Singleline | RegexOptions.Multiline);

        private static readonly List<Type> XsdIntEgerFixDstBaseDocKnownTypes = new List<Type>();

        /// <summary>
        ///     Default value upon class construction is xml processing instructions specific to Microsoft Office InfoPath & an
        ///     additional instruction set named mso-infoPathSolution specific to this application
        /// </summary>
        public WriteXmlProcessingInstructions WriteXmlProcessingInstructions;

        public XsnXmlInterpreter() { WriteXmlProcessingInstructions = WriteInfoPathProcessingInstructions; }

        public override string ContentFileExtension
        {
            get { return "xml"; }
        }

        public override string ContentType
        {
            get { return "application/vnd.ms-infopath"; }
        }

        /// <summary>
        ///     Removes empty elements from xml. Achieving the same rendering (or lack of) DefaultValue(0)
        ///     & DefaultValue(false) interpreted by XmlSerializer by removing elements with those values.
        ///     This is accomplished by altering XmlSerializer's output. Target audience of this application
        ///     is/was business users whom zero & false usually hold the same meaning over the population.
        /// </summary>
        /// <param name="DocData"></param>
        /// <returns></returns>
        public static string CollapseDefaultValueElements(string DocData, string DocTypeName)
        {
            // remove empty tags from the template.xml so they are not read as blanks when
            // the XmlSerializer reads them to be merged with an incoming create request
            // for a new Doc
            //TODO:Decide if this belongs here (should it go in the DocXmlHandler?)
            int Length = 0;
            DocData = DocXmlDefaultValueRegEx.Replace(DocData, string.Empty);

            //HACK:Need to observe my xmlns dynamically
            //TODO:Remove only empty tags of integral value types as these are the only ones that mess up the XmlSerializer parser.
            //The fact of the matter is, empty tags are significant to the infopath application and should be kept
            string rootTag = Regex.Match(DocData,
                @"<my:" + DocTypeName + "[^>]+>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase).Value;

            while (Length != DocData.Length)
            {
                Length = DocData.Length;
                // Remove empty tags
                DocData = XmlEmptyTagWithoutNullableElementsRegEx.Replace(DocData, string.Empty);
            }

            // make sure we didn't swipe the root tag out of the file
            if (DocData.IndexOf(rootTag) == -1)
                DocData += rootTag + "</my:" + DocTypeName + ">";

            return DocData;
        }

        public override BaseDoc Create(string DocTypeName) { return Read(TemplateController.Instance.OpenText(DocTypeName, "template.xml")); }

        /// <summary>
        ///     XmlSerializer writes Booleans as the words "true" & "false". InfoPath can
        ///     potentially write them as "1" & "0".
        /// </summary>
        /// <param name="docXml"></param>
        /// <param name="DocTypeName"></param>
        /// <returns></returns>
        internal static string FormatBooleansTrueFalseOrZeroOne(string docXml, string DocTypeName)
        {
            string templateDocXml = TemplateController.Instance.OpenText(DocTypeName, "template.xml");

            return Regex.Replace(
                docXml,
                @"(<my:)([^>]+)(>)(false|true)(</my:)(\2)(>)",
                match => templateDocXml.IndexOf(match.Groups[0].Value.Replace(">false<", ">0<").Replace(">true<", ">1<")) != -1
                             ? match.Groups[0].Value.Replace(">false<", ">0<").Replace(">true<", ">1<")
                             : match.Groups[0].Value,
                RegexOptions.Singleline | RegexOptions.Multiline);
        }

        /// <summary>
        ///     Parses the given form's "solutionVersion" number from the manifest.xsf. Note,
        ///     this string will change every time
        /// </summary>
        /// <param name= NavKey.DocTypeName></param>
        /// <returns></returns>
        public override string GetDescription(string DocTypeName) => ParseAttributeValue(TemplateController.Instance.OpenText(DocTypeName, "manifest.xsf"), "description");

        /// <summary>
        ///     Parses the given form's "solutionVersion" number from the manifest.xsf. Note,
        ///     this string will change every time
        /// </summary>
        /// <param name= NavKey.DocTypeName></param>
        /// <returns></returns>
        public string GetDocRev(string DocData) { return ReadDocRev(DocData); }

        public override string HrefVirtualFilename(string DocTypeName, string DocRev) => "manifest.xsf";

        private static string parseReadDocTypeName(string DocData)
        {
            return Regex.Match(DocData,
                @"(urn:schemas-microsoft-com:office:infopath:)(?<DocTypeName>\w+)(:-myXSD-\d{4}-\d{2}-\d{2}T\d{2}-\d{2}-\d{2})",
                RegexOptions.IgnoreCase).Groups["DocTypeName"].Value;
        }

        private static string ParseAttributeValue(string DocData, string attributeName) { return Regex.Match(DocData, string.Format("(?<={0}=\")(.*?)(?=\")", attributeName), RegexOptions.Singleline).Value; }

        public override bool Processable(string DocTypeName, string DocRev)
        {
            string template_xml = TemplateController.Instance.OpenText(DocTypeName, DocRev, "template.xml");
            return
                !string.IsNullOrWhiteSpace(template_xml)
                &&
                !string.IsNullOrWhiteSpace(TemplateController.Instance.OpenText(DocTypeName, DocRev, "manifest.xsf"))
                &&
                ReadDocTypeName(template_xml) == DocTypeName
                &&
                GetDocRev(template_xml) == DocRev;
        }

        /// <summary>
        /// </summary>
        /// <param name="DocData"></param>
        /// <param name="DocRevStrict"></param>
        /// <returns></returns>
        public override BaseDoc Read(string DocData, bool DocRevStrict = false)
        {
            if (string.IsNullOrWhiteSpace(DocData))
                return null;

            DocProcessingInstructions pi = ReadDocPI(DocData);

            string
                CollapsedElementsDocXml,
                DocTypeName = ReadDocTypeName(DocData),
                DocRev = ReadDocRev(DocData);

            if (string.IsNullOrWhiteSpace(DocTypeName))
                DocTypeName = pi.DocTypeName;

            if (string.IsNullOrWhiteSpace(DocRev))
                DocRev = pi.solutionVersion;

            CollapsedElementsDocXml = CollapseDefaultValueElements(DocData, DocTypeName);

            BaseDoc dstBaseDoc = null;

            Type BaseDocType = Runtime.ActivateBaseDocType(
                DocTypeName,
                DocRevStrict
                    ? DocRev
                    : TemplateController.Instance.TopDocRev(DocTypeName));

            using (StringReader _StringReader = new StringReader(CollapsedElementsDocXml))
            using (XmlTextReader _XmlTextReader = new XmlTextReader(_StringReader))
                return SetPI(
                    (BaseDoc) new XmlSerializer(BaseDocType).Deserialize(_XmlTextReader),
                    pi,
                    DocTypeName,
                    DocRev);
        }

        /// <summary>
        ///     parses XML processing instructions also
        /// </summary>
        /// <param name="SrcDocXml"></param>
        /// <param name="DstBaseDoc"></param>
        /// <returns></returns>
        public override DocProcessingInstructions ReadDocPI(string SrcDocXml)
        {
            DocProcessingInstructions _DocProcessingInstructions = new DocProcessingInstructions();
            Regex _DocIdRegEx = new Regex(string.Format(parseRegex, "DocId"));
            XmlDocument _XmlDocument = new XmlDocument();
            _XmlDocument.LoadXml(SrcDocXml);

            foreach (XmlProcessingInstruction _XmlProcessingInstruction in _XmlDocument.ChildNodes.OfType<XmlProcessingInstruction>())
            {
                if (_DocIdRegEx.IsMatch(_XmlProcessingInstruction.InnerText))
                    _DocProcessingInstructions.SetDocId(_DocIdRegEx.Match(_XmlProcessingInstruction.InnerText).Value);

                foreach (var _Kv in typeof(DocProcessingInstructions)
                    .GetProperties()
                    .Select(m => new
                    {
                        property = m,
                        matcher = new Regex(string.Format(parseRegex, m.Name))
                    }))
                    if (_Kv.property.CanWrite)
                        if (_Kv.matcher.IsMatch(_XmlProcessingInstruction.InnerText))
                            _Kv.property.SetValue(
                                _DocProcessingInstructions,
                                Convert.ChangeType(
                                    _Kv
                                        .matcher
                                        .Match(_XmlProcessingInstruction.InnerText)
                                        .Value,
                                    ExpressionParser
                                        .GetNonNullableType(_Kv.property.PropertyType),
                                    null),
                                null);
            }

            _DocProcessingInstructions.solutionVersion = GetDocRev(SrcDocXml);

            return _DocProcessingInstructions;
        }

        /// <summary>
        ///     Parses the given form's "solutionVersion" number from the manifest.xsf. Note,
        ///     this string will change every time
        /// </summary>
        /// <param name= NavKey.DocTypeName></param>
        /// <returns></returns>
        public override string ReadDocRev(string DocData) { return ParseAttributeValue(DocData, "solutionVersion"); }

        public override string ReadDocTypeName(string DocData) { return parseReadDocTypeName(DocData); }

        public static string RemoveInvalidDateElementText(string DocData) { return DocXmlInvalidDateStringRegEx.Replace(DocData, string.Empty); }

        /// <summary>
        ///     a side-effect of the XmlSerializer working on a PropertyOverlay-ApplyUninitializedObject object processing before
        ///     XmlSerializer are initialized value-typed properties.
        ///     These properties render XML like  "
        ///     <my:field1_46EnvironmentalModifications>false</my:field1_46EnvironmentalModifications>" or "
        ///     <my:TotalNumberOfDirectCareStaff>0</my:TotalNumberOfDirectCareStaff>".
        ///     InfoPath interprets this as an explicitly set element adjusting it's UI accordingly.
        ///     We do not want this. The elements should render like
        ///     "<my:field1_46EnvironmentalModifications></my:field1_46EnvironmentalModifications>" & "
        ///     <my:TotalNumberOfDirectCareStaff></my:TotalNumberOfDirectCareStaff>".
        ///     @"(<my:)([^>]+)(>)(0|false)(</my:)(\2)(>)", "$1$2$3$5$6$7" strips those element values. The template.xml file
        ///     content is also
        ///     analyzed to make sure it has not specified something like "
        ///     <my:field1_46EnvironmentalModifications>false</my:field1_46EnvironmentalModifications>"
        ///     as it;s explicit default; if it has, the element fix/replacement is skipped as it appears the designer of the form
        ///     meant to do this
        ///     This method was established simple so it can be documented with proper XML comments. As of this writing it's only
        ///     referenced once.
        /// </summary>
        /// <param name="docXml"></param>
        /// <param name="DocTypeName"></param>
        /// <returns></returns>
        internal static string RemoveValueTypeElementDefaults(string docXml, string DocTypeName)
        {
            string templateDocXml = TemplateController.Instance.OpenText(DocTypeName, "template.xml");

            return Regex.Replace(
                docXml,
                @"(<my:)([^>]+)(>)(0|false)(</my:)(\2)(>)",
                match => templateDocXml.IndexOf(match.Groups[0].Value) == -1 // checking the template.xml for existence of the matched element
                             ? string.Format("{0}{1}{2}{3}{4}{5}", match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value, match.Groups[5].Value, match.Groups[6].Value, match.Groups[7].Value)
                             : match.Groups[0].Value,
                RegexOptions.Singleline | RegexOptions.Multiline);
        }

        /// <summary>
        ///     Runs a given form's xml schema against it throwing an exception if it fails to validate
        /// </summary>
        /// <param name= NavKey.DocTypeName></param>
        /// <param name="xml"></param>
        public override void Validate(string DocData) { new SchemaValidator().Validate(DocData, Read(DocData, true)); }

        private XmlWriter WriteInfoPathProcessingInstructions(DocProcessingInstructions pi, XmlWriter _XmlTextWriter)
        {
            _XmlTextWriter.WriteProcessingInstruction("mso-infoPathSolution",
                string.Format(
                    mso_infoPathSolution,
                    pi.solutionVersion ?? TemplateController.Instance.TopDocRev(pi.DocTypeName),
                    pi.href,
                    pi.name));

            _XmlTextWriter.WriteProcessingInstruction("mso-application", mso_application);

            // there is special instructions for attachments
            if ((TemplateController.Instance.OpenText(pi.DocTypeName, pi.solutionVersion, "template.xml") ?? string.Empty).IndexOf("mso-infoPath-file-attachment-present") > 0)
                _XmlTextWriter.WriteProcessingInstruction("mso-infoPath-file-attachment-present", string.Empty);

            _XmlTextWriter.WriteProcessingInstruction("ipb-application",
                string.Format(ipb_application,
                    pi.GetDocId(),
                    pi.DocTitle,
                    pi.DocTypeName,
                    pi.DocChecksum));

            return _XmlTextWriter;
        }

        public override string WritePI(string DocData, DocProcessingInstructions _ManifestInfo)
        {
            return string.Format(
                "{0}{1}",
                WriteText(_ManifestInfo),
                Regex.Replace(DocData, XmlProcessingInstructionMatch, ""));
        }

        /// <summary>
        ///     Renders an XML document using an XmlSerializer, applies the given DocTypeName's template.xml
        ///     applies xml namespaces from that template.xml to the new text rendered & optional
        ///     xml processing instructions specific to InfoPath & custom to Rudine.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns>Initial document InfoPath Form Filler will open</returns>
        public override string WriteText<T>(T source, bool includeProcessingInformation = true)
        {
            //Encoding.Unicode
            using (StringWriter _StringWriter = new StringWriter())
            using (XmlWriter _XmlTextWriter = includeProcessingInformation
                                                  ? WriteXmlProcessingInstructions(source, XmlWriter.Create(_StringWriter, new XmlWriterSettings
                                                  {
                                                      Encoding = Encoding.Unicode
                                                  }))
                                                  : XmlWriter.Create(_StringWriter, new XmlWriterSettings
                                                  {
                                                      Encoding = Encoding.Unicode
                                                  }))
            {
                //TODO:Cache _XmlSerializerNamespaces
                XmlSerializerNamespaces _XmlSerializerNamespaces = new XmlSerializerNamespaces();
                foreach (Match xmlnsMatch in Regex.Matches(TemplateController.Instance.OpenText(source.DocTypeName, source.solutionVersion, "template.xml") ?? string.Empty, XmlRootAttributeNamespaces))
                    _XmlSerializerNamespaces.Add(xmlnsMatch.Groups[1].Value, xmlnsMatch.Groups[2].Value);

                if (source is BaseDoc)
                    new XmlSerializer(source.GetType()).Serialize(_XmlTextWriter, source, _XmlSerializerNamespaces);

                //TODO:Move regex logic to CollapseDefaultValueElements
                //TODO:Explorer the now open-source serializer to see how they detect uninitialized roperties & tell the property overlay to leave them alone.
                _XmlTextWriter.Flush();
                string DocXml = _StringWriter.ToString();

                return RemoveInvalidDateElementText(
                    source.DocStatus == null // tells us we just rendered this xml, it has not been through the infopath desktop application
                        ? RemoveValueTypeElementDefaults(FormatBooleansTrueFalseOrZeroOne(DocXml, source.DocTypeName), source.DocTypeName)
                        : DocXml);
            }
        }

        private class SchemaValidator
        {
            private static readonly Regex MatchFieldVal = new Regex("(')(?<schemaNfield>.*)(')(?<desc>.*)");
            private readonly List<string> ValidationMessages = new List<string>();
            private string ValidatingNamespace = string.Empty;

            private void _XmlValidatingReader_ValidationEventHandler(object sender, ValidationEventArgs e)
            {
                if (e.Message.Contains(ValidatingNamespace))
                    if (MatchFieldVal.IsMatch(e.Message))
                        ValidationMessages.Add(e.Message);
            }

            /// <summary>
            ///     Runs a given form's xml schema against it throwing an exception if it fails to validate
            /// </summary>
            /// <param name= NavKey.DocTypeName></param>
            /// <param name="xml"></param>
            internal void Validate(string DocData, BaseDoc _BaseDoc)
            {
                if (_BaseDoc.DocKeys.Count == 0)
                    throw new Exception("No DocKeys have been defined");

                Type t = _BaseDoc.GetType();

                using (StringReader _StringReader = new StringReader(DocData))
                using (XmlTextReader _XmlTextReader = new XmlTextReader(_StringReader))
                using (XmlValidatingReader _XmlValidatingReader = new XmlValidatingReader(_XmlTextReader)
                    {
                        ValidationType = ValidationType.Schema
                    })
                    //TODO:Use XmlReader to perform validation instead of XmlValidatingReader (http://msdn.microsoft.com/en-us/library/hdf992b8%28v=VS.80%29.aspx)
                {
                    // Grab the xml namescape that was expressed as an attribute of the class the XsnTransform.cmd auto generated
                    ValidatingNamespace = t
                        .GetCustomAttributes(false)
                        .OfType<DataContractAttribute>()
                        .FirstOrDefault()
                        .Namespace;

                    using (StringReader _StringReaderXsd = new StringReader(TemplateController.Instance.OpenText(_BaseDoc.DocTypeName, _BaseDoc.solutionVersion, "myschema.xsd")))
                    using (XmlTextReader _XmlTextReaderXsd = new XmlTextReader(_StringReaderXsd))
                    {
                        // Add that class into .Net XML XSD schema validation
                        _XmlValidatingReader.Schemas.Add(ValidatingNamespace, _XmlTextReaderXsd);

                        _XmlValidatingReader.ValidationEventHandler += _XmlValidatingReader_ValidationEventHandler;
                        //Start validating

                        while (_XmlValidatingReader.Read()) { }
                    }
                }

                if (ValidationMessages.Count > 0)
                {
                    List<string> FieldErrors = new List<string>();

                    Regex regexObj = new Regex(@"http://[^']+:(\w+)' element has an invalid value according to its data type", RegexOptions.IgnoreCase | RegexOptions.Multiline);

                    foreach (string _T in ValidationMessages)
                        if (_T.Contains("Signature(s)"))
                            FieldErrors.Add(_T);
                        else if (regexObj.IsMatch(_T))
                            FieldErrors.Add(StringTransform.Wordify(regexObj.Match(_T).Groups[1].Value.Trim().Trim(':')));
                        else
                            foreach (PropertyInfo p in t.GetProperties())
                                if (Regex.IsMatch(_T, string.Format(@"\b(?=\w){0}\b(?!\w)", p.Name)))
                                    FieldErrors.Add(StringTransform.Wordify(p.Name).Trim().Trim(':'));

                    if (FieldErrors.Count > 0)
                    {
                        string ValidationMessageMarkDown =
                            string.Format(
                                "\t\t{0}",
                                string.Join("\r\n\t\t", FieldErrors.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct()));

                        int ValidationMessagesCount = FieldErrors.Count;
                        ValidationMessages.Clear();

                        throw new Exception(
                            string.Format(
                                "TODO:Put back this valiation message from repo as I deleted the resx on accident",
                                ValidationMessagesCount,
                                ValidationMessageMarkDown));
                    }
                }
                ValidationMessages.Clear();
            }
        }

        public override void ProcessRequest(HttpContext context)
        {
            TemplateFileInfo _TemplateFileInfo = TemplateController.ParseTemplateFileInfo(context);

            if (!_TemplateFileInfo.FileName.Equals(HrefVirtualFilename(_TemplateFileInfo.DocTypeName, _TemplateFileInfo.solutionVersion), StringComparison.InvariantCultureIgnoreCase))
                base.ProcessRequest(context);
            else
            {
                context.Response.DisableKernelCache();
                context.Response.Clear();
                context.Response.ClearContent();
                context.Response.ClearHeaders();

                Regex regPublishUrl = new Regex("(?<=publishUrl=\")(.*?)(?=\")", RegexOptions.Multiline);

                // The publish URL within this file needs to be updated to the current requested URL for the InfoPath application form to like it
                //string ManifestPath = context.Request.MapPath(new Uri(RequestPaths.AbsoluteUri).LocalPath);
                string UrlReferrer_AbsoluteUri = context.Request.UrlReferrer == null ? "" : context.Request.UrlReferrer.AbsoluteUri;

                string filename;
                string[] lines = TemplateController.Instance.OpenText(context, out filename).Split('\n', '\r');

                // render the publishUrl as the calling request or that of a registered listener
                string publishUrl = UrlReferrer_AbsoluteUri.Contains("/" + ReverseProxy.DirectoryName)
                                        ? UrlReferrer_AbsoluteUri
                                        : RequestPaths.AbsoluteUri;

                context.Response.ClearContent();

                for (int i = 0; i < lines.Length; i++)
                    context.Response.Write(
                        regPublishUrl.IsMatch(lines[i]) ?
                            regPublishUrl.Replace(lines[i], publishUrl) :
                            lines[i]);

                context.Response.ContentType = "text/xml";
            }
        }
    }

    public delegate XmlWriter WriteXmlProcessingInstructions(DocProcessingInstructions pi, XmlWriter _XmlTextWriter);
}