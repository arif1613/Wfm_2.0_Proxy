using System.Collections.Generic;

namespace Edit
{
    public sealed class ChunkSet
    {
        public IStoredDataVersion Version { get; set; }
        public IEnumerable<Chunk> Chunks { get; set; }

        public ChunkSet(IEnumerable<Chunk> chunks, IStoredDataVersion version)
        {
            Chunks = new List<Chunk>(chunks);
            Version = version;
        }
    }
}