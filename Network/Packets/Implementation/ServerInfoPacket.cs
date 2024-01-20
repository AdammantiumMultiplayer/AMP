using AMP.Data;
using AMP.Discord;
using AMP.GameInteraction.Components;
using AMP.Logging;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.SERVER_INFO)]
    public class ServerInfoPacket : AMPPacket {
        [SyncedVar] public string version = "";
        [SyncedVar] public int    max_players = 99;
        [SyncedVar] public bool   allow_voicechat = false;
        [SyncedVar] public byte   base_tickrate = 10;
        [SyncedVar] public byte   player_tickrate = 10;


        public ServerInfoPacket() { }

        public ServerInfoPacket(string version, int max_players, bool allow_voicechat, byte base_tickrate, byte player_tickrate) {
            this.version = version;
            this.max_players = max_players;
            this.allow_voicechat = allow_voicechat;
            this.base_tickrate = base_tickrate;
            this.player_tickrate = player_tickrate;
        }


        public override bool ProcessClient(NetamiteClient client) {
            ModManager.clientSync.syncData.server_config = this;

            if(ModManager.clientSync != null) {
                ModManager.clientSync.UpdateVoiceChatState();
            }

            Log.Debug($"Serverinfo received:\nVersion: {version}\nMax Players: {max_players}\nVoice Chat allowed: {allow_voicechat}");

            if(!version.Equals(Defines.MOD_VERSION)) {
                Dispatcher.Enqueue(() => {
                    TextDisplay.ShowTextDisplay(new DisplayTextPacket("version_mismatch"
                                                                     , $"Your mod version is different from the servers.\nServer: {version}\nYours: {Defines.MOD_VERSION}\nEXPECT ISSUES!\nYou have been warned."
                                                                     , Color.red
                                                                     , Vector3.forward * 2
                                                                     , true
                                                                     , true
                                                                     , 20
                                                                     ).SetTextSize(300));
                });
            }

            Config.BASE_TICK_RATE = base_tickrate;

            DiscordIntegration.Instance.UpdateActivity();
            return true;
        }
    }
}
