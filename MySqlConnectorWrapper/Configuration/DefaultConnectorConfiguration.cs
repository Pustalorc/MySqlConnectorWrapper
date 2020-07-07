namespace Pustalorc.Libraries.MySqlConnectorWrapper.Configuration
{
    /// <inheritdoc />
    /// <summary>
    /// This is an example configuration. To modify it, create your own connector configuration by inheriting from the
    /// IConnectorConfiguration interface.
    /// </summary>
    public sealed class DefaultConnectorConfiguration : IConnectorConfiguration
    {
        public string DatabaseAddress => "localhost";
        public ushort DatabasePort => 3306;
        public string DatabaseUsername => "myUsername";
        public string DatabasePassword => "password";
        public string DatabaseName => "database";
        public string ConnectionStringExtras => "";
        public bool UseCache => true;
        public double CacheRefreshRequestInterval => 1250;
        public ulong CacheSize => 15;
    }
}