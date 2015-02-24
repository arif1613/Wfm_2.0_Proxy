using System;
using NodaTime;

namespace CommonDomainLibrary.Events
{
    public abstract class Event : IEvent
    {
        public Guid CausationId { get; set; }
        public Guid MessageId { get; set; }
        public Guid CorrelationId { get; set; }
        public Instant Timestamp { get; set; }
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }

        protected Event(Guid correlationId, Guid causationId, Guid id, Guid ownerId)
        {
            MessageId = Guid.NewGuid();
            Timestamp = Instant.FromDateTimeUtc(DateTime.UtcNow);
            CorrelationId = correlationId;
            CausationId = causationId;
            Id = id;
            OwnerId = ownerId;
        }
    }
}
