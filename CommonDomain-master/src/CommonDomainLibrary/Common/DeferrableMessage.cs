using System;
using NodaTime;

namespace CommonDomainLibrary.Common
{
    public class DeferrableMessage
    {
        public IMessage Message { get; set; }
        public Instant Instant { get; set; }

        private DeferrableMessage()
        {
        }

        public DeferrableMessage(IMessage message, Duration duration)
        {
            Message = message;
            Instant = Instant.FromDateTimeUtc(DateTime.UtcNow).Plus(duration);
        }

        public DeferrableMessage(IMessage message, Instant instant)
        {
            Message = message;
            Instant = instant;
        }
    }
}
