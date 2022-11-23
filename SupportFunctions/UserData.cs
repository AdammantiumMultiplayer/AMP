using AMP.Data;
using AMP.Overlay;
using AMP.Useless;
using Steamworks;
using System.Text.RegularExpressions;

namespace AMP.SupportFunctions {
    internal class UserData {

        private const string FALLBACK_NAME = "Unnamed";

        internal static string GetUserName() {
            string name = FALLBACK_NAME;

            if(ModManager.safeFile.username != null &&
               ModManager.safeFile.username.Length > 0) {
                name = ModManager.safeFile.username;
            } else {
                try {
                    if(SteamManager.Initialized) {
                        name = SteamFriends.GetPersonaName();
                    }
                } catch { }

                if(DiscordGUIManager.discordNetworking != null) {
                    name = DiscordGUIManager.discordNetworking.currentUser.Username;
                }

                if(name == null || name.Length == 0) name = FALLBACK_NAME;

                ModManager.safeFile.username = name;
                ModManager.safeFile.Save();
            }
            return NameColorizer.FormatSpecialName( // Format Name color
                        Regex.Replace(name, @"[^\u0000-\u007F]+", string.Empty) // Remove unsupported characters
                        .Trim() // Trim Spaces at front and end
                    );
        }

    }
}
