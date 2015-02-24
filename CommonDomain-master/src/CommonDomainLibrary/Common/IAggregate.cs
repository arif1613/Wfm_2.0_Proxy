using System;

namespace CommonDomainLibrary.Common
{
    public interface IAggregate
    {
        Guid Id { get; }
    }
}