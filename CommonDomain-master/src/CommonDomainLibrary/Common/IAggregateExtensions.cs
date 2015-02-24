namespace CommonDomainLibrary.Common
{
    public static class IAggregateExtensions
    {
        public static void Raise<TAggregate>(this TAggregate aggregate, dynamic e)
            where TAggregate : IAggregate, IMessageAccessor
        {
            aggregate.Messages.RaiseMessage(e);
        }
    }
}