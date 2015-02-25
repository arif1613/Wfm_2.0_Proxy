using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edit
{
    public class StorageSizeException : Exception
    {
        public StorageSizeException(String message) : base(message) { }

        public StorageSizeException(String message, Exception innerException) : base(message, innerException) { }
    }
}
