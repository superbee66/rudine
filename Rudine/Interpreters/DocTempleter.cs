using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Rudine.Template.Filesystem;
using Rudine.Util.Types;
using Rudine.Util.Xsds;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine.Interpreters
{
    public static class DocTempleter
    {
        /// <summary>
        /// </summary>
        /// <param name="_DocDirectoryInfo"></param>
        /// <param name="DocTypeName"></param>
        /// <param name="DocProperties"></param>
        /// <param name="DocRev"></param>
        /// <returns>TODO:Needs to return a DocRev & not write files to a physical directory</returns>
        public static BaseDoc Templify(string DocTypeName, List<CompositeProperty> DocProperties, string DocRev = null)
        {
            DirectoryInfo _DocDirectoryInfo =
                new DirectoryInfo(FilesystemTemplateController.GetDocDirectoryPath(DocTypeName)).mkdir();

            if (string.IsNullOrWhiteSpace(DocRev))
                DocRev = DateTime.UtcNow.AsDocRev();

            string temporaryNamespace = RuntimeTypeNamer.CalcCSharpNamespace(DocTypeName, DocRev, typeof(DocTempleter).Name);
            string cSharpClassFullName = RuntimeTypeNamer.CalcCSharpNamespace(DocTypeName, DocRev);

            FileInfo _XsdFileInfo = new FileInfo(String.Format(@"{0}\{1}", _DocDirectoryInfo.FullName, Runtime.MYSCHEMA_XSD_FILE_NAME));
            Type _template_docx_type = new CompositeType(temporaryNamespace, _DocDirectoryInfo.Name, DocProperties.ToArray());

            // the "lazy-load" CompositeType requires activation in order for the _template_docx_obj.GetType().Assembly to register as having any types defined
            object _template_docx_obj = Activator.CreateInstance(_template_docx_type);

            string xsd = XsdExporter.ExportSchemas(
                _template_docx_obj.GetType().Assembly,
                new List<string> { DocTypeName },
                RuntimeTypeNamer.CalcSchemaUri(DocTypeName, DocRev)).First();

            File.WriteAllText(_XsdFileInfo.FullName, xsd, Encoding.Unicode);

            string myclasses_cs = new Xsd().ImportSchemasAsClasses(
                new[] { xsd },
                cSharpClassFullName,
                CodeGenerationOptions.GenerateOrder | CodeGenerationOptions.GenerateProperties,
                new StringCollection());

            myclasses_cs = Runtime.CustomizeXsdToCSharpOutput(DocTypeName, myclasses_cs, null, new[] { "IDocModel" });

            if (Directory.Exists(RequestPaths.GetPhysicalApplicationPath("App_Code")))
                File.WriteAllText(RequestPaths.GetPhysicalApplicationPath("App_Code", DocTypeName + ".cs"), myclasses_cs, Encoding.Unicode);
            else
                File.WriteAllText(_DocDirectoryInfo.FullName + @"\\example.cs", myclasses_cs, Encoding.Unicode);

            BaseDoc _BaseDoc = Runtime.FindBaseDoc(Runtime.CompileCSharpCode(myclasses_cs), DocTypeName);

            // reset the values critical to this import that were implicitly set by the Rand()
            _BaseDoc.solutionVersion = DocRev;
            _BaseDoc.DocTypeName = DocTypeName;
            _BaseDoc.href = String.Empty;

            return _BaseDoc;
        }
    }
}