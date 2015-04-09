using System;

namespace GrabbingParts.Util.StringHelpers
{
    public static class StringHelpers
    {
        public static string GetLastDirectory(string String, string Char = "/")
        {
            return String.Substring(String.LastIndexOf(Char) + 1);
        }

        public static bool IsInteger(string String)
        {
            bool result = false;
            int tmp;
            try
            {
                tmp = Int32.Parse(String);
                result = true;
            }
            catch(Exception ex)
            {

            }
            return result;            
        }
    }
}
