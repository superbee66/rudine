using System;
using System.IO;
using Rudine.Web.Util;

namespace Rudine.Util
{
    /// <summary>
    ///     Provides an activated instance T in a self created & managed AppDomain
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SelfMarshalByRefObject<T> : MarshalByRefObject, IDisposable where T : class
    {
        private static AppDomain _MySlaveAppDomain;

        /// <summary>
        /// </summary>
        /// <returns>null if not a master calling</returns>
        protected T Slave()
        {
            if (!isMaster())
                return null;
            lock (_Slave)
            {
                string exeAssembly = GetType().Assembly.FullName;

                if (_MySlaveAppDomain == null)
                {
                    // Construct and initialize settings for a second AppDomain.
                    AppDomainSetup _AppDomainSetup = new AppDomainSetup
                    {
                        ApplicationBase = new Uri(RequestPaths.GetPhysicalApplicationPath()).ToString(),
                        DisallowBindingRedirects = false,
                        DisallowCodeDownload = false,
                        ConfigurationFile = new Uri(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile).ToString(),
                        ApplicationName = SlaveAppDomainFriendlyName(),
                        PrivateBinPathProbe = new Uri(Directory.Exists(RequestPaths.GetPhysicalApplicationPath("bin"))
                                                          ? RequestPaths.GetPhysicalApplicationPath("bin")
                                                          : Path.GetDirectoryName(GetType().Assembly.Location)).ToString(),
                        PrivateBinPath = new Uri(Directory.Exists(RequestPaths.GetPhysicalApplicationPath("bin"))
                                                     ? RequestPaths.GetPhysicalApplicationPath("bin")
                                                     : Path.GetDirectoryName(GetType().Assembly.Location)).ToString()
                    };

                    // Create the second AppDomain.
                    _MySlaveAppDomain = AppDomain.CreateDomain(SlaveAppDomainFriendlyName(), null, _AppDomainSetup);
                }

                return (T) _MySlaveAppDomain.CreateInstanceAndUnwrap(exeAssembly, typeof(T).FullName);
            }
        }

        private static readonly object _SlaveReloadLock = new object();
        private static readonly object _Slave = new object();

        protected T SlaveReload()
        {
            lock (_SlaveReloadLock)
            {
                Dispose();
                return Slave();
            }
        }

        protected bool isMaster()
        {
            string f = AppDomain.CurrentDomain.FriendlyName;
            AppDomain a = AppDomain.CurrentDomain;
            string s = a.FriendlyName;

            return !s.Equals(SlaveAppDomainFriendlyName());
        }

        private string SlaveAppDomainFriendlyName() { return string.Format("{0}_Slave", typeof(T).Name); }

        public void Dispose()
        {
            if (_MySlaveAppDomain != null)
            {
                AppDomain.Unload(_MySlaveAppDomain);
                _MySlaveAppDomain = null;
            }
        }
    }
}