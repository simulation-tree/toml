namespace TOML
{
    internal static class SharedFunctions
    {
        public static bool IsWhiteSpace(char character)
        {
            return char.IsWhiteSpace(character) || IsEndOfLine(character) || character == (char)65279; //BOM
        }

        public static bool IsEndOfLine(char character)
        {
            return character == '\n' || character == '\r';
        }
    }
}