using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Rudine.Web.Util;

namespace Rudine.Util
{
    /// <summary>
    ///     Given two objects of the same type copy set values from the top object over the bottom object's properties
    /// </summary>
    internal static class PropertyOverlay
    {
        /// <summary>
        ///     Recursively copy non-default property values from top object to bottom
        ///     over writing the bottom property values is necessary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        /// <param name="sizeToTop">
        ///     Ff true removes or adds elements to child property list items to size to the top item's list
        ///     count. Default behavior is false, this yields the highest count(s) possible of list items for child lists of the
        ///     given item.
        /// </param>
        /// <returns></returns>
        public static T Overlay<T>(T top, T bottom, bool sizeToTop = false)
        {
            //TODO:Explorer the now open-source serializer to see how they detect uninitialized properties & tell the property overlay to leave them alone.
            if (top != null)
                if (!top.Equals(bottom))
                    if (bottom == null || !top.GetType().GetProperties().Any(m => m.CanWrite && m.CanRead))
                        bottom = top;
                    else
                    {
                        PropertyInfo[] props = top.GetType().GetProperties().Where(m => m.CanWrite && m.CanRead).ToArray();

                        foreach (PropertyInfo p in props)
                        {
                            // Transfer the value from the top to the bottom property only if the top has something defined
                            try
                            {
                                p.SetValue(
                                    bottom,
                                    p.PropertyType.GetInterface("IDictionary") != null
                                        ? p.GetValue(top, null) ?? p.GetValue(bottom, null)
                                        : p.PropertyType != typeof(byte[]) && p.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
                                            ? OverlayList(
                                                (IList) p.GetValue(top, null),
                                                (IList) p.GetValue(bottom, null),
                                                sizeToTop)
                                            : Overlay(
                                                p.GetValue(
                                                    p.PropertyType.GetConstructors().Any(c => c.GetParameters().Length == 0)
                                                        ? top
                                                        : !top.IsDefaultValue(p)
                                                            ? top
                                                            : bottom, null),
                                                p.GetValue(bottom, null)
                                                , sizeToTop),
                                    null);
                            } catch (Exception)
                            {
                                //TODO:Evaluate if ignoring exception while overlaying property values is a good idea
                                p.SetValue(bottom, p.GetValue(top, null), null);
                            }

                            // old school serializable object properties such as dates always had a partner property that ended with the word Specified 
                            if (props.Any(m => m.Name == string.Format("{0}Specified", p.Name)))
                                props.First(m => m.Name == string.Format("{0}Specified", p.Name)).SetValue(bottom, !bottom.IsDefaultValue(p), null);
                        }
                    }

            return bottom;
        }

        /// <summary>
        ///     Lay an array of items over top of another
        /// </summary>
        /// <param name="top">
        ///     Items not default here will always win<</param>
        /// <param name="bottom">
        ///     Items will show through from this list if there top
        ///     slot has a default value or it's ok to grow the list (chopToLength=false)
        /// </param>
        /// <param name="sizeToTop">false, the biggest of the two list is returned</param>
        /// <returns></returns>
        public static T OverlayList<T>(T top, T bottom, bool sizeToTop = false) where T : IList
        {
            if (top != null)
                if (!top.Equals(bottom))
                    if (!top.Equals(default(T)))
                        if (bottom == null)
                            bottom = top;
                        else
                            for (int i = 0; i < Math.Min(top.Count, bottom.Count); i++)
                            {
                                bottom[i] = Overlay(top[i], bottom[i], sizeToTop);
                                top[i] = bottom[i];
                            }

            return sizeToTop
                       ? top
                       : top == null
                           ? bottom
                           : top.Count >= bottom.Count
                               ? top
                               : bottom;
        }
    }
}