namespace Pustalorc.Libraries.MySqlConnectorWrapper.Configuration
{
    /// <summary>
    ///     Basic configuration required for the connector to work properly.
    /// </summary>
    public interface IConnectorConfiguration
    {
        /// <summary>
        ///     The address (IP or Domain Name) of the database.
        /// </summary>
        string DatabaseAddress { get; }

        /// <summary>
        ///     The port of the database (3306 by default).
        /// </summary>
        ushort DatabasePort { get; }

        /// <summary>
        ///     The username for read (and maybe write) access to the database.
        /// </summary>
        string DatabaseUsername { get; }

        /// <summary>
        ///     The password for the username above to provide the access to the database.
        /// </summary>
        string DatabasePassword { get; }

        /// <summary>
        ///     The name of the database where main data should be stored at.
        /// </summary>
        string DatabaseName { get; }

        /// <summary>
        ///     If set to true, any read queries will also be cached and updated once in a while.
        /// </summary>
        bool UseCache { get; }

        /// <summary>
        ///     Determines how long each cache object should wait before retrieving information from the database about itself.
        /// </summary>
        ulong CacheRefreshIntervalMilliseconds { get; }
    }
}