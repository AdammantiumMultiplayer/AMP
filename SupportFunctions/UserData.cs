using AMP.Data;
using AMP.Discord;
using AMP.Logging;
using AMP.Useless;
using Discord;
using Netamite.Helper;
using Oculus.Platform;
using System;
#if STEAM
using Netamite.Steam.Integration;
#endif
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThunderRoad;

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
                    Log.Debug(Defines.AMP, "Trying to get platform name...");
                    // Await the async call
                    name = await GetPlatformNameAsync();
                }

                if (string.IsNullOrEmpty(name)) {
                    name = GenerateRandomName();
                    Log.Debug(Defines.AMP, $"Generated random name: {name}");
                    ModManager.safeFile.username = FALLBACK_NAME; // forces it to check next time, so random name is just for this session
                } else {
                    ModManager.safeFile.username = name;
                }
                ModManager.safeFile.lastNameRead = TimeHelper.Millis;
                ModManager.safeFile.Save();
            } else {
                name = safeFileUsername;
                Log.Debug(Defines.AMP, $"Got name from Safe-File: {name}");
            }
            return new UserData(name);
        }
        
        private static Task<string> GetPlatformNameAsync() {
            var tcs = new TaskCompletionSource<string>();
            try {
                GameManager.platform.store.GetUserID((id_success, id) => {
                    if (!id_success) {
                        tcs.SetResult(string.Empty);
                        return;
                    }
                    
                    Users.Get(id).OnComplete(message => {
                        if (!message.IsError && !string.IsNullOrWhiteSpace(message.Data.DisplayName)) {
                            tcs.SetResult(SanitizeName(message.Data.DisplayName));
                            Log.Debug(Defines.AMP, $"Got username from B&S: {message.Data.DisplayName}");
                            return;
                        }
                        tcs.SetResult(string.Empty);
                    });
                });
            } catch(Exception) {
                try {
                    GameManager.platform.store.GetUserName((name_success, name) => {
                        if (name_success) {
                            tcs.SetResult(SanitizeName(name));
                            Log.Debug(Defines.AMP, $"Got username from B&S: {name}");
                            return;
                        }
                        tcs.SetResult(string.Empty);
                    });
                } catch (Exception exp) {
                    Log.Err(Defines.AMP, exp);

                    tcs.SetResult(string.Empty);
                }
            }
            return tcs.Task;
        }

		internal static string SanitizeName(string name) {
			return NameColorizer.FormatSpecialName( // Format Name color
				Regex.Replace(name, @"[^\u0000-\u007F]+", string.Empty) // Remove unsupported characters
					.Trim() // Trim Spaces at front and end
			);

		}

		internal static string CleanUsername(string name) {
			name = SanitizeName(name);

			return Regex.Replace(name, @"</?color(?:=[^>]*)?>", string.Empty, RegexOptions.IgnoreCase);
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
