using System.Data.Common;
using JetBrains.Annotations;
using MySql.Data.MySqlClient;
using Pustalorc.MySqlDatabaseWrapper.Abstraction;
using Pustalorc.MySqlDatabaseWrapper.Configuration;

namespace Pustalorc.MySqlDatabaseWrapper.Implementations;

[UsedImplicitly]
public class MySqlDataWrapper<TConnectorConfiguration> : DatabaseConnectorWrapper<TConnectorConfiguration>
    where TConnectorConfiguration : IConnectorConfiguration
{
    public MySqlDataWrapper(TConnectorConfiguration configuration) : base(configuration,
        new MySqlConnectionStringBuilder(configuration.ConnectionString)
        {
            Server = configuration.MySqlServerAddress, Port = configuration.MySqlServerPort,
            Database = configuration.DatabaseName, UserID = configuration.DatabaseUsername,
            Password = configuration.DatabasePassword
        })
    {
    }

    protected override DbConnectionStringBuilder GetConnectionStringBuilder()
    {
        return new MySqlConnectionStringBuilder(Configuration.ConnectionString)
        {
            Server = Configuration.MySqlServerAddress,
            Port = Configuration.MySqlServerPort,
            Database = Configuration.DatabaseName,
            UserID = Configuration.DatabaseUsername,
            Password = Configuration.DatabasePassword
        };
    }

    protected override DbConnection GetConnection()
    {
        return new MySqlConnection(ConnectionString);
    }
}