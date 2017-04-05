using System;
using System.ServiceModel;
using System.Web;

namespace Rudine.Web.Util
{
    /// <summary>
    ///     Resolves what the quasi ApplicationPath of the current request
    ///     is for standard HttpContext requests & WCF over IIS requests
    /// </summary>
    public static class RequestPaths
    {
        public static string AbsoluteUri
        {
            get
            {
                // Needs to operate on regular request & service requests
                return
                    HttpContext.Current != null && HttpContext.Current.Handler != null
                        ? (HttpContext.Current.Request.Url.AbsoluteUri == "/" ? "" : HttpContext.Current.Request.Url.AbsoluteUri)
                        : OperationContext.Current != null && OperationContext.Current.RequestContext != null
                            ? OperationContext.Current.RequestContext.RequestMessage.Headers.To.AbsoluteUri
                            : "http://localhost";
            }
        }

        public static string ApplicationPath
        {
            get
            {
                // Needs to operate on regular request & service requests
                return HttpContext.Current != null && HttpContext.Current.Handler != null ?
                           (HttpContext.Current.Request.ApplicationPath == "/" ? "" : HttpContext.Current.Request.ApplicationPath) :
                           OperationContext.Current != null ?
                               OperationContext.Current.RequestContext.RequestMessage.Headers.To.AbsoluteUri.Substring(0,
                                   OperationContext.Current.RequestContext.RequestMessage.Headers.To.AbsoluteUri.LastIndexOf('/')) :
                               PhysicalApplicationPath.Replace('\\', '/');
            }
        }

        /// <summary>
        ///     exmaple C:\inetpub\wwwroot\myFolder
        ///     --->
        ///     HttpContext.Current.Request.PhysicalApplicationPath
        ///     -or-
        ///     AppDomain.CurrentDomain.BaseDirectory
        /// </summary>
        public static string PhysicalApplicationPath
        {
            get
            {
                return
                    HttpContext.Current != null && HttpContext.Current.Handler != null ?
                        HttpContext.Current.Request.PhysicalApplicationPath :
                        AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        public static string GetPhysicalApplicationPath(params string[] name)
        {
            return string.Format(
                @"{0}\{1}",
                PhysicalApplicationPath.TrimEnd('\\'),
                string.Join(@"\", name).TrimEnd('\\'));
        }
    }
}