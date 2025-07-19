using System;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace TOML
{
    /// <summary>
    /// A reader for TOML tokens that wraps an existing <see cref="ByteReader"/>.
    /// </summary>
    [SkipLocalsInit]
    public readonly ref struct TOMLReader
    {
        private readonly ByteReader byteReader;

        /// <summary>
        /// Initializes a new instance of the reader.
        /// </summary>
        public TOMLReader(ByteReader byteReader)
        {
            this.byteReader = byteReader;
        }

        /// <summary>
        /// Tries to peek the next <paramref name="token"/>.
        /// </summary>
        public readonly bool TryPeekToken(out Token token)
        {
            return TryPeekToken(out token, out _);
        }

        /// <summary>
        /// Tries to peek the next <paramref name="token"/>, and retrieves how many <see cref="byte"/>s were read.
        /// </summary>
        public readonly bool TryPeekToken(out Token token, out int readBytes)
        {
            token = default;
            int position = byteReader.Position;
            int length = byteReader.Length;
            while (position < length)
            {
                byte bytesRead = byteReader.PeekUTF8(position, out char c, out _);
                if (c == Token.CommentPrefix)
                {
                    token = new Token(position, bytesRead, Token.Type.CommentPrefix);
                    readBytes = position - byteReader.Position + 1;
                    return true;
                }
                else if (c == Token.EqualSign)
                {
                    token = new Token(position, bytesRead, Token.Type.Equals);
                    readBytes = position - byteReader.Position + 1;
                    return true;
                }
                else if (c == Token.Aggregator)
                {
                    token = new Token(position, bytesRead, Token.Type.Comma);
                    readBytes = position - byteReader.Position + 1;
                    return true;
                }
                else if (c == Token.StartSquareBracket)
                {
                    token = new Token(position, bytesRead, Token.Type.StartSquareBracket);
                    readBytes = position - byteReader.Position + 1;
                    return true;
                }
                else if (c == Token.EndSquareBracket)
                {
                    token = new Token(position, bytesRead, Token.Type.EndSquareBracket);
                    readBytes = position - byteReader.Position + 1;
                    return true;
                }
                else if (c == Token.StartCurlyBrace)
                {
                    token = new Token(position, bytesRead, Token.Type.StartCurlyBrace);
                    readBytes = position - byteReader.Position + 1;
                    return true;
                }
                else if (c == Token.EndCurlyBrace)
                {
                    token = new Token(position, bytesRead, Token.Type.EndCurlyBrace);
                    readBytes = position - byteReader.Position + 1;
                    return true;
                }
                else if (c == Token.DoubleQuotes)
                {
                    position += bytesRead;
                    int start = position;
                    while (position < length)
                    {
                        bytesRead = byteReader.PeekUTF8(position, out c, out _);
                        if (c == Token.DoubleQuotes)
                        {
                            token = new Token(start, position - start, Token.Type.Text);
                            readBytes = position - byteReader.Position + 1;
                            return true;
                        }

                        position += bytesRead;
                    }

                    throw new InvalidOperationException("Unterminated string literal");
                }
                else if (c == Token.SingleQuotes)
                {
                    position += bytesRead;
                    int start = position;
                    while (position < length)
                    {
                        bytesRead = byteReader.PeekUTF8(position, out c, out _);
                        if (c == Token.SingleQuotes)
                        {
                            token = new Token(start, position - start, Token.Type.Text);
                            readBytes = position - byteReader.Position + 1;
                            return true;
                        }

                        position += bytesRead;
                    }

                    throw new InvalidOperationException("Unterminated string literal");
                }
                else if (SharedFunctions.IsWhitespace(c))
                {
                    position += bytesRead;
                }
                else
                {
                    int start = position;
                    position += bytesRead;
                    while (position < length)
                    {
                        bytesRead = byteReader.PeekUTF8(position, out c, out _);
                        if (SharedFunctions.IsEndOfLine(c) || Token.Tokens.Contains(c))
                        {
                            if (c == Token.EqualSign)
                            {
                                //trim whitespace
                                int trim = 0;
                                byteReader.PeekUTF8(position - trim - 1, out c, out _);
                                while (SharedFunctions.IsWhitespace(c))
                                {
                                    trim++;
                                    byteReader.PeekUTF8(position - trim - 1, out c, out _);
                                }

                                token = new Token(start, position - start - trim, Token.Type.Text);
                                readBytes = position - byteReader.Position;
                                return true;
                            }
                            else
                            {
                                token = new Token(start, position - start, Token.Type.Text);
                                readBytes = position - byteReader.Position;
                                return true;
                            }
                        }

                        position += bytesRead;
                    }

                    token = new Token(start, position - start, Token.Type.Text);
                    readBytes = position - byteReader.Position;
                    return true;
                }
            }

            readBytes = default;
            return false;
        }

        /// <summary>
        /// Reads the next token, and advances the reader.
        /// </summary>
        public readonly Token ReadToken()
        {
            TryPeekToken(out Token token, out int readBytes);
            byteReader.Advance(readBytes);
            return token;
        }

        /// <summary>
        /// Tries to read the next token, and avances the reader.
        /// </summary>
        public readonly bool TryReadToken(out Token token)
        {
            bool read = TryPeekToken(out token, out int readBytes);
            byteReader.Advance(readBytes);
            return read;
        }

        /// <summary>
        /// Copies the underlying text of the given <paramref name="token"/> into
        /// the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/> values copied.</returns>
        public readonly int GetText(Token token, Span<char> destination)
        {
            int length = byteReader.PeekUTF8(token.position, token.length, destination);
            if (destination[0] == Token.DoubleQuotes || destination[0] == Token.SingleQuotes)
            {
                for (int i = 0; i < length - 1; i++)
                {
                    destination[i] = destination[i + 1];
                }

                return length - 2;
            }
            else return length;
        }

        /// <summary>
        /// Appends the text of the given <paramref name="token"/> to the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public readonly int GetText(Token token, Text destination)
        {
            Span<char> buffer = stackalloc char[token.length * 4];
            int length = GetText(token, buffer);
            destination.Append(buffer.Slice(0, length));
            return length;
        }
    }
}