using System;
using NodaTime;

namespace CommonDomainLibrary
{
    public interface IMessage
    {
        Guid CausationId { get; set; }
        Guid MessageId { get; set; }
        Guid CorrelationId { get; set; }
        Instant Timestamp { get; set; }
    }
}