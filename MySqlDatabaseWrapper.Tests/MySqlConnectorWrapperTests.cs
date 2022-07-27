using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using Pustalorc.Libraries.DbConnectionWrapper.QueryAbstraction;
using Pustalorc.MySqlDatabaseWrapper.Implementations;
using Xunit;

namespace Pustalorc.MySqlDatabaseWrapper.Tests;

public sealed class MySqlConnectorWrapperTests
{
    private MySqlConnectorWrapper<DefaultConnectorConfiguration> Database { get; } =
        new(new DefaultConnectorConfiguration());

    [Fact]
    public void CreateDropTableTest()
    {
        var output = Database.ExecuteQuery("CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);");
        Assert.True(output.GetNonQueryResult() == 0);

        output = Database.ExecuteQuery("DROP TABLE `test`;");
        Assert.True(output.GetNonQueryResult() == 0);
    }

    [Fact]
    public void ExecuteQueryTest()
    {
        Database.ExecuteQuery("CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);");

        var output = Database.ExecuteQuery("SHOW TABLES LIKE 'test';", EQueryType.Scalar);

        Database.ExecuteQuery("DROP TABLE `test`;");
        Assert.True(string.Equals(output.GetTFromResult<string>(), "test", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExecuteTransactionTest()
    {
        Database.ExecuteTransaction(new Query("CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);"),
            new Query("CREATE TABLE `test2` (`id` VARCHAR(5) NOT NULL);"),
            new Query("CREATE TABLE `test3` (`id` VARCHAR(5) NOT NULL);"));

        var allOut = Database.ExecuteTransaction(new Query("SHOW TABLES LIKE 'test';", EQueryType.Scalar),
            new Query("SHOW TABLES LIKE 'test2';", EQueryType.Scalar),
            new Query("SHOW TABLES LIKE 'test3';", EQueryType.Scalar));

        Database.ExecuteTransaction(new Query("DROP TABLE `test`;"), new Query("DROP TABLE `test2`;"),
            new Query("DROP TABLE `test3`;"));

        Assert.True(allOut.All(k => k.Result is not null));
    }

    [Fact]
    public void NestedQueryInQueryTest()
    {
        var output =
            Database.ExecuteQuery("SHOW TABLES LIKE 'test';", EQueryType.Scalar, NestedQueryInQueryTestCallback);

        Database.ExecuteQuery("DROP TABLE `test`;");
        Assert.True(output.Result is null);
    }

    private void NestedQueryInQueryTestCallback(QueryOutput output, DbConnection connection, DbTransaction? transaction)
    {
        if (string.Equals(output.GetTFromResult<string>(), "test", StringComparison.OrdinalIgnoreCase))
            return;

        Database.ExecuteQueryWithOpenConnection(connection, transaction,
            "CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);");

        var secondaryOutput =
            Database.ExecuteQueryWithOpenConnection(connection, transaction, "SHOW TABLES LIKE 'test';",
                EQueryType.Scalar);

        Assert.True(string.Equals("test", secondaryOutput.GetTFromResult<string>(),
            StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NestedTransactionInQueryTest()
    {
        var output = Database.ExecuteQuery("CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);",
            callback: NestedTransactionInQueryTestCallback);

        Database.ExecuteQuery("DROP TABLE `test`;");
        Assert.True(output.GetNonQueryResult() == 0);
    }

    private void NestedTransactionInQueryTestCallback(QueryOutput queryOutput, DbConnection connection,
        DbTransaction? transaction)
    {
        Database.ExecuteTransactionWithOpenConnection(connection, new Query("INSERT INTO `test` (`id`) VALUES('one')"),
            new Query("INSERT INTO `test` (`id`) VALUES('two')"),
            new Query("INSERT INTO `test` (`id`) VALUES('three')"),
            new Query("INSERT INTO `test` (`id`) VALUES('four')"),
            new Query("INSERT INTO `test` (`id`) VALUES('five')"));

        var output = Database.ExecuteQueryWithOpenConnection(connection, transaction, "SELECT COUNT(*) FROM `test`;",
            EQueryType.Scalar);

        Assert.True(output.GetTFromResult<long>() == 5);
    }

    [Fact]
    public void MultiThreadTest()
    {
        var manualReset = new ManualResetEvent(false);
        var autoReset1 = new AutoResetEvent(false);
        var autoReset2 = new AutoResetEvent(false);

        Database.ExecuteTransaction(new Query("CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);"),
            new Query("CREATE TABLE `test2` (`id` VARCHAR(5) NOT NULL);"),
            new Query("CREATE TABLE `test3` (`id` VARCHAR(5) NOT NULL);"));

        ThreadPool.QueueUserWorkItem(_ =>
        {
            manualReset.WaitOne();
            Database.ExecuteTransaction(new Query("INSERT INTO `test` (`id`) VALUES('one')"),
                new Query("INSERT INTO `test` (`id`) VALUES('two')"),
                new Query("INSERT INTO `test` (`id`) VALUES('three')"),
                new Query("INSERT INTO `test` (`id`) VALUES('four')"),
                new Query("INSERT INTO `test` (`id`) VALUES('five')"));
            autoReset1.Set();
        });
        ThreadPool.QueueUserWorkItem(_ =>
        {
            manualReset.WaitOne();
            Database.ExecuteQuery("INSERT INTO `test` (`id`) VALUES('six')");
            autoReset2.Set();
        });

        Thread.Sleep(1000);
        manualReset.Set();
        autoReset1.WaitOne();
        autoReset2.WaitOne();
        Database.ExecuteTransaction(new Query("DROP TABLE `test`;"), new Query("DROP TABLE `test2`;"),
            new Query("DROP TABLE `test3`;"));
    }
}