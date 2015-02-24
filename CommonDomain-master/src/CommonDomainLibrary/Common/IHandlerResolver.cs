using System;
using System.Collections.Generic;

namespace CommonDomainLibrary.Common
{
    public interface IHandlerResolver
    {
        object Resolve(Type handlerType);
        object Resolve(Type handlerType, Dictionary<string, object> parameters);
    }
}
