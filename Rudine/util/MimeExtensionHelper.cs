using System;
using System.Reflection;
using System.Web;

namespace Rudine.Util
{
    internal static class MimeExtensionHelper
    {
        private static readonly object locker = new object();
        private static readonly MethodInfo getMimeMappingMethodInfo;

        static MimeExtensionHelper()
        {
            Type mimeMappingType = Assembly.GetAssembly(typeof(HttpRuntime)).GetType("System.Web.MimeMapping");

            if (mimeMappingType == null)
                throw new SystemException("Couldn't find MimeMapping type");

            getMimeMappingMethodInfo = mimeMappingType.GetMethod("GetMimeMapping", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            if (getMimeMappingMethodInfo == null)
                throw new SystemException("Couldn't find GetMimeMapping method");

            if (getMimeMappingMethodInfo.ReturnType != typeof(string))
                throw new SystemException("GetMimeMapping method has invalid return type");

            if (getMimeMappingMethodInfo.GetParameters().Length != 1 && getMimeMappingMethodInfo.GetParameters()[0].ParameterType != typeof(string))
                throw new SystemException("GetMimeMapping method has invalid parameters");
        }

        public static string GetMimeType(string fileName)
        {
            lock (locker)
            {
                return (string) getMimeMappingMethodInfo.Invoke(null, new object[]
                {
                    fileName
                });
            }
        }
    }
}