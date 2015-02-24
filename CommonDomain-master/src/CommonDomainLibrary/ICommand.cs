using System;

namespace CommonDomainLibrary
{
    public interface ICommand : IMessage
    {
        Guid Id { get; set; }
    }
}
