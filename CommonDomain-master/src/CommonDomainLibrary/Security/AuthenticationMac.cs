using System;
using System.Globalization;
using System.Text;

namespace CommonDomainLibrary.Security
{
    public class AuthenticationMac
    {
        public string Username { get; private set; }
        public Guid Client { get; private set; }
        public long TimeStamp { get; private set; }
        public string Nonce { get; private set; }
        public string Mac { get; private set; }
        public string Resource { get; private set; }
        public string Method { get; private set; }

        private const string CredentialsPrefix = "MAC ";

        public AuthenticationMac(string username, Guid client, long timeStamp, string nonce, string method, string resource, ICryptoProvider cryptoProvider, byte[] authenticationKey) : this(username, client, timeStamp, nonce, method, resource, null)
        {
            Mac = CalculateMac(cryptoProvider, authenticationKey);
        }

        public AuthenticationMac(string username, Guid client, long timeStamp, string nonce, string method, string resource, string mac)
        {
            Username = username;
            Client = client;
            TimeStamp = timeStamp;
            Nonce = nonce;
            Method = method;
            Resource = resource;
            Mac = mac;
        }

        public override string ToString()
        {
            return string.Format("{0}username=\"{1}\",client=\"{2}\",ts=\"{3}\",nonce=\"{4}\",mac=\"{5}\"",
                CredentialsPrefix, Username, Client.ToString("n"), TimeStamp.ToString(CultureInfo.InvariantCulture),
                Nonce, Mac);
        }

        public bool IsValid(ICryptoProvider cryptoProvider, byte[] clientAuthenticationKey)
        {
            return CalculateMac(cryptoProvider, clientAuthenticationKey) == Mac;
        }

        private string CalculateMac(ICryptoProvider cryptoProvider, byte[] clientAuthenticationKey)
        {
            var strClientAuthenticationKey = Convert.ToBase64String(clientAuthenticationKey);
            var token = Encoding.UTF8.GetBytes(Username + strClientAuthenticationKey);
            var userAuthenticationKey = cryptoProvider.Hmac(token, clientAuthenticationKey);

            var value = string.Join("\n", new[]
                {
                    Username, 
                    TimeStamp.ToString(CultureInfo.InvariantCulture), 
                    Nonce, 
                    Method, 
                    Resource
                });

            return cryptoProvider.Hmac(value, userAuthenticationKey);
        }
    }
}