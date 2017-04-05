using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.CSharp;
using Rudine.Template;
using Rudine.Util;
using Rudine.Util.Xsds;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine
{
    public class Runtime
    {
        /// <summary>
        ///     loads & activates vanilla BaseDoc object only.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <summary>
        ///     created only when #DEBUG preprocessor variable is defined
        /// </summary>
        public const string FOLDER_FOR_COMPILE_TEMPORARY_FILES = "activate";

        /// <summary>
        ///     Utilized by CompileAssembly
        /// </summary>
        public static readonly Dictionary<string, string> USING_NAMESPACES = new[]
        {
            typeof(List<>),
            typeof(DataContractSerializer),
            typeof(XmlAttribute),
            typeof(XmlSerializer),
            typeof(IBaseDocController),
            typeof(Runtime)
        }.ToDictionary(m => m.Namespace, n => Path.GetFileName(n.Assembly.Location));

        public static string DirectoryPath
        {
            get { return RequestPaths.GetPhysicalApplicationPath(FOLDER_FOR_COMPILE_TEMPORARY_FILES); }
        }

        public const string MYSCHEMA_XSD_FILE_NAME = "myschema.xsd";

        public static BaseDoc ActivateBaseDoc(string DocTypeName, string DocRev, params string[] AdditionalRootNames) =>
            (BaseDoc) Activator.CreateInstance(ActivateBaseDocType(DocTypeName, DocRev, AdditionalRootNames));

        private static readonly ConcurrentDictionary<string, Type> ActivateBaseDocTypeDictionary = new ConcurrentDictionary<string, Type>();

        public static Type ActivateBaseDocType(string DocTypeName, string DocRev, params string[] AdditionalRootNames)
        {
            string FullName = RuntimeTypeNamer.CalcCSharpFullName(DocTypeName, DocRev, AdditionalRootNames);
            return ActivateBaseDocTypeDictionary.ContainsKey(FullName)
                       ? ActivateBaseDocTypeDictionary[FullName]
                       : (ActivateBaseDocTypeDictionary[FullName] =
                              ActivateBaseDocType_Internal(DocTypeName, DocRev, AdditionalRootNames));
        }

        private static Type ActivateBaseDocType_Internal(string DocTypeName, string DocRev, params string[] AdditionalRootNames)
        {
            return FindBaseDocType(
                MakeBaseDocAssembly(new[]
                    {
                        TemplateController.Instance.OpenText(DocTypeName, DocRev, MYSCHEMA_XSD_FILE_NAME)
                    },
                    DocTypeName,
                    DocRev,
                    AdditionalRootNames),
                DocTypeName);
        }

        private static readonly CSharpCodeProvider CSharpCodeProvider = new CSharpCodeProvider();
        private static string CompileCSharpCodeDefaultOutDirectory;
        private static readonly ConcurrentDictionary<int, Assembly> CompileCSharpCodeAssemblies = new ConcurrentDictionary<int, Assembly>();
        public static Assembly CompileCSharpCode(string cSharpCode) { return CompileCSharpCode(() => cSharpCode); }

        public static Assembly CompileCSharpCode(Func<string> cSharpCodeFactory, string OutputName = default(string))
        {
            // can't be null as it will be used in calc later
            OutputName = OutputName ?? string.Empty;

            string cSharpCode = string.IsNullOrWhiteSpace(OutputName)
                                    ? cSharpCodeFactory.Invoke()
                                    : string.Empty;

            bool IncludeDebugInformation =
#if DEBUG
                true;
#else
            false;



#endif
            int key = Math.Abs(cSharpCode.GetHashCode()
                               ^ OutputName.GetHashCode()
                               ^ IncludeDebugInformation.GetHashCode()
                               ^ typeof(Runtime).Assembly.FullName.GetHashCode()
                               ^ WindowsIdentity.GetCurrent().User.Value.GetHashCode() // just incase the user changes due to an apppool change
            );

            if (!CompileCSharpCodeAssemblies.ContainsKey(key))
            {
                CompilerParameters _CompilerParameters = new CompilerParameters
                {
                    IncludeDebugInformation = IncludeDebugInformation,
                    GenerateExecutable = false,
                    GenerateInMemory = false,
                    WarningLevel = 0,
                    TreatWarningsAsErrors = false
                };

                // it was unknown at them time of this writing 
                if (!string.IsNullOrWhiteSpace(CompileCSharpCodeDefaultOutDirectory))
                {
                    _CompilerParameters.OutputAssembly = string.Format(
                        "{0}\\{1}{2}.dll",
                        CompileCSharpCodeDefaultOutDirectory,
                        string.IsNullOrWhiteSpace(OutputName)
                            ? string.Empty
                            : string.Format("{0}.", FileSystem.CleanFileName(OutputName)),
                        Base36.Encode(key));

                    if (_CompilerParameters.OutputAssembly.Length > 250)
                        _CompilerParameters.OutputAssembly = string.Format("{0}\\{1}{2}.dll", CompileCSharpCodeDefaultOutDirectory, string.Empty, Base36.Encode(key));
                }

                if (File.Exists(_CompilerParameters.OutputAssembly))
                    CompileCSharpCodeAssemblies[key] = Assembly.LoadFile(_CompilerParameters.OutputAssembly);
                else
                {
                    // naming parts where specified above so the cSharpCode was no needed right away to calculate the base36 filename, now we actually need to cSharp code for it's primary purpose (to compile it)
                    if (string.IsNullOrWhiteSpace(cSharpCode))
                        cSharpCode = cSharpCodeFactory.Invoke();

                    lock (CSharpCodeProvider)
                    {
                        // Combine & normalize (different paths that load to the same dll) lists of referenced assemblies. Consider our custom list (UsingNamespaces.Values) & whatever is currently loaded into the AppDomain.This ensures the newly compiled object will have everything it needs.
                        Dictionary<string, string> ReferenceAssembliesDic = new Dictionary<string, string>();
                        foreach (string AppDomainAssemFileName in
                            AppDomain.CurrentDomain.GetAssemblies().Where(m => !m.IsDynamic).Select(m => m.Location))
                            if (File.Exists(AppDomainAssemFileName))
                                foreach (
                                    string DirectoryName in
                                    new[]
                                    {
                                        new FileInfo(AppDomainAssemFileName).DirectoryName,
                                        @".",
                                        @".\bin",
                                        @".\bin\debug",
                                        @".\bin\release"
                                    })
                                    if (Directory.Exists(DirectoryName))
                                        foreach (FileInfo _FileInfo in USING_NAMESPACES
                                            .Values
                                            .Select(FileName => String.Format(@"{0}\\{1}", DirectoryName, FileName))
                                            .Where(FilePath => File.Exists(FilePath))
                                            .Select(FilePath => new FileInfo(FilePath))
                                            .Where(_FileInfo => !ReferenceAssembliesDic.ContainsKey(_FileInfo.Name.ToLower())))
                                            ReferenceAssembliesDic[_FileInfo.Name.ToLower()] = _FileInfo.FullName;

                        _CompilerParameters.ReferencedAssemblies.AddRange(ReferenceAssembliesDic.Values.ToArray());

                        CompilerResults _CompilerResults = CSharpCodeProvider.CompileAssemblyFromSource(_CompilerParameters, cSharpCode);

                        if (_CompilerResults.Errors.Count == 0)
                        {
                            CompileCSharpCodeDefaultOutDirectory = Path.GetDirectoryName(_CompilerResults.PathToAssembly);
                            CompileCSharpCodeAssemblies[key] = _CompilerResults.CompiledAssembly;
                        } else
                        {
                            new DirectoryInfo(RequestPaths.GetPhysicalApplicationPath(FOLDER_FOR_COMPILE_TEMPORARY_FILES)).mkdir().Attributes = FileAttributes.Hidden;

                            int i = 0;
                            DirectoryInfo _DirectoryInfo = null;

                            // often there are processes that just won't let there handles go off previous files generated
                            // now we try delete those directories or create a new one with an auto-incremented number when the previous can't be removed
                            do
                            {
                                _DirectoryInfo = new DirectoryInfo(
                                    RequestPaths
                                        .GetPhysicalApplicationPath(FOLDER_FOR_COMPILE_TEMPORARY_FILES, string.Format("{0}_{1}", Base36.Encode(key), i++)));

                                try { _DirectoryInfo.rmdir(); } catch (Exception) {}
                            }
                            while (_DirectoryInfo.Exists);

                            _DirectoryInfo.mkdir();

                            _CompilerParameters.GenerateInMemory = false;
                            _CompilerParameters.IncludeDebugInformation = true;
                            _CompilerParameters.TempFiles = new TempFileCollection(_DirectoryInfo.FullName, true);
                            _CompilerParameters.TreatWarningsAsErrors = true;

                            CSharpCodeProvider.CompileAssemblyFromSource(_CompilerParameters, cSharpCode);

                            throw new Exception(string.Format("\"{0}\" Contains runtime the intermediate files of a runtime build (compile) that failed.", _DirectoryInfo.FullName));
                        }
                    }
                }
            }
            return CompileCSharpCodeAssemblies[key];
        }

        /// <summary>
        ///     takes the standard Xsd.exe /classes command example.cs output & customizes it to be a baseDoc for this solution
        /// </summary>
        /// <param name="DocTypeName"></param>
        /// <param name="cSharpCode"></param>
        /// <param name="PrimaryTypeParentType">default is typeof(BaseDoc)</param>
        /// <param name="PrimaryTypeAppliedInterfaces">default is IDocIdentifiers, if DocTypeName is DOCREV IDocRev also</param>
        /// <param name="SecondaryTypeParentType">default is typeof(BaseAutoIdent)</param>
        /// <param name="SecondaryTypesAppliedInterfaces">nothing is applied when null</param>
        /// <returns></returns>
        public static string CustomizeXsdToCSharpOutput(
            string DocTypeName,
            string cSharpCode,
            Type PrimaryTypeParentType = null,
            string[] PrimaryTypeAppliedInterfaces = null,
            Type SecondaryTypeParentType = null,
            string[] SecondaryTypesAppliedInterfaces = null)
        {
            #region Apply_Defaults

            /*
             BANDAID: DOCREV
             (search for this though out the code for more on DOCREV snafus in general),
             class inheritance should code customization should not be performed like this. This is specifically to cater toward IDocRev and nothing more.
            */
            if (PrimaryTypeParentType == null)
                PrimaryTypeParentType = typeof(BaseDoc);

            if (PrimaryTypeAppliedInterfaces == null || PrimaryTypeAppliedInterfaces.Length == 0)
                PrimaryTypeAppliedInterfaces = DocTypeName == "DOCREV"
                                                   ? new[]
                                                   {
                                                       typeof(IDocIdentifiers).Name,
                                                       (cSharpCode.Contains("TargetDocTypeFiles")
                                                            ? typeof(IDocRev).Name
                                                            : typeof(IDocRev).Name)
                                                   }
                                                   : new[]
                                                   {
                                                       typeof(IDocIdentifiers).Name
                                                   };

            string ApplyToPrimaryClass = string.Format("{0}", string.Join(", ", new[]
            {
                PrimaryTypeParentType.Name
            }.Union(PrimaryTypeAppliedInterfaces)));

            if (SecondaryTypeParentType == null)
                SecondaryTypeParentType = typeof(BaseAutoIdent);

            if (SecondaryTypesAppliedInterfaces == null || SecondaryTypesAppliedInterfaces.Length == 0)
                SecondaryTypesAppliedInterfaces = new string[]
                    { };

            string ApplyToSecondayClasses = string.Format("{0}", string.Join(", ", new[]
            {
                SecondaryTypeParentType.Name
            }.Union(SecondaryTypesAppliedInterfaces)));

            #endregion Apply_Defaults

            List<string> IgnoreDataTypePropertiesMapped = new List<string>
            {
                "byte",
                "System.Xml.XmlElement",
                "System.Xml.XmlElement"
            };
            List<string> IgnorePropertyPrefixes = new List<string>
            {
                "field", "calc", "tmp", "signatures1", "signatures2"
            };

            // add a Nullable variable type for each of the IgnoreDataTypePropertiesMapped defined
            foreach (string type in IgnoreDataTypePropertiesMapped.ToArray())
            {
                IgnoreDataTypePropertiesMapped.Add(string.Format("System.Nullable<{0}>", type));
                IgnoreDataTypePropertiesMapped.Add(string.Format("System.Nullable<{0}>[]", type));
                IgnoreDataTypePropertiesMapped.Add(string.Format("{0}[]", type));
            }

            // convert aliases specifically for byte arrays as the DocRev interface will be looking for byte[]; not Byte90
            cSharpCode = Regex.Replace(
                cSharpCode,
                @"Byte\[\]",
                @"byte[]");

            // apply DataMember attributes to properties in an attempt to make them more readable to developer
            // effects of these attributes do not cross over to InfoPath's primary serializer (XmlSerializer)
            cSharpCode = Regex.Replace(
                cSharpCode,
                @"([\n\r\s]+)(public)(\s+[^\s]+\s+)(\w+)([\n\r\s]+)(\{)([\n\r\s]+)(get)([\n\r\s]+)(\{)",
                match => string.Format(
                    "{0}[DataMember(Name = \"{1}\", EmitDefaultValue = false)]{2}",
                    match.Groups[1].Value, /* 0 */
                    StringTransform.PrettyCSharpIdent(match.Groups[4].Value), /* 1 */
                    match.Value), /* 3 */
                RegexOptions.IgnoreCase);

            // Stripe out legacy soap 1.2 "bool specified" properties
            // Ref: http://weblog.west-wind.com/posts/2007/Dec/13/Generated-Date-Types-in-WCF-and-unexpected-dateSpecified-fields
            cSharpCode = Regex.Replace(
                cSharpCode,
                @"((\[.*\])\s+)+public bool \w+Specified\s+\{(\s+(get|set)\s+\{\s+.*\s+\})+\s+\}\s*",
                string.Empty,
                RegexOptions.IgnoreCase);

            cSharpCode = Regex.Replace(
                cSharpCode,
                @"\s+private\s+bool \w+FieldSpecified;",
                string.Empty,
                RegexOptions.IgnoreCase);

            // TODO: Apply against class types found that are not a form type, this will force ignore for anything InfoPath Designer that is not a simple datatype
            /* Strip out XMLSerialization attributes & Signatures### properties as they seem to screw up some IIS WCF hosted services because there data type of XmlElement is not serializable */
            cSharpCode = Regex.Replace(
                cSharpCode,
                @"private.*anyAttrField;.*",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

            cSharpCode = Regex.Replace(
                cSharpCode,
                @"\[System\.Xml\.Serialization\.XmlAnyAttributeAttribute\(\)\]",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

            cSharpCode = Regex.Replace(
                cSharpCode,
                @"\[System\.Xml\.Serialization\.XmlAnyAttributeAttribute\(\)\](\s+\[[^]\n\r]+\])*([^}]+\}){3,3}", "",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

            cSharpCode = Regex.Replace(
                cSharpCode,
                @".*EmitDefaultValue\s*=\s*false\s*\)\]\s*public\s+System\.Xml\.XmlAttribute\[\]\s+AnyAttr\s*{\s*get\s*{\s*return\s*this\.anyAttrField;\s*}\s*set\s*{\s*this\.anyAttrField\s*=\s*value;\s*}\s*}",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

            string[] DocTypeNames =
            {
                DocTypeName
            };

            // Make sure encapsulated private properties are excluded as DataMembers
            cSharpCode = Regex.Replace(
                cSharpCode,
                @"([ ]+)(private\s+[^\s]+\s+\w+;)",
                "\r\n$1[IgnoreDataMember]\r\n$1$2");

            // add class attributes: DataContract for WCF
            // make classes inheritors of BaseDoc (the class representing the root object for the doc) or BaseAutoIdent (a supporting child class usually spawned for repeating elements in the main document)
            cSharpCode = Regex.Replace(
                cSharpCode,
                /**1******2***********************************************3*****************4*************5*******6***************************************7*********/
                @"(\s+\[)(System\.Xml\.Serialization\.XmlRootAttribute\(|System\.Xml\.Serialization\.XmlTypeAttribute\()(\""[\w]+\"",\s+)?(Namespace="")([^""]*)([^\]]+\]\s+public\s+partial\s+class\s+)([\w_\d]+)",
                match => string.Format(
                    "{1}DataContract(Name = \"{3}\", Namespace = \"{2}\")]{0} : {4}",
                    /*the entire match undivided*/
                    match.Value,
                    /*left square bracket with leading white-spaces*/
                    match.Groups[1].Value,
                    /*xml namespace URI*/
                    match.Groups[5].Value,
                    /*
Title-casing with underscore separate words
are applied to generate a prettier SQL SSMS Object Explorer node-set
& WCF Client proxy API that Visual Studio generates for
external applications that will consume our service.
With exception to the main/root document objects/pocos,
this is applied to all classes.
*/
                    DocTypeName == match.Groups[7].Value
                        ? match.Groups[7].Value
                        : DocTypeName + "_" + StringTransform.PrettyCSharpIdent(
                              string.IsNullOrWhiteSpace(match.Groups[3].Value)
                                  ? match.Groups[7].Value
                                  : match.Groups[3].Value.Trim(',', ' ', '"')),
                    DocTypeName == match.Groups[7].Value
                        ? ApplyToPrimaryClass
                        : ApplyToSecondayClasses),
                RegexOptions.IgnoreCase);

            /* add a parameter-less constructor to each class specifically to support XmlSerialization operations anticipated to run against the objects compiled from the code this is generating */
            //TODO:combine this RegEx replacement logic with the one directly above (the one that is decorating class attributes */
            cSharpCode = Regex.Replace(
                cSharpCode,
                @"(?<LeadingWhitespace>\s+)(?<ClassModifiers>public\s+partial\s+class\s+)(?<ClassName>[\w_\d]+)(?<InheritanceAndOpenClassBodyCurlyBrace>[\w_\d \{:,]+)",
                match => string.Format(
                    "{0}\n{1}public {2}() {{}}",
                    match.Value,
                    match.Groups["LeadingWhitespace"].Value,
                    match.Groups["ClassName"].Value),
                RegexOptions.IgnoreCase);

            // convert array properties to generic List
            cSharpCode = Regex.Replace(
                cSharpCode,
                /**1****2***************3****4***********5*****6****7*****/
                @"(\s+)(public|private)(\s+)([\.<>\w_]+)(\[\])(\s+)([^\s]+)",
                match => string.Format(
                    (IgnoreDataTypePropertiesMapped.Any(
                         m => m == match.Groups[4].Value || Regex.IsMatch(match.Groups[4].Value, @"signatures\d+"))
                         ? "{0}"
                         : "{1}{2}{3}List<{4}>{6}{7}"),
                    match.Value,
                    match.Groups[1].Value,
                    match.Groups[2].Value,
                    match.Groups[3].Value,
                    match.Groups[4].Value,
                    match.Groups[5].Value,
                    match.Groups[6].Value,
                    match.Groups[7].Value),
                RegexOptions.IgnoreCase);

            // ensure XmlElement types are ignore (mainly by binary serializer)
            cSharpCode = cSharpCode.Replace("private System.Xml.XmlElement[]", "[System.NonSerialized] private System.Xml.XmlElement[]");

            // add using statements to beginning to top of the document
            cSharpCode = string.Join("", USING_NAMESPACES.Keys.OrderBy(ns => ns).Select(ns => string.Format("using {0};\n", ns))) + cSharpCode;

            return cSharpCode;
        }

        public static BaseDoc FindBaseDoc(Assembly BaseDocAssembly, string DocTypeName) { return (BaseDoc) Activator.CreateInstance(FindBaseDocType(BaseDocAssembly, DocTypeName)); }
        public static Type FindBaseDocType(Assembly BaseDocAssembly, string DocTypeName) { return BaseDocAssembly.GetTypes().First(m => m.Name == DocTypeName); }

        internal static string GenerateCode(string[] DocXsds, string DocTypeName, string DocRev, params string[] AdditionalRootNames)
        {
            string cSharpNameSpace = RuntimeTypeNamer.CalcCSharpNamespace(DocTypeName, DocRev, AdditionalRootNames);
            string myclasses_cs = new Xsd().ImportSchemasAsClasses(
                DocXsds,
                cSharpNameSpace,
                CodeGenerationOptions.GenerateOrder | CodeGenerationOptions.GenerateProperties,
                new StringCollection());

            return CustomizeXsdToCSharpOutput(DocTypeName, myclasses_cs);
        }

        /// <summary>
        ///     Utilizes .Net API that drives XSD.EXE to auto-gen cSharp code that is further modified to suite this solution &
        ///     finally runtime compiled to yield another BaseDoc. Unix guys might think of this as a SED script.
        /// </summary>
        /// <param name="DocXsd"></param>
        /// <param name="DocTypeName"></param>
        /// <param name="DocRev"></param>
        /// <param name="cSharpNameSpace"></param>
        /// <returns></returns>
        internal static Assembly MakeBaseDocAssembly(string[] DocXsds, string DocTypeName, string DocRev, params string[] AdditionalRootNames)
        {
            return CompileCSharpCode(() =>
                                         GenerateCode(DocXsds, DocTypeName, DocRev, AdditionalRootNames),
                RuntimeTypeNamer.CalcCSharpNamespace(DocTypeName, DocRev, AdditionalRootNames));
        }
    }
}