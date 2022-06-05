using JetBrains.Annotations;

namespace Pustalorc.MySqlDatabaseWrapper.DatabaseTypes.ResultTable;

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

    /// <summary>
    /// Attempts to get a value of type T from the value of this column.
    /// </summary>
    /// <param name="defaultIfNot">A default value in the scenario where value is not of type T.</param>
    /// <typeparam name="T">The type to get or check from the value.</typeparam>
    /// <returns>The instance of type T from the value, or defaultIfNot if the result is not of type T.</returns>
    public T? GetTFromValue<T>(T? defaultIfNot = default)
    {
        return Value is T t ? t : defaultIfNot;
    }

    /// <summary>
    /// Checks if Value is null.
    /// </summary>
    /// <returns>
    /// True if it is null, false otherwise.
    /// </returns>
    [UsedImplicitly]
    public bool IsValueNull()
    {
        return Value is null;
    }
}