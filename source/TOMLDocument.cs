using Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace TOML
{
    /// <summary>
    /// Represents a TOML document, which can contain key-value pairs and tables.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct TOMLDocument : IDisposable, ISerializable
    {
        private Implementation* document;

        /// <summary>
        /// All key-value pairs in the TOML document.
        /// </summary>
        public readonly ReadOnlySpan<TOMLKeyValue> KeyValues
        {
            get
            {
                MemoryAddress.ThrowIfDefault(document);

                return document->keyValues.AsSpan();
            }
        }

        /// <summary>
        /// All tables in the TOML document.
        /// </summary>
        public readonly ReadOnlySpan<TOMLTable> Tables
        {
            get
            {
                MemoryAddress.ThrowIfDefault(document);

                return document->tables.AsSpan();
            }
        }

        /// <summary>
        /// Checks if this TOML document has been disposed.
        /// </summary>
        public readonly bool IsDisposed => document == default;

#if NET
        /// <summary>
        /// Creates an empty TOML document.
        /// </summary>
        public TOMLDocument()
        {
            document = MemoryAddress.AllocatePointer<Implementation>();
            document->keyValues = new(4);
            document->tables = new(4);
        }
#endif

        /// <summary>
        /// Initializes an existing TOML document with the given <paramref name="pointer"/>.
        /// </summary>
        public TOMLDocument(void* pointer)
        {
            document = (Implementation*)pointer;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            using Text destination = new(0);
            ToString(destination);
            return destination.ToString();
        }

        /// <summary>
        /// Appends the string representation of this TOML document to the given <paramref name="destination"/>.
        /// </summary>
        public readonly void ToString(Text destination)
        {
            MemoryAddress.ThrowIfDefault(document);

            Span<TOMLKeyValue> keyValues = document->keyValues.AsSpan();
            for (int i = 0; i < keyValues.Length; i++)
            {
                keyValues[i].ToString(destination);
                destination.AppendLine();
            }

            Span<TOMLTable> tables = document->tables.AsSpan();
            for (int i = 0; i < tables.Length; i++)
            {
                tables[i].ToString(destination);
                destination.AppendLine();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(document);

            Span<TOMLKeyValue> keyValues = document->keyValues.AsSpan();
            for (int i = 0; i < keyValues.Length; i++)
            {
                keyValues[i].Dispose();
            }

            document->keyValues.Dispose();
            Span<TOMLTable> tables = document->tables.AsSpan();
            for (int i = 0; i < tables.Length; i++)
            {
                tables[i].Dispose();
            }

            document->tables.Dispose();
            MemoryAddress.Free(ref document);
        }

        /// <summary>
        /// Appends a new key-value pair with the given <paramref name="key"/> and <paramref name="text"/> to this TOML document.
        /// </summary>
        public readonly void Add(ReadOnlySpan<char> key, ReadOnlySpan<char> text)
        {
            MemoryAddress.ThrowIfDefault(document);

            TOMLKeyValue keyValue = new(key, text);
            document->keyValues.Add(keyValue);
        }

        /// <summary>
        /// Appends a new key-value pair with the given <paramref name="key"/> and <paramref name="number"/> to this TOML document.
        /// </summary>
        public readonly void Add(ReadOnlySpan<char> key, double number)
        {
            MemoryAddress.ThrowIfDefault(document);

            TOMLKeyValue keyValue = new(key, number);
            document->keyValues.Add(keyValue);
        }

        /// <summary>
        /// Appends a new key-value pair with the given <paramref name="key"/> and <paramref name="boolean"/> to this TOML document.
        /// </summary>
        public readonly void Add(ReadOnlySpan<char> key, bool boolean)
        {
            MemoryAddress.ThrowIfDefault(document);

            TOMLKeyValue keyValue = new(key, boolean);
            document->keyValues.Add(keyValue);
        }

        /// <summary>
        /// Appends the given <paramref name="tomlArray"/> with the specified <paramref name="key"/> to this TOML document.
        /// </summary>
        public readonly void Add(ReadOnlySpan<char> key, TOMLArray tomlArray)
        {
            MemoryAddress.ThrowIfDefault(document);

            TOMLKeyValue keyValue = new(key, tomlArray);
            document->keyValues.Add(keyValue);
        }

        /// <summary>
        /// Appends the given <paramref name="tomlTable"/> to this TOML document.
        /// </summary>
        public readonly void Add(TOMLTable tomlTable)
        {
            MemoryAddress.ThrowIfDefault(document);

            document->tables.Add(tomlTable);
        }

        /// <summary>
        /// Checks if this TOML document contains a key-value pair with the specified <paramref name="key"/>.
        /// </summary>
        public readonly bool ContainsKey(ReadOnlySpan<char> key)
        {
            MemoryAddress.ThrowIfDefault(document);

            Span<TOMLKeyValue> keyValues = document->keyValues.AsSpan();
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
        /// Tries to get the value associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the key was found.</returns>
        public readonly bool TryGetValue(ReadOnlySpan<char> key, out TOMLKeyValue value)
        {
            MemoryAddress.ThrowIfDefault(document);

            Span<TOMLKeyValue> keyValues = document->keyValues.AsSpan();
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
        /// Retrieves the value associated with the specified <paramref name="key"/>.
        /// </summary>
        public readonly TOMLKeyValue GetValue(ReadOnlySpan<char> key)
        {
            MemoryAddress.ThrowIfDefault(document);
            ThrowIfKeyIsMissing(key);

            Span<TOMLKeyValue> keyValues = document->keyValues.AsSpan();
            for (int i = 0; i < keyValues.Length; i++)
            {
                if (keyValues[i].Key.SequenceEqual(key))
                {
                    return keyValues[i];
                }
            }

            throw new ArgumentException($"Key `{key.ToString()}` is missing in TOML document", nameof(key));
        }

        /// <summary>
        /// Checks if this TOML document contains a table with the specified <paramref name="name"/>.
        /// </summary>
        public readonly bool ContainsTable(ReadOnlySpan<char> name)
        {
            MemoryAddress.ThrowIfDefault(document);

            Span<TOMLTable> tables = document->tables.AsSpan();
            for (int i = 0; i < tables.Length; i++)
            {
                if (tables[i].Name.SequenceEqual(name))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to get the table with the specified <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the table was found.</returns>
        public readonly bool TryGetTable(ReadOnlySpan<char> name, out TOMLTable table)
        {
            MemoryAddress.ThrowIfDefault(document);

            Span<TOMLTable> tables = document->tables.AsSpan();
            for (int i = 0; i < tables.Length; i++)
            {
                if (tables[i].Name.SequenceEqual(name))
                {
                    table = tables[i];
                    return true;
                }
            }

            table = default;
            return false;
        }

        /// <summary>
        /// Retrieves the table with the specified <paramref name="name"/>.
        /// </summary>
        public readonly TOMLTable GetTable(ReadOnlySpan<char> name)
        {
            MemoryAddress.ThrowIfDefault(document);
            ThrowIfTableIsMissing(name);

            Span<TOMLTable> tables = document->tables.AsSpan();
            for (int i = 0; i < tables.Length; i++)
            {
                if (tables[i].Name.SequenceEqual(name))
                {
                    return tables[i];
                }
            }

            throw new ArgumentException($"Table `{name.ToString()}` is missing in TOML document", nameof(name));
        }

        void ISerializable.Read(ByteReader byteReader)
        {
            document = MemoryAddress.AllocatePointer<Implementation>();
            document->keyValues = new(4);
            document->tables = new(4);
            TOMLReader tomlReader = new(byteReader);
            while (tomlReader.TryPeekToken(out Token token))
            {
                if (token.type == Token.Type.CommentPrefix)
                {
                    tomlReader.ReadToken(); //#
                    tomlReader.ReadToken(); //text
                }
                else if (token.type == Token.Type.Text)
                {
                    TOMLKeyValue keyValue = byteReader.ReadObject<TOMLKeyValue>();
                    document->keyValues.Add(keyValue);
                }
                else if (token.type == Token.Type.StartSquareBracket)
                {
                    TOMLTable table = byteReader.ReadObject<TOMLTable>();
                    document->tables.Add(table);
                }
                else
                {
                    tomlReader.ReadToken();
                }
            }
        }

        readonly void ISerializable.Write(ByteWriter byteWriter)
        {
            using Text destination = new(32);
            ToString(destination);
            byteWriter.WriteUTF8(destination.AsSpan());
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfKeyIsMissing(ReadOnlySpan<char> key)
        {
            if (!ContainsKey(key))
            {
                throw new ArgumentException($"Key `{key.ToString()}` is missing in TOML document", nameof(key));
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTableIsMissing(ReadOnlySpan<char> name)
        {
            if (!ContainsTable(name))
            {
                throw new ArgumentException($"Table `{name.ToString()}` is missing in TOML document", nameof(name));
            }
        }

        /// <summary>
        /// Creates an empty TOML document.
        /// </summary>
        public static TOMLDocument Create()
        {
            Implementation* tomlObject = MemoryAddress.AllocatePointer<Implementation>();
            tomlObject->keyValues = new(4);
            tomlObject->tables = new(4);
            return new(tomlObject);
        }

        /// <summary>
        /// Tries to parse the given <paramref name="text"/> into a TOML document.
        /// </summary>
        public static bool TryParse(ReadOnlySpan<char> text, out TOMLDocument tomlDocument)
        {
            using ByteReader byteReader = ByteReader.CreateFromUTF8(text);
            try
            {
                tomlDocument = byteReader.ReadObject<TOMLDocument>();
                return true;
            }
            catch (Exception)
            {
                tomlDocument = default;
                return false;
            }
        }

        private struct Implementation
        {
            public List<TOMLKeyValue> keyValues;
            public List<TOMLTable> tables;
        }
    }
}