using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Threading;

namespace GrabbingParts.XmlHelpers
{
    /// <summary>
    /// To/from xml formatting helpers.
    /// </summary>
    public static class FormatHelpers
    {
        /// <summary>
        /// Convert xml date yyyy-mm-dd to format based on current culture.
        /// </summary>
        /// <param name="value">Xml formatted date.</param>
        /// <returns>Date formatted based on current culture.</returns>
        public static string fromXmlDate(string value)
        {
            DateTime dt;
            string retval = "";
            if (ParseXml(value, out dt))
                retval = dt.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, CultureInfo.CurrentCulture);

            return retval;
        }

        ///// <summary>
        ///// Convert xml date/time format into culture specific format.
        ///// </summary>
        ///// <param name="value">Xml date/time to convert.</param>
        ///// <returns>Culture-specific date/time.</returns>
        //public static string fromXmlDateAndTime(string value)
        //{
        //    DateTime dt;
        //    string retval = "";
        //    if (!string.IsNullOrEmpty(value) && DateTime.TryParse(value, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeLocal, out dt))
        //        retval = dt.ToString("g");

        //    return retval;
        //}

        ///// <summary>
        ///// Convert UTC xml date/time format into culture specific format.
        ///// </summary>
        ///// <param name="value">Xml date/time to convert.</param>
        ///// <returns>Culture-specific date/time.</returns>
        //public static string fromXmlDateAndTimeUTC(string value)
        //{
        //    DateTime dt;
        //    string retval = "";
        //    if (!string.IsNullOrEmpty(value) && DateTime.TryParse(value, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal, out dt))
        //        retval = dt.ToString("g");

        //    return retval;
        //}

        /// <summary>
        /// Convert date from current culture to xml date yyyy-mm-dd.
        /// </summary>
        /// <param name="value">Date string value to convert.</param>
        /// <param name="formatter"></param>
        /// <returns>Xml formatted date.</returns>
        public static string toXmlDate(string value, string formatter = null)
        {
            DateTime dt;
            string retval = "";
            string formatter1 = formatter ?? "yyyy-MM-dd";
            if (!String.IsNullOrEmpty(value) && DateTime.TryParse(value, out dt))
                retval = dt.ToString(formatter1, CultureInfo.CurrentCulture);

            return retval;
        }

        /// <summary>
        /// Convert a DateTime to xml format
        /// </summary>
        /// <param name="dt">DateTime value to convert</param>
        /// <returns>Xml format date/time</returns>
        public static string ToXml(DateTime dt)
        {
            return dt.ToString("s", CultureInfo.InvariantCulture.DateTimeFormat);
        }

        ///// <summary>
        ///// Convert a string value to an xml forma
        ///// </summary>
        ///// <param name="value">Value to convert</param>
        ///// <returns>Xml date/time string</returns>
        //public static string ToXmlDateTime(string value)
        //{
        //    DateTime dt;
        //    return DateTime.TryParse(value, Thread.CurrentThread.CurrentCulture.DateTimeFormat, DateTimeStyles.AssumeLocal, out dt) ? ToXml(dt) : "";
        //}

        /// <summary>
        /// Convert a Decimal to xml format
        /// </summary>
        /// <param name="d">Decimal value to convert</param>
        /// <returns>Xml format decimal</returns>
        public static string ToXml(Decimal d)
        {
            return d.ToString("g", CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Convert a Decimal to xml format
        /// </summary>
        /// <param name="d">Decimal value to convert</param>
        /// <returns>Xml format decimal</returns>
        public static string ToXml(Decimal d,string key)
        {
            return d.ToString(key, CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Convert a long to xml format
        /// </summary>
        /// <param name="d">long value to convert</param>
        /// <returns>Xml format long</returns>
        public static string ToXml(long d)
        {
            return d.ToString("g", CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Convert a string value to xml decimal format
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Xml decimal string</returns>
        public static string ToXmlDecimal(string value)
        {
            decimal d = 0;
            decimal.TryParse(value, NumberStyles.Any, Thread.CurrentThread.CurrentCulture.NumberFormat, out d);
            return ToXml(d);
        }

        /// <summary>
        /// Convert a string value to xml double format
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Xml decimal string</returns>
        public static string ToXmlDouble(string value)
        {
            double d = 0;
            double.TryParse(value, NumberStyles.Any, Thread.CurrentThread.CurrentCulture.NumberFormat, out d);
            return ToXml(d);
        }

        /// <summary>
        /// Convert a Double to xml format
        /// </summary>
        /// <param name="d">Double value to convert</param>
        /// <returns>Xml format double</returns>
        public static string ToXml(Double d)
        {
            return d.ToString("g", CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Convert a Double to xml format
        /// </summary>
        /// <param name="d">Double value to convert</param>
        /// <param name="format">A numeric format string</param>
        /// <returns>Xml format double</returns>
        public static string ToXml(Double d, string format)
        {
            return d.ToString(format, CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Parse an xml date/time.  If no timezone is specified, assumes local time.
        /// </summary>
        /// <param name="value">Xml date/time value</param>
        /// <param name="dt">Parsed DateTime value</param>
        /// <returns>True if value parsed successfully</returns>
        public static bool ParseXml(string value, out DateTime dt)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeLocal, out dt);
        }

        /// <summary>
        /// Parse an xml date/time.  If no timezone is specified, assumes UTC.
        /// </summary>
        /// <param name="value">Xml date/time value</param>
        /// <param name="dt">Parsed DateTime value</param>
        /// <returns>True if value parsed successfully</returns>
        public static bool ParseXmlUTC(string value, out DateTime dt)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dt);
        }

        /// <summary>
        /// Parse an xml decimal value.
        /// </summary>
        /// <param name="value">Xml decimal value</param>
        /// <param name="d">Parsed decimal value</param>
        /// <returns>True if value parsed successfully</returns>
        public static bool ParseXml(string value, out Decimal d)
        {
            return Decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out d);
        }

        /// <summary>
        /// Parse an xml double value.
        /// </summary>
        /// <param name="value">Xml double value</param>
        /// <param name="d">Parsed double value</param>
        /// <returns>True if value parsed successfully</returns>
        public static bool ParseXml(string value, out Double d)
        {
            return Double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out d);
        }

        /// <summary>
        /// Parse an xml int value.
        /// </summary>
        /// <param name="value">Xml int value</param>
        /// <param name="d">Parsed int value</param>
        /// <returns>True if value parsed successfully</returns>
        public static bool ParseXml(string value, out int d)
        {
            return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out d);
        }

        /// <summary>
        /// Return an xml date/time.
        /// </summary>
        /// <param name="value">String value to parse</param>
        /// <returns>DateTime value</returns>
        public static DateTime ParseXmlDateTime(string value)
        {
            return DateTime.Parse(value, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeLocal);
        }

        /// <summary>
        /// if value is invalide return DateTime.Min
        /// Default CultureInfo is US 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cultInfo"></param>
        /// <returns></returns>
        public static DateTime PareStrToDateTime(string value,CultureInfo cultInfo=null)
        {
            DateTime dt;
            if (!DateTime.TryParse(value, out dt))
            {
                return DateTime.MinValue;
            }
            if (cultInfo == null)
            {
                cultInfo = new CultureInfo("en-US");
            }
            return DateTime.Parse(value, cultInfo);
        }

        /// <summary>
        /// Return an xml decimal.
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <returns>Decimal value</returns>
        public static decimal TryParseXmlDecimal(string value)
        {
            decimal ret;
            decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out ret);
            return ret;
        }

        /// <summary>
        /// Return an xml decimal.
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <param name="defaultValue">return default value</param>
        /// <returns>Decimal value</returns>
        public static decimal TryParseXmlDecimal(string value, decimal defaultValue)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;
            else
                return TryParseXmlDecimal(value);
        }

        /// <summary>
        /// Return an xml decimal.
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <returns>Decimal value</returns>
        public static decimal ParseXmlDecimal(string value)
        {
            return decimal.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Return an xml decimal.
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <param name="defaultValue">return default value</param>
        /// <returns>Decimal value</returns>
        public static decimal ParseXmlDecimal(string value, decimal defaultValue)
        {
            decimal returnValue = defaultValue;
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            else
            {
                ParseXml(value, out returnValue);
                return returnValue;
            }
        }

        /// <summary>
        /// Return an xml double.
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <returns>Double value</returns>
        public static double ParseXmlDouble(string value)
        {
            return double.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
        }

        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double ParseXmlDouble(string value, double defaultValue)
        {
            double returnValue = defaultValue;
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            else
            {
                ParseXml(value, out returnValue);
                return returnValue;
            }
        }

        /// <summary>
        /// Return an xml int.
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <returns>Int value</returns>
        public static int ParseXmlInt(string value)
        {
            return int.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Return an xml int.
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <returns>Int value</returns>
        public static int ParseXmlInt(string value, int defaultValue)
        {
            int result = 0;
            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out result))
                return int.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);
            else
                return defaultValue;
        }
    }
}
