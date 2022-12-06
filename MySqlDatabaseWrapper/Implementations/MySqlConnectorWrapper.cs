using System.Data.Common;
using JetBrains.Annotations;
using MySqlConnector;
using Pustalorc.MySqlDatabaseWrapper.Abstractions;
using Pustalorc.MySqlDatabaseWrapper.Configuration;

namespace Pustalorc.MySqlDatabaseWrapper.Implementations;

/// <inheritdoc />
/// <summary>
///     A wrapper for MySqlConnector v2.1.10
/// </summary>
[UsedImplicitly]
public class MySqlConnectorWrapper<TMySqlConfiguration> : MySqlConnectionWrapper<TMySqlConfiguration>
    where TMySqlConfiguration : IMySqlConfiguration
{
    /// <inheritdoc />
    public MySqlConnectorWrapper(TMySqlConfiguration configuration) : base(configuration)
    {
    }

    /// <inheritdoc />
    protected override DbConnection GetConnection()
    {
        return new MySqlConnection(new MySqlConnectionStringBuilder(Configuration.ConnectionString)
        {
            Server = Configuration.MySqlServerAddress, Port = Configuration.MySqlServerPort,
            Database = Configuration.DatabaseName, UserID = Configuration.DatabaseUsername,
            Password = Configuration.DatabasePassword
        }.ConnectionString);
    }
}