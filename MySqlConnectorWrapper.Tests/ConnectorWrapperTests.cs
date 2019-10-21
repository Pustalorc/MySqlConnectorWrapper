using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pustalorc.Libraries.MySqlConnectorWrapper.Queries;

namespace Pustalorc.Libraries.MySqlConnectorWrapper.Tests
{
    [TestClass]
    public class ConnectorWrapperTests
    {
        private readonly Database _database = new Database();

        [TestMethod]
        public void ExecuteQueryTest()
        {
            var output = _database.ExecuteQuery(new Query("SHOW TABLES LIKE 'test';", EQueryType.Scalar));
            Assert.IsNull(output.Output);
        }

        [TestMethod]
        public void ExecuteTransactionTest()
        {
            var allOut = _database.ExecuteTransaction(new Query("SHOW TABLES LIKE 'test';", EQueryType.Scalar),
                new Query("SHOW TABLES LIKE 'test2';", EQueryType.Scalar),
                new Query("SHOW TABLES LIKE 'test3';", EQueryType.Scalar));

            Assert.IsFalse(allOut.Any(k => k.Output != null));
        }
    }
}