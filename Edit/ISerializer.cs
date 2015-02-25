using System;
using System.IO;

namespace Edit
{
    public interface ISerializer
    {
        void Serialize<T>(T instance, Stream target) where T : class;
        T Deserialize<T>(Stream source);
        object Deserialize(Type type, Stream source);
    }
}
