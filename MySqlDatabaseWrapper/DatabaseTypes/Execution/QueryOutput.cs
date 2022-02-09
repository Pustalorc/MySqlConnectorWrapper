using JetBrains.Annotations;

namespace Pustalorc.MySqlDatabaseWrapper.DatabaseTypes.Execution;

[UsedImplicitly]
public class QueryOutput
{
    public Query Query { get; }

    [UsedImplicitly] public object? Result { get; set; }

    public QueryOutput(Query query, object? result)
    {
        Query = query;
        Result = result;
    }
}