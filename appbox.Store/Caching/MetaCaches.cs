using System;
using appbox.Caching;

namespace appbox.Store
{
    static class MetaCaches
    {

        internal static readonly LRUCache<BytesKey, ulong> PartitionCaches =
            new LRUCache<BytesKey, ulong>(128, BytesKeyEqualityComparer.Default); //TODO: fix limit

    }
}
