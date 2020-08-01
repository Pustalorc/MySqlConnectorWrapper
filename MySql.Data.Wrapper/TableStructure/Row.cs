using System;
using System.Collections.Generic;
using System.Linq;

namespace Pustalorc.MySql.Data.Wrapper.TableStructure
{
    /// <summary>
    /// Defines a Row in a table.
    /// </summary>
    public sealed class Row
    {
        /// <summary>
        /// The columns that this row has.
        /// </summary>
        private readonly List<Column> m_Columns;

        public Row(IEnumerable<Column> columns)
        {
            m_Columns = columns.ToList();
        }

        /// <summary>
        /// Retrieves a value based on the column name.
        /// </summary>
        /// <param name="key">The name of the column that should have the value.</param>
        public object this[string key] =>
            m_Columns.FirstOrDefault(k => k.Name.Equals(key, StringComparison.Ordinal))?.Value;

        /// <summary>
        /// Retrieves the column with the specified index.
        /// </summary>
        /// <param name="index">The index of the column to retrieve the value of.</param>
        public Column this[int index] => m_Columns.Count < index ? null : m_Columns[index];
    }
}