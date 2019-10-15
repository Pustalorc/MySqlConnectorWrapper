using Pustalorc.Libraries.MySqlConnectorWrapper.Configuration;

namespace Pustalorc.Libraries.MySqlConnectorWrapper.Tests
{
    public sealed class DefaultConnectorConfiguration : IConnectorConfiguration
    {
        public string DatabaseAddress => "localhost";
        public ushort DatabasePort => 3306;
        public string DatabaseUsername => "root";
        public string DatabasePassword => "";
        public string DatabaseName => "database";
        public bool UseCache => true;
        public ulong CacheRefreshIntervalMilliseconds => 1250;
    }
}