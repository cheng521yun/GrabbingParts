using System;
using System.Data;
using System.Globalization;

namespace GrabbingParts.Util.SqlHelpers
{
    public static class TableDataHelpers
    {
        public static string GetColumnValue(DataTable dtData, int NumRec, int NumCol)
        {
            //CultureInfo.InvariantCulture.NumberFormat
            string strColValue = "";
            try
            {
                strColValue = Convert.ToString(dtData.Rows[NumRec][NumCol], CultureInfo.InvariantCulture);
            }
            catch
            {
                return strColValue;
            }
            return strColValue;
        }

        public static string GetColumnValue(DataTable dtData, int NumRec, string NameCol)
        {
            string strColValue = "";
            try
            {
                strColValue = Convert.ToString(dtData.Rows[NumRec][NameCol], CultureInfo.InvariantCulture);
            }
            catch
            {
                return strColValue;
            }
            return strColValue;
        }     
    }
}
