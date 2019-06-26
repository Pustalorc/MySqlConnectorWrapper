namespace Pustalorc.Libraries.MySqlConnector.Queueing
{
    /// <summary>
    ///     The possible types of queueable queries.
    /// </summary>
    public enum EQueryType
    {
        Scalar,
        NonQuery,
        Reader
    }
}