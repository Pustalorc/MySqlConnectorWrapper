using System;

namespace Pustalorc.Libraries.MySqlConnector.Caching
{
    // Basic cache object, to be modified to be compliant with latest changes.
    // Requires self-updating using a Timer object.
    public sealed class Cache
    {
        public readonly string Query;
        public readonly DateTime TimeCreated = DateTime.Now;
        public ulong AccessRecord;
        public DateTime LastCacheUpdate;

        public object Output;

        public Cache(string query, object output)
        {
            Query = query;
            Output = output;
            LastCacheUpdate = DateTime.Now;
        }
    }
}