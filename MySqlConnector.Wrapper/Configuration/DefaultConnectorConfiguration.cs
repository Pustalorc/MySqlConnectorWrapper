namespace Pustalorc.MySqlConnector.Wrapper.Configuration
{
    /// <inheritdoc />
    /// <summary>
    /// This is an example configuration. To modify it, create your own connector configuration by inheriting from the
    /// IConnectorConfiguration interface.
    /// </summary>
    public sealed class DefaultConnectorConfiguration : IConnectorConfiguration
    {
        /// <inheritdoc />
        public string ConnectionStringFormat => "SERVER={0};DATABASE={1};UID={2};PASSWORD={3};PORT={4};";

        /// <inheritdoc />
        public string DatabaseAddress => "localhost";

        /// <inheritdoc />
        public ushort DatabasePort => 3306;

        /// <inheritdoc />
        public string DatabaseUsername => "root";

        /// <inheritdoc />
        public string DatabasePassword => "toor";

        /// <inheritdoc />
        public string DatabaseName => "database";

        /// <inheritdoc />
        public bool UseCache => true;

        /// <inheritdoc />
        public double CacheRefreshRequestInterval => 1250;

        /// <inheritdoc />
        public int CacheSize => 15;
    }
}