using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace TOML
{
    /// <summary>
    /// Represents a value in the TOML format.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct TOMLValue : IDisposable
    {
        /// <summary>
        /// The type of value stored in this.
        /// </summary>
        public readonly ValueType valueType;

        /// <summary>
        /// Length of the value data.
        /// </summary>
        public readonly int length;

        private MemoryAddress data;

        /// <summary>
        /// Text value containted.
        /// </summary>
        public readonly ReadOnlySpan<char> Text
        {
            get
            {
                MemoryAddress.ThrowIfDefault(data);
                ThrowIfNotTypeOf(ValueType.Text);

                return data.GetSpan<char>(length);
            }
        }

        /// <summary>
        /// Number value contained.
        /// </summary>
        public readonly ref double Number
        {
            get
            {
                MemoryAddress.ThrowIfDefault(data);
                ThrowIfNotTypeOf(ValueType.Number);

                return ref data.Read<double>();
            }
        }

        /// <summary>
        /// Boolean value contained.
        /// </summary>
        public readonly ref bool Boolean
        {
            get
            {
                MemoryAddress.ThrowIfDefault(data);
                ThrowIfNotTypeOf(ValueType.Boolean);

                return ref data.Read<bool>();
            }
        }

        /// <summary>
        /// Date time value contained.
        /// </summary>
        public readonly ref DateTime DateTime
        {
            get
            {
                MemoryAddress.ThrowIfDefault(data);
                ThrowIfNotTypeOf(ValueType.DateTime);

                return ref data.Read<DateTime>();
            }
        }

        /// <summary>
        /// Time span value contained.
        /// </summary>
        public readonly ref TimeSpan TimeSpan
        {
            get
            {
                MemoryAddress.ThrowIfDefault(data);
                ThrowIfNotTypeOf(ValueType.TimeSpan);

                return ref data.Read<TimeSpan>();
            }
        }

        /// <summary>
        /// Array value contained.
        /// </summary>
        public readonly TOMLArray Array
        {
            get
            {
                MemoryAddress.ThrowIfDefault(data);
                ThrowIfNotTypeOf(ValueType.Array);

                return new(data.pointer);
            }
        }

        /// <summary>
        /// Table value contained.
        /// </summary>
        public readonly TOMLTable Table
        {
            get
            {
                MemoryAddress.ThrowIfDefault(data);
                ThrowIfNotTypeOf(ValueType.Table);

                return new(data.pointer);
            }
        }

        /// <summary>
        /// Checks if this value has been disposed.
        /// </summary>
        public readonly bool IsDisposed => data == default;

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public TOMLValue()
        {
        }
#endif

        /// <summary>
        /// Initializes a new instance containing the given <paramref name="text"/>.
        /// </summary>
        public TOMLValue(ReadOnlySpan<char> text)
        {
            valueType = ValueType.Text;
            length = text.Length;
            data = MemoryAddress.Allocate(text);
        }

        /// <summary>
        /// Initializes a new instance containing the given <paramref name="number"/>.
        /// </summary>
        public TOMLValue(double number)
        {
            valueType = ValueType.Number;
            length = 1;
            data = MemoryAddress.AllocateValue(number);
        }

        /// <summary>
        /// Initializes a new instance containing the given <paramref name="boolean"/>.
        /// </summary>
        public TOMLValue(bool boolean)
        {
            valueType = ValueType.Boolean;
            length = 1;
            data = MemoryAddress.AllocateValue(boolean);
        }

        /// <summary>
        /// Initializes a new instance containing the given <paramref name="dateTime"/>.
        /// </summary>
        public TOMLValue(DateTime dateTime)
        {
            valueType = ValueType.DateTime;
            length = 1;
            data = MemoryAddress.AllocateValue(dateTime);
        }

        /// <summary>
        /// Initializes a new instance containing the given <paramref name="timeSpan"/>.
        /// </summary>
        public TOMLValue(TimeSpan timeSpan)
        {
            valueType = ValueType.TimeSpan;
            length = 1;
            data = MemoryAddress.AllocateValue(timeSpan);
        }

        /// <summary>
        /// Initializes a new instance containing the given <paramref name="array"/>.
        /// </summary>
        public TOMLValue(TOMLArray array)
        {
            valueType = ValueType.Array;
            length = 1;
            data = new(array.array);
        }

        /// <summary>
        /// Initializes a new instance containing the given <paramref name="table"/>.
        /// </summary>
        public TOMLValue(TOMLTable table)
        {
            valueType = ValueType.Table;
            length = 1;
            data = new(table.table);
        }
        
        /// <inheritdoc/>
        public readonly override string ToString()
        {
            using Text destination = new(0);
            ToString(destination);
            return destination.ToString();
        }

        /// <summary>
        /// Appends the string representation of this value to the given <paramref name="destination"/>.
        /// </summary>
        public readonly void ToString(Text destination)
        {
            MemoryAddress.ThrowIfDefault(data);

            if (valueType == ValueType.Text)
            {
                Span<char> text = data.GetSpan<char>(length);
                if (text.Contains(' '))
                {
                    destination.Append(Token.DoubleQuotes);
                    destination.Append(text);
                    destination.Append(Token.DoubleQuotes);
                }
                else
                {
                    destination.Append(text);
                }
            }
            else if (valueType == ValueType.Boolean)
            {
                destination.Append(data.Read<bool>() ? Token.True : Token.False);
            }
            else if (valueType == ValueType.Number)
            {
                destination.Append(data.Read<double>());
            }
            else if (valueType == ValueType.DateTime)
            {
                destination.Append(data.Read<DateTime>());
            }
            else if (valueType == ValueType.TimeSpan)
            {
                destination.Append(data.Read<TimeSpan>());
            }
            else if (valueType == ValueType.Array)
            {
                new TOMLArray(data).ToString(destination);
            }
            else if (valueType == ValueType.Table)
            {
                new TOMLTable(data).ToString(destination);
            }
            else
            {
                throw new NotSupportedException($"Unsupported TOML value type `{valueType}`");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(data);

            if (valueType == ValueType.Array)
            {
                TOMLArray array = new(data.pointer);
                array.Dispose();
            }
            else if (valueType == ValueType.Table)
            {
                TOMLTable table = new(data.pointer);
                table.Dispose();
            }
            else
            {
                data.Dispose();
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfNotTypeOf(ValueType valueType)
        {
            if (this.valueType != valueType)
            {
                throw new InvalidOperationException($"Array element is not of type `{valueType}`");
            }
        }
    }
}