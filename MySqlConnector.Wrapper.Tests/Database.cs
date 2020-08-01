namespace Pustalorc.MySqlConnector.Wrapper.Tests
{
    public class Database : ConnectorWrapper<DefaultConnectorConfiguration>
    {
        public Database() : base(new DefaultConnectorConfiguration())
        {
        }
    }
}