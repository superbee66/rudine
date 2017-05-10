using System.Collections.Generic;
using System.ServiceModel;
using Rudine.Web.Util;

namespace Rudine.Web
{
    [ServiceContract]
    public interface IBaseDocTemplateBuilder
    {
        /// <summary>
        ///     Original files that would support making DocRev from. Most likely, one file will have an extension matching listed
        ///     by Interpreters()
        /// </summary>
        /// <param name="Files"></param>
        /// <returns></returns>
        [OperationContract]
        DocRev CreateTemplate(List<Rudine.Web.DocRevEntry> docFiles, string docTypeName = null, string docRev = null, string schemaXml = null, List<CompositeProperty> schemaFields = null);

        /// <summary>
        /// file/mime information that are likely canidates for CreateTemplate calls
        /// </summary>
        [OperationContract]
        List<ContentInfo> TemplateSources();
    }
}