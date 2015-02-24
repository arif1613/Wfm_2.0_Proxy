using System;
using NodaTime;

namespace CommonDomainLibrary.Commands
{
    public class RebuildReadModelView : ICommand
    {
        public string ViewType { get; set; }
        public Guid CausationId { get; set; }
        public Guid MessageId { get; set; }
        public Guid CorrelationId { get; set; }
        public Instant Timestamp { get; set; }
        public Guid Id { get; set; }

        public RebuildReadModelView(string viewType)
        {
            ViewType = viewType;
            MessageId = Guid.NewGuid();
            CorrelationId = Guid.NewGuid();
            Timestamp = Instant.FromDateTimeUtc(DateTime.UtcNow);
        }
    }
}
