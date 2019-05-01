using System;

namespace Pustalorc.Libraries.AbstractDatabase
{
    public sealed class Cache
    {
        public readonly DateTime LastCacheUpdate;
        public readonly object Output;
        public readonly string Query;

        public Cache(string query, object output)
        {
            Query = query;
            Output = output;
            LastCacheUpdate = DateTime.Now;
        }
    }
}