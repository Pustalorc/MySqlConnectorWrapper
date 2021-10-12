using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Pustalorc.MySql.Data.Wrapper.Configuration;
using Pustalorc.MySql.Data.Wrapper.Queries;

namespace Pustalorc.MySql.Data.Wrapper.Tests
{
    [TestClass]
    public class ConnectorWrapperTests
    {
        private readonly ConnectorWrapper<DefaultConnectorConfiguration, string> m_Database = new(new DefaultConnectorConfiguration(), StringComparer.OrdinalIgnoreCase);

        [TestMethod]
        public void CreateDropTableTest()
        {
            var output = m_Database.ExecuteQuery(m_Database.CreateQuery("CREATE TABLE",
                "CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);", EQueryType.NonQuery));

            Assert.IsTrue(int.TryParse(output.Output?.ToString(), out var result) && result >= 0);

            output = m_Database.ExecuteQuery(m_Database.CreateQuery("DROP TABLE", "DROP TABLE `test`;", EQueryType.NonQuery));

            Assert.IsTrue(int.TryParse(output.Output?.ToString(), out result) && result >= 0);
        }

        [TestMethod]
        public void ExecuteQueryTest()
        {
            m_Database.ExecuteQuery(m_Database.CreateQuery("CREATE TABLE", "CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);", EQueryType.NonQuery));

            var output = m_Database.ExecuteQuery(m_Database.CreateQuery("SHOW TABLES", "SHOW TABLES LIKE 'test';", EQueryType.Scalar));

            m_Database.ExecuteQuery(m_Database.CreateQuery("DROP TABLE", "DROP TABLE `test`;", EQueryType.NonQuery));

            Assert.IsTrue("test".Equals(output.Output?.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void ExecuteTransactionTest()
        {
            m_Database.ExecuteTransaction(
                m_Database.CreateQuery("CREATE TABLE", "CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);", EQueryType.NonQuery),
                m_Database.CreateQuery("CREATE TABLE", "CREATE TABLE `test2` (`id` VARCHAR(5) NOT NULL);", EQueryType.NonQuery),
                m_Database.CreateQuery("CREATE TABLE", "CREATE TABLE `test3` (`id` VARCHAR(5) NOT NULL);", EQueryType.NonQuery));

            var allOut = m_Database.ExecuteTransaction(m_Database.CreateQuery("SHOW TABLE", "SHOW TABLES LIKE 'test';", EQueryType.Scalar),
                m_Database.CreateQuery("SHOW TABLE", "SHOW TABLES LIKE 'test2';", EQueryType.Scalar),
                m_Database.CreateQuery("SHOW TABLE", "SHOW TABLES LIKE 'test3';", EQueryType.Scalar));

            m_Database.ExecuteTransaction(m_Database.CreateQuery("DROP TABLE", "DROP TABLE `test`;", EQueryType.NonQuery),
                m_Database.CreateQuery("DROP TABLE", "DROP TABLE `test2`;", EQueryType.NonQuery),
                m_Database.CreateQuery("DROP TABLE", "DROP TABLE `test3`;", EQueryType.NonQuery));

            Assert.IsTrue(allOut.All(k => k.Output != null));
        }

        [TestMethod]
        public void NestedQueryTest()
        {
            var output = m_Database.ExecuteQuery(m_Database.CreateQuery("SHOW TABLE", "SHOW TABLES LIKE 'test';", EQueryType.Scalar, callbacks: new Query<string>.QueryCallback[]
            {
                o =>
                {
                    if (o.Output is true) return;

                    m_Database.CreateQuery("CREATE TABLE", "CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);", EQueryType.NonQuery);
                    o.Output = true;
                }
            }));

            Assert.IsTrue(output.Output is true);
        }

        [TestMethod]
        public void MultiThreadTest()
        {
            var manualReset = new ManualResetEvent(false);
            var autoReset1 = new AutoResetEvent(false);
            var autoReset2 = new AutoResetEvent(false);

            m_Database.ExecuteTransaction(
                m_Database.CreateQuery("CREATE TABLE", "CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);",
                    EQueryType.NonQuery),
                m_Database.CreateQuery("CREATE TABLE", "CREATE TABLE `test2` (`id` VARCHAR(5) NOT NULL);",
                    EQueryType.NonQuery),
                m_Database.CreateQuery("CREATE TABLE", "CREATE TABLE `test3` (`id` VARCHAR(5) NOT NULL);",
                    EQueryType.NonQuery));

            ThreadPool.QueueUserWorkItem(_ =>
            {
                manualReset.WaitOne();
                Logger.LogMessage($"[{DateTime.Now.Ticks}]: Task 1 initialized");
                try
                {
                    m_Database.ExecuteTransaction(m_Database.CreateQuery("SHOW TABLE", "SHOW TABLES LIKE 'test';", EQueryType.Scalar),
                        m_Database.CreateQuery("SHOW TABLE", "SHOW TABLES LIKE 'test2';", EQueryType.Scalar),
                        m_Database.CreateQuery("SHOW TABLE", "SHOW TABLES LIKE 'test3';", EQueryType.Scalar));
                }
                catch (Exception e)
                {
                    Logger.LogMessage(e.ToString());
                }

                Logger.LogMessage($"[{DateTime.Now.Ticks}]: Task 1 finalized");
                autoReset2.Set();
            });
            ThreadPool.QueueUserWorkItem(_ =>
            {
                manualReset.WaitOne();
                Logger.LogMessage($"[{DateTime.Now.Ticks}]: Task 2 initialized");
                try
                {
                    m_Database.ExecuteTransaction(
                        m_Database.CreateQuery("DROP TABLE", "DROP TABLE `test`;", EQueryType.NonQuery),
                        m_Database.CreateQuery("DROP TABLE", "DROP TABLE `test2`;", EQueryType.NonQuery),
                        m_Database.CreateQuery("DROP TABLE", "DROP TABLE `test3`;", EQueryType.NonQuery));
                }
                catch (Exception e)
                {
                    Logger.LogMessage(e.ToString());
                }

                Logger.LogMessage($"[{DateTime.Now.Ticks}]: Task 2 finalized");
                autoReset1.Set();
            });

            Thread.Sleep(1000);
            manualReset.Set();
            autoReset1.WaitOne();
            autoReset2.WaitOne();
            m_Database.ExecuteTransaction(m_Database.CreateQuery("DROP TABLE", "DROP TABLE IF EXISTS `test`;", EQueryType.NonQuery),
                m_Database.CreateQuery("DROP TABLE", "DROP TABLE IF EXISTS `test2`;", EQueryType.NonQuery),
                m_Database.CreateQuery("DROP TABLE", "DROP TABLE IF EXISTS `test3`;", EQueryType.NonQuery));
        }
    }
}