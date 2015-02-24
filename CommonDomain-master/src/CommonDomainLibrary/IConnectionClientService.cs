using Microsoft.WindowsAzure.MediaServices.Client;

namespace CommonDomainLibrary
{
    public interface IConnectionClientService
    {
        CloudMediaContext WamsClient(string connectionString);
    }
}