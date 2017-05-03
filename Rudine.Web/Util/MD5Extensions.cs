using System.Security.Cryptography;
using System.Text;

namespace Rudine.Web.Util {
    public static class MD5Extensions {
        public static int TransformBytes(this MD5 o, byte[] b) =>
            o.TransformBlock(b, 0, b.Length, b, 0);

        public static int TransformString(this MD5 o, string s) =>
            o.TransformBytes(Encoding.UTF8.GetBytes(s));
    }
}