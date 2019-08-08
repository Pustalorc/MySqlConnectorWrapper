namespace Pustalorc.Libraries.MySqlConnectorWrapper.TableStructure
{
    /// <summary>
    ///     Defines a column of a table.
    /// </summary>
    public sealed class Column
    {
        /// <summary>
        ///     The name of the column in question.
        /// </summary>
        public string Name;

        /// <summary>
        ///     The value (if any) of this column for the first or the defined row.
        /// </summary>
        public object Value;

        public Column(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}