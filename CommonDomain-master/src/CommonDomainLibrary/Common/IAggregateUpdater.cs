using System;
using System.Threading.Tasks;

namespace CommonDomainLibrary.Common
{
    public interface IAggregateUpdater
    {
        Task Update<T>(Guid id, Guid causationId, Action<T> action, bool lastTry, bool onlyExistingAggregates = false) where T : class, IAggregate, IMessageAccessor;
        Task Update<T>(Guid id, Guid causationId, Func<T, Task> action, bool lastTry, bool onlyExistingAggregates = false) where T : class, IAggregate, IMessageAccessor;
    }
}