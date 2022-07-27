using System.Data.Common;
using JetBrains.Annotations;
using MySql.Data.MySqlClient;
using Pustalorc.MySqlDatabaseWrapper.Abstractions;
using Pustalorc.MySqlDatabaseWrapper.Configuration;

namespace Pustalorc.MySqlDatabaseWrapper.Implementations;

/// <inheritdoc />
/// <summary>
/// A wrapper for MySql.Data v8.0.29
/// </summary>
[UsedImplicitly]
public class MySqlDataWrapper<TMySqlConfiguration> : MySqlConnectionWrapper<TMySqlConfiguration>
    where TMySqlConfiguration : IMySqlConfiguration
{
    /// <inheritdoc />
    public MySqlDataWrapper(TMySqlConfiguration configuration) : base(configuration)
    {
    }

    /// <inheritdoc />
    protected override DbConnection GetConnection()
    {
        return new MySqlConnection(new MySqlConnectionStringBuilder(Configuration.ConnectionString)
        {
            Server = Configuration.MySqlServerAddress,
            Port = Configuration.MySqlServerPort,
            Database = Configuration.DatabaseName,
            UserID = Configuration.DatabaseUsername,
            Password = Configuration.DatabasePassword
        }.ConnectionString);
    }
}