using AMP.Useless;
using System.Text.RegularExpressions;

namespace AMP.SupportFunctions {
    internal class UserData {

        internal static string GetUserName() {
            string name = "Unnamed";

            if(DiscordGUIManager.discordNetworking != null) {
                name = DiscordGUIManager.discordNetworking.currentUser.Username;
            }

            return NameColorizer.FormatSpecialName( // Format Name color
                        Regex.Replace(name, @"[^\u0000-\u007F]+", string.Empty) // Remove unsupported characters
                        .Trim() // Trim Spaces at front and end
                    );
        }

    }
}
