using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Edit
{
    public interface IStreamStore
    {
        Task WriteAsync(string streamName, IEnumerable<Chunk> chunks, IStoredDataVersion expectedVersion);

        Task WriteAsync(string streamName, IEnumerable<Chunk> chunks, TimeSpan timeout,
            IStoredDataVersion expectedVersion);

        Task WriteAsync(string streamName, IEnumerable<Chunk> chunks, CancellationToken token,
            IStoredDataVersion expectedVersion);

        Task WriteAsync(string streamName, IEnumerable<Chunk> chunks, TimeSpan timeout, CancellationToken token,
            IStoredDataVersion expectedVersion);

        Task<ChunkSet> ReadAsync(string streamName);
        Task<ChunkSet> ReadAsync(string streamName, TimeSpan timeout);
        Task<ChunkSet> ReadAsync(string streamName, CancellationToken token);
        Task<ChunkSet> ReadAsync(string streamName, TimeSpan timeout, CancellationToken token);
    }

}

