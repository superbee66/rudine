using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Rudine.Web.Util;

namespace Rudine.Web
{
    /// <summary>
    ///     Rudine Relay
    ///     Forwards requests for InfoPath form content files & the form XML itself
    ///     to the principle service. This facilities the common
    ///     WWW -> WCF -> DB
    ///     tier pattern utilized by many enterprises
    /// </summary>
    public class ReverseProxy : IHttpHandler
    {
        public static readonly string DirectoryName = typeof(ReverseProxy).Name;

        /// <summary>
        ///     Parses service URL from anywhere in a config file by looking for the IPB.svc or DocExchange.svc filename
        /// </summary>
        private static readonly Regex FormHandlerUrlParse = new Regex(@"(?<="")(.*)(?=/(DocExchange)\.svc"")", RegexOptions.IgnoreCase);

        /// <summary>
        ///     Service URL straight from IPB folder specific configuration
        /// </summary>
        private static string FormHandlerUrl;

        /// <summary>
        ///     Empty means we need to go look for it, null means we didn't find it
        /// </summary>
        private static string _GetRelayUrlFound = string.Empty;

        /// <summary>
        ///     URL path to the virtual directory running this
        /// </summary>
        private string baseAbsoluteUri;

        public bool IsReusable {
            get { return false; }
        }

        /// <summary>
        ///     Forwards GET & HEAD requests to back-end server hosting the WCF & content files
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            if (context.Request.HttpMethod != "GET" && context.Request.HttpMethod != "HEAD")
            {
                context.Response.ClearHeaders();
                context.Response.ClearContent();
                context.Response.Status = "405 Method Not Allowed";
                context.Response.StatusCode = 405;
                context.Response.StatusDescription = string.Format("A request was made of a resource using a request method not supported by that resource; {0} supports on HEAD & GET verbs.", DirectoryName);
                context.Response.TrySkipIisCustomErrors = true;
            }
            else
            {
                //EXAMPLE:http://theWebServer.progablab.com/Rudine/Dev/Service
                string relay_svc_app_url = ResolveFormHandlerUrl();

                //EXAMPLE:http://theWebServer.progablab.com/applicationDir/SubDir
                baseAbsoluteUri =
                    string.Format("{0}/{1}", context.Request.Url.AbsoluteUri.Replace(
                        context.Request.Url.AbsoluteUri.Substring(
                            context.Request.Url.AbsoluteUri.IndexOf(
                                context.Request.ApplicationPath,
                                StringComparison.CurrentCultureIgnoreCase) + context.Request.ApplicationPath.Length)
                        , string.Empty), DirectoryName); //TODO:ReverseProxy folder dynamically gleamed

                // TODO:Update example, the ashx is no longer present
                // http://theWebServer.progablab.com/Rudine/Dev/Service/DocDataHandler.ashx?FormName=SomeDocumentName1512A&MEMBER_NAME=WU9VU0VGIEFMS0FSS0hJ&LOCATION_OF_MEETING=Ly9UT0RPOkpBRz8gQWxzbywgaXMgdGhlIGRhdGUgdGhlIGRheSB0aGV5IGRvd25sb2FkZWQ_&DATE=MTEvMTcvMjAxNCA0OjAyOjM5IFBN&DocId=MGQyU2dxMUhvY1VfNGU5dTdsdXM3WVRSdi1yTDk4TEItb1o0VnhuVFNvWi1lX3MtS2EtM1RNSnJKcV8wbUxnTG55T0l2bXUtdkpzVF9Ydmp1Zm1MOGclM2QlM2Q$&FormListener=http%3a%2f%2ftheWebServer.progablab.com%2fSubDir%2fSubDir
                string _HttpWebRequest_Url =
                    relay_svc_app_url
                    + context.Request.Url.AbsoluteUri.Substring(
                        context.Request.Url.AbsoluteUri.ToLower().IndexOf(context.Request.ApplicationPath.ToLower()) + (context.Request.ApplicationPath + "/" + DirectoryName).Length);

                // the relay is not located on the a client box, the relay is hosted threw the same application as the core itself
                if (_HttpWebRequest_Url.StartsWith("/"))
                    _HttpWebRequest_Url = string.Format("{0}{1}", baseAbsoluteUri.Substring(0, baseAbsoluteUri.Length - DirectoryName.Length).TrimEnd('/'), _HttpWebRequest_Url);

                HttpWebRequest _HttpWebRequest = (HttpWebRequest)WebRequest.Create(_HttpWebRequest_Url);
                _HttpWebRequest.Proxy = null;
                _HttpWebRequest.AllowAutoRedirect = true;
                _HttpWebRequest.Referer = context.Request.Url.AbsoluteUri;

                using (HttpWebResponse _HttpWebResponse = (HttpWebResponse)_HttpWebRequest.GetResponse())
                {
                    _HttpWebResponse.GetResponseStream().CopyTo(context.Response.OutputStream);
                    context.Response.ContentType = _HttpWebResponse.ContentType;
                    context.Response.StatusCode = (int)_HttpWebResponse.StatusCode;
                    context.Response.Cache.SetCacheability(HttpCacheability.NoCache);

                    // header must be cloned in this manner in order to support integrated & classic IIS modes,
                    // context.Response.Headers.Add((NameValueCollection)_HttpWebResponse.Headers) will fail
                    NameValueCollection _NameValueCollection = _HttpWebResponse.Headers;
                    foreach (string key in _NameValueCollection.AllKeys)
                        context.Response.AddHeader(key, _NameValueCollection[key]);
                }
            }
        }

        /// <summary>
        ///     When operating on the client, a search is performed on the ~/MyName/web.config
        ///     & the configuration is read or path is assumed simply by the ~/MyName. At this time, only http(s) protocols are
        ///     supported.
        /// </summary>
        /// <returns></returns>
        internal static string GetRelayUrl()
        {
            if (_GetRelayUrlFound == string.Empty)
                if (File.Exists(RequestPaths.GetPhysicalApplicationPath(DirectoryName, "web.config")))
                {
                    string path = string.Format("{0}/{1}", RequestPaths.ApplicationPath, DirectoryName);
                    if (path.ToLower().StartsWith("http"))
                        _GetRelayUrlFound = path;
                }

            // one last effort to
            return _GetRelayUrlFound;
        }

        /// <summary>
        ///     Attempts to get the Rudine web service URL from an appsetting set explicitly in the MyName folder
        ///     or by looking for a web service entry in the application main web.config
        /// </summary>
        /// <returns></returns>
        private static string ResolveFormHandlerUrl()
        {
            if (string.IsNullOrWhiteSpace(FormHandlerUrl))
            {
                // try parse from explicit set in the MyName\web.config
                if (string.IsNullOrWhiteSpace(FormHandlerUrl))
                    FormHandlerUrl = string.Format("{0}", ConfigurationManager.AppSettings["ServiceUrl"]);

                // try parse from WCF service address in the App root by search for something that references IPB.svc

                string webconfigXml = File.ReadAllText(RequestPaths.GetPhysicalApplicationPath("web.config"));
                if (string.IsNullOrWhiteSpace(FormHandlerUrl))
                    FormHandlerUrl = string.Format("{0}", FormHandlerUrlParse.Match(webconfigXml).Value);

                // make sure the URL is pretty and escaped
                if (!string.IsNullOrWhiteSpace(FormHandlerUrl))
                    // are we dealing with a file?
                    if (FormHandlerUrl.LastIndexOf('.') > FormHandlerUrl.LastIndexOf('/'))
                        FormHandlerUrl = new Uri(FormHandlerUrl.Substring(0, FormHandlerUrl.LastIndexOf("/") - 1), UriKind.RelativeOrAbsolute).ToString();
            }

            return FormHandlerUrl;
        }
    }
}