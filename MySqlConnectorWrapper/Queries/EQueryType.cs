namespace Pustalorc.Libraries.MySqlConnectorWrapper.Queries
{
    /// <summary>
    ///     The possible types of queueable queries.
    /// </summary>
    public enum EQueryType
    {
        /// <summary>
        ///     A query that requires a single output value. Eg: SELECT `Column` FROM
        /// </summary>
        Scalar,

        /// <summary>
        ///     A query that does not expect an output value (other than the number of affected rows). Eg: INSERT INTO
        /// </summary>
        NonQuery,

        /// <summary>
        ///     A Query that requires multiple output values. Eg: SELECT * FROM
        /// </summary>
        Reader
    }
}