using System.Collections.Generic;
using System.Linq;

namespace AMP.Security {
    public class ModChecker {

        // yes yes, i know this is easy to circumvent, and there is nothing else i can really do without getting invasive
        // Maybe in the future i will try to check against class names of the mods, but thats not really easy to figure out and
        // not really much fun to configure properly without any coding knowledge

        /// <summary>
        /// Checks all mods against the whitelist
        /// </summary>
        /// <param name="whitelist">Whitelist to check</param>
        /// <param name="modlist">Client Modlist to check</param>
        /// <returns>All mods that didn't match with the whitelist</returns>
        public static string[] CheckWhitelistedMods(string[] whitelist, string[] modlist) {
            if(whitelist == null || modlist == null) return new string[0];
            if(modlist.Length == 0) return new string[0];

            if(!whitelist.Contains("Adammantium's Multiplayer Mod")) { // Make sure we are not blocking the multiplayer mod itself, that would be a pretty stupid reason to kick someone ^^
                whitelist.Append("Adammantium's Multiplayer Mod");
            }

            MatchList matchList = GetModMatchlist(whitelist, modlist);

            return matchList.unmatched;
        }

        /// <summary>
        /// Checks if all mods are in the whitelist
        /// </summary>
        /// <returns>true if all mods are compatible with the whitelist</returns>
        public static bool isWhitelistedCompatible(string[] whitelist, string[] modlist) {
            return CheckWhitelistedMods(whitelist, modlist).Length == 0; // If all mods are on the whitelist, you are good to go
        }

        /// <summary>
        /// Checks all mods against the blacklist
        /// </summary>
        /// <param name="blacklist">Blacklist to check</param>
        /// <param name="modlist">Client Modlist to check</param>
        /// <returns>All mods that matched with the blacklist</returns>
        public static string[] CheckBlacklistedMods(string[] blacklist, string[] modlist) {
            if(blacklist == null || modlist == null) return new string[0];
            if(modlist.Length == 0) return new string[0];

            MatchList matchList = GetModMatchlist(blacklist, modlist);
            return matchList.matched;
        }

        /// <summary>
        /// Checks if no mod is in the blacklist
        /// </summary>
        /// <returns>true if all mods are compatible with the blacklist</returns>
        public static bool isBlacklistedCompatible(string[] blacklist, string[] modlist) {
            return CheckBlacklistedMods(blacklist, modlist).Length == 0; // If no mod from the blacklist is found, you are good to go
        }

        /// <summary>
        /// Checks all mods against the requirelist
        /// </summary>
        /// <param name="requirelist">requirelist to check</param>
        /// <param name="modlist">Client Modlist to check</param>
        /// <returns>All mods that are missing from the requirelist</returns>
        public static string[] CheckRequirelistedMods(string[] requirelist, string[] modlist) {
            if(requirelist == null || modlist == null) return new string[0];
            if(requirelist.Length == 0) return new string[0];

            List<string> missing = new List<string>();
            foreach(string mod in requirelist) {
                if(!modlist.Contains(mod)) missing.Add(mod);
            }

            return missing.ToArray();
        }

        private class MatchList {
            public string[] matched;
            public string[] unmatched;

            public MatchList(string[] matched, string[] unmatched) {
                this.matched = matched;
                this.unmatched = unmatched;
            }

            public override string ToString() {
                return "Matched [" + string.Join(",", matched) + "] | Unmatched [" + string.Join(",", unmatched) + "]" + "]";
            }
        }

        private static MatchList GetModMatchlist(string[] list, string[] mods) {
            List<string> matchedMods = new List<string>();
            List<string> unmatchedMods = new List<string>();

            foreach(string mod in mods) {
                if(list.Contains(mod)) matchedMods.Add(mod);
                else unmatchedMods.Add(mod);
            }

            return new MatchList(matchedMods.ToArray(), unmatchedMods.ToArray());
        }
    }
}
