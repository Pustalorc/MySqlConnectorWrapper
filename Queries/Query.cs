using Pustalorc.Libraries.MySqlConnector.Queueing;

namespace Pustalorc.Libraries.MySqlConnector.Queries
{
    public sealed class Query
    {
        public string QueryString;
        public EQueryType QueryType;

        public Query(string query, EQueryType type)
        {
            QueryString = query;
            QueryType = type;
        }
    }
}