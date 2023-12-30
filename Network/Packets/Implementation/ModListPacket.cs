using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet.Attributes;
using Netamite.Network.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Netamite.Server.Definition;
using AMP.Network.Data;
using AMP.Security;
using AMP.Logging;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.MOD_LIST)]
    public class ModListPacket : AMPPacket {
        [SyncedVar] public string[] modList;

        public ModListPacket() { }

        public ModListPacket(string[] modList) {
            this.modList = modList;
        }

        public override bool ProcessClient(NetamiteClient client) {
                

            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(ModManager.safeFile.hostingSettings.useModWhitelist) {
                string[] unallowed_mods = ModChecker.CheckWhitelistedMods(ModManager.safeFile.hostingSettings.modWhitelist, modList);
                if(unallowed_mods.Length > 0) {
                    client.Kick("Some of your mods are not whitelisted on the server: " + string.Join(", ", unallowed_mods));
                    return true;
                }
            }
            if(ModManager.safeFile.hostingSettings.useModBlacklist) {
                string[] unallowed_mods = ModChecker.CheckBlacklistedMods(ModManager.safeFile.hostingSettings.modBlacklist, modList);
                if(unallowed_mods.Length > 0) {
                    client.Kick("Some of your mods are blacklisted on the server: " + string.Join(", ", unallowed_mods));
                    return true;
                }
            }
            if(ModManager.safeFile.hostingSettings.useModRequirelist) {
                string[] missing_mods = ModChecker.CheckRequirelistedMods(ModManager.safeFile.hostingSettings.modRequirelist, modList);
                if(missing_mods.Length > 0) {
                    client.Kick("You are missing some mods the server is requiring: " + string.Join(", ", missing_mods));
                    return true;
                }
            }

            return true;
        }
    }
}
