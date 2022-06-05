using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;

namespace Pustalorc.MySqlDatabaseWrapper.DatabaseTypes.ResultTable;

/// <summary>
/// Defines a row in a table.
/// </summary>
[UsedImplicitly]
public class Row
{
    /// <summary>
    /// The columns and their respective values for this row.
    /// </summary>
    public readonly IReadOnlyList<Column> Columns;

    /// <summary>
    /// Columns indexed by name.
    /// </summary>
    protected readonly Dictionary<string, int> IndexedColumns;

    /// <summary>
    /// Constructs a new row.
    /// </summary>
    /// <param name="columns">The columns and their respective values for this row.</param>
    public Row(IEnumerable<Column> columns)
    {
        Columns = new ReadOnlyCollection<Column>(columns.ToList());
        IndexedColumns = new Dictionary<string, int>();

        for (var i = 0; i < Columns.Count; i++)
            IndexedColumns.Add(Columns[i].Name, i);
    }

    /// <summary>
    /// Gets a column from the column name. If no column is found, returns null.
    /// </summary>
    /// <param name="key">The name of the column to get.</param>
    [UsedImplicitly]
    public Column? this[string key] => !IndexedColumns.TryGetValue(key, out var value) ? null : this[value];

    /// <summary>
    /// Gets a column by index.
    /// </summary>
    /// <param name="index">The index of the column.</param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public Column this[int index] => Columns[index];

    /// <summary>
    /// Attempts to get a value of type T from the result in this class.
    /// </summary>
    /// <param name="columnName">The name of the column to get the value from.</param>
    /// <param name="defaultIfNot">A default value in the scenario where the value from the column is not of type T, or the column is not found.</param>
    /// <typeparam name="T">The type to get for the column's value.</typeparam>
    /// <returns>The instance of type T from the column's value, or defaultIfNull if the result is not of type T.</returns>
    [UsedImplicitly]
    public T? GetColumnValue<T>(string columnName, T? defaultIfNot = default)
    {
        return IndexedColumns.TryGetValue(columnName, out var value)
            ? this[value].GetTFromValue(defaultIfNot)
            : defaultIfNot;
    }

    /// <summary>
    /// Attempts to get a value of type T from the result in this class.
    /// </summary>
    /// <param name="index">The index of the column to get the value from.</param>
    /// <param name="defaultIfNot">A default value in the scenario where the value from the column is not of type T, or the column is not found.</param>
    /// <typeparam name="T">The type to get for the column's value.</typeparam>
    /// <returns>The instance of type T from the column's value, or defaultIfNull if the result is not of type T.</returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
    [UsedImplicitly]
    public T? GetColumnValue<T>(int index, T? defaultIfNot = default)
    {
        return this[index].GetTFromValue(defaultIfNot);
    }
}