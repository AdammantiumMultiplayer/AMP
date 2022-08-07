namespace AMP.Useless {
    public class NameColorizer {

        public static string FormatSpecialName(string name) {
            return FormatSpecialName(DiscordGUIManager.discordNetworking.currentUser.Id.ToString(), name);
        }

        public static string FormatSpecialName(string id, string name) {
            switch(id) {
                case "76561198061480942": // Adammantium
                case "199898798380679168":
                case "960606378014150727": // Cookily
                    return $"<color=#FF8C00>{name}</color>";

                case "76561198260642380": // Nibebra98
                case "190479455418974208":
                    return $"<color=#f542ec>{name}</color>";

                case "76561198065250505": // Freelease
                case "137246468900651020":
                    return $"<color=#00A41C>{name}</color>";

                default: break;
            }

            return name;
        }
        
    }
}
