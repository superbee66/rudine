using System;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using Rudine.Exceptions;
using Rudine.Template;
using Rudine.Web;
using Rudine.Web.Util;

namespace Rudine
{
    /// <summary>
    ///     Closes the DocExchange ServiceHost & clears all MemoryCache when any changes occur in the
    ///     ImporterController.DirectoryPath. The files within that directory influence  This is similar behavior at
    ///     someone editing files in the IIS App_Code directory triggering IIS to recompile. the ServiceKnownType list for our
    ///     ServiceHost.
    /// </summary>
    internal class DocExchangeServiceHostFactory : ServiceHostFactory
    {
        /// <summary>
        ///     directory that will be created if not existing then watched. non-app_code directories are recursively watched
        /// </summary>
        private static readonly FileSystemWatcher[] _FileSystemWatchers =
            new[]
                {
                    ImporterController.DirectoryPath,
                    Directory.Exists(RequestPaths.GetPhysicalApplicationPath("App_Code"))
                        ? RequestPaths.GetPhysicalApplicationPath("App_Code")
                        : string.Empty
                }
                .Where(Directory.Exists)
                .Select(path =>
                            new FileSystemWatcher(new DirectoryInfo(path).mkdir().FullName)
                            {
                                EnableRaisingEvents = false,
                                IncludeSubdirectories = true,
                                NotifyFilter = NotifyFilters.CreationTime |
                                               NotifyFilters.DirectoryName |
                                               NotifyFilters.FileName |
                                               NotifyFilters.LastWrite |
                                               NotifyFilters.Size,
                                Filter = "*.*"
                            }).ToArray();

        /// <summary>
        ///     tell filesystemwatcher to start monitoring & adds the Reset event listener in order to react to those events
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="baseAddresses"></param>
        /// <returns></returns>
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            ServiceHost _ServiceHost = base.CreateServiceHost(serviceType, baseAddresses);

            foreach (ServiceMetadataBehavior behavior in _ServiceHost.Description.Behaviors.OfType<ServiceMetadataBehavior>())
                behavior.MetadataExporter = new DocExchangeWsdlExporter();

            foreach (FileSystemWatcher _FileSystemWatcher in _FileSystemWatchers)
            {
                _FileSystemWatcher.Changed += (o, args) => Reset(_ServiceHost);
                _FileSystemWatcher.Created += (o, args) => Reset(_ServiceHost);
                _FileSystemWatcher.Deleted += (o, args) => Reset(_ServiceHost);
                _FileSystemWatcher.Renamed += (o, args) => Reset(_ServiceHost);
                // reset when new DocRev(s) are submitted also
                DocExchange.AfterSubmit += (o, args) =>
                {
                    if (!string.IsNullOrEmpty(args.LightDoc.GetTargetDocVer()) 
                        && TemplateController.Instance.TopDocRev(args.LightDoc.GetTargetDocName(), true) == args.LightDoc.GetTargetDocVer())
                        Reset(_ServiceHost);
                };

                _FileSystemWatcher.EnableRaisingEvents = true;
            }

            return _ServiceHost;
        }

        /// <summary>
        ///     force servicehost to rebuild & clear out all caches when something changes on the filesystem
        /// </summary>
        /// <param name="_FileSystemWatcher">Disposes of</param>
        /// <param name="_ServiceHost">Closes</param>
        private static void Reset(ICommunicationObject _ServiceHost)
        {
            // don't need to listen to events now that it's known something has changed, this will be Enabled again with someone taps the CreateServiceHost
            foreach (FileSystemWatcher _FileSystemWatcher in _FileSystemWatchers)
                _FileSystemWatcher.EnableRaisingEvents = false;

            if (_ServiceHost.State != CommunicationState.Closed)
                if (_ServiceHost.State != CommunicationState.Closing)
                    if (_ServiceHost.State != CommunicationState.Faulted)
                        _ServiceHost.Close();

            CacheMan.Clear();
        }
    }
}