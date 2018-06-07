using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Design;
using System.Data.Metadata.Edm;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.TextTemplating;

namespace dCForm.Core.Storage.Sql.Reverser
{
    /// <summary>
    /// EFCF = Entity Framework Code First
    /// </summary>
    internal class TextTemplatingEngineHost : ITextTemplatingEngineHost
    {
        public EntityContainer EntityContainer { get; set; }
        public Version EntityFrameworkVersion { get; set; }
        public EntityType EntityType { get; set; }
        public Dictionary<AssociationType, Tuple<EntitySet, Dictionary<RelationshipEndMember, Dictionary<EdmMember, string>>>> ManyToManyMappings { get; set; }
        public string MappingNamespace { get; set; }
        public string ModelsNamespace { get; set; }
        public string Namespace { get; set; }
        public Dictionary<EdmProperty, EdmProperty> PropertyToColumnMappings { get; set; }
        public EntitySet TableSet { get; set; }

        #region T4 plumbing

        public CompilerErrorCollection Errors { get; set; }
        public string FileExtension { get; set; }
        public Encoding OutputEncoding { get; set; }
        public string TemplateFile { get; set; }

        public virtual string ResolveAssemblyReference(string assemblyReference)
        {
            if (File.Exists(assemblyReference))
                return assemblyReference;

            try
            {
                // TODO: This is failing to resolve partial assembly names (e.g. "System.Xml")
                var assembly = Assembly.Load(assemblyReference);

                if (assembly != null)
                    return assembly.Location;
            }
            catch (FileNotFoundException) { }
            catch (FileLoadException) { }
            catch (BadImageFormatException) { }

            return string.Empty;
        }

        IList<string> ITextTemplatingEngineHost.StandardAssemblyReferences {
            get {
                return new[]
                {
                    Assembly.GetExecutingAssembly().Location,
                    typeof (Uri).Assembly.Location,
                    typeof (Enumerable).Assembly.Location,
                    typeof (AcceptRejectRule).Assembly.Location,
                    typeof (EdmToObjectNamespaceMap).Assembly.Location,
                    typeof (ConformanceLevel).Assembly.Location,
                    typeof (Extensions).Assembly.Location
                };
            }
        }

        IList<string> ITextTemplatingEngineHost.StandardImports {
            get {
                return new[]
                {
                    "System",
                    "Microsoft.DbContextPackage.Utilities"
                };
            }
        }

        object ITextTemplatingEngineHost.GetHostOption(string optionName)
        {
            if (optionName == "CacheAssemblies")
                return 1;

            return null;
        }

        bool ITextTemplatingEngineHost.LoadIncludeText(string requestFileName, out string content, out string location)
        {
            location = string.Empty;
            content = string.Empty;

            return false;
        }

        void ITextTemplatingEngineHost.LogErrors(CompilerErrorCollection errors) { Errors = errors; }

        AppDomain ITextTemplatingEngineHost.ProvideTemplatingAppDomain(string content) { return AppDomain.CurrentDomain; }

        Type ITextTemplatingEngineHost.ResolveDirectiveProcessor(string processorName) { throw new Exception("Error.UnknownDirectiveProcessor(processorName)"); }

        string ITextTemplatingEngineHost.ResolveParameterValue(string directiveId, string processorName, string parameterName) { return string.Empty; }

        string ITextTemplatingEngineHost.ResolvePath(string path)
        {
            if (!Path.IsPathRooted(path) && Path.IsPathRooted(TemplateFile))
                return Path.Combine(Path.GetDirectoryName(TemplateFile), path);

            return path;
        }

        void ITextTemplatingEngineHost.SetFileExtension(string extension) { FileExtension = extension; }

        void ITextTemplatingEngineHost.SetOutputEncoding(Encoding encoding, bool fromOutputDirective) { OutputEncoding = encoding; }

        #endregion
    }
}