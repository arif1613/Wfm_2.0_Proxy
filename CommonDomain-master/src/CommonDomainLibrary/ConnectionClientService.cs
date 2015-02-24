using System.Collections.Generic;
using Microsoft.WindowsAzure.MediaServices.Client;
using Newtonsoft.Json;
using System.Linq;

namespace CommonDomainLibrary
{
    public class ConnectionClientService : IConnectionClientService
    {
        private readonly ICredentialsEncryptionService _encryptionService;

        public ConnectionClientService(ICredentialsEncryptionService encryptionService)
        {
            _encryptionService = encryptionService;
        }

        public CloudMediaContext WamsClient(string connectionString)
        {
            var credentials = JsonConvert.DeserializeObject<Dictionary<string, string>>(connectionString);

            var cloudMContext = new CloudMediaContext(credentials["account"], _encryptionService.Decrypt(credentials["key"]));
            return Queryable.Count(cloudMContext.StorageAccounts) > 0 ? cloudMContext : null;
        }
    }
}
