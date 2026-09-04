using System;
using System.Text.RegularExpressions;

namespace SP2FreeCamera
{
    internal static class LogPrivacy
    {
        private const string RedactedUserProfile = "<user-profile>";
        private const string RedactedNetworkShare = "<network-share>";
        private const string RedactedPrivateAddress = "<private-address>";
        private const string RedactedEmail = "<email>";

        private static readonly Regex WindowsUserPath = new Regex(
            @"(?i)\b[A-Z]:\\Users\\[^\\/:*?""<>|\r\n]+",
            RegexOptions.Compiled);

        private static readonly Regex UnixUserPath = new Regex(
            @"(?i)(?<![A-Za-z0-9_])/(?:home|Users)/[^/\s:]+",
            RegexOptions.Compiled);

        private static readonly Regex NetworkShare = new Regex(
            @"\\\\[^\\\s]+\\[^\\\s]+",
            RegexOptions.Compiled);

        private static readonly Regex PrivateAddress = new Regex(
            @"(?<!\d)(?:10(?:\.\d{1,3}){3}|192\.168(?:\.\d{1,3}){2}|172\.(?:1[6-9]|2\d|3[01])(?:\.\d{1,3}){2})(?!\d)",
            RegexOptions.Compiled);

        private static readonly Regex EmailAddress = new Regex(
            @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SensitiveAssignment = new Regex(
            @"(?i)\b(password|passwd|secret|api[_-]?key|access[_-]?token)\b\s*[:=]\s*[^,;\s]+",
            RegexOptions.Compiled);

        internal static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            value = ReplaceOrdinalIgnoreCase(value, userProfile, RedactedUserProfile);
            value = WindowsUserPath.Replace(value, RedactedUserProfile);
            value = UnixUserPath.Replace(value, RedactedUserProfile);
            value = NetworkShare.Replace(value, RedactedNetworkShare);
            value = PrivateAddress.Replace(value, RedactedPrivateAddress);
            value = EmailAddress.Replace(value, RedactedEmail);
            value = SensitiveAssignment.Replace(value, "$1=<redacted>");
            return value;
        }

        internal static string ExceptionSummary(Exception exception)
        {
            if (exception == null)
            {
                return "Unknown error";
            }

            return exception.GetType().Name + ": " + Sanitize(exception.Message);
        }

        private static string ReplaceOrdinalIgnoreCase(
            string value,
            string search,
            string replacement)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(search))
            {
                return value;
            }

            int startIndex = 0;
            while (true)
            {
                int index = value.IndexOf(search, startIndex, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    return value;
                }

                value = value.Substring(0, index) + replacement +
                    value.Substring(index + search.Length);
                startIndex = index + replacement.Length;
            }
        }
    }
}
