namespace CommonDomainLibrary
{
    public interface IErrorEvent : IEvent
    {
        string ErrorMessage { get; set; }
    }
}
