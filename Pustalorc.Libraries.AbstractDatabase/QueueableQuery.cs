namespace Pustalorc.Libraries.AbstractDatabase
{
    public sealed class QueueableQuery
    {
        public readonly string Query;
        public readonly EQueueableType QueryType;

        public QueueableQuery(string query, EQueueableType type)
        {
            Query = query;
            QueryType = type;
        }
    }
}