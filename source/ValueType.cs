namespace TOML
{
    /// <summary>
    /// All types of TOML values.
    /// </summary>
    public enum ValueType : byte
    {
        /// <summary>
        /// Uninitialized or unknown value type.
        /// </summary>
        Unknown,

        /// <summary>
        /// Text.
        /// </summary>
        Text,

        /// <summary>
        /// Number.
        /// </summary>
        Number,

        /// <summary>
        /// Boolean.
        /// </summary>
        Boolean,

        /// <summary>
        /// Date time.
        /// </summary>
        DateTime,

        /// <summary>
        /// Time span.
        /// </summary>
        TimeSpan,

        /// <summary>
        /// Array.
        /// </summary>
        Array,

        /// <summary>
        /// Dictionary table.
        /// </summary>
        Table
    }
}