using AMP.Discord;
using AMP.Logging;
using AMP.Useless;
using Discord;
using System.Text.RegularExpressions;

namespace AMP.SupportFunctions {
    internal class UserData {

        private const string FALLBACK_NAME = "Unnamed";

        internal static string GetUserName() {
            string name = FALLBACK_NAME;

            if(   ModManager.safeFile.username.Equals(FALLBACK_NAME)
               || string.IsNullOrEmpty(ModManager.safeFile.username)) {
                if(DiscordIntegration.Instance != null && !DiscordIntegration.Instance.currentUser.Equals(default(User))) {
                    name = DiscordIntegration.Instance.currentUser.Username;
                    Log.Debug("Got name from discord: " + name);
                }

                if(name != null && name.Length > 0) {
                    ModManager.safeFile.username = name;
                    ModManager.safeFile.Save();
                }
            } else {
                name = ModManager.safeFile.username;
            }

            return NameColorizer.FormatSpecialName( // Format Name color
                        Regex.Replace(name, @"[^\u0000-\u007F]+", string.Empty) // Remove unsupported characters
                        .Trim() // Trim Spaces at front and end
                    );
        }

    }
}
