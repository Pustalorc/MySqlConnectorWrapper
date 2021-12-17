using JetBrains.Annotations;

namespace Pustalorc.MySqlConnector.Wrapper.TableStructure;

/// <summary>
/// Defining a column for a row in a table.
/// </summary>
[UsedImplicitly]
public class Column
{
    /// <summary>
    /// The name of the column.
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// The value in the column.
    /// </summary>
    [UsedImplicitly] public readonly object? Value;

    /// <summary>
    /// Constructs a new instance.
    /// </summary>
    /// <param name="name">The name of the column.</param>
    /// <param name="value">The value in the column.</param>
    public Column(string name, object? value)
    {
        Name = name;
        Value = value;
    }
}