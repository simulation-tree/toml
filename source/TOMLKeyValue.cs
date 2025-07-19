using System;
using System.Diagnostics;
using System.Globalization;
using Unmanaged;

namespace TOML
{
    /// <summary>
    /// Represents a key-value pair in TOML format.
    /// </summary>
    public unsafe struct TOMLKeyValue : IDisposable, ISerializable
    {
        private Implementation* keyValue;

        /// <summary>
        /// The key of this pair.
        /// </summary>
        public readonly ReadOnlySpan<char> Key
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);

                return keyValue->data.GetSpan<char>(keyValue->keyLength);
            }
        }

        /// <summary>
        /// The type of value being stored.
        /// </summary>
        public readonly ValueType ValueType
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);

                return keyValue->valueType;
            }
        }

        /// <summary>
        /// The contained value as text.
        /// </summary>
        public readonly ReadOnlySpan<char> Text
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);
                ThrowIfNotTypeOf(ValueType.Text);

                return keyValue->data.AsSpan<char>(keyValue->keyLength * sizeof(char), keyValue->valueLength);
            }
        }

        /// <summary>
        /// The contained value as a number.
        /// </summary>
        public readonly ref double Number
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);
                ThrowIfNotTypeOf(ValueType.Number);

                return ref keyValue->data.Read<double>(keyValue->keyLength * sizeof(char));
            }
        }

        /// <summary>
        /// The contained value as a boolean.
        /// </summary>
        public readonly ref bool Boolean
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);
                ThrowIfNotTypeOf(ValueType.Boolean);

                return ref keyValue->data.Read<bool>(keyValue->keyLength * sizeof(char));
            }
        }

        /// <summary>
        /// The contained value as a date time.
        /// </summary>
        public readonly ref DateTime DateTime
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);
                ThrowIfNotTypeOf(ValueType.DateTime);

                return ref keyValue->data.Read<DateTime>(keyValue->keyLength * sizeof(char));
            }
        }

        /// <summary>
        /// The contained value as a time span.
        /// </summary>
        public readonly ref TimeSpan TimeSpan
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);
                ThrowIfNotTypeOf(ValueType.TimeSpan);

                return ref keyValue->data.Read<TimeSpan>(keyValue->keyLength * sizeof(char));
            }
        }

        /// <summary>
        /// The contained value as an array.
        /// </summary>
        public readonly TOMLArray Array
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);
                ThrowIfNotTypeOf(ValueType.Array);

                return keyValue->data.Read<TOMLArray>(keyValue->keyLength * sizeof(char));
            }
        }

        /// <summary>
        /// The contained value as a table.
        /// </summary>
        public readonly TOMLTable Table
        {
            get
            {
                MemoryAddress.ThrowIfDefault(keyValue);
                ThrowIfNotTypeOf(ValueType.Table);

                return keyValue->data.Read<TOMLTable>(keyValue->keyLength * sizeof(char));
            }
        }

        /// <summary>
        /// Checks if this key-value pair has been disposed.
        /// </summary>
        public readonly bool IsDisposed => keyValue == default;

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public TOMLKeyValue()
        {
        }
#endif

        /// <summary>
        /// Creates a new key-value pair with the given <paramref name="key"/> and <paramref name="text"/>.
        /// </summary>
        public TOMLKeyValue(ReadOnlySpan<char> key, ReadOnlySpan<char> text)
        {
            keyValue = MemoryAddress.AllocatePointer<Implementation>();
            keyValue->valueType = ValueType.Text;
            keyValue->keyLength = key.Length;
            keyValue->valueLength = text.Length;

            int keyByteLength = sizeof(char) * key.Length;
            int textByteLength = sizeof(char) * text.Length;
            keyValue->data = MemoryAddress.Allocate(keyByteLength + textByteLength);
            keyValue->data.CopyFrom(key, 0);
            keyValue->data.CopyFrom(text, keyByteLength);
        }

        /// <summary>
        /// Creates a new key-value pair with the given <paramref name="key"/> and <paramref name="number"/>.
        /// </summary>
        public TOMLKeyValue(ReadOnlySpan<char> key, double number)
        {
            keyValue = MemoryAddress.AllocatePointer<Implementation>();
            keyValue->valueType = ValueType.Number;
            keyValue->keyLength = key.Length;
            keyValue->valueLength = 1;

            int keyByteLength = sizeof(char) * key.Length;
            keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(double));
            keyValue->data.CopyFrom(key, 0);
            keyValue->data.Write(keyByteLength, number);
        }

        /// <summary>
        /// Creates a new key-value pair with the given <paramref name="key"/> and <paramref name="boolean"/>.
        /// </summary>
        public TOMLKeyValue(ReadOnlySpan<char> key, bool boolean)
        {
            keyValue = MemoryAddress.AllocatePointer<Implementation>();
            keyValue->valueType = ValueType.Boolean;
            keyValue->keyLength = key.Length;
            keyValue->valueLength = 1;

            int keyByteLength = sizeof(char) * key.Length;
            keyValue->data = MemoryAddress.Allocate(keyByteLength + 1);
            keyValue->data.CopyFrom(key, 0);
            keyValue->data.Write(keyByteLength, boolean);
        }

        /// <summary>
        /// Creates a new key-value pair with the given <paramref name="key"/> and <paramref name="dateTime"/>.
        /// </summary>
        public TOMLKeyValue(ReadOnlySpan<char> key, DateTime dateTime)
        {
            keyValue = MemoryAddress.AllocatePointer<Implementation>();
            keyValue->valueType = ValueType.DateTime;
            keyValue->keyLength = key.Length;
            keyValue->valueLength = 1;

            int keyByteLength = sizeof(char) * key.Length;
            keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(DateTime));
            keyValue->data.CopyFrom(key, 0);
            keyValue->data.Write(keyByteLength, dateTime);
        }

        /// <summary>
        /// Creates a new key-value pair with the given <paramref name="key"/> and <paramref name="timeSpan"/>.
        /// </summary>
        public TOMLKeyValue(ReadOnlySpan<char> key, TimeSpan timeSpan)
        {
            keyValue = MemoryAddress.AllocatePointer<Implementation>();
            keyValue->valueType = ValueType.TimeSpan;
            keyValue->keyLength = key.Length;
            keyValue->valueLength = 1;

            int keyByteLength = sizeof(char) * key.Length;
            keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(TimeSpan));
            keyValue->data.CopyFrom(key, 0);
            keyValue->data.Write(keyByteLength, timeSpan);
        }

        /// <summary>
        /// Creates a new key-value pair with the given <paramref name="key"/> and <paramref name="tomlArray"/>.
        /// </summary>
        public TOMLKeyValue(ReadOnlySpan<char> key, TOMLArray tomlArray)
        {
            keyValue = MemoryAddress.AllocatePointer<Implementation>();
            keyValue->valueType = ValueType.Array;
            keyValue->keyLength = key.Length;
            keyValue->valueLength = 1;

            int keyByteLength = sizeof(char) * key.Length;
            keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(TOMLArray));
            keyValue->data.CopyFrom(key, 0);
            keyValue->data.Write(keyByteLength, tomlArray);
        }

        /// <summary>
        /// Creates a new key-value pair with the given <paramref name="key"/> and <paramref name="tomlTable"/>.
        /// </summary>
        public TOMLKeyValue(ReadOnlySpan<char> key, TOMLTable tomlTable)
        {
            keyValue = MemoryAddress.AllocatePointer<Implementation>();
            keyValue->valueType = ValueType.Table;
            keyValue->keyLength = key.Length;
            keyValue->valueLength = 1;

            int keyByteLength = sizeof(char) * key.Length;
            keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(TOMLTable));
            keyValue->data.CopyFrom(key, 0);
            keyValue->data.Write(keyByteLength, tomlTable);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            using Text destination = new(0);
            ToString(destination);
            return destination.ToString();
        }

        /// <summary>
        /// Appends the string representation of this key-value pair to the given <paramref name="destination"/>.
        /// </summary>
        public readonly void ToString(Text destination)
        {
            MemoryAddress.ThrowIfDefault(keyValue);

            destination.Append(keyValue->data.GetSpan<char>(keyValue->keyLength));
            destination.Append(Token.EqualSign);
            if (keyValue->valueType == ValueType.Text)
            {
                Span<char> text = keyValue->data.AsSpan<char>(keyValue->keyLength * sizeof(char), keyValue->valueLength);
                if (text.Contains(' '))
                {
                    destination.Append(Token.DoubleQuotes);
                    destination.Append(text);
                    destination.Append('"');
                }
                else
                {
                    destination.Append(text);
                }
            }
            else if (keyValue->valueType == ValueType.Number)
            {
                double number = keyValue->data.Read<double>(keyValue->keyLength * sizeof(char));
                Span<char> buffer = stackalloc char[32];
                int length = number.ToString(buffer);
                destination.Append(buffer.Slice(0, length));
            }
            else if (keyValue->valueType == ValueType.Boolean)
            {
                bool boolean = keyValue->data.Read<bool>(keyValue->keyLength * sizeof(char));
                destination.Append(boolean ? Token.True : Token.False);
            }
            else if (keyValue->valueType == ValueType.DateTime)
            {
                DateTime dateTime = keyValue->data.Read<DateTime>(keyValue->keyLength * sizeof(char));
                Span<char> buffer = stackalloc char[32];
                int length = dateTime.ToString(buffer);
                destination.Append(buffer.Slice(0, length));
            }
            else if (keyValue->valueType == ValueType.TimeSpan)
            {
                TimeSpan timeSpan = keyValue->data.Read<TimeSpan>(keyValue->keyLength * sizeof(char));
                Span<char> buffer = stackalloc char[32];
                int length = timeSpan.ToString(buffer);
                destination.Append(buffer.Slice(0, length));
            }
            else if (keyValue->valueType == ValueType.Array)
            {
                TOMLArray array = keyValue->data.Read<TOMLArray>(keyValue->keyLength * sizeof(char));
                array.ToString(destination);
            }
            else if (keyValue->valueType == ValueType.Table)
            {
                TOMLTable table = keyValue->data.Read<TOMLTable>(keyValue->keyLength * sizeof(char));
                table.ToString(destination);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported value type `{keyValue->valueType}` for ToString()");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(keyValue);

            if (keyValue->valueType == ValueType.Array)
            {
                TOMLArray array = keyValue->data.Read<TOMLArray>(keyValue->keyLength * sizeof(char));
                array.Dispose();
            }
            else if (keyValue->valueType == ValueType.Table)
            {
                TOMLTable table = keyValue->data.Read<TOMLTable>(keyValue->keyLength * sizeof(char));
                table.Dispose();
            }

            keyValue->data.Dispose();
            MemoryAddress.Free(ref keyValue);
        }

        readonly void ISerializable.Write(ByteWriter byteWriter)
        {
        }

        void ISerializable.Read(ByteReader byteReader)
        {
            keyValue = MemoryAddress.AllocatePointer<Implementation>();
            TOMLReader tomlReader = new(byteReader);

            //read text
            Token keyToken = tomlReader.ReadToken();
            Span<char> keyBuffer = stackalloc char[keyToken.length * 4];
            keyValue->keyLength = tomlReader.GetText(keyToken, keyBuffer);
            ReadOnlySpan<char> keyText = keyBuffer.Slice(0, keyValue->keyLength);

            //read equals
            Token equalsToken = tomlReader.ReadToken();
            ThrowIfNotEqualsAfterKey(equalsToken.type);

            //read text or array or table
            int keyByteLength = sizeof(char) * keyValue->keyLength;
            tomlReader.TryPeekToken(out Token valueToken);
            if (valueToken.type == Token.Type.Text)
            {
                tomlReader.ReadToken();
                Span<char> valueBuffer = stackalloc char[valueToken.length * 4];
                int valueLength = tomlReader.GetText(valueToken, valueBuffer);
                ReadOnlySpan<char> valueText = valueBuffer.Slice(0, valueLength);

                //build data
                if (TimeSpan.TryParse(valueText, out TimeSpan timeSpan))
                {
                    keyValue->valueType = ValueType.TimeSpan;
                    keyValue->valueLength = 1;
                    keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(TimeSpan));
                    keyValue->data.CopyFrom(keyText, 0);
                    keyValue->data.Write(keyByteLength, timeSpan);
                }
                else if (DateTimeOffset.TryParse(valueText, default, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset dateTimeOffset))
                {
                    keyValue->valueType = ValueType.DateTime;
                    keyValue->valueLength = 1;
                    keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(DateTime));
                    keyValue->data.CopyFrom(keyText, 0);
                    keyValue->data.Write(keyByteLength, dateTimeOffset.DateTime);
                }
                else if (DateTime.TryParse(valueText, out DateTime dateTime))
                {
                    keyValue->valueType = ValueType.DateTime;
                    keyValue->valueLength = 1;
                    keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(DateTime));
                    keyValue->data.CopyFrom(keyText, 0);
                    keyValue->data.Write(keyByteLength, dateTime);
                }
                else if (double.TryParse(valueText, out double number))
                {
                    keyValue->valueType = ValueType.Number;
                    keyValue->valueLength = 1;
                    keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(double));
                    keyValue->data.CopyFrom(keyText, 0);
                    keyValue->data.Write(keyByteLength, number);
                }
                else if (bool.TryParse(valueText, out bool boolean))
                {
                    keyValue->valueType = ValueType.Boolean;
                    keyValue->valueLength = 1;
                    keyValue->data = MemoryAddress.Allocate(keyByteLength + 1);
                    keyValue->data.CopyFrom(keyText, 0);
                    keyValue->data.Write(keyByteLength, boolean);
                }
                else
                {
                    keyValue->valueType = ValueType.Text;
                    keyValue->valueLength = valueLength;
                    keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(char) * valueLength);
                    keyValue->data.CopyFrom(keyText, 0);
                    keyValue->data.CopyFrom(valueText, keyByteLength);
                }
            }
            else if (valueToken.type == Token.Type.StartSquareBracket)
            {
                TOMLArray newArray = byteReader.ReadObject<TOMLArray>();
                keyValue->valueType = ValueType.Array;
                keyValue->valueLength = 1;
                keyValue->data = MemoryAddress.Allocate(keyByteLength + sizeof(TOMLArray));
                keyValue->data.CopyFrom(keyText, 0);
                keyValue->data.Write(keyByteLength, newArray);
            }
            else if (valueToken.type == Token.Type.StartCurlyBrace)
            {
                //todo: read table
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfNotTypeOf(ValueType type)
        {
            if (keyValue->valueType != type)
            {
                throw new InvalidOperationException($"Expected value type `{type}`, but got `{keyValue->valueType}`");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfNotEqualsAfterKey(Token.Type type)
        {
            if (type != Token.Type.Equals)
            {
                throw new InvalidOperationException($"Expected `{Token.EqualSign}` after key, but got `{type}`");
            }
        }

        private struct Implementation
        {
            public ValueType valueType;
            public int keyLength;
            public int valueLength;
            public MemoryAddress data;
        }
    }
}