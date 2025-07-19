using Unmanaged;

namespace TOML
{
    /// <summary>
    /// A TOML token.
    /// </summary>
    public readonly struct Token
    {
        /// <summary>
        /// Constant for true boolean values in TOML.
        /// </summary>
        public const string True = "true";

        /// <summary>
        /// Constant for false boolean values in TOML.
        /// </summary>
        public const string False = "false";

        /// <summary>
        /// Constant describing all possible TOML tokens.
        /// </summary>
        public const string Tokens = "#=,[]{}";

        /// <summary>
        /// Token character for a hash in TOML, used for comments.
        /// </summary>
        public const char CommentPrefix = '#';

        /// <summary>
        /// Token character for an equals sign in TOML, used for key-value pairs.
        /// </summary>
        public const char EqualSign = '=';

        /// <summary>
        /// Token character for a comma in TOML, used to separate items in arrays or tables.
        /// </summary>
        public const char Aggregator = ',';

        /// <summary>
        /// Token character for containing text.
        /// </summary>
        public const char DoubleQuotes = '"';

        /// <summary>
        /// Token character for containing text.
        /// </summary>
        public const char SingleQuotes = '\'';

        /// <summary>
        /// Token characters for square brackets in TOML, used for arrays and tables.
        /// </summary>
        public const char StartSquareBracket = '[';

        /// <summary>
        /// Token character for the end of a square bracket in TOML, used for arrays and tables.
        /// </summary>
        public const char EndSquareBracket = ']';

        /// <summary>
        /// Token characters for curly braces in TOML, used for inline tables.
        /// </summary>
        public const char StartCurlyBrace = '{';

        /// <summary>
        /// Token character for the end of a curly brace in TOML, used for inline tables.
        /// </summary>
        public const char EndCurlyBrace = '}';

        /// <summary>
        /// Start position of the token.
        /// </summary>
        public readonly int position;

        /// <summary>
        /// Length of the token.
        /// </summary>
        public readonly int length;

        /// <summary>
        /// Type of the token.
        /// </summary>
        public readonly Type type;

        /// <summary>
        /// Initializes the token with the given <paramref name="position"/>, <paramref name="length"/>, and <paramref name="type"/>.
        /// </summary>
        public Token(int position, int length, Type type)
        {
            this.position = position;
            this.length = length;
            this.type = type;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return $"Token(type: {type} position:{position} length:{length})";
        }

        /// <summary>
        /// Retrieves the string representation of this token from the given <paramref name="tomlReader"/>.
        /// </summary>
        public readonly string GetText(TOMLReader tomlReader)
        {
            using Text destination = new(0);
            GetText(tomlReader, destination);
            return destination.ToString();
        }

        /// <summary>
        /// Appends the string representation of this token to the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/> values added.</returns>
        public readonly int GetText(TOMLReader tomlReader, Text destination)
        {
            switch (type)
            {
                case Type.Text:
                    return tomlReader.GetText(this, destination);
                case Type.CommentPrefix:
                    destination.Append(CommentPrefix);
                    return 1;
                case Type.Equals:
                    destination.Append(EqualSign);
                    return 1;
                case Type.Comma:
                    destination.Append(Aggregator);
                    return 1;
                case Type.StartSquareBracket:
                    destination.Append(StartSquareBracket);
                    return 1;
                case Type.EndSquareBracket:
                    destination.Append(EndSquareBracket);
                    return 1;
                case Type.StartCurlyBrace:
                    destination.Append(StartCurlyBrace);
                    return 1;
                case Type.EndCurlyBrace:
                    destination.Append(EndCurlyBrace);
                    return 1;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// TOML token types.
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// Uninitialized or unknown token type.
            /// </summary>
            Unknown,

            /// <summary>
            /// Text token.
            /// </summary>
            Text,

            /// <summary>
            /// Comment token, starts with a hash (#).
            /// </summary>
            CommentPrefix,

            /// <summary>
            /// Equals sign token, used for key-value pairs.
            /// </summary>
            Equals,

            /// <summary>
            /// Comma separator token, used to separate items in arrays or tables.
            /// </summary>
            Comma,

            /// <summary>
            /// Start of a square bracket token, used for arrays and tables.
            /// </summary>
            StartSquareBracket,

            /// <summary>
            /// End of a square bracket token, used for arrays and tables.
            /// </summary>
            EndSquareBracket,

            /// <summary>
            /// Start of a curly brace token, used for inline tables.
            /// </summary>
            StartCurlyBrace,

            /// <summary>
            /// End of a curly brace token, used for inline tables.
            /// </summary>
            EndCurlyBrace
        }
    }
}