using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NodaTime;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Mpp5Integration.Models
{
    public class AddVodAssetModel : ICommand
    {
        public Guid CausationId { get; set; }
        public Guid MessageId { get; set; }
        public Guid CorrelationId { get; set; }
        public Instant Timestamp { get; set; }
        public Guid Id { get; set; }

        public Guid AssetId { get; set; }
        public Guid OwnerId { get; set; }
        public string Name { get; set; }
        public string WamsAssetId { get; set; }
        public Guid WamsAccountId { get; set; }
        public bool Encoded { get; set; }


        public AddVodAssetModel()
        {
        }

        public AddVodAssetModel(Guid id, Guid ownerId, string name, Guid assetId, string wamsAssetId, Guid wamsAccountId, bool encoded,
            Guid causationId = new Guid(), Guid correlationId = new Guid())
        {
            Id = id;
            AssetId = assetId;
            OwnerId = ownerId;
            MessageId = Guid.NewGuid();
            Timestamp = Instant.FromDateTimeUtc(DateTime.UtcNow);
            CorrelationId = correlationId == Guid.Empty ? Guid.NewGuid() : correlationId;
            CausationId = causationId;
            Name = name;
            WamsAssetId = wamsAssetId;
            WamsAccountId = wamsAccountId;
            Encoded = encoded;
        }
    }
}
