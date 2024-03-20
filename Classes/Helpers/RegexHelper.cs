using System.Text.RegularExpressions;

namespace ChromeRiverService.Classes.HelperClasses
{
    public static partial class RegexHelper{
        public static string PlaceSpacesBeforeUppercase(string value)
        {
            return lowercaseThenUppercase().Replace(value, "$1 $2");
        }

        [GeneratedRegex("([a-z])([A-Z])")]
        private static partial Regex lowercaseThenUppercase();
    }
}