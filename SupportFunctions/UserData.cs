using AMP.Useless;
using Steamworks;
using System.Text.RegularExpressions;

namespace AMP.SupportFunctions {
    public class UserData {

        public static string GetUserName() {
            return NameColorizer.FormatSpecialName( // Format Name color
                        Regex.Replace(SteamFriends.GetPersonaName(), @"[^\u0000-\u007F]+", string.Empty) // Remove unsupported characters
                        .Trim() // Trim Spaces at front and end
                    );
        }

    }
}
