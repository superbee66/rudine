#region Usings

using System;
using System.Text;

#endregion

namespace Rudine.Util
{
    /// <summary>
    ///     a modified base64 string encoding utility that yields values safe to cram into web browser address bar
    /// </summary>
    internal static class Url
    {
        public static string DecodeParameter(string value)
        {
            value = value.Replace("-", "+").Replace("_", "/").Replace("$", "=");
            byte[] arrBytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(arrBytes);
        }

        public static string EncodeParameter(string value)
        {
            byte[] arrBytes = Encoding.UTF8.GetBytes(value);
            value = Convert.ToBase64String(arrBytes);
            value = value.Replace("+", "-").Replace("/", "_").Replace("=", "$");
            return value;
        }
    }
}