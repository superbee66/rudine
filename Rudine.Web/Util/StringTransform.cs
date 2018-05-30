// StringTransform.cs

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

namespace Rudine.Web.Util
{
    public static class StringTransform
    {
        public enum JoinLogic
        {
            and,
            or
        }

        private static readonly CSharpCodeProvider _CSharpCodeProvider = new CSharpCodeProvider();

        private static readonly TextInfo enUSTextInfo = new CultureInfo("en-US", false).TextInfo;

        public static string SPLIT_DELIM_DEFAULT = " ";

        private static readonly Regex WordifyRegex = new Regex(
            @"(?<=[A-Za-z])(?<x>[0-9])|
		(?<=[0-9])(?<x>[A-Za-z])|
		(?<=[a-z])(?<x>[A-Z])|
		(?<=\w)(?<x>[A-Z])(?=[a-z])|
		(_)(?<x>[\w-[_]])",
            RegexOptions.IgnorePatternWhitespace);

        public static string CamelCase(string subject, string wordDelim = "", int maxLength = int.MaxValue)
        {
            if (maxLength == int.MaxValue)
                return
                    enUSTextInfo.ToTitleCase(
                            Wordify(subject).ToLower())
                        .Replace(" ", wordDelim);
            string s = string.Empty;
            foreach (string word in Wordify(subject).ToLower().Split(' ').Where(word => s.Length + word.Length + 1 < maxLength))
                if (s.Length == 0)
                    s += word;
                else
                    s += " " + word;

            return enUSTextInfo.ToTitleCase(s).Replace(" ", wordDelim);
        }

        private static string CollapseUnderscores(string original) =>
            Regex.Replace(original, "[_]+", "_");


        public static string PrettyCSharpIdent(string original, int characters = int.MaxValue) =>
            CollapseUnderscores(PrettyMsSqlIdent(
                Regex.IsMatch(original[0].ToString(), "[^a-z_]", RegexOptions.IgnoreCase)
                    ? string.Format("var{0}", original)
                    : original, characters));

        /// <summary>
        /// </summary>
        /// <param name="original"></param>
        /// <param name="characters"></param>
        /// <returns>Title-casing, underscore separated with 128 character limitation ensuring MSSQL column name compatibility</returns>
        public static string PrettyMsSqlIdent(string original, int characters = 128) =>
            CollapseUnderscores(SafeIdentifier(CamelCase(original, "_", characters)).TrimEnd('_'));

        public static string SafeIdentifier(string subject)
        {
            //TODO:write a proper regex for clean this

            string s = Regex
                .Replace(subject, @"[^\w_]+", "_")
                .TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0');

            return _CSharpCodeProvider.IsValidIdentifier(s)
                ? s
                : "_" + s;
        }

        /// <summary>
        ///     Make a pretty readable string from an array of them
        /// </summary>
        /// <param name="o"></param>
        /// <param name="word"></param>
        /// <returns></returns>
        public static string ToInlineList(this string[] o, JoinLogic word)
        {
            ListDictionary list = new ListDictionary();

            foreach (string s in o)
                if (!list.Contains(s.ToLower()))
                    list.Add(s.ToLower(),
                        s);

            string[] vals = new string[list.Values.Count];
            list.Values.CopyTo(vals,
                0);

            if (vals.Length > 1)
            {
                vals[0] = string.Join(", ",
                    vals).Trim(',',
                    ' ');

                int LastIndexOf = vals[0].LastIndexOf(',');
                if (LastIndexOf > 0)
                {
                    vals[0] = vals[0].Remove(LastIndexOf,
                        1);
                    vals[0] = vals[0].Insert(LastIndexOf,
                        " " + Enum.GetName(typeof(JoinLogic),
                            word).Replace("and",
                            "&") + " ");
                }
            }

            return vals[0];
        }

        /// <summary>
        ///     Converts Pascal, camel cased & underscored/hyphenated identifiers to coherent phrases
        ///     (?<=[A-Za-z])(?<x>[0-9])|(?<=[a-z])(?<x>[A-Z])|(?<=\w)(?<x>[A-Z])(?=[a-z])
        /// </summary>
        /// <param name="subject">thisIs_The-Subject</param>
        /// <returns>This Is The Subject</returns>
        public static string Wordify(string subject) => !string.IsNullOrWhiteSpace(subject)
            ? WordifyRegex.Replace(subject,
                " ${x}")
            : subject;
    }
}