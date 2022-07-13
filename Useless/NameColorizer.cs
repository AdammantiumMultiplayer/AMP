using Steamworks;

namespace AMP.Useless {
    public class NameColorizer {

        public static string FormatSpecialName(string name) {
            switch(SteamUser.GetSteamID().m_SteamID) {
                case 76561198061480942: // Adammantium
                    return $"<color=#FF8C00>{name}</color>";

                case 76561198260642380: // Nibebra98
                    return $"<color=#f542ec>{name}</color>";

                case 76561198065250505: // Freelease
                    return $"<color=#00A41C>{name}</color>";

                default: break;
            }
            return name;
        }
        
    }
}
