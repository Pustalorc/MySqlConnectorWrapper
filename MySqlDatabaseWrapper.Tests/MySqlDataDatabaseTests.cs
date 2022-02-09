using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Pustalorc.MySqlDatabaseWrapper.DatabaseTypes.Execution;
using Xunit;

namespace Pustalorc.MySqlDatabaseWrapper.Tests;

public sealed class MySqlDataDatabaseTests
{
    private MySqlDataDatabase<DefaultConnectorConfiguration, string> Database { get; } =
        new(new DefaultConnectorConfiguration(), StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void CreateDropTableTest()
    {
        var output =
            Database.ExecuteQuery("CREATE TABLE", new Query("CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);"));

        Assert.True(output.Result is 0);

        output = Database.ExecuteQuery("DROP TABLE", new Query("DROP TABLE `test`;"));

        Assert.True(output.Result is 0);
    }

    [Fact]
    public void ExecuteQueryTest()
    {
        Database.ExecuteQuery("CREATE TABLE", new Query("CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);"));

        var output = Database.ExecuteQuery("SHOW TABLES", new Query("SHOW TABLES LIKE 'test';", EQueryType.Scalar));

        Database.ExecuteQuery("DROP TABLE", new Query("DROP TABLE `test`;"));

        Assert.True(
            output.Result != null && "test".Equals(output.Result.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExecuteTransactionTest()
    {
        Database.ExecuteTransaction(
            new KeyValuePair<string, Query>("CREATE TABLE",
                new Query("CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);")),
            new KeyValuePair<string, Query>("CREATE TABLE",
                new Query("CREATE TABLE `test2` (`id` VARCHAR(5) NOT NULL);")),
            new KeyValuePair<string, Query>("CREATE TABLE",
                new Query("CREATE TABLE `test3` (`id` VARCHAR(5) NOT NULL);")));

        var allOut = Database.ExecuteTransaction(
            new KeyValuePair<string, Query>("SHOW TABLE", new Query("SHOW TABLES LIKE 'test';", EQueryType.Scalar)),
            new KeyValuePair<string, Query>("SHOW TABLE", new Query("SHOW TABLES LIKE 'test2';", EQueryType.Scalar)),
            new KeyValuePair<string, Query>("SHOW TABLE", new Query("SHOW TABLES LIKE 'test3';", EQueryType.Scalar))
        );

        Database.ExecuteTransaction(
            new KeyValuePair<string, Query>("DROP TABLE", new Query("DROP TABLE `test`;")),
            new KeyValuePair<string, Query>("DROP TABLE", new Query("DROP TABLE `test2`;")),
            new KeyValuePair<string, Query>("DROP TABLE", new Query("DROP TABLE `test3`;"))
        );

        Assert.True(allOut.All(k => k.Result != null));
    }

    [Fact]
    public void NestedQueryInQueryTest()
    {
        var output = Database.ExecuteQuery("SHOW TABLE", new Query("SHOW TABLES LIKE 'test';",
            EQueryType.Scalar, mySqlDataCallback: (queryOutput, connection) =>
            {
                if (queryOutput.Result is string result &&
                    string.Equals("test", result, StringComparison.OrdinalIgnoreCase))
                {
                    queryOutput.Result = true;
                    return;
                }

                Database.ExecuteQueryWithOpenConnection(connection, "CREATE TABLE",
                    new Query("CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);"));

                var secondaryOutput = Database
                    .ExecuteQuery("SHOW TABLE", new Query("SHOW TABLES LIKE 'test';", EQueryType.Scalar)).Result;
                queryOutput.Result = secondaryOutput is string secondaryResult &&
                                     string.Equals("test", secondaryResult, StringComparison.OrdinalIgnoreCase);
            }));

        Database.ExecuteQuery("DROP TABLE", new Query("DROP TABLE `test`;"));
        Assert.True(output.Result is true);
    }

    [Fact]
    public void NestedTransactionInQueryTest()
    {
        var output = Database.ExecuteQuery("CREATE TABLE", new Query("CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);",
            mySqlDataCallback: (queryOutput, connection) =>
            {
                Database.ExecuteTransactionWithOpenConnection(connection,
                    new KeyValuePair<string, Query>("INSERT 1", new Query("INSERT INTO `test` (`id`) VALUES('one')")),
                    new KeyValuePair<string, Query>("INSERT 2", new Query("INSERT INTO `test` (`id`) VALUES('two')")),
                    new KeyValuePair<string, Query>("INSERT 3", new Query("INSERT INTO `test` (`id`) VALUES('three')")),
                    new KeyValuePair<string, Query>("INSERT 4", new Query("INSERT INTO `test` (`id`) VALUES('four')")),
                    new KeyValuePair<string, Query>("INSERT 5", new Query("INSERT INTO `test` (`id`) VALUES('five')"))
                );

                var output = Database.ExecuteQueryWithOpenConnection(connection, "COUNT TEST",
                    new Query("SELECT COUNT(*) FROM `test`;", EQueryType.Scalar));

                queryOutput.Result = output.Result is 5L;
            }));

        Database.ExecuteQuery("DROP TABLE", new Query("DROP TABLE `test`;"));
        Assert.True(output.Result is true);
    }

    [Fact]
    public void MultiThreadTest()
    {
        var manualReset = new ManualResetEvent(false);
        var autoReset1 = new AutoResetEvent(false);
        var autoReset2 = new AutoResetEvent(false);

        Database.ExecuteTransaction(
            new KeyValuePair<string, Query>("CREATE TABLE",
                new Query("CREATE TABLE `test` (`id` VARCHAR(5) NOT NULL);")),
            new KeyValuePair<string, Query>("CREATE TABLE",
                new Query("CREATE TABLE `test2` (`id` VARCHAR(5) NOT NULL);")),
            new KeyValuePair<string, Query>("CREATE TABLE",
                new Query("CREATE TABLE `test3` (`id` VARCHAR(5) NOT NULL);"))
        );

        ThreadPool.QueueUserWorkItem(_ =>
        {
            manualReset.WaitOne();
            Database.ExecuteTransaction(
                new KeyValuePair<string, Query>("INSERT 1", new Query("INSERT INTO `test` (`id`) VALUES('one')")),
                new KeyValuePair<string, Query>("INSERT 2", new Query("INSERT INTO `test2` (`id`) VALUES('two')")),
                new KeyValuePair<string, Query>("INSERT 3", new Query("INSERT INTO `test` (`id`) VALUES('three')")),
                new KeyValuePair<string, Query>("INSERT 4", new Query("INSERT INTO `test3` (`id`) VALUES('four')")),
                new KeyValuePair<string, Query>("INSERT 5", new Query("INSERT INTO `test` (`id`) VALUES('five')"))
            );
            autoReset1.Set();
        });
        ThreadPool.QueueUserWorkItem(_ =>
        {
            manualReset.WaitOne();
            Database.ExecuteQuery("INSERT 6", new Query("INSERT INTO `test` (`id`) VALUES('six')"));
            autoReset2.Set();
        });

        Thread.Sleep(1000);
        manualReset.Set();
        autoReset1.WaitOne();
        autoReset2.WaitOne();
        Database.ExecuteTransaction(
            new KeyValuePair<string, Query>("DROP TABLE", new Query("DROP TABLE `test`;")),
            new KeyValuePair<string, Query>("DROP TABLE", new Query("DROP TABLE `test2`;")),
            new KeyValuePair<string, Query>("DROP TABLE", new Query("DROP TABLE `test3`;"))
        );
    }
}