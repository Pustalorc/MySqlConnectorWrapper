using System;
using Pustalorc.Libraries.MySqlConnectorWrapper.Queries;

namespace Pustalorc.Libraries.MySqlConnectorWrapper.Caching
{
    public sealed class CachedQuery : QueryOutput
    {
        public int AccessCount;
        public DateTime LastAccess;
        public readonly DateTime Created;

        public double Weight => AccessCount == 0
            ? 1000
            : DateTime.Now.Subtract(Created).TotalMilliseconds * DateTime.Now.Subtract(LastAccess).TotalMilliseconds /
              (AccessCount * 1000);

        public CachedQuery(QueryOutput query) : base(query.Query, query.Output)
        {
            LastAccess = DateTime.Now;
            Created = DateTime.Now;
        }
    }
}