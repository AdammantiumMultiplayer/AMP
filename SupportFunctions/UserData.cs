using AMP.Useless;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
