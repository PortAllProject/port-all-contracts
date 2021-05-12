using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
namespace ReportGenerator
{
    static public class Helpers
    {
        public static string ConvertHex(String hexString)
        {
            try
            {
                string ascii = string.Empty;
 
                for (int i = 0; i < hexString.Length; i += 2)
                {
                    String hs = string.Empty;
 
                    hs = hexString.Substring(i, 2);
                    uint decval = System.Convert.ToUInt32(hs, 16);
                    char character = System.Convert.ToChar(decval);
                    ascii += character;
 
                }
 
                return ascii;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
 
            return string.Empty;
        }
 
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp); // .ToLocalTime();
            return dtDateTime;
        }
 
        public static DateTime JavaTimeStampToDateTime(double javaTimeStamp)
        {
            // Java timestamp is milliseconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(javaTimeStamp); // .ToLocalTime();
            return dtDateTime;
        }
    }
}