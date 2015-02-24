using System.Threading;

namespace CommonDomainLibrary.Security
{
    public static class MessageExtensions
    {
         public static ICommonIdentity Sender(this IMessage message)
         {
             return Thread.CurrentPrincipal.Identity as ICommonIdentity;
         }
    }
}