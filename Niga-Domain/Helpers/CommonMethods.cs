using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Niga_Domain.Helpers
{
    public class CommonMethods
    {
        public static string Encoding(string Value)
        {
            byte[] Valueinbyte = System.Text.Encoding.ASCII.GetBytes(Value);
            var Valueinbase64 = Convert.ToBase64String(Valueinbyte);
            var Encryptedtoken= HttpUtility.UrlEncode(Valueinbase64);
            return  Encryptedtoken;
        }
        
        public static string Decoding(string Value)
        {
            var EncryptedValue = HttpUtility.UrlDecode(Value);
            byte[] Valueinbyte = Convert.FromBase64String(EncryptedValue);
            var Valueinbase64 = System.Text.Encoding.ASCII.GetString(Valueinbyte);
            return Valueinbase64;
        }
    }
}