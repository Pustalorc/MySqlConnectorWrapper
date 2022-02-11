# MySql Database Wrapper [![NuGet](https://img.shields.io/nuget/v/Pustalorc.MySqlConnectorWrapper.svg)](https://www.nuget.org/packages/Pustalorc.MySqlConnectorWrapper/)

Library that wraps [Connector/NET (aka: MySql.Data)](https://www.nuget.org/packages/MySql.Data/) and [MySqlConnector](https://www.nuget.org/packages/MySqlConnector/) in order to reduce duplicate code when interacting with the connectors.

# Installation & Usage
***

Before you begin, please make sure that your current solution has installed the nuget package of this library.
You can find this package [here.](https://www.nuget.org/packages/Pustalorc.MySqlConnectorWrapper)

## Configuration Setup

Firstly, you need a configuration for the connector to use.
Without a configuration, the connector will not know what server to connect to, which database to use, which user to use, etc.

You will want to create a class and have it inherit the Interface `IConnectorConfiguration`.
You are free to include extra information in this class to use, such as table names.
For example:

```C#
public class DatabaseConfig : IConnectorConfiguration
{
    public string MySqlServerAddress => "127.0.0.1";
    public ushort MySqlServerPort => 3306;
    public string DatabaseUsername => "myUsername";
    public string DatabasePassword => "myPassword";
    public string DatabaseName => "myDatabase";
    public string ConnectionString => "";
}
```
Note: These are `{ get; }` only properties, but you can make them `{ get; set; }` if you wish to be able to modify them mid run-time, or if you wish to serialize them into a configuration file.
`ConnectionString` is also available and used on the `DbConnectionStringBuilder` classes to build a full connection string with the provided details.

## Wrapper setup

Once you have a configuration and you think you are ready to move on, you can then use the connector wrapping.

There are 2 default implementations of the wrapper, one for [MySql.Data](https://www.nuget.org/packages/MySql.Data) and one for [MySqlConnector](https://www.nuget.org/packages/MySqlConnector).

If either one of these doesn't fit your use case, you can always create your own by inheriting from `DatabaseConnectorWrapper` and overriding things as necessary.

Since both wrappers function the same and are abstract to the point where they are virtually identical in method calls, I'll be using the MySqlConnector wrapper:

```C#
public class MyDatabaseLayerClass
{
    private MySqlConnectorWrapper<DatabaseConfig> Database { get; }

    public MyDatabaseLayerClass(DatabaseConfig config)
    {
        Database = new MySqlConnectorWrapper<DatabaseConfig>(config);

        if (!Database.TestConnection(out var exception))
        {
            Log.Exception(exception);
            throw exception;
        }

        CheckCreateSchema();
    }

    private void CheckCreateSchema()
    {
        var output = Database.ExecuteQuery($"SHOW TABLES LIKE '{Configuration.TableName}';", EQueryType.Scalar);

        if (!output.IsResultNull()) return;

        Database.ExecuteQuery(new Query($"CREATE TABLE `{Configuration.TableName}` (`Id` INT NOT NULL, PRIMARY KEY (`Id`));"));
    }
}
```

As shown in the example above, you can execute queries in 2 ways, either by just providing the details of the query (the query string, the type of query, etc.) or constructing a `Query` class yourself with the same details.

The `Query` class holds basic information about a query.
This includes the following:
- A query string - This is the command that the underlying SQL server will execute.
- The type of the query - This tells the wrapper if it should run the query as a non query, as a scalar query or as a reader.
- A list of Parameters for the query - These are used in binding data to the query. The type for the parameters is an abstract class, so make sure to use the parameter types exposed by your connector.
- A callback - This is ran after the query has fully executed, and passes the output, as well as the current `DbConnection` and `DbTransaction` for further query execution. The callback is useful when using asynchronous calls in a run and forget style.

And that's about it, you can run queries from here, or full transactions with multiple queries (for example creating a table and filling it with data, or doing a heavy operation on a DB where rolling back is needed in case of an error), or even add a caching layer to your database. The limit will be yours in here.