using System;
using System.IO;
using System.Linq;
using System.Web;
using Rudine.Template.Filesystem;
using Rudine.Util;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine.Template
{
    /// <summary>
    ///     Handles file content requests on a per DocTypeName & DocRev basis. An orderly search of the local file system
    ///     (DirectoryPath/{DocTypeName}/*.*), the DOCDB datastore (LuceneController) and lastly any other
    ///     IDocResourceController in memory (Rudine.Forms.DocResourceController from it's embedded contents) are searched.
    ///     First one to return something wins.
    /// </summary>
    public class TemplateController : ITemplateController
    {
        /// <summary>
        ///     when this file is requested the entire contents of the given form/doctypename/docrev/*.* will be compressed & sent
        ///     down the wire if "doc/doctypename/docrev/mycontents.cab" is specified
        /// </summary>
        public const string FOLDER_CONTENTS_VIRTUAL_CAB_FILE = "mycontents.cab";

        private static readonly Lazy<TemplateController> _Instance = new Lazy<TemplateController>(() => new TemplateController());

        /// <summary>
        ///     the default controller that will be queried first for the TopDoc
        /// </summary>
        private static readonly FilesystemTemplateController _DefaultTopDocFilesystemTemplateController = new FilesystemTemplateController();

        private static ITemplateController[] _OtherIDocResourceControllers;

        /// <summary>
        ///     Seeds the underlying datastore with a single DocRev document if it does not exist. Without this document not would
        ///     exist.  Scans current AppDomain for BaseDoc instances & fabricates a form/* folder with what is now the emerging
        ///     technique
        ///     of serving the forms, the JsonInterpreter.
        ///     //TODO:Require a custom class attribute to direct this on how to process it
        /// </summary>
        private TemplateController()
        {
            Reflection.LoadBinDlls();

            // locate other instances of IDocResourceControllers available for fallback
            _OtherIDocResourceControllers = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(_Assembly => _Assembly.GetTypes(), (_Assembly, _Type) => new
                {
                    _Assembly,
                    _Type
                })
                .Where(t => t._Type != GetType())
                .Where(t => !t._Type.IsInterface)
                .Where(t => t._Type.GetInterfaces().Any(i => i == typeof(ITemplateController)))
                .Select(t => ((ITemplateController) Activator.CreateInstance(t._Type)))
                .ToArray();
        }

        /// <summary>
        ///     singleton instance safe for multithreading
        /// </summary>
        public static TemplateController Instance => _Instance.Value;

        /// <summary>
        ///     Checks contents processed & persisted by ImportInfoPathXsnContents to find the files first. Goes to the
        ///     ~/forms/[DocTypeName] directory if nothing was found in the previous. If the physical disk-based folder yields the
        ///     requested contents, that content will be of whatever DocRev stored there.
        /// </summary>
        /// <param name="DocTypeName"></param>
        /// <param name="DocTypeVer"></param>
        /// <param name="filename"></param>
        /// <returns>stream of file requested or it's head version if the requested version can't be found</returns>
        public MemoryStream OpenRead(string DocTypeName, string DocTypeVer, string filename)
        {
            MemoryStream _MemoryStream = null;

            foreach (ITemplateController _OtherIDocResourceController in _OtherIDocResourceControllers)
                if (_MemoryStream != null)
                    break;
                else
                    _MemoryStream = _OtherIDocResourceController.OpenRead(DocTypeName, DocTypeVer, filename);
            return _MemoryStream;
        }

        public string TopDocRev(string DocTypeName) =>
            TopDocRev(DocTypeName, false);

        public static string GetHttpContextFileName(HttpContext context) =>
            context.Request.Url.Segments[context.Request.Url.Segments.Length - 1].Trim('/');

        /// <summary>
        ///     searches other appdomain assemblies for IDocResourceControllers to ask them if they have anything
        /// </summary>
        /// <param name="DocTypeName"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public MemoryStream OpenRead(string DocTypeName, string filename) =>
            OpenRead(DocTypeName, TopDocRev(DocTypeName), filename);

        public MemoryStream OpenRead(HttpContext context, out TemplateFileInfo templatefileinfo)
        {
            templatefileinfo = ParseTemplateFileInfo(context);

            return OpenRead(templatefileinfo.DocTypeName, templatefileinfo.solutionVersion, templatefileinfo.FileName);
        }

        public TemplateFileInfo ParseTemplateFileInfo(HttpContext context) =>
            new TemplateFileInfo
            {
                FileName = GetHttpContextFileName(context),
                solutionVersion = context.Request.Url.Segments[context.Request.Url.Segments.Length - 2].Trim('/'),
                DocTypeName = context.Request.Url.Segments[context.Request.Url.Segments.Length - 3].Trim('/')
            };

        public string OpenText(string DocTypeName, string filename) =>
            OpenText(
                DocTypeName,
                TopDocRev(DocTypeName),
                filename);

        public string OpenText(string DocTypeName, string DocTypeVer, string filename) =>
            CacheMan.Cache(() =>
                           {
                               using (MemoryStream _MemoryStream = OpenRead(DocTypeName, DocTypeVer, filename))
                                   return _MemoryStream.AsString();
                           }, false, "OpenText", DocTypeName, DocTypeVer, filename);

        public string OpenText(HttpContext context, out string filename)
        {
            filename = GetHttpContextFileName(context);
            return CacheMan.Cache(() =>
                                  {
                                      TemplateFileInfo r;
                                      using (MemoryStream _MemoryStream = OpenRead(context, out r))
                                          return _MemoryStream.AsString();
                                  }, false, "OpenText", context.Request.Url.ToString());
        }

        /// <summary>
        ///     Reads the DocRev from the local AppDomain working_folder\form\*. When nothing is found the Docdb store for the most
        ///     current DocRev. The first item in descending string order.
        /// </summary>
        /// <param name="DocTypeName"></param>
        /// <returns>string.Empty if nothing is found</returns>
        public string TopDocRev(string DocTypeName, bool forceRefresh) =>
            CacheMan.Cache(() =>
                               DocTypeName.Equals(DocRev.MyOnlyDocName, StringComparison.CurrentCultureIgnoreCase)
                                   ? DocRev.MyOnlyDocVersion.ToString()
                                   : _DefaultTopDocFilesystemTemplateController.TopDocRev(DocTypeName)
                                     ?? _OtherIDocResourceControllers
                                         //DOCREVs should always come from the embedded controller
                                         .Select(m => m.TopDocRev(DocTypeName))
                                         .Where(DocRev => !string.IsNullOrWhiteSpace(DocRev))
                                         .OrderByDescending(DocRev => new Version(DocRev))
                                         .ToArray()
                                         .FirstOrDefault(),
                forceRefresh,
                "TopDocRev",
                DocTypeName);
    }
}