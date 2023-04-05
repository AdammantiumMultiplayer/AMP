using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Data;
using AMP.SupportFunctions;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.DO_LEVEL_CHANGE)]
    public class LevelChangePacket : NetPacket {
        [SyncedVar] public string    level = "";
        [SyncedVar] public string    mode = "";
        [SyncedVar] public string[]  options = new string[0];
        [SyncedVar] public EventTime eventTime = EventTime.OnEnd;

        public Dictionary<string, string> option_dict {
            get {
                Dictionary<string, string> result = new Dictionary<string, string>();

                if(options == null) return result;

                int i = 0;
                while(i < options.Length) {
                    result.Add(options[i++], options[i++]);
                }

                return result;
            }
        }

        public LevelChangePacket() { }

        public LevelChangePacket(string level, string mode) {
            this.level     = level;
            this.mode      = mode;
            this.options   = new string[0];
        }

        public LevelChangePacket(string levelName, string mode, string[] options) : this(levelName, mode) {
            this.options   = options;
        }

        public LevelChangePacket(string levelName, string mode, Dictionary<string, string> options) : this(levelName, mode) {
            this.options = new string[options.Count * 2];
            int i = 0;
            foreach(KeyValuePair<string, string> kvp in options) {
                this.options[i++] = kvp.Key;
                this.options[i++] = kvp.Value;
            }
        }

        public LevelChangePacket(string levelName, string mode, Dictionary<string, string> options, EventTime eventTime) : this(levelName, mode, options) {
            this.eventTime = eventTime;
        }

        public override bool ProcessClient(NetamiteClient client) {
            // Writeback data to client cache
            ModManager.clientSync.syncData.serverlevel = level;
            ModManager.clientSync.syncData.servermode = mode;
            ModManager.clientSync.syncData.serveroptions = option_dict;

            string currentLevel = "";
            string currentMode = "";
            Dictionary<string, string> currentOptions = new Dictionary<string, string>();
            LevelInfo.ReadLevelInfo(out currentLevel, out currentMode, out currentOptions);

            if(!(currentLevel.Equals(ModManager.clientSync.syncData.serverlevel, StringComparison.OrdinalIgnoreCase))) {
                Dispatcher.Enqueue(() => {
                    LevelInfo.TryLoadLevel(ModManager.clientSync.syncData.serverlevel, ModManager.clientSync.syncData.servermode, ModManager.clientSync.syncData.serveroptions);
                });
            } else {
                this.SendToServerReliable();
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            ClientData cd = client.GetData();

            if(!cd.greeted) {
                if(eventTime == EventTime.OnEnd) {
                    ModManager.serverInstance.GreetPlayer(client, true);
                }
                return true;
            }

            if(level == null) return true;
            if(mode == null) return true;

            if(level.Equals("characterselection", StringComparison.OrdinalIgnoreCase)) return true;

            if(!(level.Equals(ModManager.serverInstance.currentLevel, StringComparison.OrdinalIgnoreCase) 
              && mode.Equals(ModManager.serverInstance.currentMode, StringComparison.OrdinalIgnoreCase))) { // Player is the first to join that level
                if(!ModManager.safeFile.hostingSettings.allowMapChange) {
                    Log.Err(Defines.SERVER, $"{client.ClientName} tried changing level.");
                    ModManager.serverInstance.LeavePlayer(client, "Player tried to change level.");
                    return true;
                }
                if(level.ToLower().Equals("mainmenu")) {
                    Log.Err(Defines.SERVER, $"{client.ClientName} tried to load into MainMenu.");
                    ModManager.serverInstance.LeavePlayer(client, "Player tried to load into MainMenu.");
                    return true;
                }

                if(eventTime == EventTime.OnStart) {
                    Log.Info(Defines.SERVER, $"{client.ClientName} started to load level {level} with mode {mode}.");
                    server.SendToAllExcept(new PrepareLevelChangePacket(client.ClientName, level, mode), client.ClientId);

                    server.SendToAllExcept(
                          new DisplayTextPacket("level_change", $"Player {client.ClientName} is loading into <color=#0099FF>{level}</color>.\n<color=#FF0000>Please stay in your level.</color>", Color.yellow, Vector3.forward * 2, true, true, 60)
                        , client.ClientId
                    );

                    ModManager.serverInstance.ClearItemsAndCreatures();
                    server.SendToAllExcept(new AllowTransmissionPacket(false));
                    server.SendToAllExcept(new ClearPacket(true, true, false), client.ClientId);
                    server.SendTo(client, new ClearPacket(true, true, false));
                } else {
                    ModManager.serverInstance.currentLevel = level;
                    ModManager.serverInstance.currentMode = mode;

                    ModManager.serverInstance.currentOptions = option_dict;

                    server.SendToAllExcept(this, client.ClientId);
                    server.SendTo(client, new AllowTransmissionPacket(true));
                    return true;
                }
            }

            if(eventTime == EventTime.OnEnd) {
                Log.Info(Defines.SERVER, $"{client.ClientName} loaded level {ModManager.serverInstance.currentLevel} with mode {ModManager.serverInstance.currentMode}.");
                ModManager.serverInstance.SendItemsAndCreatures(client); // If its the first player changing the level, this will send nothing other than the permission to start sending stuff
            }
            return true;
        }
    }
}
