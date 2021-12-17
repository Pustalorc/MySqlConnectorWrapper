using Pustalorc.Libraries.FrequencyCache.Interfaces;

namespace Pustalorc.MySqlConnector.Wrapper.Configuration;

/// <summary>
/// Basic configuration required for the connector to work properly.
/// </summary>
public interface IConnectorConfiguration : ICacheConfiguration
{
    /// <summary>
    /// The format of the connection string. May include extras to it.
    /// </summary>
    public string ConnectionStringFormat { get; }

    /// <summary>
    /// The address (IP or Domain Name) of the database.
    /// </summary>
    public string DatabaseAddress { get; }

    /// <summary>
    /// The port of the database (3306 by default).
    /// </summary>
    public ushort DatabasePort { get; }

    /// <summary>
    /// The username for read (and maybe write) access to the database.
    /// </summary>
    public string DatabaseUsername { get; }

    /// <summary>
    /// The password for the username above to provide the access to the database.
    /// </summary>
    public string DatabasePassword { get; }

    /// <summary>
    /// The name of the database where main data should be stored at.
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    /// If set to true, any read queries will also be cached and updated once in a while.
    /// </summary>
    public bool UseCache { get; }
}