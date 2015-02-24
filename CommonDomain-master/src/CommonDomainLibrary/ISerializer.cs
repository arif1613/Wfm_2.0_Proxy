using System;
using System.IO;

namespace CommonDomainLibrary
{
    public interface ISerializer
    {
        T Deserialize<T>(Stream stream) where T : class;
        object Deserialize(Type type, Stream stream);
        void Serialize(object obj, Stream stream);
    }
}
