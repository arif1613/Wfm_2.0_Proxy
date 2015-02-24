using System;
using NodaTime;

namespace CommonDomainLibrary.Events
{
    public class AggregateInWrongStateForAction : IErrorEvent
    {
        public Guid CausationId { get; set; }
        public Guid MessageId { get; set; }
        public Guid CorrelationId { get; set; }
        public Instant Timestamp { get; set; }
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string ErrorMessage { get; set; }

        public AggregateInWrongStateForAction(Guid correlationId, Guid causationId, Guid id, Guid ownerId, string errorMessage)
        {
            CorrelationId = correlationId;
            CausationId = causationId;
            Id = id;
            OwnerId = ownerId;
            ErrorMessage = errorMessage;
            Timestamp = Instant.FromDateTimeUtc(DateTime.UtcNow);
            MessageId = Guid.NewGuid();
        }
    }
}
