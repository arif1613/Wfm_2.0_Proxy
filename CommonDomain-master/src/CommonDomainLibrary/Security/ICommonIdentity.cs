using System;
using System.Security.Principal;

namespace CommonDomainLibrary.Security
{
    public interface ICommonIdentity : IIdentity
    {
        Guid Id { get; }
        Guid OwnerId { get; }
    }
}