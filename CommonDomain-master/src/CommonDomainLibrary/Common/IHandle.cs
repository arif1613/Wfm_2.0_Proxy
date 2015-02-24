using System.Threading.Tasks;

namespace CommonDomainLibrary.Common
{
    public interface IHandle<in T> : IHandler where T: class, IMessage
    {
        Task Handle(T e, bool lastTry);
    }

    public interface IHandler
    {
    }
}