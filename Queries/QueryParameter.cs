namespace Pustalorc.Libraries.MySqlConnectorWrapper.Queries
{
    /// <summary>
    ///     Parameters that can be used within a query.
    /// </summary>
    public class QueryParameter
    {
        /// <summary>
        ///     The name of the parameter to be used as it is in the query.
        /// </summary>
        public string Name;

        /// <summary>
        ///     The value of the parameter to be passed into the query.
        /// </summary>
        public object Value;

        /// <summary>
        ///     Creates a new parameter with the specified name and value.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        public QueryParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}