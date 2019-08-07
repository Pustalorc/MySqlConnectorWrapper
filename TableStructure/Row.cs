using System.Collections.Generic;

namespace Pustalorc.Libraries.MySqlConnector.TableStructure
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
    }
}