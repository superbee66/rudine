using System;
using System.Reflection;
using Microsoft.VisualBasic;

namespace Rudine.Web.Util
{
    /// <summary>
    ///     Class for dealing with the translation of database null values.
    /// </summary>
    internal class Null
    {
        public static bool NullBoolean
        {
            get { return false; }
        }

        public static DateTime NullDate
        {
            get { return DateTime.MinValue; }
        }

        public static decimal NullDecimal
        {
            get { return decimal.MinValue; }
        }

        public static double NullDouble
        {
            get { return double.MinValue; }
        }

        public static Guid NullGuid
        {
            get { return Guid.Empty; }
        }

        public static int NullInteger
        {
            get { return -1; }
        }

        // define application encoded null values
        public static short NullShort
        {
            get { return -1; }
        }

        public static float NullSingle
        {
            get { return float.MinValue; }
        }

        public static string NullString
        {
            get { return ""; }
        }

        // convert an application encoded null value to a database null value ( used in DAL )
        public static object GetNull(object objField, object objDBNull)
        {
            object returnValue;
            returnValue = objField;
            if (objField == null)
                returnValue = objDBNull;
            else if (objField is short)
            {
                if (Convert.ToInt16(objField) == NullShort)
                    returnValue = objDBNull;
            } else if (objField is int)
            {
                if (Convert.ToInt32(objField) == NullInteger)
                    returnValue = objDBNull;
            } else if (objField is Single)
            {
                if (Convert.ToSingle(objField) == NullSingle)
                    returnValue = objDBNull;
            } else if (objField is double)
            {
                if (Convert.ToDouble(objField) == NullDouble)
                    returnValue = objDBNull;
            } else if (objField is decimal)
            {
                if (Convert.ToDecimal(objField) == NullDecimal)
                    returnValue = objDBNull;
            } else if (objField is DateTime)
            {
                // compare the Date part of the DateTime with the DatePart of the NullDate ( this avoids subtle time differences )
                if (Convert.ToDateTime(objField).Date == NullDate.Date)
                    returnValue = objDBNull;
            } else if (objField is string)
            {
                if (objField == null)
                    returnValue = objDBNull;
                else if (objField.ToString() == NullString)
                    returnValue = objDBNull;
            } else if (objField is bool)
            {
                if (Convert.ToBoolean(objField) == NullBoolean)
                    returnValue = objDBNull;
            } else if (objField is Guid)
                if (((Guid) objField).Equals(NullGuid))
                    returnValue = objDBNull;
            return returnValue;
        }

        // checks if a field contains an application encoded null value
        public static bool IsNull(object objField)
        {
            bool returnValue;
            if (objField != null)
                if (objField is int)
                    returnValue = objField.Equals(NullInteger);
                else if (objField is Single)
                    returnValue = objField.Equals(NullSingle);
                else if (objField is double)
                    returnValue = objField.Equals(NullDouble);
                else if (objField is decimal)
                    returnValue = objField.Equals(NullDecimal);
                else if (objField is DateTime)
                {
                    DateTime objDate = Convert.ToDateTime(objField);
                    returnValue = objDate.Date.Equals(NullDate.Date);
                } else if (objField is string)
                    returnValue = objField.Equals(NullString);
                else if (objField is bool)
                    returnValue = objField.Equals(NullBoolean);
                else if (objField is Guid)
                    returnValue = objField.Equals(NullGuid);
                else // complex object
                    returnValue = false;
            else
                returnValue = true;
            return returnValue;
        }

        // sets a field to an application encoded null value ( used in BLL layer )
        public static object SetNull(object objValue, object objField)
        {
            object returnValue;
            if (Information.IsDBNull(objValue))
                if (objField is short)
                    returnValue = NullShort;
                else if (objField is int)
                    returnValue = NullInteger;
                else if (objField is Single)
                    returnValue = NullSingle;
                else if (objField is double)
                    returnValue = NullDouble;
                else if (objField is decimal)
                    returnValue = NullDecimal;
                else if (objField is DateTime)
                    returnValue = NullDate;
                else if (objField is string)
                    returnValue = NullString;
                else if (objField is bool)
                    returnValue = NullBoolean;
                else if (objField is Guid)
                    returnValue = NullGuid;
                else // complex object
                    returnValue = null;
            else // return value
                returnValue = objValue;
            return returnValue;
        }

        // sets a field to an application encoded null value ( used in BLL layer )
        public static object SetNull(PropertyInfo objPropertyInfo)
        {
            object returnValue;
            switch (objPropertyInfo.PropertyType.ToString())
            {
                case "System.Int16":
                    returnValue = NullShort;
                    break;
                case "System.Int32":
                    returnValue = NullInteger;
                    break;
                case "System.Int64":
                    returnValue = NullInteger;
                    break;
                case "System.Single":
                    returnValue = NullSingle;
                    break;
                case "System.Double":
                    returnValue = NullDouble;
                    break;
                case "System.Decimal":
                    returnValue = NullDecimal;
                    break;
                case "System.DateTime":
                    returnValue = NullDate;
                    break;
                case "System.String":
                    returnValue = NullString;
                    break;
                case "System.Char":
                    returnValue = NullString;
                    break;
                case "System.Boolean":
                    returnValue = NullBoolean;
                    break;
                case "System.Guid":
                    returnValue = NullGuid;
                    break;
                default:

                    // Enumerations default to the first entry
                    Type pType = objPropertyInfo.PropertyType;
                    if (pType.BaseType.Equals(typeof(Enum)))
                    {
                        Array objEnumValues = Enum.GetValues(pType);
                        Array.Sort(objEnumValues);
                        returnValue = Enum.ToObject(pType,
                            objEnumValues.GetValue(0));
                    } else // complex object
                        returnValue = null;
                    break;
            }
            return returnValue;
        }
    }
}