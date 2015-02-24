using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CommonDomainLibrary
{
    public class CredentialsEncryptionService : ICredentialsEncryptionService
    {
        private readonly X509Certificate2 _certificate;

        public CredentialsEncryptionService(X509Certificate2 certificate)
        {
            _certificate = certificate;
        }

        public string Encrypt(string value)
        {
            var rsaEncryptor = (RSACryptoServiceProvider)_certificate.PrivateKey;
            byte[] cipherData = rsaEncryptor.Encrypt(Encoding.UTF8.GetBytes(value), true);
            return Convert.ToBase64String(cipherData);
        }

        public string Decrypt(string cypher)
        {
            var rsaEncryptor = (RSACryptoServiceProvider)_certificate.PrivateKey;
            byte[] plainData = rsaEncryptor.Decrypt(Convert.FromBase64String(cypher), true);
            return Encoding.UTF8.GetString(plainData);
        }
    }
}
