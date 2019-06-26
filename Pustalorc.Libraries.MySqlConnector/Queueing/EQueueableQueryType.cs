namespace Pustalorc.Libraries.MySqlConnector.Queueing
{
    /// <summary>
    ///     The possible types of queueable queries.
    /// </summary>
    public enum EQueueableQueryType
    {
        Scalar,
        NonQuery,
        Reader
    }
}