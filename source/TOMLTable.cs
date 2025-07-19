using Collections.Generic;
using System;
using System.Diagnostics;
using Unmanaged;

namespace TOML
{
    /// <summary>
    /// Representation of a table dictionary in TOML format.
    /// </summary>
    public unsafe struct TOMLTable : IDisposable, ISerializable
    {
        internal Implementation* table;

        /// <summary>
        /// The name of the TOML table.
        /// </summary>
        public readonly ReadOnlySpan<char> Name
        {
            get
            {
                MemoryAddress.ThrowIfDefault(table);

                return table->name.GetSpan<char>(table->nameLength);
            }
        }

        /// <summary>
        /// The key-value pairs contained in this TOML table.
        /// </summary>
        public readonly ReadOnlySpan<TOMLKeyValue> KeyValues
        {
            get
            {
                MemoryAddress.ThrowIfDefault(table);

                return table->keyValues.AsSpan();
            }
        }

        /// <summary>
        /// Checks if this TOML table has been disposed.
        /// </summary>
        public readonly bool IsDisposed => table == default;

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public TOMLTable()
        {
        }
#endif

        /// <summary>
        /// Creates a new TOML table with the given <paramref name="name"/>.
        /// </summary>
        public TOMLTable(ReadOnlySpan<char> name)
        {
            table = MemoryAddress.AllocatePointer<Implementation>();
            table->name = MemoryAddress.Allocate(name.Length * sizeof(char));
            table->keyValues = new(4);
            table->nameLength = name.Length;
        }

        /// <summary>
        /// Initializes an existing TOML table from the given <paramref name="pointer"/>..
        /// </summary>
        public TOMLTable(void* pointer)
        {
            table = (Implementation*)pointer;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            using Text destination = new(0);
            ToString(destination);
            return destination.ToString();
        }

        /// <summary>
        /// Appends the string representation of this TOML table to the given <paramref name="destination"/>.
        /// </summary>
        public readonly void ToString(Text destination)
        {
            //todo: implement ToString for TOMLTable
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(table);

            table->name.Dispose();

            Span<TOMLKeyValue> keyValues = table->keyValues.AsSpan();
            foreach (TOMLKeyValue keyValue in keyValues)
            {
                keyValue.Dispose();
            }

            table->keyValues.Dispose();
            MemoryAddress.Free(ref table);
        }

        void ISerializable.Read(ByteReader byteReader)
        {
            table = MemoryAddress.AllocatePointer<Implementation>();
            table->keyValues = new(4);

            TOMLReader tomlReader = new(byteReader);
            tomlReader.ReadToken(); //[
            Token nameToken = tomlReader.ReadToken();
            tomlReader.ReadToken(); //]

            Span<char> nameBuffer = stackalloc char[nameToken.length * 4];
            table->nameLength = tomlReader.GetText(nameToken, nameBuffer);
            table->name = MemoryAddress.Allocate(nameBuffer.Slice(0, table->nameLength));

            while (tomlReader.TryPeekToken(out Token nextToken))
            {
                if (nextToken.type == Token.Type.CommentPrefix)
                {
                    tomlReader.ReadToken(); //#
                    tomlReader.ReadToken(); //text
                }
                else if (nextToken.type == Token.Type.Text)
                {
                    TOMLKeyValue keyValue = byteReader.ReadObject<TOMLKeyValue>();
                    table->keyValues.Add(keyValue);
                }
                else if (nextToken.type == Token.Type.StartSquareBracket)
                {
                    break;
                }
                else
                {
                    tomlReader.ReadToken();
                }
            }
        }

        readonly void ISerializable.Write(ByteWriter byteWriter)
        {
            using Text destination = new(0);
            ToString(destination);
            byteWriter.WriteUTF8(destination.AsSpan());
        }

        /// <summary>
        /// Checks if this TOML table contains a key with the given <paramref name="key"/>.
        /// </summary>
        public readonly bool ContainsKey(ReadOnlySpan<char> key)
        {
            MemoryAddress.ThrowIfDefault(table);

            Span<TOMLKeyValue> keyValues = table->keyValues.AsSpan();
            for (int i = 0; i < keyValues.Length; i++)
            {
                if (keyValues[i].Key.SequenceEqual(key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to retrieve the value associated with the given <paramref name="key"/>.
        /// </summary>
        public readonly bool TryGetValue(ReadOnlySpan<char> key, out TOMLKeyValue value)
        {
            MemoryAddress.ThrowIfDefault(table);

            Span<TOMLKeyValue> keyValues = table->keyValues.AsSpan();
            for (int i = 0; i < keyValues.Length; i++)
            {
                if (keyValues[i].Key.SequenceEqual(key))
                {
                    value = keyValues[i];
                    return true;
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Retrieves the value associated with the given <paramref name="key"/>.
        /// </summary>
        public readonly TOMLKeyValue GetValue(ReadOnlySpan<char> key)
        {
            MemoryAddress.ThrowIfDefault(table);
            ThrowIfKeyIsMissing(key);

            Span<TOMLKeyValue> keyValues = table->keyValues.AsSpan();
            for (int i = 0; i < keyValues.Length; i++)
            {
                if (keyValues[i].Key.SequenceEqual(key))
                {
                    return keyValues[i];
                }
            }

            return default;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfKeyIsMissing(ReadOnlySpan<char> key)
        {
            if (!ContainsKey(key))
            {
                throw new ArgumentException($"Key `{key.ToString()}` is missing in TOML object", nameof(key));
            }
        }

        internal struct Implementation
        {
            public int nameLength;
            public MemoryAddress name;
            public List<TOMLKeyValue> keyValues;
        }
    }
}