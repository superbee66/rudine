using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.ServiceModel.Description;
using System.Xml.Schema;
using Rudine.Template;
using Rudine.Template.Filesystem;
using Rudine.Util;
using Rudine.Web.Util;

namespace Rudine
{
    /// <summary>
    ///     loads datacontract metadata by creating it on the fly directly from the doctypes discovered; ops to load directly
    ///     from the persisted datacontract.xsd located in doc/*/datacontract.xsd if found avoiding runtime type loading as
    ///     that is very expensive
    /// </summary>
    internal class DocExchangeWsdlExporter : WsdlExporter
    {
        /// <summary>
        ///     first response performance critical
        /// </summary>
        /// <returns></returns>
        public override MetadataSet GetGeneratedMetadata()
        {
            ImporterController.TryDocRevImporting();

            int i = 0;
            // let the base build up metadata for the service contracts as these are static
            MetadataSet _MetadataSet = base.GetGeneratedMetadata();

            foreach (DirectoryInfo _DirectoryInfo in Directory
                .EnumerateDirectories(FilesystemTemplateController.DirectoryPath)
                .Select(path => new DirectoryInfo(path)))
            {
                // persist the datacontract xml schema for the datacontract to the user's temporary directory
                //TODO:combine this logic and Runtime's that calculates it's dll output location
                int key = Math.Abs(File.ReadAllText(
                                       RequestPaths.GetPhysicalApplicationPath(
                                           "doc",
                                           _DirectoryInfo.Name,
                                           Runtime.MYSCHEMA_XSD_FILE_NAME)).GetHashCode()
                                   ^ WindowsIdentity.GetCurrent().User.Value.GetHashCode()); // just incase the user changes due to an apppool change

                string tempDataContractXsdPath = string.Format("{0}\\{1}.xsd", Path.GetTempPath(), Base36.Encode(key));

                if (!File.Exists(tempDataContractXsdPath))
                {
                    // the datacontracts are the things that are dynamic & change according to what DocTypes are present
                    XsdDataContractExporter _XsdDataContractExporter = new XsdDataContractExporter();
                    Type DocType = Runtime.ActivateBaseDocType(_DirectoryInfo.Name, TemplateController.Instance.TopDocRev(_DirectoryInfo.Name));
                    _XsdDataContractExporter.Export(DocType);
                    // _XsdDataContractExporter.Schemas.CompilationSettings.EnableUpaCheck = true;
                    _XsdDataContractExporter.Schemas.Compile();

                    foreach (XmlSchema _XmlSchema in _XsdDataContractExporter.Schemas.Schemas(_XsdDataContractExporter.GetRootElementName(DocType).Namespace))
                        using (Stream _Stream = File.OpenWrite(tempDataContractXsdPath))
                        {
                            _MetadataSet.MetadataSections.Add(MetadataSection.CreateFromSchema(_XmlSchema));
                            _XmlSchema.Write(_Stream);
                            break;
                        }
                }

                using (Stream _Stream = File.OpenRead(tempDataContractXsdPath))
                    _MetadataSet.MetadataSections.Add(MetadataSection.CreateFromSchema(
                        XmlSchema.Read(_Stream, (s, o) =>
                                                {
                                                    /*if (o != null && o.Exception != null) throw o.Exception;*/
                                                })));
            }

            return _MetadataSet;
        }
    }
}