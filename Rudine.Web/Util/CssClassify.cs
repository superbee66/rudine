using System.Web.UI;
using System.Web.UI.WebControls;

namespace Rudine.Web.Util
{
    /// <summary>
    ///     factory methods that set or return lower-case (great for CSS4 case-sensitive)
    ///     CSS legal class name(s)
    /// </summary>
    internal static class CssClassify
    {
        /// <summary>
        ///     goes one further step by simply settings the target webcontrol's CssClass for you. If defined
        ///     with existing values, new values applied will be concatenated. These methods were originally
        ///     used to automatically set given control's CssClass/class DOM attribute by reflecting the CSharp's
        ///     class/type names.
        /// </summary>
        /// <param name="target">the web control to alter the CssClass property</param>
        /// <param name="source">if defined, CSS strings are cooked based on that menu and applied to the target</param>
        public static void ApplyDefaultCssClass(WebControl target, Control source = null)
        {
            if (source == null)
                source = target;

            if (string.IsNullOrWhiteSpace(target.CssClass))
                target.CssClass = string.Empty;

            string cssClass = GetDefaultCssClass(source.GetType().Name);
            if (target.CssClass != cssClass && target.CssClass.IndexOf(" " + cssClass) == -1 && target.CssClass.IndexOf(cssClass + " ") == -1)
                target.CssClass += " " + cssClass;

            target.CssClass = target.CssClass.Trim();
        }

        /// <summary>
        ///     utilizes the control's typeof(o).Name as basis for the Css class name
        /// </summary>
        /// <param name="source"></param>
        /// <returns>a single css class name</returns>
        public static string GetDefaultCssClass(Control source) { return GetDefaultCssClass(source.GetType().Name); }

        /// <summary>
        /// </summary>
        /// <param name="label"></param>
        /// <returns>the original label lower case & stippled of css illegal characters replaced by single underscores</returns>
        public static string GetDefaultCssClass(string label)
        {
            string _CssClassify = Null.NullString;

            label = FileSystem.CleanFileName(label.Replace("ascx",
                "")); // Generated automatically when reflecting a .Net System.Web.UI.WebControls class <code>GetType().Name</code>

            //TODO:rethink the formatting technique to output something more like JQuery does with its hyphened class names
            foreach (string _ToWordify in label.Split(' ',
                '_'))
                if (_ToWordify != Null.NullString)
                    if (_ToWordify != " ")
                        if (_ToWordify != "_")
                            _CssClassify += _ToWordify + "_";

            _CssClassify = _CssClassify.Replace("_",
                "-");

            return _CssClassify == Null.NullString ? label.Trim().ToLower() : _CssClassify.Trim('-',
                       ' ').ToLower();
        }
    }
}