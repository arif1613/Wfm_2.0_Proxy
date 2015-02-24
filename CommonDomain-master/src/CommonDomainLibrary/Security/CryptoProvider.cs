using System;
using System.Security.Cryptography;
using System.Text;

namespace CommonDomainLibrary.Security
{
    public class CryptoProvider : ICryptoProvider
    {
        public string Hmac(string text, byte[] key)
        {
            return Convert.ToBase64String(Hmac(Encoding.UTF8.GetBytes(text), key));
        }

        public byte[] Hmac(byte[] data, byte[] key)
        {
            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(data);
            }
        }
    }
}