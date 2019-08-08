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
        public List<Column> Columns;

        public Row(IEnumerable<Column> columns)
        {
            Columns = columns.ToList();
        }
    }
}