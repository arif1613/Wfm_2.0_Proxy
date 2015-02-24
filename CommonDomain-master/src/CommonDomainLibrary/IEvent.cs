using System;

namespace CommonDomainLibrary
{
    public interface IEvent : IMessage
    {
        Guid Id { get; set; }
        Guid OwnerId { get; set; }
    }
}