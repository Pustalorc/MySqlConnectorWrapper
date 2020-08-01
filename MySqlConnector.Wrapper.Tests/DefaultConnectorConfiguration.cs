using Pustalorc.MySqlConnector.Wrapper.Configuration;

namespace Pustalorc.MySqlConnector.Wrapper.Tests
{
    public sealed class DefaultConnectorConfiguration : IConnectorConfiguration
    {
        public string DatabaseAddress => "localhost";
        public ushort DatabasePort => 3306;
        public string DatabaseUsername => "root";
        public string DatabasePassword => "toor";
        public string DatabaseName => "database";
        public string ConnectionStringExtras => "";
        public bool UseCache => true;
        public double CacheRefreshRequestInterval => 1250;
        public ulong CacheSize => 15;
    }
}