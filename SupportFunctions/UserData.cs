using AMP.Data;
using AMP.Discord;
using AMP.Logging;
using AMP.Useless;
using Discord;
using Netamite.Helper;
#if STEAM
using Netamite.Steam.Integration;
#endif
using System.Text.RegularExpressions;

namespace AMP.SupportFunctions {
    internal class UserData {

        private const string FALLBACK_NAME = "Unnamed";

        internal static string GetUserName() {
            string name = FALLBACK_NAME;

            if(   ModManager.safeFile.username.Equals(FALLBACK_NAME)
               || string.IsNullOrEmpty(ModManager.safeFile.username)
               || ModManager.safeFile.lastNameRead < TimeHelper.Millis - TimeHelper.DAY) {
#if STEAM
                if(SteamIntegration.IsInitialized && SteamIntegration.Username != null && SteamIntegration.Username.Length > 0) {
                    name = SteamIntegration.Username;
                    Log.Debug(Defines.AMP, "Got name from Steam: " + name);
                }else
 #endif
                if(DiscordIntegration.Instance != null && !DiscordIntegration.Instance.currentUser.Equals(default(User))) {
                    name = DiscordIntegration.Instance.currentUser.Username;
                    Log.Debug(Defines.AMP, "Got name from Discord: " + name);
                }

                if(name != null && name.Length > 0) {
                    ModManager.safeFile.lastNameRead = TimeHelper.Millis;
                    ModManager.safeFile.username = name;
                    ModManager.safeFile.Save();
                }
            } else {
                name = ModManager.safeFile.username;
                Log.Debug(Defines.AMP, "Got name from Safe-File: " + name);
            }

            return NameColorizer.FormatSpecialName( // Format Name color
                        Regex.Replace(name, @"[^\u0000-\u007F]+", string.Empty) // Remove unsupported characters
                        .Trim() // Trim Spaces at front and end
                    );
        }

    }
}
