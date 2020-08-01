using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pustalorc.MySql.Data.Wrapper.Queries;

namespace Pustalorc.MySql.Data.Wrapper.Tests
{
    [TestClass]
    public class ConnectorWrapperTests
    {
        private readonly Database m_Database = new Database();

        [TestMethod]
        public void CreateDropTableTest()
        {
            var output = m_Database.ExecuteQuery(new Query(null, "CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);",
                EQueryType.NonQuery));

            Assert.IsTrue(int.TryParse(output.Output?.ToString(), out var result) && result >= 0);

            output = m_Database.ExecuteQuery(new Query(null, "DROP TABLE `test`;", EQueryType.NonQuery));

            Assert.IsTrue(int.TryParse(output.Output?.ToString(), out result) && result >= 0);
        }

        [TestMethod]
        public void ExecuteQueryTest()
        {
            m_Database.ExecuteQuery(new Query(null, "CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);",
                EQueryType.NonQuery));

            var output = m_Database.ExecuteQuery(new Query(null, "SHOW TABLES LIKE 'test';", EQueryType.Scalar));

            m_Database.ExecuteQuery(new Query(null, "DROP TABLE `test`;", EQueryType.NonQuery));

            Assert.IsTrue("test".Equals(output.Output.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void ExecuteTransactionTest()
        {
            m_Database.ExecuteTransaction(
                new Query(null, "CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);", EQueryType.NonQuery),
                new Query(null, "CREATE TABLE `test2` (`id` VARCHAR(5) NOT NULL);", EQueryType.NonQuery),
                new Query(null, "CREATE TABLE `test3` (`id` VARCHAR(5) NOT NULL);", EQueryType.NonQuery));

            var allOut = m_Database.ExecuteTransaction(new Query(null, "SHOW TABLES LIKE 'test';", EQueryType.Scalar),
                new Query(null, "SHOW TABLES LIKE 'test2';", EQueryType.Scalar),
                new Query(null, "SHOW TABLES LIKE 'test3';", EQueryType.Scalar));

            m_Database.ExecuteTransaction(new Query(null, "DROP TABLE `test`;", EQueryType.NonQuery),
                new Query(null, "DROP TABLE `test2`;", EQueryType.NonQuery),
                new Query(null, "DROP TABLE `test3`;", EQueryType.NonQuery));

            Assert.IsTrue(allOut.All(k => k.Output != null));
        }
    }
}