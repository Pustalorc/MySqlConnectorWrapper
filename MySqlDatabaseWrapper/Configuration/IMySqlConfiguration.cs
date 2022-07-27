namespace Pustalorc.MySqlDatabaseWrapper.Configuration;

/// <summary>
/// Basic configuration required for the connector to work properly.
/// </summary>
public interface IMySqlConfiguration
{
    /// <summary>
    /// The address (IP or Domain Name) of the database.
    /// </summary>
    public string MySqlServerAddress { get; }

    /// <summary>
    /// The port of the database (3306 by default).
    /// </summary>
    public ushort MySqlServerPort { get; }

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
    /// The connection string used on the string builder. Used to include extra options not exposed by default.
    /// </summary>
    public string ConnectionString { get; }
}