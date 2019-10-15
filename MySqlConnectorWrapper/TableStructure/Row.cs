using System.Collections.Generic;
using System.Linq;

namespace Pustalorc.Libraries.MySqlConnectorWrapper.TableStructure
{
    /// <summary>
    ///     Defines a Row in a table.
    /// </summary>
    public sealed class Row
    {
        /// <summary>
        ///     The columns that this row has.
        /// </summary>
        private readonly List<Column> _columns;

        /// <summary>
        ///     Retrieves a value based on the column name.
        /// </summary>
        /// <param name="key">The name of the column that should have the value.</param>
        public object this[string key] => _columns.FirstOrDefault(k => k.Name.Equals(key))?.Value;

        /// <summary>
        ///     Retrieves the column with the specified index.
        /// </summary>
        /// <param name="index">The index of the column to retrieve the value of.</param>
        public Column this[int index] => _columns.Count < index ? null : _columns[index];

        public Row(IEnumerable<Column> columns)
        {
            _columns = columns.ToList();
        }
    }
}