namespace iPhoneController.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        ///     A string extension method that get the string between the two specified string.
        /// </summary>
        /// <param name="this">The @this to act on.</param>
        /// <param name="before">The string before to search.</param>
        /// <param name="after">The string after to search.</param>
        /// <returns>The string between the two specified string.</returns>
        public static string GetBetween(this string value, string before, string after)
        {
            var beforeStartIndex = value.IndexOf(before);
            var startIndex = beforeStartIndex + before.Length;
            var afterStartIndex = value.IndexOf(after, startIndex);
            if (beforeStartIndex == -1 || afterStartIndex == -1)
            {
                return string.Empty;
            }

            return value.Substring(startIndex, afterStartIndex - startIndex);
        }

        public static string[] RemoveSpaces(this string value)
        {
            return value.Replace(" ,", ",")
                        .Replace(", ", ",")
                        .Split(',');
        }
    }
}