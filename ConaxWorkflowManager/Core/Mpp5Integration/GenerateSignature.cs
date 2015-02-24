using System;
using System.Security.Cryptography;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration.Models;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration
{
    class GenerateSignature
    {
        private Mpp5IdentityModel Identity { get; set; }

         private static readonly RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();

         public GenerateSignature(Mpp5IdentityModel identity)
        {
            Identity = identity;
        }

        private string getTimeStamp()
        {
            return DateTime.Now.Subtract(DateTime.MinValue.AddYears(1969)).Ticks.ToString();
        }

        private string getNonce()
        {
            var data = new byte[5];
            random.GetNonZeroBytes(data);
            return Convert.ToBase64String(data).Substring(0,4);
        }

        public string Build(HttpMethodType method, string resource)
        {
            var timeStamp = getTimeStamp();
            var nonce = getNonce();
            var sha = new HMACSHA256();

            // Create a token byte[] from the username and private key
            var token = Encoding.UTF8.GetBytes(Identity.UserName + Identity.PrivateKey);
            // Create user authentication key from token using private key
            sha.Key = Convert.FromBase64String(Identity.PrivateKey);
            var userAuthenticationKey = sha.ComputeHash(token);
            // Create mac by hashing all values using userAuthenticationKey.
            sha.Key = userAuthenticationKey;
            var mac = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(
                string.Join("\n", Identity.UserName, timeStamp, nonce, method, resource))));
            // Create signature
            var signature =  string.Format("username=\"{0}\",client=\"{1}\",ts=\"{2}\",nonce=\"{3}\",mac=\"{4}\"",
                Identity.UserName,
                Identity.ClientID,
                timeStamp,
                nonce,
                mac
                );

            return signature;
        }

    }
}
