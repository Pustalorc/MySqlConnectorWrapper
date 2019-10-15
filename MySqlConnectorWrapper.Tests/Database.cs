namespace Pustalorc.Libraries.MySqlConnectorWrapper.Tests
{
    public class Database : ConnectorWrapper<DefaultConnectorConfiguration>
    {
        public Database() : base(new DefaultConnectorConfiguration())
        {
        }
    }
}