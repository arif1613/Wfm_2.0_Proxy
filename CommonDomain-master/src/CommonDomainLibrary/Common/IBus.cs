using System;
using System.Threading.Tasks;
using CommonDomainLibrary.Security;
using NodaTime;

namespace CommonDomainLibrary.Common
{
    public interface IBus
    {
        Task Publish(IMessage message, ICommonIdentity identity = null);
        Task Defer(IMessage message, Instant instant, ICommonIdentity identity = null);
        Task Subscribe(Type message, Type handler);
    }
}
