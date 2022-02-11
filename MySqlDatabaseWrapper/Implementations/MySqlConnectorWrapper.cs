using System.Data.Common;
using JetBrains.Annotations;
using MySqlConnector;
using Pustalorc.MySqlDatabaseWrapper.Abstraction;
using Pustalorc.MySqlDatabaseWrapper.Configuration;

namespace Pustalorc.MySqlDatabaseWrapper.Implementations;

/// <inheritdoc />
/// <summary>
/// A wrapper for MySqlConnector v2.1.6
/// </summary>
[UsedImplicitly]
public class MySqlConnectorWrapper<TConnectorConfiguration> : DatabaseConnectorWrapper<TConnectorConfiguration>
    where TConnectorConfiguration : IConnectorConfiguration
{
    /// <inheritdoc />
    public MySqlConnectorWrapper(TConnectorConfiguration configuration) : base(configuration,
        new MySqlConnectionStringBuilder(configuration.ConnectionString)
        {
            Server = configuration.MySqlServerAddress, Port = configuration.MySqlServerPort,
            Database = configuration.DatabaseName, UserID = configuration.DatabaseUsername,
            Password = configuration.DatabasePassword
        })
    {
    }

    /// <inheritdoc />
    protected override DbConnectionStringBuilder GetConnectionStringBuilder()
    {
        return new MySqlConnectionStringBuilder(Configuration.ConnectionString)
        {
            Server = Configuration.MySqlServerAddress, Port = Configuration.MySqlServerPort,
            Database = Configuration.DatabaseName, UserID = Configuration.DatabaseUsername,
            Password = Configuration.DatabasePassword
        };
    }

    /// <inheritdoc />
    protected override DbConnection GetConnection()
    {
        return new MySqlConnection(ConnectionString);
    }
}