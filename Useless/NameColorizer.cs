using AMP.Discord;
using Netamite.Steam.Integration;
using System.Collections.Generic;

namespace AMP.Useless {
    internal class NameColorizer {

        internal static string FormatSpecialName(string name) {
            List<string> ids = new List<string>();

            try {
                ids.Add(DiscordIntegration.Instance.currentUser.Id.ToString());
            } catch { }

            try {
                if(SteamIntegration.IsInitialized && SteamIntegration.Username != null && SteamIntegration.Username.Length > 0) {
                    ids.Add(SteamIntegration.SteamID.ToString());
                }
            }catch { }

            return FormatSpecialName(name, ids.ToArray());
        }

        internal static string FormatSpecialName(string name, params string[] ids) {
            foreach(string id in ids) {
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
            }

            return name;
        }
        
    }
}
