using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;

namespace Pustalorc.MySqlConnector.Wrapper.TableStructure;

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
    public Column this[int index] => Columns[index];
}