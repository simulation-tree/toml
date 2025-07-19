using Collections.Generic;
using System;
using System.Diagnostics;
using System.Globalization;
using Unmanaged;

namespace TOML
{
    /// <summary>
    /// Represents an array in TOML format.
    /// </summary>
    public unsafe struct TOMLArray : IDisposable, ISerializable
    {
        internal Implementation* array;

        /// <summary>
        /// All elements in the array.
        /// </summary>
        public readonly ReadOnlySpan<TOMLValue> Elements
        {
            get
            {
                MemoryAddress.ThrowIfDefault(array);

                return array->elements.AsSpan();
            }
        }

        /// <summary>
        /// Length of the array.
        /// </summary>
        public readonly int Length
        {
            get
            {
                MemoryAddress.ThrowIfDefault(array);

                return array->elements.Count;
            }
        }

        /// <summary>
        /// Indexer to access elements in the array.
        /// </summary>
        public readonly TOMLValue this[int index]
        {
            get
            {
                MemoryAddress.ThrowIfDefault(array);

                return array->elements[index];
            }
        }

#if NET
        /// <summary>
        /// Creates an empty array.
        /// </summary>
        public TOMLArray()
        {
            array = MemoryAddress.AllocatePointer<Implementation>();
            array->elements = new(4);
        }
#endif

        /// <summary>
        /// Creates a new TOML array with the given <paramref name="elements"/>.
        /// </summary>
        public TOMLArray(ReadOnlySpan<TOMLValue> elements)
        {
            array = MemoryAddress.AllocatePointer<Implementation>();
            array->elements = new(elements);
        }

        /// <summary>
        /// Creates a new TOML array with the given <paramref name="numbers"/>.
        /// </summary>
        public TOMLArray(ReadOnlySpan<double> numbers)
        {
            array = MemoryAddress.AllocatePointer<Implementation>();
            array->elements = new(numbers.Length);
            for (int i = 0; i < numbers.Length; i++)
            {
                TOMLValue value = new(numbers[i]);
                array->elements.Add(value);
            }
        }

        /// <summary>
        /// Initializes an existing array with the given <paramref name="pointer"/>.
        /// </summary>
        public TOMLArray(void* pointer)
        {
            array = (Implementation*)pointer;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            using Text destination = new(0);
            ToString(destination);
            return destination.ToString();
        }

        /// <summary>
        /// Appends the string representation of this array to the given <paramref name="destination"/>.
        /// </summary>
        public readonly void ToString(Text destination)
        {
            MemoryAddress.ThrowIfDefault(array);

            destination.Append(Token.StartSquareBracket);
            Span<TOMLValue> elements = array->elements.AsSpan();
            for (int i = 0; i < elements.Length; i++)
            {
                elements[i].ToString(destination);
                if (i != elements.Length - 1)
                {
                    destination.Append(Token.Aggregator);
                }
            }

            destination.Append(Token.EndSquareBracket);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(array);

            Span<TOMLValue> elements = array->elements.AsSpan();
            foreach (TOMLValue element in elements)
            {
                element.Dispose();
            }

            array->elements.Dispose();
            MemoryAddress.Free(ref array);
        }

        /// <summary>
        /// Appends the given <paramref name="text"/> as a new element.
        /// </summary>
        public readonly void Add(ReadOnlySpan<char> text)
        {
            MemoryAddress.ThrowIfDefault(array);

            TOMLValue value = new(text);
            array->elements.Add(value);
        }

        /// <summary>
        /// Appends the given <paramref name="number"/> as a new element.
        /// </summary>
        public readonly void Add(double number)
        {
            MemoryAddress.ThrowIfDefault(array);

            TOMLValue value = new(number);
            array->elements.Add(value);
        }

        /// <summary>
        /// Appends the given <paramref name="boolean"/> as a new element.
        /// </summary>
        public readonly void Add(bool boolean)
        {
            MemoryAddress.ThrowIfDefault(array);

            TOMLValue value = new(boolean);
            array->elements.Add(value);
        }

        /// <summary>
        /// Appends the given <paramref name="dateTime"/> as a new element.
        /// </summary>
        public readonly void Add(DateTime dateTime)
        {
            MemoryAddress.ThrowIfDefault(array);

            TOMLValue value = new(dateTime);
            array->elements.Add(value);
        }

        /// <summary>
        /// Appends the given <paramref name="timeSpan"/> as a new element.
        /// </summary>
        public readonly void Add(TimeSpan timeSpan)
        {
            MemoryAddress.ThrowIfDefault(array);

            TOMLValue value = new(timeSpan);
            array->elements.Add(value);
        }

        /// <summary>
        /// Appends the given <paramref name="tomlArray"/> as a new element.
        /// </summary>
        public readonly void Add(TOMLArray tomlArray)
        {
            MemoryAddress.ThrowIfDefault(array);

            TOMLValue value = new(tomlArray);
            array->elements.Add(value);
        }

        /// <summary>
        /// Appends the given <paramref name="tomlTable"/> as a new element.
        /// </summary>
        public readonly void Add(TOMLTable tomlTable)
        {
            MemoryAddress.ThrowIfDefault(array);

            TOMLValue value = new(tomlTable);
            array->elements.Add(value);
        }

        void ISerializable.Read(ByteReader byteReader)
        {
            array = MemoryAddress.AllocatePointer<Implementation>();
            array->elements = new(4);

            using Text buffer = new(256);
            TOMLReader tomlReader = new(byteReader);
            Token startToken = tomlReader.ReadToken(); //[
            ThrowIfNotArrayStart(startToken.type);
            while (tomlReader.TryPeekToken(out Token token) && token.type != Token.Type.EndSquareBracket)
            {
                if (token.type == Token.Type.Text)
                {
                    if (buffer.Length < token.length * 4)
                    {
                        buffer.SetLength(token.length * 4);
                    }

                    tomlReader.ReadToken();
                    int length = tomlReader.GetText(token, buffer.AsSpan());
                    ReadOnlySpan<char> valueText = buffer.Slice(0, length);
                    if (double.TryParse(valueText, out double number))
                    {
                        TOMLValue value = new(number);
                        array->elements.Add(value);
                    }
                    else if (bool.TryParse(valueText, out bool boolean))
                    {
                        TOMLValue value = new(boolean);
                        array->elements.Add(value);
                    }
                    else if (TimeSpan.TryParse(valueText, out TimeSpan timeSpan))
                    {
                        TOMLValue value = new(timeSpan);
                        array->elements.Add(value);
                    }
                    else if (DateTimeOffset.TryParse(valueText, default, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset dateTimeOffset))
                    {
                        TOMLValue value = new(dateTimeOffset.DateTime);
                        array->elements.Add(value);
                    }
                    else if (DateTime.TryParse(valueText, out DateTime dateTime))
                    {
                        TOMLValue value = new(dateTime);
                        array->elements.Add(value);
                    }
                    else
                    {
                        TOMLValue value = new(valueText);
                        array->elements.Add(value);
                    }
                }
                else if (token.type == Token.Type.StartSquareBracket)
                {
                    //nested array
                    TOMLArray nestedArray = byteReader.ReadObject<TOMLArray>();
                    TOMLValue value = new(nestedArray);
                    array->elements.Add(value);
                }
                else
                {
                    ThrowIfNotComma(token.type);
                    tomlReader.ReadToken();
                }
            }

            Token endToken = tomlReader.ReadToken(); //]
            ThrowIfNotArrayEnd(endToken.type);
        }

        readonly void ISerializable.Write(ByteWriter byteWriter)
        {
        }

        [Conditional("DEBUG")]
        private static void ThrowIfNotArrayStart(Token.Type type)
        {
            if (type != Token.Type.StartSquareBracket)
            {
                throw new InvalidOperationException($"Expected `{Token.StartSquareBracket}` to start the array, but got {type}");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfNotComma(Token.Type type)
        {
            if (type != Token.Type.Comma)
            {
                throw new InvalidOperationException($"Expected `{Token.Aggregator}`, but got {type}");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfNotArrayEnd(Token.Type type)
        {
            if (type != Token.Type.EndSquareBracket)
            {
                throw new InvalidOperationException($"Expected `{Token.EndSquareBracket}` to end the array, but got {type}");
            }
        }

        internal struct Implementation
        {
            public List<TOMLValue> elements;
        }
    }
}