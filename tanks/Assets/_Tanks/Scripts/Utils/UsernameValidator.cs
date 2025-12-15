using System.Text.RegularExpressions;
using System.Globalization;

namespace Tanks.Complete.Utils
{
    /// <summary>
    ///     Username validation helper that enforces length and character rules.
    /// </summary>
    public static class UsernameValidator
    {
        public const int MinLength = 3;
        public const int MaxLength = 15;

        // Allow: letters, digits, spaces (half/full width), prolonged sound mark, katakana middle dot.
        private static readonly Regex AllowedCharacters =
            new Regex(@"^[\p{L}\p{Nd}\p{Zs}ー・]+$", RegexOptions.Compiled);

        public static bool TryValidate(string username, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                errorMessage = "Please enter a username.";
                return false;
            }

            var trimmed = username.Trim();
            var length = new StringInfo(trimmed).LengthInTextElements;
            if (length < MinLength || length > MaxLength)
            {
                errorMessage = $"Username must be between {MinLength} and {MaxLength} characters.";
                return false;
            }

            if (!AllowedCharacters.IsMatch(trimmed))
            {
                errorMessage = "Username can include letters, digits, spaces, 'ー', and '・' only.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public static string Normalize(string username)
        {
            return username?.Trim();
        }
    }
}
