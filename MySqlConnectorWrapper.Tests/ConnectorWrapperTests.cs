using System;
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
        public void CreateDropTableTest()
        {
            var output = _database.ExecuteQuery(new Query("CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);",
                EQueryType.NonQuery));

            Assert.IsTrue(int.TryParse(output.Output?.ToString(), out var result) && result >= 0);

            output = _database.ExecuteQuery(new Query("DROP TABLE `test`;", EQueryType.NonQuery));

            Assert.IsTrue(int.TryParse(output.Output?.ToString(), out result) && result >= 0);
        }

        [TestMethod]
        public void ExecuteQueryTest()
        {
            _database.ExecuteQuery(new Query("CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);",
                EQueryType.NonQuery));

            var output = _database.ExecuteQuery(new Query("SHOW TABLES LIKE 'test';", EQueryType.Scalar));

            _database.ExecuteQuery(new Query("DROP TABLE `test`;", EQueryType.NonQuery));

            Assert.IsTrue("test".Equals(output.Output.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void ExecuteTransactionTest()
        {
            _database.ExecuteTransaction(
                new Query("CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);", EQueryType.NonQuery),
                new Query("CREATE TABLE `test2` (`id` VARCHAR(5) NOT NULL);", EQueryType.NonQuery),
                new Query("CREATE TABLE `test3` (`id` VARCHAR(5) NOT NULL);", EQueryType.NonQuery));

            var allOut = _database.ExecuteTransaction(new Query("SHOW TABLES LIKE 'test';", EQueryType.Scalar),
                new Query("SHOW TABLES LIKE 'test2';", EQueryType.Scalar),
                new Query("SHOW TABLES LIKE 'test3';", EQueryType.Scalar));


            _database.ExecuteTransaction(new Query("DROP TABLE `test`;", EQueryType.NonQuery),
                new Query("DROP TABLE `test2`;", EQueryType.NonQuery),
                new Query("DROP TABLE `test3`;", EQueryType.NonQuery));

            Assert.IsTrue(allOut.All(k => k.Output != null));
        }
    }
}