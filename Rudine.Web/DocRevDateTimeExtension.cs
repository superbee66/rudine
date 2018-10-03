using System;

namespace Rudine
{
    /// <summary>
    ///     Produces a string legal for parsing by the System.Version Type
    /// </summary>
    public static class DocRevDateTimeExtension
    {
        /// <summary>
        ///     makes a DocRev from datetime
        /// </summary>
        /// <param name="DateTime">recommended to be of UTC</param>
        /// <returns></returns>
        public static string AsDocRev(this DateTime DateTime) =>
            string.Format("{0}.{1}.{2}.{3}",
                DateTime.Year,
                string.Format("{0}{1}", DateTime.Month.ToString().PadLeft(2, '0'), DateTime.Day.ToString().PadLeft(2, '0')),
                string.Format("{0}{1}", DateTime.Hour.ToString().PadLeft(2, '0'), DateTime.Minute.ToString().PadLeft(2, '0')),
                DateTime.Second);
    }
}