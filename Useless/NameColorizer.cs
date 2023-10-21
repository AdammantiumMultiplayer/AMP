using AMP.Discord;

namespace AMP.Useless {
    internal class NameColorizer {

        internal static string FormatSpecialName(string name) {
            string id = "";
            try {
                id = DiscordIntegration.Instance.currentUser.Id.ToString();
            } catch { }

            return FormatSpecialName(id, name);
        }

        internal static string FormatSpecialName(string id, string name) {
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

                case "426543559056031794": // Drifter
                    return $"<color=#4DA7F6>{name}</color>";

                case "558160890164281364": // Mythicus420
                    return $"<color=#4DA7F6>{name}</color>";

                case "76561198354868673":
                case "446173403615985675": // flex
                    return $"<color=#295620>{name}</color>";

                default: break;
            }

            return name;
        }
        
    }
}
