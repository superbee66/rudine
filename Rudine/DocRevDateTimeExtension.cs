using System;

namespace Rudine
{
    public static class DocRevDateTimeExtension
    {
        /// <summary>
        ///     makes a docrev from datetime
        /// </summary>
        /// <param name="d">recommended to be of UTC</param>
        /// <returns></returns>
        public static string AsDocRev(this DateTime d)
        {
            return string.Format("{0}.{1}.{2}.{3}",
                d.Year,
                string.Format("{0}{1}", d.Month.ToString().PadLeft(2, '0'), d.Day.ToString().PadLeft(2, '0')),
                string.Format("{0}{1}", d.Hour.ToString().PadLeft(2, '0'), d.Minute.ToString().PadLeft(2, '0')),
                d.Second);
        }
    }
}