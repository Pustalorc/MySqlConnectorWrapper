namespace Pustalorc.Libraries.MySqlConnector.Configuration
{
    /// <inheritdoc />
    /// <summary>
    ///     This is an example configuration. To modify it, create your own connector configuration by inheriting from the
    ///     IConnectorConfiguration interface.
    /// </summary>
    public sealed class DefaultConnectorConfiguration : IConnectorConfiguration
    {
        public string DatabaseAddress => "localhost";
        public ushort DatabasePort => 3306;
        public string DatabaseUsername => "root";
        public string DatabasePassword => "password";
        public string DatabaseName => "database";
        public bool UseSeparateThread => true;
        public bool UseCache => true;
        public int MaxCacheSize => 10;
        public ulong CacheRefreshIntervalMilliseconds => 10000;
    }
}