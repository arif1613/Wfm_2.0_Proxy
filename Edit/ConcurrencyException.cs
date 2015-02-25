using System;

namespace Edit
{
    public class ConcurrencyException : Exception
    {
        public string StreamName { get; private set; }
        public IStoredDataVersion ExpectedVersion { get; private set; }

        public ConcurrencyException(string streamName, IStoredDataVersion expectedVersion)
            : base(string.Format("Expected version {0} in stream '{1}' but it has been changed since", expectedVersion, streamName))
        {
            StreamName = streamName;
            ExpectedVersion = expectedVersion;
        }
    }
}