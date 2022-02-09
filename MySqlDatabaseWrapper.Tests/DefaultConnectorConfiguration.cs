using Pustalorc.MySqlDatabaseWrapper.Configuration;

namespace Pustalorc.MySqlDatabaseWrapper.Tests;

public sealed class DefaultConnectorConfiguration : IConnectorConfiguration
{
    public bool EnableCacheRefreshes => false;
    public double CacheRefreshRequestInterval => 0;
    public int CacheSize => -1;
    public string MySqlServerAddress => "127.0.0.1";
    public ushort MySqlServerPort => 3306;
    public string DatabaseUsername => "root";
    public string DatabasePassword => "toor";
    public string DatabaseName => "test";
    public string ConnectionString => "";
    public bool UseCache => false;
}