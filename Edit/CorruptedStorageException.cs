using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edit
{
    public class CorruptedStorageException : Exception
    {
        public CorruptedStorageException(String message) : base(message) { }

        public CorruptedStorageException(String message, Exception innerException) : base(message, innerException) { }
    }
}
