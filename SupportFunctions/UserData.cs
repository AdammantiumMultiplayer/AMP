using AMP.Useless;
using System.Text.RegularExpressions;

namespace AMP.SupportFunctions {
    internal class UserData {

        private const string FALLBACK_NAME = "Unnamed";

        internal static string GetUserName() {
            string name = FALLBACK_NAME;

            if(DiscordGUIManager.discordNetworking != null) {
                name = DiscordGUIManager.discordNetworking.currentUser.Username;
            }

            if(name == null || name.Length == 0) name = FALLBACK_NAME;

            return NameColorizer.FormatSpecialName( // Format Name color
                        Regex.Replace(name, @"[^\u0000-\u007F]+", string.Empty) // Remove unsupported characters
                        .Trim() // Trim Spaces at front and end
                    );
        }

    }
}
