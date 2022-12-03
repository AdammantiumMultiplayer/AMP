using AMP.Data;
using AMP.Logging;
using AMP.Network.Handler;
using Discord;
using System;
using System.Drawing;
using ThunderRoad;
using UnityEngine;

namespace AMP.Discord {
    internal class DiscordIntegration : NetworkHandler {

        private static DiscordIntegration instance;

        ActivityManager activityManager;
        UserManager userManager;

        internal User currentUser;

        public static DiscordIntegration Instance { 
            get { 
                if(instance == null) instance = new DiscordIntegration();
                return instance;
            }
        }

        private static global::Discord.Discord discord;
        internal DiscordIntegration() {
            try {
                if(discord == null) discord = new global::Discord.Discord(Config.DISCORD_APP_ID, (UInt64)CreateFlags.NoRequireDiscord);
            }catch(DllNotFoundException) {
                return;
            }

            activityManager = discord.GetActivityManager();
            userManager = discord.GetUserManager();

            // Get yourself
            //currentUser = userManager.GetCurrentUser();
            userManager.OnCurrentUserUpdate += () => {
                currentUser = userManager.GetCurrentUser();
            };

            activityManager.OnActivityJoin += ActivityManager_OnActivityJoin;

            RegisterEvents();
        }

        private void ActivityManager_OnActivityJoin(string secret) {
            Log.Debug(secret);
        }

        internal override void RunCallbacks() {
            discord.RunCallbacks();
        }

        internal void RegisterEvents() {

        }

        private float last_update = 0;
        internal void UpdateActivity() {
            if(discord == null) return;
            if(last_update > Time.time - 1) return;
            last_update = Time.time;

            Activity activity;

            string details = "Blade & Sorcery";
            string join_key = "";
            string large_image_key = "default";

            if(ModManager.clientInstance != null && ModManager.clientSync != null) {
                join_key = ModManager.clientInstance.nw.GetJoinSecret();
            }

            if(Level.current != null) {
                details         = Level.current.data.id;
                large_image_key = Level.current.data.id.ToLower();
            }

            if(join_key.Length > 0) {
                activity = new global::Discord.Activity {
                    State = "Playing Multiplayer",
                    Details = details,
                    Instance = true,
                    Party = {
                        Id = currentUser.Id.ToString(),
                        Size = {
                            CurrentSize = ModManager.clientSync.syncData.players.Count
                            #if !DEBUG_SELF
                                            + 1
                            #endif
                                            ,
                            MaxSize     = ModManager.clientInstance.serverInfo.max_players
                        }
                    },
                    // TODO: Implement it back in
                    //Secrets = {
                    //    Join = join_key
                    //},
                    Assets = {
                        LargeImage = large_image_key,
                        LargeText  = details
                    }
                };
            } else {
                activity = new global::Discord.Activity {
                    State = "Playing Solo",
                    Details = details,
                    Instance = true,
                    Assets = {
                        LargeImage = large_image_key,
                        LargeText  = details
                    }
                };
            }

            activityManager.UpdateActivity(activity, (result) => {
                //Log.Debug($"Updated Activity {result}");
            });
        }
    }
}
