using Edit;
namespace CommonDomainLibrary.Common
{
    public class AggregateRepositoryResponse
    {
        public object Aggregate { get; set; }
        public IStoredDataVersion Version { get; set; }

        public AggregateRepositoryResponse(object aggregate, IStoredDataVersion version)
        {
            Aggregate = aggregate;
            Version = version;
        }
    }
}