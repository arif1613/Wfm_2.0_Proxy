using System;
using System.Threading.Tasks;
using Edit;

namespace CommonDomainLibrary.Common
{
	public interface IAggregateRepository
	{
	    Task<AggregateRepositoryResponse> GetById<T>(Guid aggregateId)
	        where T : class, IAggregate, IMessageAccessor;

	    Task Save<T>(Guid causationId, T aggregate, IStoredDataVersion version)
            where T : class, IAggregate, IMessageAccessor;
	}
}