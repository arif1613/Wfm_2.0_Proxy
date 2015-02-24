using System;
using System.Collections.Generic;

namespace CommonDomainLibrary.Common
{
    public interface IState
    {
        Guid Id { get; }
        OrderedDictionary<Guid, List<dynamic>> Messages { get; }        
    }
}