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
using System.Threading.Tasks;

namespace AMP.SupportFunctions {
    public class UserData {

        private const string FALLBACK_NAME = "Unnamed";
        public readonly string Name;
        
        private UserData(string name) {
            this.Name = name;
        }
        
        public static async Task<UserData> CreateAsync() {
            string name = null;
            var safeFileUsername = ModManager.safeFile.username;
            if (safeFileUsername.Equals(FALLBACK_NAME) || string.IsNullOrEmpty(safeFileUsername) || ModManager.safeFile.lastNameRead < TimeHelper.Millis - TimeHelper.DAY) {
                if (DiscordIntegration.Instance != null && !DiscordIntegration.Instance.currentUser.Equals(default(User))) {
                    name = SanitizeName(DiscordIntegration.Instance.currentUser.Username);
                    Log.Debug(Defines.AMP, $"Got name from Discord: {name}");
                } else {
                    // Await the async call
                    name = await GetPlatformNameAsync();
                }

                if (string.IsNullOrEmpty(name))
                {
                    name = GenerateRandomName();
                    Log.Debug(Defines.AMP, $"Generated random name: {name}");
                    ModManager.safeFile.lastNameRead = TimeHelper.Millis;
                    ModManager.safeFile.username = FALLBACK_NAME; // forces it to check next time, so random name is just for this session
                    ModManager.safeFile.Save();
                }
                else
                {
                    ModManager.safeFile.username = name;
                    ModManager.safeFile.lastNameRead = TimeHelper.Millis;
                    ModManager.safeFile.Save();
                }
            } else {
                name = safeFileUsername;
                Log.Debug(Defines.AMP, $"Got name from Safe-File: {name}");
            }
            return new UserData(name);
        }

        private static Task<string> GetPlatformNameAsync() {
            var tcs = new TaskCompletionSource<string>();
            try {
                /* TODO READD
                ThunderRoad.GameManager.platform.store.GetUserName((success, platformName) => {
                    if (success) {
                        tcs.SetResult(SanitizeName(platformName));
                        Log.Debug(Defines.AMP, $"Got name from B&S: {platformName}");
                    } else {
                        tcs.SetResult(string.Empty);
                    }
                });
                */
            } catch { }
            tcs.SetResult(string.Empty);
            return tcs.Task;
        }
        
        private static string SanitizeName(string name) {
            return NameColorizer.FormatSpecialName( // Format Name color
                Regex.Replace(name, @"[^\u0000-\u007F]+", string.Empty) // Remove unsupported characters
                    .Trim() // Trim Spaces at front and end
            );

        }
        
        private static string GenerateRandomName() {
            System.Random rand = new System.Random();
            string adjective = Adjectives[rand.Next(Adjectives.Length)];
            string noun = Nouns[rand.Next(Nouns.Length)];
            return $"{adjective}{noun}";
        }

        private static readonly string[] Adjectives =
        {
            "Swift", "Silent", "Brave", "Fierce", "Clever", "Mighty", "Dark", "Lucky",
            "Wild", "Iron", "Shadow", "Crimson", "Golden", "Epic", "Storm", "Frozen"
        };

        private static readonly string[] Nouns =
        {
            "Wolf", "Tiger", "Eagle", "Dragon", "Knight", "Ninja", "Wizard", "Hunter",
            "Rider", "Samurai", "Rogue", "Phoenix", "Viking", "Golem", "Ranger", "Beast"
        };

    }
}
